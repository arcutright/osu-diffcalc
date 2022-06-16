namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.Globalization;
	using System.IO;

	abstract class ParserBase {
		/// <summary>
		/// Try to skip to the next line that starts with <paramref name="prefix"/>. Will consume the line that contains it.
		/// </summary>
		protected static bool TrySkipTo(ref StreamReader reader, string prefix, out string failureMessage) {
			if (reader.EndOfStream) {
				failureMessage = $"Could not skip to '{prefix}': End of stream";
				return false;
			}
			failureMessage = "";
			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			failureMessage = $"Could not skip to '{prefix}': Not found in remaining stream";
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

		protected static float? GetFloatFromLine(string line, string startsWith, char delimiter = ':') {
			if (!line.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
				return null;
			int index = line.IndexOf(delimiter);
			if (index != -1 && float.TryParse(line[(index + 1)..], out var value))
				return value;
			else
				return null;
		}

		protected static double? GetDoubleFromNextLine(ref StreamReader reader, string startsWith, char delimiter = ':') {
			try {
				string line = reader.ReadLine()?.Trim();
				if (!string.IsNullOrEmpty(line))
					return GetFloatFromLine(line, startsWith, delimiter);
				else
					return null;
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error using reader to get float from next line");
				Console.WriteLine(e.GetBaseException());
				return null;
			}
		}

		protected static string GetStringFromLine(string line, string startsWith, char delimiter = ':') {
			if (!line.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
				return null;
			int index = line.IndexOf(delimiter);
			if (index != -1)  
				return line[(index + 1)..].Trim();
			else
				return null;
		}

		protected static string GetStringFromNextLine(ref StreamReader reader, string startsWith, char delimiter = ':') {
			try {
				string line = reader.ReadLine()?.Trim();
				if (!string.IsNullOrEmpty(line))
					return GetStringFromLine(line, startsWith, delimiter);
				else
					return null;
			}
			catch (Exception e) {
				Console.WriteLine("!! -- Error using reader to get string from next line");
				Console.WriteLine(e.GetBaseException());
				return null;
			}
		}

		protected static bool TryAssignStringFromLine(string line, string startsWith, out string result, char delimeter = ':') {
			var rawResult = GetStringFromLine(line, startsWith, delimeter);
			if (rawResult is not null) {
				result = rawResult;
				return true;
			}
			result = null;
			return false;
		}

		protected static bool TryAssignFloatFromLine(string line, string startsWith, out float result, char delimeter = ':') {
			var rawResult = GetFloatFromLine(line, startsWith, delimeter);
			if (rawResult.HasValue) {
				result = rawResult.Value;
				return true;
			}
			result = float.NaN;
			return false;
		}

		protected static bool TryAssignIntFromLine(string line, string startsWith, out int result, char delimeter = ':') {
			if (TryAssignFloatFromLine(line, startsWith, out var value, delimeter)) {
				result = (int)Math.Round(value, MidpointRounding.AwayFromZero);
				return true;
			}
			result = int.MinValue;
			return false;
		}

		protected static bool TryProcessSection(ref StreamReader reader, string sectionHeader, ref string failureMessage, Func<string, bool> tryProcessLine) {
			string line;
			if (reader.EndOfStream) {
				failureMessage = $"Could not reach section '{sectionHeader}': End of stream";
				return false;
			}

			bool anyLinesProcessed = false;
			bool foundSectionHeader = false;
			var prevPos = reader.BaseStream.Position;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				// continue past empty lines
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				// handle section headers
				else if (line[0] == '[') {
					if (line.StartsWith(sectionHeader, StringComparison.OrdinalIgnoreCase)) {
						// skip over expected section header, now we will process lines
						foundSectionHeader = true;
						continue;
					}
					else {
						// move back, exit before next section header
						reader.BaseStream.Seek(prevPos, SeekOrigin.Begin);
						break;
					}
				}
				if (foundSectionHeader) {
					if (!tryProcessLine(line)) {
						if (string.IsNullOrEmpty(failureMessage))
							failureMessage = $"Failed to process line '{line}' in section '{sectionHeader}'";
						return false;
					}
					anyLinesProcessed = true;
				}
				// exit before end of stream or next section header if we know it is coming
				if (!reader.EndOfStream && reader.Peek() == '[')
					break;
				prevPos = reader.BaseStream.Position;
			}
			if (!foundSectionHeader) {
				failureMessage = $"Did not find section '{sectionHeader}'";
				return false;
			}
			else if (!anyLinesProcessed) {
				if (string.IsNullOrEmpty(failureMessage))
					failureMessage = $"Did not find any lines in section '{sectionHeader}'";
				return false;
			}
			return true;
		}
	}
}
