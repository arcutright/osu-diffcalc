namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.IO;
	using System.Linq;
	using BeatmapObjects;

	class TimingParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[TimingPoints]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if there were errors parsing the line, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			/* the general form of timing points are x,x,x,x,x,x,x,x
			 * 1st x means the offset
			 * 2nd x (positive) means the BPM but in the other format, the BPM in Edit is equal to 60,000/x
			 * 2nd x (negative) means the BPM multiplier - kind of, BPM multiplier in Edit is equal to -100/x
			 * 3rd x is related to metronome, 3 means 3/4 beat, 4 means 4/4 beats
			 * 4th x is about hitsounds set: soft/normal...etc
			 * 5th x is custom hitsounds: 0 means no custom hitsound, 1 means set 1, 2 means set 2
			 * 6th x is volume
			 * 7th x means if it's inherit timing point
			 * 8th x is kiai time */
			// modern maps: [offset, beatLength, Meter, Sample Type, Sample Set, Volume, Inherited, Kiai Mode]
			// old maps:    [offset, msPerBeat]
			failureMessage = "";
			string[] data = line.Split(',');
			if (data.Length < 2) {
				failureMessage = $"Incomplete timing point at line {lineNumber}";
				return false;
			}
			if (!double.TryParse(data[0], out double offset)) {
				failureMessage = $"Could not parse offset for timing point at line {lineNumber}";
				return false;
			}
			if (!double.TryParse(data[1], out double beatLength)) {
				failureMessage = $"Could not parse beat length for timing point at line {lineNumber}";
				return false;
			}
			// NOTE: the .osu file format requires timing points to be in chronological order
			TimingPoint lastTimingPoint = beatmap.TimingPoints.Count != 0 ? beatmap.TimingPoints[^1] : null;
			bool isInherited;
			if (data.Length > 6 && double.TryParse(data[6], out double uninherited))
				isInherited = (int)Math.Round(uninherited) == 0 || (beatLength < 0 && lastTimingPoint is not null);
			else
				isInherited = beatLength < 0;
			var timingPoint = new TimingPoint(offset, beatLength, isInherited, lastTimingPoint, beatmap.SliderMultiplier);
			beatmap.AddTiming(timingPoint);
			return true;
		}

		public static double GetEffectiveBPM(double bpm, double msPerBeat) {
			// inherited timing point
			if (msPerBeat < 0)
				return bpm * (-100.0) / msPerBeat;
			// independent timing point
			else
				return bpm;
		}

		public static double GetBPM(double msPerBeat) {
			return 60000.0 / msPerBeat;
		}

		public static double GetMsPerBeat(double bpm) {
			return 60000.0 / bpm;
		}

		public static string GetTimeStamp(double ms, bool hours = false) {
			var ts = TimeSpan.FromMilliseconds(ms);
			return ts.Hours != 0 
				? $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}"
				: $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
		}
	}
}
