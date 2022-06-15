namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using FileParserHelpers;

	/// <summary> 
	/// An interactable object in a beatmap with a time and position (eg. everything but a break section)
	/// </summary>
	abstract class HitObject : BeatmapObject {
		public HitObject(float x, float y, double startTime, double endTime) : base(startTime, endTime) {
			X = x;
			Y = y;
		}

		/// <summary> X initial position of the HitObject in osupixels </summary>
		public float X { get; protected init; }
		/// <summary> Y initial position of the HitObject in osupixels </summary>
		public float Y { get; protected init; }

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{GetType().Name}:  xy({X} {Y})  time({TimingParser.GetTimeStamp(StartTime)} {TimingParser.GetTimeStamp(EndTime)})");
			Console.WriteLine(append);
		}
	}
}
