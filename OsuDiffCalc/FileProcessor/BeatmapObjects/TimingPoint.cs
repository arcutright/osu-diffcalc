namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Text;
	using FileParserHelpers;

	/// <summary>
	/// A new timing point for the map
	/// </summary>
	/// <param name="Offset">
	/// Start time of the timing section, in milliseconds from the beginning of the beatmap's audio.
	/// The end of the timing section is the next timing point's time (or never, if this is the last timing point).
	/// </param>
	/// <param name="Bpm"> Beats per minute </param>
	/// <param name="MsPerBeat"> Duration of a beat in ms </param>
	/// <param name="SliderVelocityMultiplier"> Effective slider velocity multiplier for this timing point </param>
	/// <param name="EffectiveSliderBPM"> Effective bpm for sliders including velocity multipliers </param>
	record TimingPoint(int Offset, double Bpm, double MsPerBeat, double SliderVelocityMultiplier, double EffectiveSliderBPM)
		                 : IComparable<TimingPoint> {
		public static TimingPoint Create(int offset, double beatLength, bool isInherited, TimingPoint prevTimingPoint, double beatmapSliderMultiplier) {
			double msPerBeat, sliderMultiplier;
			double bpm, effectiveSliderBPM;

			// new timing point
			if (!isInherited || prevTimingPoint is not TimingPoint prevPoint) {
				msPerBeat = beatLength;
				bpm = TimingParser.GetBPM(beatLength);
				effectiveSliderBPM = bpm * beatmapSliderMultiplier;
				sliderMultiplier = beatmapSliderMultiplier;
			}
			// inherited timing point
			else {
				msPerBeat = prevPoint.MsPerBeat;
				bpm = prevPoint.Bpm;
				// from the updated osu!wiki on `beatLength`, for inherited timing points:
				// "A negative inverse slider velocity multiplier, as a percentage. 
				//  For example, -50 would make all sliders in this section twice as fast as SliderMultiplier"
				sliderMultiplier = (-100.0 / beatLength) * beatmapSliderMultiplier;
				effectiveSliderBPM = TimingParser.GetEffectiveBPM(bpm, msPerBeat) * sliderMultiplier;
			}

			return new TimingPoint(offset, bpm, msPerBeat, sliderMultiplier, effectiveSliderBPM);
		}

		public int CompareTo(TimingPoint other) {
			if (other is null) return 2;
			return Offset.CompareTo(other.Offset);
		}

		public override string ToString()
			=> $"{TimingParser.GetTimeStamp(Offset)}, bpm:{Bpm:f1}, sliderMult:{SliderVelocityMultiplier:f2}, msPerBeat:{MsPerBeat:f1}, effectiveSliderBPM:{EffectiveSliderBPM:f1}";
	}
}
