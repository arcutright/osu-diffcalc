namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class DifficultyParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[Difficulty]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if this was an 'osu!standard' file, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (beatmap.HpDrain < 0 && TryAssignDoubleFromLine(line, "HPDrain", out var hp))
				beatmap.HpDrain = hp;
			else if (beatmap.CircleSize < 0 && TryAssignDoubleFromLine(line, "CircleSize", out var cs)) {
				beatmap.CircleSize = cs;
				beatmap.CircleSizePx = -8.28127 * cs + 100.597; // empirically determined
			}
			else if (beatmap.OverallDifficulty < 0 && TryAssignDoubleFromLine(line, "OverallDiff", out var od)) {
				beatmap.OverallDifficulty = od;
				beatmap.MarginOfErrorMs300 = -6 * od + 79.5; // empirically determined
				beatmap.MarginOfErrorMs50 = -10 * od + 199.5;
			}
			else if (beatmap.ApproachRate < 0 && TryAssignDoubleFromLine(line, "Approach", out var ar))
				beatmap.ApproachRate = ar;
			else if (TryAssignDoubleFromLine(line, "SliderMult", out var sliderMultiplier))
				beatmap.SliderMultiplier = sliderMultiplier;
			else if (TryAssignDoubleFromLine(line, "SliderTick", out var sliderTick))
				beatmap.SliderTickRate = sliderTick;
			return true;
		}
		
	}
}
