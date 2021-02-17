namespace OsuDiffCalc.FileProcessor {
	using AnalyzerObjects;
	using BeatmapObjects;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	
	class Analyzer {
		const int MinBurstLength = 3;
		const int MinStreamLength = 6;
		const double BurstMultiplier = 0.75;
		static readonly double MaxDistPx = Math.Sqrt((Beatmap.MaxX * Beatmap.MaxX) + (Beatmap.MaxY * Beatmap.MaxY));

		public static void Analyze(Beatmap beatmap, bool clearLists = true) {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"analyze[{beatmap.Version}]";

			// sort objects and timing points by time (ascending)
			beatmap.TimingPoints.Sort();
			beatmap.BreakSections.Sort();
			beatmap.BeatmapObjects.Sort();

			// analyze slider shape and speed (requires timing points)
			int timingPointIndex = 0;
			TimingPoint timingPoint = beatmap.TimingPoints[0];
			foreach (BeatmapObject obj in beatmap.BeatmapObjects) {
				if (obj is not Slider slider) continue;
				// find current timing point
				if (timingPointIndex < beatmap.NumTimingPoints) {
					while (timingPointIndex < beatmap.NumTimingPoints - 1 && slider.StartTime > beatmap.TimingPoints[timingPointIndex].Offset) {
						timingPointIndex++;
					}
					timingPoint = beatmap.TimingPoints[timingPointIndex];
				}
				slider.AnalyzeShape(timingPoint, beatmap.SliderMultiplier);
			}

			// analysis variables
			double minStreamAvgMs = -1, minCoupletAvgMs = -1, minTripletAvgMs = -1, minBurstAvgMs = -1;
			var streams = new List<Shape>();
			var couplets = new List<Shape>();
			var triplets = new List<Shape>();
			var bursts = new List<Shape>();

			var shape = new Shape();
			var lastShape = new Shape();
			HitObject lastHitObject = null;
			bool shapeAdded = false;

			var jumpDifficultyList = new List<double>();
			var sliderDifficultyList = new List<double>();

			// macro to make life easier
			void addShapeToAppropriateList() {
				shape.Analyze();
				shape.PrevShape = lastShape;
				if (shape.NumObjects == 2) {
					shape.Type = Shape.ShapeType.Couplet;
					couplets.Add(shape);
					UpdateMin(ref minCoupletAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects < MinBurstLength) {
					shape.Type = Shape.ShapeType.Triplet;
					triplets.Add(shape);
					UpdateMin(ref minTripletAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects < MinStreamLength) {
					shape.Type = Shape.ShapeType.Burst;
					bursts.Add(shape);
					UpdateMin(ref minBurstAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects >= MinStreamLength) {
					shape.Type = Shape.ShapeType.Stream;
					streams.Add(shape);
					UpdateMin(ref minStreamAvgMs, shape.AvgTimeGapMs);
				}
				lastShape = shape;
				shapeAdded = true;
			}

			foreach (var obj in beatmap.BeatmapObjects) {
				shapeAdded = false;
				// TODO: fast spinners can act like jumps
				if (obj is Slider or Hitcircle) {
					var hitObj = obj as HitObject;
					// shape analysis for triplet, streams, etc
					if (lastHitObject is not null) {
						//second object
						if (shape.NumObjects == 1)
							shape.Add(hitObj);
						//third, fourth, etc. 
						else if (shape.NumObjects >= 2) {
							//continuing stream
							if (shape.CompareTiming(hitObj.StartTime) == 0)
								shape.Add(hitObj);
							//end of stream, start of new stream
							else {
								addShapeToAppropriateList();
								shape = new Shape(lastHitObject, hitObj);
							}
						}
					}
					//first hit object
					else
						shape.Add(hitObj);
					// slider difficulty
					if (hitObj is Slider slider)
						sliderDifficultyList.Add(GetSliderDifficulty(slider, beatmap));
					// jump difficulty
					if (lastHitObject is not null)
						jumpDifficultyList.Add(GetJumpDifficulty(hitObj, lastHitObject, beatmap));

					lastHitObject = hitObj;
				}
				//obj is not a hit object
				else
					addShapeToAppropriateList();
			}
			if (!shapeAdded)
				addShapeToAppropriateList();

			double streamsDifficulty = GetStreamsDifficulty(streams, minStreamAvgMs, beatmap);
			double coupletsDifficulty = GetCoupletsDifficulty(couplets, minCoupletAvgMs, beatmap);
			double burstsDifficulty = GetStreamsDifficulty(bursts, minBurstAvgMs, beatmap);
			//Console.Write("jump ");
			double jumpsDifficulty = GetWeightedSumOfList(jumpDifficultyList, 1.5);
			//Console.Write("slider ");
			double slidersDifficulty = GetWeightedSumOfList(sliderDifficultyList, 1.5);
			double totalDifficulty = streamsDifficulty + burstsDifficulty + coupletsDifficulty + jumpsDifficulty + slidersDifficulty;

			beatmap.DiffRating.JumpDifficulty = jumpsDifficulty;
			beatmap.DiffRating.StreamDifficulty = streamsDifficulty;
			beatmap.DiffRating.BurstDifficulty = burstsDifficulty;
			beatmap.DiffRating.CoupletDifficulty = coupletsDifficulty;
			beatmap.DiffRating.SliderDifficulty = slidersDifficulty;
			beatmap.DiffRating.TotalDifficulty = totalDifficulty;
			beatmap.IsAnalyzed = true;
			/*
			Console.WriteLine("\n{1:000} jumps diff    = {0:0.0}", jumpsDifficulty, jumpDifficultyList.Count);
			Console.WriteLine("{1:000} streams diff  = {0:0.0}", streamsDifficulty, streams.Count);
			Console.WriteLine("{1:000} bursts diff   = {0:0.0}", burstsDifficulty, bursts.Count);
			Console.WriteLine("{1:000} couplets diff = {0:0.0}", coupletsDifficulty, couplets.Count);
			Console.WriteLine("{1:000} sliders diff  = {0:0.0}", slidersDifficulty, sliderDifficultyList.Count);
			Console.WriteLine("* total diff  = {0:0.0}", beatmap.diffRating.totalDifficulty);
			//printDebug(couplets, "couplets");
			//printDebug(triplets, "triplets");
			//printDebug(streams, "streams");
			*/

			if (clearLists) {
				beatmap.BeatmapObjects.Clear();
				beatmap.TimingPoints.Clear();
				beatmap.BreakSections.Clear();
			}
		}

		#region Difficulty calculations

		static double GetStreamsDifficulty(List<Shape> streams, double minStreamMs, Beatmap map) {
			if (map is null || streams is null || streams.Count == 0)
				return 0;

			double avgObjects = 0;
			double avgBPM = 0;
			double avgDistancePx = 0;
			double numStreams = 0; // double for fewer type casts
			var streamDiffs = new List<double>();
			foreach (var stream in streams) {
				double streamDiff = 0;
				if (stream.NumObjects >= MinBurstLength && stream.AvgTimeGapMs > 0) {
					double streamBPM = 15000 / stream.AvgTimeGapMs;
					//calculate the difficulty of the stream
					streamDiff = Math.Pow(stream.NumObjects, 0.8) * Math.Pow(streamBPM, 3.4) / 1e9 * (stream.AvgDistancePx + 1);
					//addition for OD. Maxes ~10%
					streamDiff += streamDiff * 1000 / (stream.AvgTimeGapMs * map.MarginOfErrorMs300);
					//multiplier to represent that bursts are significantly easier
					if (stream.NumObjects < MinStreamLength) {
						streamDiff *= BurstMultiplier;
						map.DiffRating.AddBurst(stream.StartTime, streamDiff);
					}
					else //a stream, not a burst
						map.DiffRating.AddStream(stream.StartTime, streamDiff);
					streamDiffs.Add(streamDiff);

					//Console.WriteLine("s({0}) {1}  bpm:{2:0.0}   avgDist:{3:0.0}  localDiff:{4:0.00}", 
					//   stream.numObjects, FileParserHelpers.TimingParser.getTimeStamp(stream.startTime), streamBPM, stream.avgDistancePx, localDiff);
					avgObjects = RollingAverage(avgObjects, stream.NumObjects, numStreams);
					avgBPM = RollingAverage(avgBPM, streamBPM, numStreams);
					avgDistancePx = RollingAverage(avgDistancePx, stream.AvgDistancePx, numStreams);
					numStreams++;
				}
			}
			//Console.WriteLine("\ntypical stream len:{0:0.0}  bpm:{1:0.0}  dist:{2:0.0}  diff:{3:0.0}", avgObjects, avgBPM, avgDistancePx,
			//   Math.Pow(avgObjects, 0.6) * Math.Pow(avgBPM, 3.2) / 1000000000 * Math.Pow(avgDistancePx, 1.6));

			//Console.Write("stream ");
			if (streamDiffs.Count != 0)
				return GetWeightedSumOfList(streamDiffs, 1.5);
			else
				return 0;
		}

		static double GetCoupletsDifficulty(List<Shape> couplets, double minCoupletsMs, Beatmap map) {
			if (couplets.Count == 0)
				return 0;

			// TODO: actually include the couplets difficulty (their difficulty is still poorly-defined)

			// difficulty is also dependent on the spacing between couplets... ie, closely timed objects are harder to hit than far-timed ones
			// TODO: introduce extra scaling for tongue-twisters
			double difficulty = 0;
			double coupletBPM;
			for (int i = 0; i < couplets.Count; ++i) {
				var couplet = couplets[i];
				double coupletDiff = 0;
				double timeGapMs = couplet.StartTime - couplet.PrevShape.EndTime;

				// measure how abrubt changing between shapes is
				double timeDifferenceForTransition = Math.Abs(couplet.AvgTimeGapMs * 2 - timeGapMs);

				// similar to difference in BPM between streams, but in ms/tick
				double timeDifferenceForSpeeds = Math.Abs(couplet.AvgTimeGapMs - couplet.PrevShape.AvgTimeGapMs);

				// let's define these parameters to make a couplet difiicult
				if (timeGapMs > 0) {
					if (timeDifferenceForTransition <= 20 && timeDifferenceForSpeeds <= 1.5 * couplet.AvgTimeGapMs + 20) {
						coupletBPM = 15000 / couplet.AvgTimeGapMs;
						coupletDiff = Math.Pow(coupletBPM, 3.2) / 1e9 * Math.Pow(couplet.AvgDistancePx + 1, 1.6);
					}
					// difficulty += coupletDiff; // once balancing with other map features is determined
					map.DiffRating.AddCouplet(couplet.StartTime, coupletDiff);
				}
			}
			return difficulty;
		}

		static double GetJumpDifficulty(HitObject current, HitObject prev, Beatmap map) {
			if (prev is not (Hitcircle or Slider))
				return 0;

			double difficulty = 0;
			double dx = current.X;
			double dy = current.Y;
			if (prev is Slider prevSlider) {
				dx -= prevSlider.X2;
				dy -= prevSlider.Y2;
			}
			else {
				dx -= prev.X;
				dy -= prev.Y;
			}

			double distance = Math.Sqrt((dx * dx) + (dy * dy));
			double time = current.StartTime - prev.EndTime;
			if (time > 0) {
				// adjust distance based on CS
				distance -= map.CircleSizePx / 2;
				if (distance > 0) {
					// increase time window based on OD (more time = slower jump)
					double timeWindowFromOD(HitObject obj) => obj is Slider ? (map.MarginOfErrorMs50 / 2) : (map.MarginOfErrorMs300 / 2);
					time += timeWindowFromOD(current);
					time += timeWindowFromOD(prev);

					// difficulty due to the speed of the jump
					double speedDiff = (10 * distance / time);
					speedDiff *= speedDiff; // square is faster than Math.Pow

					// difficulty due to the distance
					double distDiff = speedDiff * 0.4 * distance / (MaxDistPx - map.CircleSizePx / 2);
					difficulty = speedDiff + distDiff;

					//adjustment to make jumps vs streams more reasonable
					difficulty *= 0.1;

					map.DiffRating.AddJump(prev.EndTime, difficulty);
					//Console.WriteLine("jump {0}  {1}  spDiff:{2:0.0}  dstDiff:{3:0.0}  dist:{5:0.0}  bpm:{6:0.0}  ={4:0.00}",  FileParserHelpers.TimingParser.getTimeStamp(prevObject.endTime), FileParserHelpers.TimingParser.getTimeStamp(hitObject.startTime), speedDiff, distDiff, difficulty, distPx, 15000 / distMs);
				}
			}
			return difficulty;
		}

		static double GetSliderDifficulty(Slider slider, Beatmap map) {
			if (slider is null || map is null)
				return 0;
			double difficulty = 0;
			double distance = slider.TotalLength;
			//adjust slider length to consider the margin allowed by od
			distance -= slider.NumSlides * (map.CircleSizePx + map.OverallDifficulty); // TODO: FIX ME (OD MARGIN)
			if (distance > 0) {
				double time = Math.Max(slider.EndTime - slider.StartTime, 1);
				difficulty += Math.Pow(distance / time, 1.5) * Math.Pow(distance, 0.6) / 2;

				//Console.WriteLine("slider {0} => {1}", FileParserHelpers.TimingParser.getTimeStamp(slider.startTime), difficulty);
				/*
				//adjust for tickrate
				double msPerTick = map.sliderTickRate * slider.msPerBeat; //may have to correct this (what are units for tickrate?)
				double pxPerTick = slider.pxPerSecond / 1000.0 * msPerTick;
				//difficulty *= (precisionDelta - 1);
				*/
				map.DiffRating.AddSlider(slider.StartTime, difficulty);
			}

			return difficulty;
		}

		#endregion

		#region Helper functions

		static void UpdateMin(ref double min, double toCompare) {
			if (min < 0 || toCompare < min)
				min = toCompare;
		}

		static double GetWeightedSumOfList(List<double> list, double stdevWeight = 1.5, double weightFactor = 0.9) {
			int n = list.Count;
			if (n == 0)
				return 0;
			else if (n == 1)
				return list[0];
			
			double weightedSum = 0;
			list.Sort();
			//Console.WriteLine("max: {0:0.0}", list[numItems - 1]);

			// weight the highest difficulties the most, decrease by weightFactor each time
			// note that stdev is calculated using Welford's algorithm
			double weight = 1;
			double count = 0; // double for fewer type casts
			double mean = 0;
			double m2 = 0;
			double delta, x;
			for (int i = n - 1; i >= 0; --i) {
				x = list[i] * weight;
				weightedSum += x;
				if (stdevWeight != 0) {
					count += 1;
					delta = x - mean;
					mean += delta / count;
					m2 += delta * (x - mean);
				}
				weight *= weightFactor;
			}

			if (stdevWeight != 0) {
				double stdev = Math.Sqrt(m2 / count);
				double bonus = mean + (stdevWeight * stdev);
				weightedSum += bonus;
				//Console.WriteLine("bonus: {0:0.0}", bonus);
			}
			return weightedSum;
		}

		static double RollingAverage(double currentAvg, double toAdd, double currentNumValues) {
			return (currentAvg * currentNumValues + toAdd) / (currentNumValues + 1);
		}

		#endregion

		//DEBUG
		static void PrintDebug(List<Shape> shapeList, string phrase = "streams") {
			Console.WriteLine("\n---------------{0}-------------------", phrase);
			Console.WriteLine("       (time)               (bpm)  (dist)  (avgdist)");
			foreach (Shape shape in shapeList) {
				shape.PrintDebug();
			}
		}

	}
}
