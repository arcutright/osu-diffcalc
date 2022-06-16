namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using FileParserHelpers;

	/// <summary>
	/// An object in a beatmap which has a defined start and end time
	/// </summary>
	abstract class BeatmapObject : IComparable<BeatmapObject> {
		public BeatmapObject(int startTime, int endTime) {
			StartTime = startTime;
			EndTime = endTime;
		}

		/// <summary> Start time for the object </summary>
		public int StartTime { get; protected init; }
		/// <summary> End time for the object </summary>
		public int EndTime { get; protected set; }

		public int CompareTo(BeatmapObject other) {
			if (other is null) return 2;
			return StartTime != other.StartTime
				? StartTime.CompareTo(other.StartTime)
				: EndTime.CompareTo(other.EndTime);
		}

		public virtual void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write(ToString());
			Console.WriteLine(append);
		}

		public override string ToString() => $"{GetType().Name}:  time({TimingParser.GetTimeStamp(StartTime)} {TimingParser.GetTimeStamp(EndTime)})";
	}
}
