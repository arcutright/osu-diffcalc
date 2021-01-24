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

			beatmap.Title = GetStringFromLine(ReadNext(ref reader, "Title"), "Title");
			if (beatmap.Title is null)
				return false;
			beatmap.Artist = GetStringFromLine(ReadNext(ref reader, "Artist"), "Artist");
			if (beatmap.Artist is null)
				return false;
			beatmap.Creator = GetStringFromLine(ReadNext(ref reader, "Creator"), "Creator");
			if (beatmap.Creator is null)
				return false;
			beatmap.Version = GetStringFromLine(ReadNext(ref reader, "Version"), "Version");
			if (beatmap.Version is null)
				return false;
			return true;
		}

	}
}
