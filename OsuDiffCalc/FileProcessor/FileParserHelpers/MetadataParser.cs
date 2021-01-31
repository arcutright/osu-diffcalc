namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;

	class MetadataParser : ParserBase {
		/// <summary>
		/// Try to parse the '[Metadata]' beatmap region from the <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if all the metadata was read, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[Metadata]"))
				return false;
			// TODO: these do not need to be in-order based on the spec
			beatmap.Title = GetStringFromNextLine(ref reader, "Title");
			if (beatmap.Title is null)
				return false;
			beatmap.Artist = GetStringFromNextLine(ref reader, "Artist");
			if (beatmap.Artist is null)
				return false;
			beatmap.Creator = GetStringFromNextLine(ref reader, "Creator");
			if (beatmap.Creator is null)
				return false;
			beatmap.Version = GetStringFromNextLine(ref reader, "Version");
			if (beatmap.Version is null)
				return false;
			return true;
		}

	}
}
