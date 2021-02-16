namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using FileParserHelpers;

	/// <summary>
	/// An object in a beatmap which has a defined start and end time
	/// </summary>
	abstract class BeatmapObject : BeatmapElement {
		public BeatmapObject(int startTime, int endTime) {
			StartTime = startTime;
			EndTime = endTime;
		}

		/// <summary> Start time for the object </summary>
		public int StartTime { get; protected init; }
		/// <summary> End time for the object </summary>
		public int EndTime { get; protected set; }

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{GetType().Name}:  time({TimingParser.GetTimeStamp(StartTime)} {TimingParser.GetTimeStamp(EndTime)})");
			Console.WriteLine(append);
		}
	}
}
