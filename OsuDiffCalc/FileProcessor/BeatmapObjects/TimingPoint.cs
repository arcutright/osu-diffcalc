namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using OsuDiffCalc.FileProcessor.FileParserHelpers;

	class TimingPoint : BeatmapElement {
		public TimingPoint(int offset, double beatLength, bool isInherited, TimingPoint prevTimingPoint, double beatmapSliderMultiplier) {
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

		public int Offset { get; protected init; } = -1;
		public double Bpm { get; protected init; } = -1;
		public double EffectiveSliderBPM { get; protected init; } = -1;
		public double MsPerBeat { get; protected init; } = -1;

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write("{0}   msPerBeat:{1:0.00}   bpm:{2:0.0}   effectiveSliderBPM:{3:0.0}",
					TimingParser.GetTimeStamp(Offset), MsPerBeat, Bpm, EffectiveSliderBPM);
			Console.WriteLine(append);
		}
	}
}
