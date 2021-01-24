namespace OsuDiffCalc.FileProcessor {
	using AnalyzerObjects;
	using BeatmapObjects;
	using System;
	using System.Collections.Generic;
	using System.Threading;

	class Analyzer {
		const int MinBurstLength = 3;
		const int MinStreamLength = 6;
		const double BurstMultiplier = 0.75;
		static readonly double MaxDistPx = Math.Sqrt((Beatmap.MaxX * Beatmap.MaxX) + (Beatmap.MaxY * Beatmap.MaxY));

		public static void Analyze(Beatmap beatmap, bool clearLists = true) {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"analyze[{beatmap.Version}]";

			//analysis variables
			var streams = new List<Shape>();
			double minStreamAvgMs = -1;

			var couplets = new List<Shape>();
			double minCoupletAvgMs = -1;

			var triplets = new List<Shape>();
			double minTripletAvgMs = -1;

			var bursts = new List<Shape>();
			double minBurstAvgMs = -1;

			var shape = new Shape();
			var lastShape = new Shape();
			HitObject lastHitObject = null;
			bool shapeAdded = false;

			var jumpDifficultyList = new List<double>();
			var sliderDifficultyList = new List<double>();

			//macro to make life easier
			void addShapeToAppropriateList() {
				shape.PrevShape = lastShape;
				if (shape.NumObjects == 2) {
					shape.Type = Shape.ShapeType.COUPLET;
					couplets.Add(shape);
					UpdateMin(ref minCoupletAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects < MinBurstLength) {
					shape.Type = Shape.ShapeType.TRIPLET;
					triplets.Add(shape);
					UpdateMin(ref minTripletAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects < MinStreamLength) {
					shape.Type = Shape.ShapeType.BURST;
					bursts.Add(shape);
					UpdateMin(ref minBurstAvgMs, shape.AvgTimeGapMs);
				}
				else if (shape.NumObjects >= MinStreamLength) {
					shape.Type = Shape.ShapeType.STREAM;
					streams.Add(shape);
					UpdateMin(ref minStreamAvgMs, shape.AvgTimeGapMs);
				}
				lastShape = shape;
				shapeAdded = true;
			}

			foreach (var obj in beatmap.BeatmapObjects) {
				shapeAdded = false;
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
			int numShapes = 0;
			var diffs = new List<double>();
			foreach (var stream in streams) {
				double streamDiff = 0;
				if (stream.NumObjects >= MinBurstLength && stream.AvgTimeGapMs > 0) {
					double streamBPM = 15000 / stream.AvgTimeGapMs;
					//calculate the difficulty of the stream
					streamDiff = Math.Pow(stream.NumObjects, 0.8) * Math.Pow(streamBPM, 3.4) / 1000000000 * (stream.AvgDistancePx + 1);
					//addition for OD. Maxes ~10%
					streamDiff += streamDiff * 1000 / (stream.AvgTimeGapMs * map.MarginOfErrorMs300);
					//multiplier to represent that bursts are significantly easier
					if (stream.NumObjects < MinStreamLength) {
						streamDiff *= BurstMultiplier;
						map.DiffRating.AddBurst(stream.StartTime, streamDiff);
					}
					else //a stream, not a burst
						map.DiffRating.AddStream(stream.StartTime, streamDiff);
					diffs.Add(streamDiff);

					//Console.WriteLine("s({0}) {1}  bpm:{2:0.0}   avgDist:{3:0.0}  localDiff:{4:0.00}", 
					//   stream.numObjects, FileParserHelpers.TimingParser.getTimeStamp(stream.startTime), streamBPM, stream.avgDistancePx, localDiff);
					avgObjects = RollingAverage(avgObjects, stream.NumObjects, numShapes);
					avgBPM = RollingAverage(avgBPM, streamBPM, numShapes);
					avgDistancePx = RollingAverage(avgDistancePx, stream.AvgDistancePx, numShapes);
					numShapes++;
				}
			}
			//Console.WriteLine("\ntypical stream len:{0:0.0}  bpm:{1:0.0}  dist:{2:0.0}  diff:{3:0.0}", avgObjects, avgBPM, avgDistancePx,
			//   Math.Pow(avgObjects, 0.6) * Math.Pow(avgBPM, 3.2) / 1000000000 * Math.Pow(avgDistancePx, 1.6));

			//Console.Write("stream ");
			double difficulty = 0;
			if (diffs.Count > 0)
				difficulty += GetWeightedSumOfList(diffs, 1.5);

			return difficulty;
		}

		static double GetCoupletsDifficulty(List<Shape> couplets, double minCoupletsMs, Beatmap map) {
			double difficulty = 0;
			//are dependent on the spacing between couplets... ie, closely timed couplets are harder to hit than far-timed ones
			//track with a shape.previous
			double timeDifferenceForTransition; //to measure how abrubt changing between shapes is
			double timeDifferenceForSpeeds; //similar to difference in BPM between streams, but in ms/tick
			double coupleBPM;
			foreach (var couple in couplets) {
				double coupletDiff = 0;
				int timeGapMs = couple.StartTime - couple.PrevShape.EndTime;
				timeDifferenceForTransition = Math.Abs(couple.AvgTimeGapMs * 2 - timeGapMs);
				timeDifferenceForSpeeds = Math.Abs(couple.AvgTimeGapMs - couple.PrevShape.AvgTimeGapMs);
				//let's define these parameters to make a couplet difiicult
				if (timeGapMs > 0) {
					if (timeDifferenceForTransition <= 20 && timeDifferenceForSpeeds <= 1.5 * couple.AvgTimeGapMs + 20) {
						coupleBPM = 15000 / couple.AvgTimeGapMs;
						coupletDiff = Math.Pow(coupleBPM, 3.2) / 1000000000 * Math.Pow(couple.AvgDistancePx + 1, 1.6);
					}
					map.DiffRating.AddCouplet(couple.StartTime, coupletDiff);
				}
			}
			return difficulty;
		}

		static double GetJumpDifficulty(HitObject current, HitObject prev, Beatmap map) {
			if (prev is not (Hitcircle or Slider))
				return 0;

			double difficulty = 0;
			int dx = current.X;
			int dy = current.Y;
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
				//adjust distance based on CS
				distance -= map.CircleSizePx / 2;
				if (distance > 0) {
					//adjust time based on OD
					double timeOffsetFromOD(HitObject obj) => obj is Slider ? (map.MarginOfErrorMs50 / 2) : (map.MarginOfErrorMs300 / 2);
					time += timeOffsetFromOD(current);
					time += timeOffsetFromOD(prev);

					//difficulty due to the speed of the jump
					double speedDiff = Math.Pow(10 * distance / time, 2);
					//difficulty due to the distance
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
			// TODO: check to make sure timegapms > 0
			double difficulty = 0;
			double distance = slider.TotalLength;
			//adjust slider length to consider the margin allowed by od
			distance -= ((double)slider.Repeat * (map.CircleSizePx + map.Accuracy)); // TODO: FIX ME (OD MARGIN)
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

		static double GetWeightedSumOfList(List<double> list, double addMeanAndDeviations = 0, double weightFactor = 0.9) {
			double difficulty = 0;
			int numItems = list.Count;
			if (numItems > 1) {
				list.Sort();
				//Console.WriteLine("max: {0:0.0}", list[numItems - 1]);

				//weight the highest difficulties the greatest, decrease by weightFactor each time
				double weight = 1;
				double average = 0;
				for (int i = numItems - 1; i >= 0; i--) {
					double localDiff = list[i] * weight;
					difficulty += localDiff;
					if (addMeanAndDeviations > 0)
						average = RollingAverage(average, localDiff, numItems - i);
					weight *= weightFactor;

				}
				if (addMeanAndDeviations > 0) {
					double bonus = average + addMeanAndDeviations * GetStandardDeviation(list, average);
					difficulty += bonus;
					//Console.WriteLine("bonus: {0:0.0}", bonus);
				}
			}
			else if (numItems == 1)
				difficulty = list[0];
			else
				difficulty = 0;
			return difficulty;
		}

		static double GetStandardDeviation(List<double> list, double avg) {
			double variance = 0;
			foreach (double value in list) {
				//variance += Math.Pow(value - avg, 2);
				variance += value * value;
			}
			variance -= list.Count * avg * avg; //
			variance /= list.Count;
			return Math.Sqrt(variance);
		}

		static double RollingAverage(double currentAvg, double toAdd, int currentNumValues) {
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
