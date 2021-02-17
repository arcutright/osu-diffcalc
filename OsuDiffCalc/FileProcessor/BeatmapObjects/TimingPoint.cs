namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using OsuDiffCalc.FileProcessor.FileParserHelpers;

	class TimingPoint : BeatmapElement, IComparable<TimingPoint> {
		public TimingPoint(double offset, double beatLength, bool isInherited, TimingPoint prevTimingPoint, double beatmapSliderMultiplier) {
			Offset = offset;

			// new timing point
			if (!isInherited || prevTimingPoint is null) {
				MsPerBeat = beatLength;
				Bpm = TimingParser.GetBPM(beatLength);
				EffectiveSliderBPM = Bpm * beatmapSliderMultiplier;
			}
			// inherited timing point
			else {
				MsPerBeat = prevTimingPoint.MsPerBeat;
				Bpm = prevTimingPoint.Bpm;
				// from the updated osu!wiki on `beatLength`, for inherited timing points:
				// "A negative inverse slider velocity multiplier, as a percentage. 
				//  For example, -50 would make all sliders in this section twice as fast as SliderMultiplier"
				double timingSliderMultiplier = -100.0 / beatLength;
				EffectiveSliderBPM = TimingParser.GetEffectiveBPM(Bpm, MsPerBeat) * beatmapSliderMultiplier * timingSliderMultiplier;
			}
		}

		public double Offset { get; protected init; } = -1;
		public double Bpm { get; protected init; } = -1;
		public double EffectiveSliderBPM { get; protected init; } = -1;
		public double MsPerBeat { get; protected init; } = -1;

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{TimingParser.GetTimeStamp(Offset)}   msPerBeat:{MsPerBeat:0.00}   bpm:{Bpm:0.0}   effectiveSliderBPM:{EffectiveSliderBPM:0.0}");
			Console.WriteLine(append);
		}

		public int CompareTo(TimingPoint other) {
			if (other is null) return 2;
			return Offset.CompareTo(other.Offset);
		}
	}
}
