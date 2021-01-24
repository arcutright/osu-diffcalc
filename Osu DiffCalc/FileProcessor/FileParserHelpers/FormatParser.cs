namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;

	class FormatParser {
		//static int[] officiallySupportedFormats = { 8, 9, 10, 11, 12, 13, 14 };

		/// <summary>
		/// Try to parse the next line from the <paramref name="reader"/> and populate <see cref="Beatmap.Format"/>
		/// </summary>
		/// <returns> <see langword="true"/> if the format was parsed, otherwise <see langword="false"/> </returns>
		public static bool Parse(Beatmap beatmap, ref StreamReader reader) {
			//beatmap.isOfficiallySupported = false;
			try {
				string formatString = reader.ReadLine();
				if (formatString is not null) {
					formatString = Regex.Replace(formatString, "[^0-9]+", "").Trim();
					if (formatString.Length != 0 && double.TryParse(formatString, out double value)) {
						beatmap.Format = (int)value;
						//if (officiallySupportedFormats.Contains(beatmap.format))
						//    beatmap.isOfficiallySupported = true;
						return true;
					}
				}
			}
			catch (Exception e) {
				Console.WriteLine("missing osu file format");
				Console.WriteLine(e.GetBaseException());
			}
			return false;
		}

	}
}
