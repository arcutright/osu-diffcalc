namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Text;
	using FileParserHelpers;

	readonly record struct TimingPoint(float Offset = -1, float Bpm = -1, float EffectiveSliderBPM = -1, float MsPerBeat = -1)
		                                : IComparable<TimingPoint>, IComparable<TimingPoint?> {
		public static TimingPoint Create(float offset, float beatLength, bool isInherited, TimingPoint? prevTimingPoint, float beatmapSliderMultiplier) {
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
			return Offset.CompareTo(other.Offset);
		}

		public int CompareTo(TimingPoint? other) {
			if (!other.HasValue) return 2;
			return CompareTo(other.Value);
		}

		public override string ToString() {
			return $"{TimingParser.GetTimeStamp(Offset)}   msPerBeat:{MsPerBeat:0.00}   bpm:{Bpm:0.0}   effectiveSliderBPM:{EffectiveSliderBPM:0.0}";
		}
	}
}
