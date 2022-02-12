﻿namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Collections.Generic;
	using BeatmapObjects;
	using FileParserHelpers;

	class Shape {
		protected List<HitObject> HitObjects = new();
		protected double Effective1_4bpm = -1; //bpm when mapped at 1/4 time-spacing for 4/4 timing
		public double AvgTimeGapMs = 0;
		public double TotalDistancePx = 0;
		public double AvgDistancePx = 0;
		public double MinDistancePx = -1, MaxDistancePx = -1;
		public ShapeType Type;

		public int NumObjects => HitObjects.Count;

		public double StartTime = -1, EndTime = -1;
		public Shape PrevShape = null;

		public Shape() {
		}

		public Shape(params HitObject[] objs) : this() {
			foreach (HitObject obj in objs) {
				Add(obj);
			}
			Analyze();
		}

		public void Add(HitObject obj) {
			HitObjects.Add(obj);
		}

		/// <summary>
		/// Analyze timing and distance between shape objects 
		/// </summary>
		public void Analyze() {
			// TODO: actual shape-analysis logic (will also want to be able to analyze non-constant-timing shapes)
			if (HitObjects.Count == 0)
				return;

			StartTime = HitObjects[0].StartTime;
			EndTime = HitObjects[^1].EndTime;

			if (HitObjects.Count >= 2) {
				// update avg time gap
				// if the second to last uses endTime instead of startTime, stream detection will consider ends of sliders as continuing the stream
				double lastTimeGapMs = HitObjects[NumObjects - 1].StartTime - HitObjects[NumObjects - 2].StartTime;
				AvgTimeGapMs = (AvgTimeGapMs * (NumObjects - 2) + lastTimeGapMs) / (NumObjects - 1);

				// update distances
				double lastDistanceX = HitObjects[NumObjects - 1].X - HitObjects[NumObjects - 2].X;
				double lastDistanceY = HitObjects[NumObjects - 1].Y - HitObjects[NumObjects - 2].Y;
				double lastDistance = Math.Sqrt((lastDistanceX * lastDistanceX) + (lastDistanceY * lastDistanceY));
				AvgDistancePx = (AvgDistancePx * (NumObjects - 2) + lastDistance) / (NumObjects - 1);
				TotalDistancePx += lastDistance;

				if (MinDistancePx < 0 || lastDistance < MinDistancePx)
					MinDistancePx = lastDistance;
				if (MaxDistancePx < 0 || lastDistance > MaxDistancePx)
					MaxDistancePx = lastDistance;
			}
		}

		public double GetEffectiveBPM() {
			Effective1_4bpm = TimingParser.GetBPM(4 * AvgTimeGapMs);
			return Effective1_4bpm;
		}

		//check if next object has constant timing with the current stream
		public int CompareTiming(double nextObjectStartTime) {
			if (HitObjects.Count == 0)
				return -2;
			double timeGap = nextObjectStartTime - HitObjects[^1].StartTime;
			double difference = timeGap - AvgTimeGapMs;
			if (Math.Abs(difference) <= 20) //20ms margin of error = spacing for 3000bpm stream at 1/4 mapping
				return 0;
			else if (difference < 0)
				return -1;
			else
				return 1;
		}

		public void Clear() {
			HitObjects.Clear();
		}

		public void PrintDebug(string prepend = "", string append = "", bool printType = false) {
			Console.Write(prepend);
			if (printType) {
				Console.Write("{0}({1}):  {2}  {3:0.0}ms  {4:0.0}bpm  {5:0.0}px  {6:0.0}px", Type, NumObjects,
						TimingParser.GetTimeStamp(HitObjects[0].StartTime), AvgTimeGapMs,
						GetEffectiveBPM(), TotalDistancePx, AvgDistancePx);
			}
			else {
				Console.Write("({0}):  {1}  {2:0.0}ms  {3:0.0}bpm  {4:0.0}px  {5:0.0}px", NumObjects,
						TimingParser.GetTimeStamp(HitObjects[0].StartTime), AvgTimeGapMs,
						GetEffectiveBPM(), TotalDistancePx, AvgDistancePx);
			}
			Console.WriteLine(append);
		}

		public enum ShapeType {
			Unknown = 0,
			Double,
			Triplet,
			Burst,
			Stream,
			Line,
			Triangle,
			Square,
			RegularPolygon,
			Polygon,
		}
	}
}
