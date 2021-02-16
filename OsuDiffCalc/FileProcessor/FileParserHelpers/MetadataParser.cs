namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class MetadataParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[Metadata]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (string.IsNullOrEmpty(beatmap.Title) && TryAssignStringFromLine(line, "Title", out string title))
				beatmap.Title = title;
			else if (string.IsNullOrEmpty(beatmap.Artist) && TryAssignStringFromLine(line, "Artist", out string artist))
				beatmap.Artist = artist;
			else if (string.IsNullOrEmpty(beatmap.Creator) && TryAssignStringFromLine(line, "Creator", out string creator))
				beatmap.Creator = creator;
			else if (string.IsNullOrEmpty(beatmap.Version) && TryAssignStringFromLine(line, "Version", out string version))
				beatmap.Version = version;
			return true;
		}

	}
}
