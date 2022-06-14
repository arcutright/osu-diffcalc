namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using FileFinder;

	class MapsetManager {
		private static readonly List<Mapset> _allMapsets = new();
		private static readonly Regex _titleRegex = new(@"(.*)\s*\[\s*(.*)\s*\]");

		public static void Clear() {
			foreach (var mapset in _allMapsets) {
				mapset?.Dispose();
			}
			_allMapsets.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		/// <summary>
		/// [Dirty hack] Get mapset directory based on osu's window title
		/// (only works while user is playing a map and doesn't work 100% of the time)
		/// </summary>
		/// <returns>
		/// Path to current mapset directory, or null if it couldn't be determined
		/// </returns>
		public static string GetCurrentMapsetDirectory(Process osuProcess, string inGameWindowTitle, string prevMapsetDirectory) {
			try {
				// in game window title is: `osu! - artist - song title [difficulty]`
				string titleInfo = inGameWindowTitle[(inGameWindowTitle.IndexOf('-')+1)..].Trim();
				var match = _titleRegex.Match(titleInfo);
				if (!match.Success)
					return null;

				// TODO: for improved dirty hack, read the osu songs database to find this song

				// find path to osu! Songs folder (where all the beatmaps live)
				string songsDir;
				if (!string.IsNullOrEmpty(prevMapsetDirectory))
					songsDir = Path.GetDirectoryName(prevMapsetDirectory);
				else
					songsDir = Finder.GetOsuSongsDirectory(osuProcess);

				// look for a directory in the Songs folder with the mapset title from the in-game window
				string mapsetTitle = match.Groups[1].Value.Trim();
				string diffName = match.Groups[2].Value.Trim();
				var possibleMapsetDirectories = Directory.EnumerateDirectories(songsDir, $"*{mapsetTitle}*", SearchOption.TopDirectoryOnly);
				foreach (string directory in possibleMapsetDirectories) {
					// name match for the .osu map file
					var beatmapFiles = Directory.EnumerateFiles(directory, $"*{diffName}*.osu", SearchOption.TopDirectoryOnly);
					if (beatmapFiles.Any())
						return directory;
				}
			}
			catch {
			}
			return null;
		}

		//entry point from GUI.cs
		public static Mapset AnalyzeMapset(string directory, UserInterface.GUI gui, bool clearLists, bool enableXml) {
			//timing
			var sw = Stopwatch.StartNew();
			try {
				if (Directory.Exists(directory)) {
					//initalize allMapsets array from xml if needed
					if (enableXml) {
						if (!SavefileXMLManager.IsInitialized)
							SavefileXMLManager.Parse(_allMapsets);
						Console.WriteLine("xml analyzed");
					}

					//parse the mapset by iterating on the directory's .osu files
					var mapPaths = Directory.GetFiles(directory, "*.osu", SearchOption.TopDirectoryOnly);
					Console.WriteLine("got osu files");

					Mapset set = BuildSet(mapPaths);
					Console.WriteLine("set built");

					if (set.Beatmaps.Any()) {
						set = AnalyzeMapset(set, clearLists, enableXml);
						Console.WriteLine("mapset analyzed");
					}

					//timing
					sw.Stop();
					if (gui is not null)
						gui.SetAnalyzeTime($"{sw.ElapsedMilliseconds} ms");

					return set;
				}
			}
			catch (Exception e) {
				sw.Stop();
				Console.WriteLine("!!-- Error: could not analyze set");
				Console.WriteLine(e.GetBaseException());
			}
			return null;
		}

		//main analysis method - every path leads to this
		public static bool AnalyzeMap(Beatmap map, bool clearLists = true) {
			var totwatch = Stopwatch.StartNew();
			var localwatch = Stopwatch.StartNew();
			//parse map if needed
			if (!map.IsParsed && !Parser.TryParse(ref map, out _))
				return false;
			localwatch.Stop();
			Console.WriteLine($"parse [{map.Version}]: {localwatch.ElapsedMilliseconds}ms");

			//analyze map: streams, jumps, etc
			localwatch.Restart();
			if (!map.IsAnalyzed)
				Analyzer.Analyze(map, clearLists);
			localwatch.Stop();
			Console.WriteLine($"analyze [{map.Version}]: {localwatch.ElapsedMilliseconds}ms");

			//timing
			totwatch.Stop();
			Console.WriteLine($"tot [{map.Version}]: {totwatch.ElapsedMilliseconds}ms");
			return true;
		}

		/// <summary>
		/// Save map into cache and xml. This is meant to save maps that are manually chosen
		/// </summary>
		public static void SaveMap(Beatmap map, bool saveToXml) {
			if (string.IsNullOrEmpty(map?.Title)) return;
				var set = new Mapset(map);
			if (map.IsAnalyzed)
				set.IsAnalyzed = true;
			//check if the mapset has been saved
			int index = CheckForMapset(set);
			if (index != -1) {
				//check if the map has been saved
				var storedSet = _allMapsets[index];
				var storedMaps = storedSet.Beatmaps.ToList(); // avoid collection-was-modified
				foreach (var storedMap in storedMaps) {
					if (storedMap.Version == map.Version) {
						storedSet.Beatmaps.Remove(storedMap);
						storedMap.Dispose();
						break;
					}
				}
				//save map 
				storedSet.Add(map);
				if (saveToXml) storedSet.SaveToXML();
			}
			else {
				_allMapsets.Add(set);
				if (saveToXml) set.SaveToXML();
			}
		}

		#region Private helpers

		public static Mapset BuildSet(IList<string> mapPaths) {
			var allMaps = new List<Beatmap>();
			foreach (string mapPath in mapPaths) {
				if (string.IsNullOrEmpty(mapPath)) continue;
				if (Parser.ParseBasicMetadata(mapPath) is Beatmap map)
					allMaps.Add(map);
			}
			return new Mapset(allMaps);
		}

		private static Mapset AnalyzeMapset(Mapset set, bool clearLists, bool saveToXml) {
			int index = CheckForMapset(set);
			//Console.Write("analyzing set...");
			//check if the mapset has been analyzed
			if (index != -1) {
				//Console.Write("mapset has been analyzed...");
				//check for missing versions (difficulties)
				var missingMaps = GetMissingAnalyzedDiffs(set, index);
				if (missingMaps.Any()) {
					_allMapsets[index].IsAnalyzed = false;
					//Console.Write("some maps are missing...");
					Parallel.ForEach(missingMaps, map => AnalyzeMap(map));
					foreach (Beatmap map in missingMaps) {
						if (map.IsAnalyzed)
							_allMapsets[index].Add(map);
					}
					//Console.WriteLine("missing maps analyzed");
				}
				else {
					Console.WriteLine("found cached result, no maps are missing");
					saveToXml = false;
				}
				set = _allMapsets[index];
			}
			else {
				//Console.WriteLine("mapset not analyzed...");
				Parallel.ForEach(set.Beatmaps, map => AnalyzeMap(map));
				_allMapsets.Add(set);
				//Console.WriteLine("analyzed");
			}
			set.IsAnalyzed = true;
			if (saveToXml) {
				//Console.Write("saving set...");
				if (set.SaveToXML()) { /*Console.WriteLine("set saved");*/ }
				else { /*Console.WriteLine("!! could not save");*/ }
			}
			return set;
		}

		private static int CheckForMapset(Mapset set) {
			if (!_allMapsets.Any())
				return -1;
			// TODO: may be slow once cache is large, could replace with custom hash function + hashset
			for (int i = 0; i < _allMapsets.Count; ++i) {
				var stored = _allMapsets[i];
				if (stored.Title == set.Title && set.Artist == stored.Artist && set.Creator == stored.Creator)
					return i;
			}
			return -1;
		}

		private static IList<Beatmap> GetMissingAnalyzedDiffs(Mapset set, int indexForAllMapsetsSearch) {
			if (indexForAllMapsetsSearch >= _allMapsets.Count)
				return Array.Empty<Beatmap>();

			var missing = new List<Beatmap>();
			Mapset searching = _allMapsets[indexForAllMapsetsSearch];
			if (set.Title == searching.Title && set.Artist == searching.Artist && set.Creator == searching.Creator) {
				// TODO: may be slow for large sets (lots of diffs)
				foreach (Beatmap toFind in set.Beatmaps) {
					bool found = false;
					foreach (Beatmap storedMap in searching.Beatmaps) {
						if (storedMap.Version == toFind.Version) {
							found = true;
							break;
						}
					}
					if (!found)
						missing.Add(toFind);
				}
			}
			return missing;
		}

		#endregion
	}
}
