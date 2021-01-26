namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class DifficultyParser : ParserBase {
		/// <summary>
		/// Try to parse the '[Difficulty]' beatmap region from the <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if the difficulty info was parsed, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[Difficulty]"))
				return false;
			beatmap.HpDrain = GetDoubleFromNextLine(ref reader, "HPDrain");
			beatmap.CircleSize = GetDoubleFromNextLine(ref reader, "CircleSize");
			beatmap.Accuracy = GetDoubleFromNextLine(ref reader, "OverallDiff");

			beatmap.CircleSizePx = -8.28127 * beatmap.CircleSize + 100.597; //empirically determined
			beatmap.MarginOfErrorMs300 = -6 * beatmap.Accuracy + 79.5;
			beatmap.MarginOfErrorMs50 = -10 * beatmap.Accuracy + 199.5;

			if (beatmap.Format > 7)
				beatmap.ApproachRate = GetDoubleFromNextLine(ref reader, "Approach");
			else //back when ar and od were tied
				beatmap.ApproachRate = beatmap.Accuracy;
			beatmap.SliderMultiplier = GetDoubleFromNextLine(ref reader, "SliderMult");
			beatmap.SliderTickRate = GetDoubleFromNextLine(ref reader, "SliderTick");
			return true;
		}
	}
}
