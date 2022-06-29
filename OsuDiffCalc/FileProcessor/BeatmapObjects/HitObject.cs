namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using FileParserHelpers;

	/// <summary> 
	/// An interactable object in a beatmap with a time and position (eg. everything but a break section)
	/// </summary>
	/// <param name="X"> X initial position of the HitObject in osupixels </param>
	/// <param name="Y"> Y initial position of the HitObject in osupixels </param>
	abstract record HitObject(float X, float Y, double StartTime, double EndTime) : BeatmapObject(StartTime, EndTime) {
		public override string ToString() => $"{GetType().Name}:  xy({X} {Y})  time({TimingParser.GetTimeStamp(StartTime)} {TimingParser.GetTimeStamp(EndTime)})";
	}
}
