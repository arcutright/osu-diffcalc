namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using BeatmapObjects;
	using FileParserHelpers;

	class Shape {
		protected List<HitObject> HitObjects = new();
		protected float Effective1_4bpm = -1; //bpm when mapped at 1/4 time-spacing for 4/4 timing
		public float AvgTimeGapMs = 0;
		public double TotalDistancePx = 0;
		public float AvgDistancePx = 0;
		public float MinDistancePx = -1, MaxDistancePx = -1;
		public ShapeType Type;

		public int NumObjects => HitObjects.Count;

		public int StartTime = -1, EndTime = -1;
		public Shape PrevShape = null;

		public Shape() {
		}

		public Shape(HitObject start, HitObject next) : this() {
			Add(start);
			Add(next);
		}

		public void Add(HitObject obj) {
			HitObjects.Add(obj);

			int n = HitObjects.Count;

			// update start and end time
			if (n == 1 || StartTime < 0)
				StartTime = HitObjects.First().StartTime;
			EndTime = obj.EndTime;

			if (n >= 2) {
				var prevObj = HitObjects[n - 2];

				// update average time spacing 
				int lastTimeGapMs = obj.StartTime - prevObj.StartTime;
				AvgTimeGapMs = (AvgTimeGapMs * (n - 2) + lastTimeGapMs) / (n - 1);

				// update distances
				float lastDistanceX = obj.X - prevObj.X;
				float lastDistanceY = obj.Y - prevObj.Y;
				float lastDistance = (float)Math.Sqrt((lastDistanceX * lastDistanceX) + (lastDistanceY * lastDistanceY));
				AvgDistancePx = (AvgDistancePx * (n - 2) + lastDistance) / (n - 1);
				TotalDistancePx += lastDistance;

				if (MinDistancePx < 0 || lastDistance < MinDistancePx)
					MinDistancePx = lastDistance;
				if (MaxDistancePx < 0 || lastDistance > MaxDistancePx)
					MaxDistancePx = lastDistance;
			}
		}

		/// <summary>
		/// Analyze timing and distance between shape objects 
		/// </summary>
		public void Analyze() {
			// TODO: actual shape-analysis logic (may also want to be able to analyze non-constant-timing shapes)
		}

		public float GetEffectiveBPM() {
			Effective1_4bpm = TimingParser.GetBPM(4 * AvgTimeGapMs);
			return Effective1_4bpm;
		}

		//check if next object has constant timing with the current stream
		public int CompareTiming(int nextObjectStartTime) {
			if (HitObjects.Count == 0)
				return -2;
			var timeGapMs = nextObjectStartTime - HitObjects[^1].StartTime;
			var difference = timeGapMs - AvgTimeGapMs;
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

		public void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write(ToString());
			Console.WriteLine(append);
		}

		public override string ToString() {
			return string.Format("{0}({1}):  {2}  {3:0.0}ms  {4:0.0}bpm  {5:0.0}px  {6:0.0}px",
				Type, NumObjects,
				TimingParser.GetTimeStamp(HitObjects[0].StartTime),
				AvgTimeGapMs, GetEffectiveBPM(), TotalDistancePx, AvgDistancePx);
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
