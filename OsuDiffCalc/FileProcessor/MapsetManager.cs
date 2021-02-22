namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using FileFinder;

	class MapsetManager {
		private static List<Mapset> _allMapsets = new List<Mapset>();

		public static void Clear() {
			_allMapsets.Clear();
		}

		//get mapset directory based on osu's window title (only works while user is playing a map)
		public static string GetCurrentMapsetDirectory(string inGameWindowTitle, string prevMapsetDirectory) {
			try {
				//title info is organized: artist - song title [difficulty]
				string titleInfo = inGameWindowTitle[(inGameWindowTitle.IndexOf('-')+1)..].Trim();
				string mapsetDirectoryTitle = titleInfo[..titleInfo.LastIndexOf('[')].Trim();
				string diffName = titleInfo.Substring(titleInfo.LastIndexOf('[')+1, titleInfo.LastIndexOf(']'));
				string songsDirectoryX = Path.GetDirectoryName(prevMapsetDirectory);
				string songsDirectory = prevMapsetDirectory[..prevMapsetDirectory.LastIndexOf('\\')];
				var possibleMapsetDirectories = Directory.EnumerateDirectories(songsDirectory, $"*{mapsetDirectoryTitle}*", SearchOption.TopDirectoryOnly);
				foreach (string directory in possibleMapsetDirectories) {
					if (Directory.EnumerateFiles(directory, $"*{diffName}*.osu", SearchOption.TopDirectoryOnly).Any()) {
						return directory;
					}
				}
			}
			catch { }
			return null;
		}

		//entry point from GUI.cs
		public static Mapset AnalyzeMapset(string directory, UserInterface.GUI gui, bool clearLists = true) {
			//timing
			var sw = Stopwatch.StartNew();
			try {
				if (Directory.Exists(directory)) {
					//initalize allMapsets array from xml if needed
					if (!SavefileXMLManager.IsInitialized)
						SavefileXMLManager.Parse(ref _allMapsets);
					Console.WriteLine("xml analyzed");

					//parse the mapset by iterating on the directory's .osu files
					var mapPaths = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
						.Where(path => path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));
					Console.WriteLine("got osu files");

					Mapset set = BuildSet(mapPaths);
					Console.WriteLine("set built");

					if (set.Beatmaps.Count > 0) {
						set = AnalyzeMapset(set, clearLists);
						Console.WriteLine("mapset analyzed");
					}

					//timing
					sw.Stop();
					if (gui is not null)
						gui.SetTime2($"{sw.ElapsedMilliseconds} ms");

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

		//this is meant to save maps that are manually chosen
		public static void SaveMap(Beatmap map) {
			if (string.IsNullOrEmpty(map?.Title)) return;
				var set = new Mapset(map);
			if (map.IsAnalyzed)
				set.IsAnalyzed = true;
			//check if the mapset has been saved
			int index = CheckForMapset(set);
			if (index != -1) {
				//check if the map has been saved
				var storedSet = _allMapsets[index];
				bool found = false;
				foreach (var storedMap in storedSet.Beatmaps) {
					if (storedMap.Version == map.Version) {
						found = true;
						break;
					}
				}
				//save map 
				if (!found) {
					storedSet.Add(map);
					storedSet.SaveToXML();
				}
			}
			else {
				_allMapsets.Add(set);
				set.SaveToXML();
			}
		}

		#region Private helpers

		public static Mapset BuildSet(IEnumerable<string> mapPaths) {
			var allMaps = new List<Beatmap>();
			foreach (string mapPath in mapPaths) {
				if (string.IsNullOrEmpty(mapPath)) continue;
				Beatmap map = Parser.ParseBasicMetadata(mapPath);
				if (map is not null)
					allMaps.Add(map);
			}
			return new Mapset(allMaps);
		}

		private static Mapset AnalyzeMapset(Mapset set, bool clearLists = true) {
			bool save = true;
			int index = CheckForMapset(set);
			//Console.Write("analyzing set...");
			//check if the mapset has been analyzed
			if (index != -1) {
				//Console.Write("mapset has been analyzed...");
				//check for missing versions (difficulties)
				List<Beatmap> missingMaps = GetMissingAnalyzedDiffs(set, index);
				if (missingMaps.Any()) {
					_allMapsets[index].IsAnalyzed = false;
					//Console.Write("some maps are missing...");

					foreach (Beatmap map in missingMaps) {
						if (AnalyzeMap(map, clearLists))
							_allMapsets[index].Add(map);
					}
					//Console.WriteLine("missing maps analyzed");
				}
				else {
					//Console.WriteLine("no maps are missing");
					save = false;
				}
				set = _allMapsets[index];
			}
			else {
				//Console.WriteLine("mapset not analyzed...");
				foreach (Beatmap map in set.Beatmaps) {
					AnalyzeMap(map);
				}
				_allMapsets.Add(set);
				//Console.WriteLine("analyzed");
			}
			set.IsAnalyzed = true;
			if (save) {
				//Console.Write("saving set...");
				if (set.SaveToXML()) { /*Console.WriteLine("set saved");*/ }
				else { /*Console.WriteLine("!! could not save");*/ }
			}
			return set;
		}


		private static int CheckForMapset(Mapset set, bool completeOnly = false) {
			if (!_allMapsets.Any())
				return -1;
			int i = 0;
			foreach (var stored in _allMapsets) {
				if (stored.Title == set.Title && set.Artist == stored.Artist && set.Creator == stored.Creator) {
					if (completeOnly) {
						if (set.Beatmaps.Count == stored.Beatmaps.Count)
							return i;
					}
					else
						return i;
				}
				i++;
			}
			return -1;
		}

		private static List<Beatmap> GetMissingAnalyzedDiffs(Mapset set, int indexForAllMapsetsSearch) {
			var missing = new List<Beatmap>();
			if (indexForAllMapsetsSearch >= _allMapsets.Count)
				return missing;

			Mapset searching = _allMapsets[indexForAllMapsetsSearch];
			if (set.Title == searching.Title && set.Artist == searching.Artist && set.Creator == searching.Creator) {
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
