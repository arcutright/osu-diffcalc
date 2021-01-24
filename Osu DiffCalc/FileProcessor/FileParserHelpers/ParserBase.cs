namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.Globalization;
	using System.IO;

	abstract class ParserBase {
		//public static abstract bool Parse(Beatmap beatmap, ref StreamReader reader);

		/// <summary>
		/// Try to skip to the next line that starts with <paramref name="prefix"/>. Will consume the line that contains it.
		/// </summary>
		protected static bool TrySkipTo(ref StreamReader reader, string prefix) {
			string line;
			while ((line = reader.ReadLine()) is not null) {
				if (line.StartsWith(prefix))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Return the next line that contains the given <paramref name="keyword"/>, or <see langword="null"/> if not found.
		/// </summary>
		/// <param name="stopBeforeHeadings"> 
		/// If <see langword="true"/>, this will only search within the current section and
		///  will stop before headings (if peeking the first char in the next line is '[')
		///  </param>
		protected static string ReadNext(ref StreamReader reader, string keyword, bool stopBeforeHeadings = true) {
			string line;
			while ((line = reader.ReadLine()) is not null) {
				if (line.Contains(keyword))
					return line.Trim();
				if (stopBeforeHeadings && !reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			return null;
		}

		protected static float GetFloatFromLine(string line, string startsWith, string delimiter = ":") {
			try {
				line = line.Trim();
				if (!line.StartsWith(startsWith))
					return float.NaN;
				int index = line.IndexOf(delimiter);
				if (index >= 0)
					return (float)double.Parse(line[(index + 1)..], CultureInfo.InvariantCulture);
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error pulling float from line");
				Console.WriteLine(e.GetBaseException());
			}
			return float.NaN;
		}

		protected static float GetFloatFromNextLine(ref StreamReader reader, string startsWith, string delimiter = ":") {
			try {
				string line = reader.ReadLine();
				if (!string.IsNullOrWhiteSpace(line)) 
					return GetFloatFromLine(line, startsWith, delimiter);
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error using reader to get float from next line");
				Console.WriteLine(e.GetBaseException());
			}
			return float.NaN;
		}

		protected static string GetStringFromLine(string line, string startsWith, string delimiter = ":") {
			try {
				line = line.Trim();
				if (!line.StartsWith(startsWith))
					return null;
				int index = line.IndexOf(delimiter);
				if (index >= 0)  
					return line[(index + 1)..];
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error pulling string from line");
				Console.WriteLine(e.GetBaseException());
			}
			return null;
		}

		protected static string GetStringFromNextLine(ref StreamReader reader, string startsWith, string delimiter = ":") {
			try {
				string line = reader.ReadLine();
				if (!string.IsNullOrWhiteSpace(line))
					return GetStringFromLine(line, startsWith, delimiter);
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error using reader to get string from next line");
				Console.WriteLine(e.GetBaseException());
			}
			return null;
		}
	}
}
