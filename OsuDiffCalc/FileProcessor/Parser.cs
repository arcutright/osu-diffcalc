namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using FileParserHelpers;

	class Parser {
		/// <summary>
		/// Try to parse an entire beatmap at a given <paramref name="filepath"/>
		/// </summary>
		/// <returns> <see langword="true"/> if there were no errors, otherwise <see langword="false"/> </returns>
		public static bool TryParse(string filepath, out Beatmap beatmap, out string failureMessage) {
			beatmap = new Beatmap(Path.GetFullPath(filepath));
			return TryParse(ref beatmap, out failureMessage);
		}

		/// <inheritdoc cref="TryParse(string, out Beatmap, out string)"/>
		public static bool TryParse(ref Beatmap beatmap, out string failureMessage) {
			using var reader = new StreamReader(new FileStream(beatmap.Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024), System.Text.Encoding.UTF8);
			return TryParse(reader, ref beatmap, out failureMessage);
		}

		/// <summary>
		/// Callback for trying to process a single non-empty line within a section in a beatmap
		/// </summary>
		/// <param name="lineNumber"> line number in .osu file (1 = first line) </param>
		/// <param name="line"> non-empty trimmed line from .osu file </param>
		/// <param name="beatmap"> the beatmap which is being processed </param>
		/// <param name="failureMsg"> optional failure message, if any problems are encountered </param>
		/// <returns></returns>
		delegate bool TryProcessLineCallback(int lineNumber, string line, Beatmap beatmap, out string failureMsg);

		/// <summary>
		/// Parse just the basic map metadata for an osu!standard map (format, file name/paths, mode, artist, title, creator, version)
		/// </summary>
		/// <returns> <see cref="Beatmap"/> if parse was successful, otherwise <see langword="null"/> </returns>
		public static Beatmap ParseBasicMetadata(string mapPath) {
			if (string.IsNullOrEmpty(mapPath))
				return null;

			string failureMessage = null;
			try {
				var beatmap = new Beatmap(Path.GetFullPath(mapPath));
				// TODO: use this to check if we need to re-parse a map
				beatmap.LastModifiedTime = File.GetLastWriteTimeUtc(beatmap.Filepath);

				using var reader = new StreamReader(new FileStream(beatmap.Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 2 * 1024), System.Text.Encoding.UTF8);

				var sectionParsers = new Dictionary<string, TryProcessLineCallback> {
					{ "[General]", GeneralParser.TryProcessLine },
					{ "[Metadata]", MetadataParser.TryProcessLine },
				};
				if (!FormatParser.TryParse(beatmap, reader, out failureMessage))
					return null;
				if (!TryParseSections(reader, sectionParsers, ref beatmap, out failureMessage))
					return null;
				// in ye olde times, ar = od. Not sure if ar can still be omitted
				if (beatmap.ApproachRate < 0)
					beatmap.ApproachRate = beatmap.OverallDifficulty;

				beatmap.IsMetadataParsed = true;
				return beatmap;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map metadata at path '{mapPath}'. Failure message: '{failureMessage}'");
				Console.WriteLine(e.GetBaseException());
				return null;
			}
		}

		/// <summary>
		/// Try to parse an osu!standard beatmap (.osu file) from <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <param name="reader"> stream reader which points to an .osu file </param>
		/// <param name="beatmap"> beatmap to update based on parsing results </param>
		/// <param name="failureMessage"> failure message if the beatmap cannot be parsed </param>
		/// <returns> <see langword="true"/> if all sections in <paramref name="sectionParsers"/> were found and processed, otherwise <see langword="false"/> </returns>
		private static bool TryParse(StreamReader reader, ref Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (beatmap.IsParsed)
				return true;

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"parse[{beatmap.Version}]";

			try {
				if (!beatmap.IsMetadataParsed && !FormatParser.TryParse(beatmap, reader, out failureMessage))
					return false;
				var sectionParsers = new Dictionary<string, TryProcessLineCallback> {
					{ "[Events]", EventsParser.TryProcessLine },
					{ "[TimingPoints]", TimingParser.TryProcessLine },
					{ "[Difficulty]", DifficultyParser.TryProcessLine },
					{ "[HitObjects]", HitObjectsParser.TryProcessLine }
				};
				if (!beatmap.IsMetadataParsed) {
					sectionParsers.Add("[General]", GeneralParser.TryProcessLine);
					sectionParsers.Add("[Metadata]", MetadataParser.TryProcessLine);
				}
				if (!TryParseSections(reader, sectionParsers, ref beatmap, out failureMessage))
					return false;
				// in ye olde times, ar = od. Not sure if ar can still be omitted
				if (beatmap.ApproachRate < 0)
					beatmap.ApproachRate = beatmap.OverallDifficulty;

				// data validation
				if (beatmap.TimingPoints.Count == 0) {
					if (string.IsNullOrEmpty(failureMessage))
						failureMessage = "No timing points in file";
					return false;
				}
				beatmap.IsParsed = true;
				return true;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map at path '{beatmap.Filepath}'");
				Console.WriteLine(e.GetBaseException());
				return false;
			}
		}

		/// <summary>
		/// Try to parse an osu!standard beatmap (.osu file) from <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <param name="reader"> stream reader which points to an .osu file </param>
		/// <param name="sectionParsers"> dict of sectionHeader: callback where sectionHeader includes brackets, ex: '[General]' </param>
		/// <param name="beatmap"> beatmap to update based on parsing results </param>
		/// <param name="failureMessage"> failure message if the beatmap cannot be parsed </param>
		/// <returns> <see langword="true"/> if all sections in <paramref name="sectionParsers"/> were found and processed, otherwise <see langword="false"/> </returns>
		private static bool TryParseSections(StreamReader reader, Dictionary<string, TryProcessLineCallback> sectionParsers, ref Beatmap beatmap, out string failureMessage) {
			if (reader.EndOfStream) {
				failureMessage = "Cannot read map: End of file stream";
				return false;
			}
			bool exitOnSectionParseErrors = false;

			failureMessage = "";
			TryProcessLineCallback tryProcessLine = null;
			HashSet<string> remainingSectionHeaders = sectionParsers.Keys.ToHashSet();
			string line = "", sectionHeader = "";
			int lineNumber = 1; // we already parsed the format line, so we are at least at line 2
			bool anyLinesProcessed = false;
			bool foundSectionHeader = false;
			try {
				while ((line = reader.ReadLine()) is not null) {
					lineNumber++;
					// skip indented storyboard lines or variables (scripting)
					// https://osu.ppy.sh/wiki/en/Storyboard_Scripting
					// https://osu.ppy.sh/community/forums/topics/1869
					if (sectionHeader == "[Events]" && line.Length != 0 && line[0] is ' ' or '_' or '$')
						continue;
					line = line.Trim();
					// continue past empty lines
					if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
						continue;
					// handle section headers
					else if (line[0] == '[') {
						sectionHeader = line[..Math.Min(line.Length, line.IndexOf(']') + 1)];
						foundSectionHeader = sectionParsers.TryGetValue(sectionHeader, out tryProcessLine);
						if (foundSectionHeader)
							remainingSectionHeaders.Remove(sectionHeader);
					}
					// process lines in expected section
					else if (foundSectionHeader) {
						if (!tryProcessLine(lineNumber, line, beatmap, out failureMessage)) {
							if (string.IsNullOrEmpty(failureMessage))
								failureMessage = $"Failed to process line {lineNumber}: '{line}' in section '{sectionHeader}'";
							if (exitOnSectionParseErrors || !beatmap.IsOsuStandard)
								return false;
							else
								System.Console.WriteLine(failureMessage);
						}
						anyLinesProcessed = true;
					}
				}
				if (remainingSectionHeaders.Any()) {
					failureMessage = $"Did not find section(s) [{string.Join(", ", remainingSectionHeaders.Select(h => $"'{h}'"))}]";
					return false;
				}
				else if (!anyLinesProcessed) {
					if (string.IsNullOrEmpty(failureMessage))
						failureMessage = $"Empty file";
					return false;
				}
				return true;
			}
			catch (Exception ex) {
				if (string.IsNullOrEmpty(failureMessage))
					failureMessage = $"Error when processing line {lineNumber}: '{line}' in section '{sectionHeader}'\nException: '{ex.Message}'";
				throw;
			}
		}

	}
}
