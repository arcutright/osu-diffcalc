namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using FileParserHelpers;

	/// <summary>
	/// An object in a beatmap which has a defined start and end time
	/// </summary>
	abstract class BeatmapObject : BeatmapElement, IComparable<BeatmapObject> {
		public BeatmapObject(double startTime, double endTime) {
			StartTime = startTime;
			EndTime = endTime;
		}

		/// <summary> Start time for the object </summary>
		public double StartTime { get; protected init; }
		/// <summary> End time for the object </summary>
		public double EndTime { get; protected set; }

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{GetType().Name}:  time({TimingParser.GetTimeStamp(StartTime)} {TimingParser.GetTimeStamp(EndTime)})");
			Console.WriteLine(append);
		}

		public int CompareTo(BeatmapObject other) {
			if (other is null) return 2;
			return StartTime != other.StartTime
				? StartTime.CompareTo(other.StartTime)
				: EndTime.CompareTo(other.EndTime);
		}
	}
}
