namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.IO;
	using System.Linq;
	using BeatmapObjects;

	class TimingParser : ParserBase {
		/* the general form of timing points are x,x,x,x,x,x,x,x
		1st x means the offset
		2nd x (positive) means the BPM but in the other format, the BPM in Edit is equal to 60,000/x
		2nd x (negative) means the BPM multiplier - kind of, BPM multiplier in Edit is equal to -100/x
		3rd x is related to metronome, 3 means 3/4 beat, 4 means 4/4 beats
		4th x is about hitsounds set: soft/normal...etc
		5th x is custom hitsounds: 0 means no custom hitsound, 1 means set 1, 2 means set 2
		6th x is volume
		7th x means if it's inherit timing point
		8th x is kiai time */

		/// <summary>
		/// Try to parse the '[TimingPoints]' beatmap region from the <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if any timing points were parsed, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[TimingPoints]"))
				return false;
			TimingPoint lastTimingPoint = beatmap.TimingPoints.Count != 0 ? beatmap.TimingPoints[^1] : null;

			string line;
			while ((line = reader.ReadLine()) is not null) {
				string[] data = line.Split(',');
				if (data.Length >= 2) {
					//Offset, Milliseconds per Beat, Meter, Sample Type, Sample Set, Volume, Inherited, Kiai Mode
					//old maps only have   offset,msPerBeat
					int offset = (int)double.Parse(data[0]);
					double msPerBeat = double.Parse(data[1]);
					var timingPoint = new TimingPoint(lastTimingPoint, offset, msPerBeat);
					beatmap.AddTiming(timingPoint);
					lastTimingPoint = timingPoint;
				}
				if (!reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			if (beatmap.NumTimingPoints > 0)
				return true;
			else
				return false;
		}

		public static double GetEffectiveBPM(double bpm, double msPerBeat) {
			//inherited timing point
			if (msPerBeat < 0)
				return bpm * (-100.0) / msPerBeat;
			//independent timing point
			else
				return bpm;
		}

		public static double GetBPM(double msPerBeat) {
			return 60000.0 / msPerBeat;
		}

		public static double GetMsPerBeat(double bpm) {
			return 60000.0 / bpm;
		}

		public static string GetTimeStamp(long ms, bool hours = false) {
			var ts = TimeSpan.FromMilliseconds(ms);
			return hours 
				? $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}"
				: $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
		}
	}
}
