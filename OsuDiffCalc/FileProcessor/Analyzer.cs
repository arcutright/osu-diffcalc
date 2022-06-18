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
		const float BurstMultiplier = 0.75f;
		static readonly float MaxDistPx = (float)Math.Sqrt((Beatmap.MaxX * Beatmap.MaxX) + (Beatmap.MaxY * Beatmap.MaxY));

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
			foreach (var obj in beatmap.BeatmapObjects) {
				if (obj is not Slider slider) continue;
				// find current timing point
				while (timingPointIndex < beatmap.TimingPoints.Count && slider.StartTime >= beatmap.TimingPoints[timingPointIndex].Offset) {
					timingPoint = beatmap.TimingPoints[timingPointIndex];
						++timingPointIndex;
					}
				slider.AnalyzeShape(timingPoint);
				}
				slider.AnalyzeShape(timingPoint, beatmap.SliderMultiplier);
			}

			// analysis variables
			float minStreamAvgMs = -1, minDoubleAvgMs = -1, minTripletAvgMs = -1, minBurstAvgMs = -1;
			var streams = new List<Shape>();
			var doubles = new List<Shape>();
			var triplets = new List<Shape>();
			var bursts = new List<Shape>();

			var shape = new Shape();
			var lastShape = new Shape();
			HitObject lastHitObject = null;

			var jumpDifficultyList = new List<float>();
			var sliderDifficultyList = new List<float>();

			// macro to make life easier
			void addShapeToAppropriateList(Shape shape) {
				shape.Analyze();
				shape.PrevShape = lastShape;
				if (shape.NumObjects == 2) {
					shape.Type = Shape.ShapeType.Double;
					doubles.Add(shape);
					UpdateMin(ref minDoubleAvgMs, shape.AvgTimeGapMs);
				}
				// currently considering a triplet as a burst
				//else if (shape.NumObjects < MinBurstLength) { 
				//	shape.Type = Shape.ShapeType.Triplet;
				//	triplets.Add(shape);
				//	UpdateMin(ref minTripletAvgMs, shape.AvgTimeGapMs);
				//}
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
			}

			foreach (var obj in beatmap.BeatmapObjects) {
				// TODO: fast spinners can act like jumps
				// TODO: calculate spinner properties (min speed for 300, 100, etc -- include some 'get to spinner' margin)
				if (obj is HitObject hitObj) {
					// first hit object
					if (lastHitObject is null)
						shape.Add(hitObj);
					// shape analysis for triplet, streams, etc
					else {
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
								addShapeToAppropriateList(shape);
								shape = new Shape(lastHitObject, hitObj);
							}
						}
					}
					// slider difficulty
					if (hitObj is Slider slider)
						sliderDifficultyList.Add(GetSliderDifficulty(slider, beatmap));
					// jump difficulty
					if (lastHitObject is not null)
						jumpDifficultyList.Add(GetJumpDifficulty(hitObj, lastHitObject, beatmap));

					lastHitObject = hitObj;
				}
				// obj is a Spinner, BreakSection, etc.
				else if (shape.NumObjects != 0) {
					addShapeToAppropriateList(shape);
					shape = new();
					lastHitObject = null;
				}
			}
			if (shape.NumObjects != 0)
				addShapeToAppropriateList(shape);

			var streamsDiff = GetStreamsDifficulty(streams, minStreamAvgMs, beatmap);
			var doublesDifficulty = GetDoublesDifficulty(doubles, minDoubleAvgMs, beatmap);
			var burstsDiff = GetStreamsDifficulty(bursts, minBurstAvgMs, beatmap);
			var (streamsDifficulty, streamsAvgBPM, streamsMaxBPM) = streamsDiff;
			var (burstsDifficulty, burstsAvgBPM, burstsMaxBPM) = burstsDiff;
			//Console.Write("jump ");
			var jumpsDifficulty = GetWeightedSumOfList(jumpDifficultyList, 1.5f);
			//Console.Write("slider ");
			var slidersDifficulty = GetWeightedSumOfList(sliderDifficultyList, 1.5f);
			var totalDifficulty = streamsDifficulty + burstsDifficulty + doublesDifficulty + jumpsDifficulty + slidersDifficulty;

			beatmap.DiffRating.JumpsDifficulty = jumpsDifficulty;
			beatmap.DiffRating.StreamsDifficulty = streamsDifficulty;
			beatmap.DiffRating.BurstsDifficulty = burstsDifficulty;
			beatmap.DiffRating.DoublesDifficulty = doublesDifficulty;
			beatmap.DiffRating.SlidersDifficulty = slidersDifficulty;
			beatmap.DiffRating.TotalDifficulty = totalDifficulty;
			beatmap.DiffRating.StreamsAverageBPM = streamsAvgBPM > 0 ? streamsAvgBPM : burstsAvgBPM;
			beatmap.DiffRating.StreamsMaxBPM = streamsMaxBPM > 0 ? streamsMaxBPM : burstsMaxBPM;
			beatmap.IsAnalyzed = true;
			/*
			Console.WriteLine("\n{1:000} jumps diff    = {0:0.0}", jumpsDifficulty, jumpDifficultyList.Count);
			Console.WriteLine("{1:000} streams diff  = {0:0.0}", streamsDifficulty, streams.Count);
			Console.WriteLine("{1:000} bursts diff   = {0:0.0}", burstsDifficulty, bursts.Count);
			Console.WriteLine("{1:000} doubles diff = {0:0.0}", doublesDifficulty, doubles.Count);
			Console.WriteLine("{1:000} sliders diff  = {0:0.0}", slidersDifficulty, sliderDifficultyList.Count);
			Console.WriteLine("* total diff  = {0:0.0}", beatmap.diffRating.totalDifficulty);
			//printDebug(doubles, "doubles");
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

		private readonly record struct StreamsResult(double Difficulty, float AverageBPM, float MaxBPM);

		static StreamsResult GetStreamsDifficulty(IReadOnlyList<Shape> streams, double minStreamMs, Beatmap map) {
			if (map is null || streams is null || streams.Count == 0)
				return default;

			float avgObjects = 0;
			float avgBPM = 0;
			float maxBPM = 0;
			float avgDistancePx = 0;
			int numStreams = 0;
			var streamDiffs = new List<float>();
			foreach (var stream in streams) {
				float streamDiff = 0;
				if (stream.NumObjects >= MinBurstLength && stream.AvgTimeGapMs > 0) {
					float streamBPM = 15000f / stream.AvgTimeGapMs;
					//calculate the difficulty of the stream
					streamDiff = (float)(Math.Pow(stream.NumObjects, 0.8) * Math.Pow(streamBPM, 3.4) / 1e9 * (stream.AvgDistancePx + 1));
					//addition for OD. Maxes ~10%
					streamDiff += streamDiff * 1000f / (stream.AvgTimeGapMs * map.MarginOfErrorMs300);
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
					maxBPM = Math.Max(maxBPM, streamBPM);
					++numStreams;
				}
			}
			//Console.WriteLine("\ntypical stream len:{0:0.0}  bpm:{1:0.0}  dist:{2:0.0}  diff:{3:0.0}", avgObjects, avgBPM, avgDistancePx,
			//   Math.Pow(avgObjects, 0.6) * Math.Pow(avgBPM, 3.2) / 1000000000 * Math.Pow(avgDistancePx, 1.6));

			//Console.Write("stream ");
			if (streamDiffs.Count != 0)
				return new StreamsResult(GetWeightedSumOfList(streamDiffs, 1.5f), avgBPM, maxBPM);
			else
				return default;
		}

		static float GetDoublesDifficulty(IReadOnlyList<Shape> doubles, float minDoubleMs, Beatmap map) {
			if (doubles.Count == 0)
				return 0;

			// TODO: actually include the doubles difficulty (their difficulty is still poorly-defined)

			// difficulty is also dependent on the spacing between doubles... ie, closely timed objects are harder to hit than far-timed ones
			// TODO: introduce extra scaling for tongue-twisters
			float difficulty = 0;
			float doubleBPM;
			for (int i = 0; i < doubles.Count; ++i) {
				var theDouble = doubles[i];
				float doubleDiff = 0;
				var timeGapMs = theDouble.StartTime - theDouble.PrevShape.EndTime;

				// measure how abrubt changing between shapes is
				var timeDifferenceForTransition = Math.Abs(theDouble.AvgTimeGapMs * 2 - timeGapMs);

				// similar to difference in BPM between streams, but in ms/tick
				var timeDifferenceForSpeeds = Math.Abs(theDouble.AvgTimeGapMs - theDouble.PrevShape.AvgTimeGapMs);

				// let's define these parameters to make a double difiicult
				if (timeGapMs > 0) {
					if (timeDifferenceForTransition <= 20 && timeDifferenceForSpeeds <= 1.5 * theDouble.AvgTimeGapMs + 20) {
						doubleBPM = 15000 / theDouble.AvgTimeGapMs;
						doubleDiff = (float)(Math.Pow(doubleBPM, 3.2) / 1e9 * Math.Pow(theDouble.AvgDistancePx + 1, 1.6));
					}
					// difficulty += doubleDiff; // once balancing with other map features is determined
					map.DiffRating.AddDouble(theDouble.StartTime, doubleDiff);
				}
			}
			return difficulty;
		}

		static float GetJumpDifficulty(HitObject current, HitObject prev, Beatmap map) {
			if (prev is not (Hitcircle or Slider))
				return 0;

			float difficulty = 0;
			float dx = current.X;
			float dy = current.Y;
			if (prev is Slider prevSlider) {
				dx -= prevSlider.X2;
				dy -= prevSlider.Y2;
			}
			else {
				dx -= prev.X;
				dy -= prev.Y;
			}

			float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
			float time = (float)(current.StartTime - prev.EndTime);
			if (time > 0) {
				// adjust distance based on CS
				distance -= map.CircleSizePx / 2;
				if (distance > 0) {
					// increase time window based on OD (more time = slower jump)
					float timeWindowFromOD(HitObject obj) => obj is Slider ? (map.MarginOfErrorMs50 / 2) : (map.MarginOfErrorMs300 / 2);
					time += timeWindowFromOD(current);
					time += timeWindowFromOD(prev);

					// difficulty due to the speed of the jump
					float speedDiff = (10 * distance / time);
					speedDiff *= speedDiff; // square is faster than Math.Pow

					// difficulty due to the distance
					float distDiff = speedDiff * 0.4f * distance / (MaxDistPx - map.CircleSizePx / 2);
					difficulty = speedDiff + distDiff;

					//adjustment to make jumps vs streams more reasonable
					difficulty *= 0.1f;

					map.DiffRating.AddJump(prev.EndTime, difficulty);
					//Console.WriteLine("jump {0}  {1}  spDiff:{2:0.0}  dstDiff:{3:0.0}  dist:{5:0.0}  bpm:{6:0.0}  ={4:0.00}",  FileParserHelpers.TimingParser.getTimeStamp(prevObject.endTime), FileParserHelpers.TimingParser.getTimeStamp(hitObject.startTime), speedDiff, distDiff, difficulty, distPx, 15000 / distMs);
				}
			}
			return difficulty;
		}

		static float GetSliderDifficulty(Slider slider, Beatmap map) {
			if (slider is null || map is null)
				return 0;
			float difficulty = 0;
			double distance = slider.TotalLength;
			//adjust slider length to consider the margin allowed by od
			distance -= slider.NumSlides * (map.CircleSizePx + map.OverallDifficulty); // TODO: FIX ME (OD MARGIN)
			if (distance > 0) {
				var time = Math.Max(slider.EndTime - slider.StartTime, 1);
				difficulty += (float)(Math.Pow(distance / time, 1.5) * Math.Pow(distance, 0.6) / 2);

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

		static void UpdateMin(ref float min, float toCompare) {
			if (min < 0 || toCompare < min)
				min = toCompare;
		}

		static double GetWeightedSumOfList(List<float> list, float stdevWeight = 1.5f, float weightFactor = 0.9f) {
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
			float weight = 1;
			float count = 0; // double for fewer type casts
			float mean = 0;
			float m2 = 0;
			float delta, x;
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
				float stdev = (float)Math.Sqrt(m2 / count);
				float bonus = mean + (stdevWeight * stdev);
				weightedSum += bonus;
				//Console.WriteLine("bonus: {0:0.0}", bonus);
			}
			return weightedSum;
		}

		static float RollingAverage(float currentAvg, float toAdd, int currentNumValues) {
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
