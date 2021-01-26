namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.IO;
	using System.Threading;
	using FileParserHelpers;

	class Parser {
		/// <summary>
		/// Try to parse an entire beatmap at a given <paramref name="filepath"/>
		/// </summary>
		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap)"/>
		public static bool TryParse(string filepath, out Beatmap beatmap) {
			var path = Path.GetFullPath(filepath);
			using var reader = File.OpenText(path);
			beatmap = new Beatmap(path);
			return TryParse(reader, ref beatmap);
		}

		/// <inheritdoc cref="TryParse(StreamReader, ref Beatmap)"/>
		public static bool TryParse(ref Beatmap beatmap) {
			using var reader = File.OpenText(beatmap.Filepath);
			return TryParse(reader, ref beatmap);
		}

		/// <summary>
		/// Try to parse an entire beatmap
		/// </summary>
		/// <returns> <see langword="true"/> if there were no errors, otherwise <see langword="false"/> </returns>
		public static bool TryParse(StreamReader reader, ref Beatmap beatmap) {
			if (beatmap.IsParsed)
				return true;

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"parse[{beatmap.Version}]";

			try {
				// note: order matters because it is parsing the file sequentially (to avoid polynomial time search)
				if (!beatmap.IsMetadataParsed) {
					if (!FormatParser.Parse(beatmap, ref reader))
						return false;
					if (!GeneralParser.TryParse(beatmap, ref reader))
						return false;
					if (!MetadataParser.TryParse(beatmap, ref reader)) {
						Console.WriteLine("\n\n !!!\nError parsing metadata\n!!! \n\n");
						return false;
					}
				}
				if (!DifficultyParser.TryParse(beatmap, ref reader))
					return false;
				if (!EventsParser.TryParse(beatmap, ref reader))
					return false;
				if (!TimingParser.TryParse(beatmap, ref reader))
					return false;
				if (!HitObjectsParser.TryParse(beatmap, ref reader))
					return false;

				beatmap.IsParsed = true;
				return true;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map at path '{beatmap.Filepath}'");
				Console.WriteLine(e.GetBaseException());
			}
			return false;
		}


		/// <summary>
		/// Parse just the basic map metadata (format, file name/paths, mode, artist, title, creator, version)
		/// </summary>
		public static Beatmap ParseBasicMetadata(string mapPath) { 
			StreamReader reader = null;
			try {
				var path = Path.GetFullPath(mapPath);
				var beatmap = new Beatmap(path);
				reader = File.OpenText(path);

				if (!FormatParser.Parse(beatmap, ref reader))
					return null;
				if (!GeneralParser.TryParse(beatmap, ref reader))
					return null;
				if (!MetadataParser.TryParse(beatmap, ref reader)) 
					return null;

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
	}
}
