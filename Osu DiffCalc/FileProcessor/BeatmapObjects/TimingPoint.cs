namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using OsuDiffCalc.FileProcessor.FileParserHelpers;

	class TimingPoint : BeatmapElement {
		public int Offset = -1;
		public int InheritedOffset = -1;
		public double Bpm = -1;
		public double EffectiveSliderBPM = -1;
		public double MsPerBeat = -1;

		public TimingPoint(TimingPoint lastRootPoint, int offset, double msPerBeat) {
			Offset = offset;

			//new timing point
			if (msPerBeat >= 0) {
				MsPerBeat = msPerBeat;
				EffectiveSliderBPM = TimingParser.GetBPM(msPerBeat);
				Bpm = EffectiveSliderBPM;
				InheritedOffset = offset;
			}
			//inherited timing point
			else if (lastRootPoint is not null) {
				Bpm = lastRootPoint.Bpm;
				InheritedOffset = lastRootPoint.InheritedOffset;
				EffectiveSliderBPM = TimingParser.GetEffectiveBPM(Bpm, msPerBeat);
				MsPerBeat = lastRootPoint.MsPerBeat;
			}
		}

		public override void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write("{0}   msPerBeat:{1:0.00}   bpm:{2:0.0}   effectiveSliderBPM:{3:0.0}",
					TimingParser.GetTimeStamp(Offset), MsPerBeat, Bpm, EffectiveSliderBPM);
			Console.WriteLine(append);
		}
	}
}
