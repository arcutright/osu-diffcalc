namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;

	class FormatParser {
		//static int[] officiallySupportedFormats = { 8, 9, 10, 11, 12, 13, 14 };
		private static readonly Regex _fileFormatRegex = new Regex(
			"^\\s*(?:osu\\s+file\\s+format\\s+v)?(\\d+(?:\\.\\d+)?).*",
			RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

		/// <summary>
		/// Try to parse the first line from the <paramref name="reader"/>
		/// and populate <see cref="Beatmap.Format"/>
		/// </summary>
		/// <returns> <see langword="true"/> if the format was parsed and is supported, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader, out string failureMessage) {
			//beatmap.isOfficiallySupported = false;
			if (reader.BaseStream.CanSeek)
				reader.BaseStream.Seek(0, SeekOrigin.Begin);
			if (reader.EndOfStream) {
				failureMessage = "Cannot read map: End of file stream";
				return false;
			}

			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				// continue past empty lines
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				// assume the first non-empty non-comment line is the format line
				// ex: 'osu file format v14'
				try {
					var match = _fileFormatRegex.Match(line);
					if (match.Success && int.TryParse(match.Groups[1].Value, out var format)) {
						beatmap.Format = format;
						//if (officiallySupportedFormats.Contains(format))
						//    beatmap.isOfficiallySupported = true;
						failureMessage = "";
						return true;
					}
				}
				catch (Exception e) {
					failureMessage = $"Could not parse osu file format! Line: '{line}'";
					Console.WriteLine("missing osu file format");
					Console.WriteLine(e.GetBaseException());
					return false;
				}
			}
			failureMessage = $"Did not find osu file format in first line '{line}'";
			return false;
		}

	}
}
