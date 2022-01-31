namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class GeneralParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[General]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if this was an 'osu!standard' file, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (string.IsNullOrEmpty(beatmap.Mp3FileName)) {
				if (TryAssignStringFromLine(line, "AudioFile", out string fn))
					beatmap.Mp3FileName = fn;
			}
			else if (beatmap.Format > 5 && TryAssignIntFromLine(line, "Mode", out int mode)) {
				beatmap.Mode = mode;
				if (mode == 0) // osu!standard
					return true;
				else {
					failureMessage = $"Mode '{mode}', not an osu!standard map: '{Path.GetFileName(beatmap?.Filepath)}'";
					return false;
				}
			}
			// old file formats were only osu!standard
			return true;
		}
	}
}
