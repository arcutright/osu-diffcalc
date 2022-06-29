namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class DifficultyParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[Difficulty]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if this was an 'osu!standard' file, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (beatmap.HpDrain < 0 && TryAssignFloatFromLine(line, "HPDrain", out var hp))
				beatmap.HpDrain = hp;
			else if (beatmap.CircleSize < 0 && TryAssignFloatFromLine(line, "CircleSize", out var cs)) {
				beatmap.CircleSize = cs;
				beatmap.CircleSizePx = (float)(100.597 - 8.28127 * cs); // TODO: empirically determined, try to validate
			}
			else if (beatmap.OverallDifficulty < 0 && TryAssignFloatFromLine(line, "OverallDiff", out var od)) {
				beatmap.OverallDifficulty = od;
				beatmap.MarginOfErrorMs300 = -6 * od + 79.5f; // TODO: empirically determined, try to validate
				beatmap.MarginOfErrorMs50 = -10 * od + 199.5f; // TODO: empirically determined, try to validate
			}
			else if (beatmap.ApproachRate < 0 && TryAssignFloatFromLine(line, "Approach", out var ar))
				beatmap.ApproachRate = ar;
			else if (TryAssignFloatFromLine(line, "SliderMult", out var sliderMultiplier))
				beatmap.SliderMultiplier = sliderMultiplier;
			else if (TryAssignFloatFromLine(line, "SliderTick", out var sliderTick))
				beatmap.SliderTickRate = sliderTick;
			return true;
		}
		
	}
}
