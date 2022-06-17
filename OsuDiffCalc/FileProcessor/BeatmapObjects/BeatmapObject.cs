namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Collections.Generic;
	using FileParserHelpers;

	/// <summary>
	/// An object in a beatmap which has a defined start and end time
	/// </summary>
	/// <param name="StartTime"> Start time for the object in ms </param>
	/// <param name="EndTime"> End time for the object in ms </param>
	abstract record BeatmapObject(int StartTime, int EndTime) : IComparable<BeatmapObject> {
		/// <summary> End time for the object in ms </summary>
		public int EndTime { get; protected set; } = EndTime;

		public int CompareTo(BeatmapObject other) {
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
