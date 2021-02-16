namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using FileParserHelpers;
	using OsuDiffCalc.FileProcessor.BeatmapObjects;

	class Parser {
		delegate bool TryProcessLineCallback(int lineNumber, string line, Beatmap beatmap, out string failureMsg);

		/// <summary>
		/// Try to parse an entire beatmap at a given <paramref name="filepath"/>
		/// </summary>
		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap, out string)"/>
		public static bool TryParse(string filepath, out Beatmap beatmap, out string failureMessage) {
			var path = Path.GetFullPath(filepath);
			using var reader = File.OpenText(path);
			beatmap = new Beatmap(path);
			return TryParse(reader, ref beatmap, out failureMessage);
		}

		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap, out string)"/>
		public static bool TryParse(ref Beatmap beatmap, out string failureMessage) {
			using var reader = File.OpenText(beatmap.Filepath);
			return TryParse(reader, ref beatmap, out failureMessage);
		}

		/// <summary>
		/// Try to parse an entire beatmap
		/// </summary>
		/// <returns> <see langword="true"/> if there were no errors, otherwise <see langword="false"/> </returns>
		public static bool TryParse(StreamReader reader, ref Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			if (beatmap.IsParsed)
				return true;

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"parse[{beatmap.Version}]";

			try { 
				if (!FormatParser.TryParse(beatmap, ref reader, out failureMessage))
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
				if (!TryParse2(reader, sectionParsers, ref beatmap, out failureMessage))
					return false;
				// in ye olde times, ar = od. Not sure if ar can still be omitted
				if (beatmap.ApproachRate < 0)
					beatmap.ApproachRate = beatmap.OverallDifficulty;

				// TODO: move validation logic after releasing file handles
				// data validation
				if (beatmap.TimingPoints.Count == 0) {
					if (string.IsNullOrEmpty(failureMessage))
						failureMessage = "No timing points in file";
					return false;
				}

				beatmap.IsParsed = true;

				// TODO: sort objects and timing points after releasing any file handles (disposing the stream)
				static int compareObjects(BeatmapObject a, BeatmapObject b) {
					return a.StartTime != b.StartTime
						? a.StartTime.CompareTo(b.StartTime) 
						: a.EndTime.CompareTo(b.EndTime);
				}

				beatmap.TimingPoints.Sort((a, b) => a.Offset - b.Offset);
				beatmap.BreakSections.Sort(compareObjects);
				beatmap.BeatmapObjects.Sort(compareObjects);

				// TODO: analyze slider shape after releasing any file handles (disposing the stream)
				int timingPointIndex = 0;
				TimingPoint timingPoint = beatmap.TimingPoints[0];
				foreach (BeatmapObject obj in beatmap.BeatmapObjects) {
					if (obj is not Slider slider) continue;
					// find current timing point
					if (timingPointIndex < beatmap.NumTimingPoints) {
						while (timingPointIndex < beatmap.NumTimingPoints - 1 && slider.StartTime > beatmap.TimingPoints[timingPointIndex].Offset) {
							timingPointIndex++;
						}
						timingPoint = beatmap.TimingPoints[timingPointIndex];
					}
					slider.AnalyzeShape(timingPoint, beatmap.SliderMultiplier);
				}
				return true;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map at path '{beatmap.Filepath}'");
				Console.WriteLine(e.GetBaseException());
				return false;
			}
		}

		/// <summary>
		/// Parse just the basic map metadata (format, file name/paths, mode, artist, title, creator, version)
		/// </summary>
		public static Beatmap ParseBasicMetadata(string mapPath) { 
			StreamReader reader = null;
			string failureMessage = null;
			try {
				var path = Path.GetFullPath(mapPath);
				var beatmap = new Beatmap(path);
				reader = File.OpenText(path);

				var sectionParsers = new Dictionary<string, TryProcessLineCallback> {
					{ "[General]", GeneralParser.TryProcessLine },
					{ "[Metadata]", MetadataParser.TryProcessLine },
				};
				if (!TryParse2(reader, sectionParsers, ref beatmap, out failureMessage))
					return null;
				// in ye olde times, ar = od. Not sure if ar can still be omitted
				if (beatmap.ApproachRate < 0)
					beatmap.ApproachRate = beatmap.OverallDifficulty;

				beatmap.IsMetadataParsed = true;
				return beatmap;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map metadata at path '{mapPath}'");
				Console.WriteLine(e.GetBaseException());
				return null;
			}
			finally {
				reader?.Dispose();
			}
		}

		#region Overrides to discard the failure message

		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap, out string)"/>
		public static bool TryParse(string filepath, out Beatmap beatmap) => TryParse(filepath, out beatmap, out _);

		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap, out string)"/>
		public static bool TryParse(ref Beatmap beatmap) => TryParse(ref beatmap, out _);

		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap, out string)"/>
		public static bool TryParse(StreamReader reader, ref Beatmap beatmap) => TryParse(reader, ref beatmap, out _);

		#endregion

		private static bool TryParse2(StreamReader reader, Dictionary<string, TryProcessLineCallback> sectionParsers, ref Beatmap beatmap, out string failureMessage) {
			if (reader.EndOfStream && reader.BaseStream.CanSeek) 
				reader.BaseStream.Seek(0, SeekOrigin.Begin);
			if (reader.EndOfStream) {
				failureMessage = "Cannot read map: End of file stream";
				return false;
			}
			bool exitOnSectionParseErrors = false;

			failureMessage = "";
			TryProcessLineCallback tryProcessLine = null;
			HashSet<string> remainingSectionHeaders = sectionParsers.Keys.ToHashSet();
			string line, sectionHeader = "";
			int lineNumber = 0;
			bool anyLinesProcessed = false;
			bool foundSectionHeader = false;
			try {
				while ((line = reader.ReadLine()?.Trim()) is not null) {
					lineNumber++;
					// continue past empty lines
					if (line.Length == 0 || line.StartsWith("//"))
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
							if (exitOnSectionParseErrors)
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
				throw;
			}
		}
	}
}
