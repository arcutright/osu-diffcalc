namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.IO;
	using System.Threading;
	using FileParserHelpers;

	class Parser {
		/// <summary>
		/// Try to parse an entire beatmap
		/// </summary>
		/// <returns> <see langword="true"/> if there were no errors, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap) {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"parse[{beatmap.Version}]";

			StreamReader reader = null;
			try {
				//note: order matters, because it is parsing sequentially (to avoid polynomial time search)
				reader = File.OpenText(beatmap.Filepath);

				if (!FormatParser.Parse(beatmap, ref reader))
					return false;
				if (!GeneralParser.TryParse(beatmap, ref reader))
					return false;
				if (!MetadataParser.TryParse(beatmap, ref reader)) {
					Console.WriteLine("\n\n !!!\nError parsing metadata\n!!! \n\n");
					return false;
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
				reader.Close();
				return true;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map at path '{beatmap.Filepath}'");
				Console.WriteLine(e.GetBaseException());
			}
			finally {
				reader?.Dispose();
			}
			return false;
		}

		/// <summary>
		/// Parse just the basic map metadata (format, file name/paths, mode, artist, title, creator, version)
		/// </summary>
		public static Beatmap ParseBasicMetadata(string mapPath) { 
			StreamReader reader = null;
			try {
				reader = File.OpenText(mapPath);
				var beatmap = new Beatmap(mapPath);

				if (!FormatParser.Parse(beatmap, ref reader))
					return null;
				if (!GeneralParser.TryParse(beatmap, ref reader))
					return null;
				if (!MetadataParser.TryParse(beatmap, ref reader)) 
					return null;

				return beatmap;
			}
			catch (Exception e) {
				Console.WriteLine($"!! -- Error parsing map at path '{mapPath}'");
				Console.WriteLine(e.GetBaseException());
				return null;
			}
			finally {
				reader?.Dispose();
			}
		}
	}
}
