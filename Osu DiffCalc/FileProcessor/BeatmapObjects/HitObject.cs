namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using static FileParserHelpers.TimingParser;

	abstract class HitObject : BeatmapObject {
		public HitObject(int x, int y, int startTime, int endTime) : base(startTime, endTime) {
			X = x;
			Y = y;
		}

		public int X { get; protected init; }
		public int Y { get; protected init; }

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{GetType().Name}:  xy({X} {Y})  time({GetTimeStamp(StartTime)} {GetTimeStamp(EndTime)})");
			Console.WriteLine(append);
		}
	}
}
