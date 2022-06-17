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
	/// <param name="EffectiveSliderBPM"> Effective bpm for sliders including velocity multipliers </param>
	/// <param name="MsPerBeat"></param>
	record TimingPoint(int Offset = -1, float Bpm = -1, float EffectiveSliderBPM = -1, float MsPerBeat = -1)
		                 : IComparable<TimingPoint> {
		public static TimingPoint Create(int offset, float beatLength, bool isInherited, TimingPoint prevTimingPoint, float beatmapSliderMultiplier) {
			float msPerBeat, bpm, effectiveSliderBPM;

			// new timing point
			if (!isInherited || prevTimingPoint is not TimingPoint prevPoint) {
				msPerBeat = beatLength;
				bpm = TimingParser.GetBPM(beatLength);
				effectiveSliderBPM = bpm * beatmapSliderMultiplier;
			}
			// inherited timing point
			else {
				msPerBeat = prevPoint.MsPerBeat;
				bpm = prevPoint.Bpm;
				// from the updated osu!wiki on `beatLength`, for inherited timing points:
				// "A negative inverse slider velocity multiplier, as a percentage. 
				//  For example, -50 would make all sliders in this section twice as fast as SliderMultiplier"
				float timingSliderMultiplier = -100.0f / beatLength;
				effectiveSliderBPM = TimingParser.GetEffectiveBPM(bpm, msPerBeat) * beatmapSliderMultiplier * timingSliderMultiplier;
			}

			return new TimingPoint(offset, bpm, effectiveSliderBPM, msPerBeat);
		}

		public int CompareTo(TimingPoint other) {
			if (other is null) return 2;
			return Offset.CompareTo(other.Offset);
		}

		public override string ToString()
			=> $"{TimingParser.GetTimeStamp(Offset)}   msPerBeat:{MsPerBeat:f2}   bpm:{Bpm:f1}   effectiveSliderBPM:{EffectiveSliderBPM:f1}";
	}
}
