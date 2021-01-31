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
			if (reader.EndOfStream)
				return false;
			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Return until the next line that contains the given <paramref name="startsWith"/>, or <see langword="null"/> if not found.
		/// </summary>
		/// <param name="stopBeforeHeadings"> 
		/// If <see langword="true"/>, this will only search within the current section and
		/// will stop before headings (if peeking the first char in the next line is '[')
		/// </param>
		protected static string ReadNext(ref StreamReader reader, string startsWith, bool stopBeforeHeadings = true) {
			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				if (line.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
					return line;
				if (stopBeforeHeadings && !reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			return null;
		}

		protected static double GetDoubleFromLine(string line, string startsWith, char delimiter = ':') {
			try {
				if (!line.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
					return double.NaN;
				int index = line.IndexOf(delimiter);
				if (index != -1)
					return double.Parse(line[(index + 1)..], CultureInfo.InvariantCulture);
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error pulling float from line");
				Console.WriteLine(e.GetBaseException());
			}
			return double.NaN;
		}

		protected static double GetDoubleFromNextLine(ref StreamReader reader, string startsWith, char delimiter = ':') {
			try {
				string line = reader.ReadLine()?.Trim();
				if (!string.IsNullOrEmpty(line)) 
					return GetDoubleFromLine(line, startsWith, delimiter);
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error using reader to get float from next line");
				Console.WriteLine(e.GetBaseException());
			}
			return double.NaN;
		}

		protected static string GetStringFromLine(string line, string startsWith, char delimiter = ':') {
			try {
				if (!line.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
					return null;
				int index = line.IndexOf(delimiter);
				if (index != -1)  
					return line[(index + 1)..].Trim();
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error pulling string from line");
				Console.WriteLine(e.GetBaseException());
			}
			return null;
		}

		protected static string GetStringFromNextLine(ref StreamReader reader, string startsWith, char delimiter = ':') {
			try {
				string line = reader.ReadLine()?.Trim();
				if (!string.IsNullOrEmpty(line))
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
