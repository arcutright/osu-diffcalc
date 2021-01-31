namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class GeneralParser : ParserBase {
		/// <summary>
		/// Try to parse the '[General]' beatmap region from the <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if this was an 'osu!standard' file, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[General]"))
				return false;
			beatmap.Mp3FileName = GetStringFromNextLine(ref reader, "AudioFile");
			if (beatmap.Format > 5) {
				int mode = (int)GetDoubleFromNextLine(ref reader, "Mode");
				return mode == 0;
			}
			else {
				// old file formats were only osu!standard
				return true;
			}
		}
	}
}
