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
	using Utility;
	using static CSharpPolyfills.MissingExtensionMethods;

	class MapsetManager {
		private static readonly LRUCache<string, Mapset> _allMapsets = new(20, autoDispose: true);
		private static readonly Regex _titleRegex = new(@"(.*)\s*\[\s*(.*)\s*\]");
		private static string[] _prevMapFilesInDir = Array.Empty<string>();
		private static string _prevMapsetDirectory = "";

		/// <inheritdoc cref="LRUCache{TKey, TValue}.Clear(bool?, IList{TValue})"/>
		public static void Clear(bool? autoDispose = null, IList<Mapset> exceptions = null) {
			_allMapsets.Clear(autoDispose, exceptions);
			_prevMapFilesInDir = Array.Empty<string>();
			_prevMapsetDirectory = "";
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public static IEnumerable<Mapset> CachedMapsets => _allMapsets.Values;

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

				// TODO: for more reliable hack, read the osu songs database to find this song

				// find path to osu! Songs folder (where all the beatmaps live)
				string songsDir;
				if (!string.IsNullOrEmpty(prevMapsetDirectory))
					songsDir = Path.GetDirectoryName(prevMapsetDirectory);
				else
					songsDir = Finder.GetOsuSongsDirectory(osuProcess);

				// look for a directory in the Songs folder with the mapset title from the in-game window
				string mapsetTitle = match.Groups[1].Value.Trim();
				string diffName = match.Groups[2].Value.Trim();

				if (tryFindMap(mapsetTitle, out var mapsetDir))
					return mapsetDir;
				else {
					// sometimes the window title is 'artist - song name (mapper) [difficulty]'
					// not sure if the parentheses are meaningful so we split on space instead
					int idx = -1;
					while ((idx = mapsetTitle.LastIndexOf(' ')) != -1) {
						mapsetTitle = mapsetTitle[..idx];
						if (tryFindMap(mapsetTitle, out mapsetDir))
							return mapsetDir;
					}
					return null;
				}

				bool tryFindMap(string mapsetTitle, out string foundDir) {
					if (string.IsNullOrEmpty(songsDir)) {
						foundDir = null;
						return false;
					}
					var possibleMapsetDirectories = Directory.EnumerateDirectories(songsDir, $"*{mapsetTitle}*", SearchOption.TopDirectoryOnly);
					foreach (string directory in possibleMapsetDirectories) {
						// name match for the .osu map file
						var beatmapFiles = Directory.EnumerateFiles(directory, $"*{diffName}*.osu", SearchOption.TopDirectoryOnly);
						if (beatmapFiles.Any()) {
							foundDir = directory;
							return true;
						}
					}
					foundDir = null;
					return false;
				}
			}
			catch (Exception ex) {
				Console.WriteLine("!!-- Error: could not find current mapset directory");
				Console.WriteLine(ex);
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
			}
			return null;
		}

		//entry point from GUI.cs
		public static Mapset AnalyzeMapset(string directory, UserInterface.GUI gui, bool clearLists, bool enableXml) {
			try {
				// TODO: may need logic to avoid infinite loops here...
				if (Directory.Exists(directory)) {
					// parse the mapset by iterating on the directory's .osu files
					var mapPaths = Directory.GetFiles(directory, "*.osu", SearchOption.TopDirectoryOnly);

					// avoid lots of work on unchanged empty directories
					if (mapPaths is null)
						return null;
					if (mapPaths.Length == 0 && _prevMapsetDirectory == directory && (_prevMapFilesInDir is null || _prevMapFilesInDir.Length == 0))
						return null;

					_prevMapFilesInDir = mapPaths;
					if (mapPaths.Length != 0 && _prevMapsetDirectory != directory)
						Console.WriteLine($"got {mapPaths.Length} .osu files");

					Mapset set = BuildSet(mapPaths);
					if (mapPaths.Length != 0 && (set.Count != 0 || _prevMapsetDirectory != directory))
						Console.WriteLine($"set built: {set.Count} supported maps");

					if (set.Any()) {
						void onUIThread(Action action) {
							if (gui.InvokeRequired)
								gui.Invoke(action);
							else
								action();
						}
						set = AnalyzeMapset(set, clearLists, enableXml, onUIThread);
						Console.WriteLine($"mapset analyzed: {set.Count} supported maps");
					}
					return set;
				}
				else {
					_prevMapFilesInDir = Array.Empty<string>();
				}
			}
			catch (Exception e) {
				_prevMapFilesInDir = Array.Empty<string>();
				Console.WriteLine("!!-- Error: could not analyze set");
				Console.WriteLine(e.GetBaseException());
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
			}
			finally {
				_prevMapsetDirectory = directory;
			}
			return null;
		}

		// main analysis method - every path leads to this
		public static bool AnalyzeMap(Beatmap map, bool clearLists = true) {
			var totwatch = Stopwatch.StartNew();
			var localwatch = new Stopwatch();
			bool didWork = false;

			// parse map if needed
			if (!map.IsParsed) {
				localwatch.Restart();
				if (!Parser.TryParse(ref map, out _))
					return false;
				localwatch.Stop();
				Console.WriteLine($"parse [{map.Version}]: {localwatch.ElapsedMilliseconds}ms");
				didWork = true;
			}

			// analyze map: streams, jumps, etc
			if (!map.IsAnalyzed) {
				localwatch.Restart();
				Analyzer.Analyze(map, clearLists);
				localwatch.Stop();
				Console.WriteLine($"analyze [{map.Version}]: {localwatch.ElapsedMilliseconds}ms");
				didWork = true;
			}

			// timing
			totwatch.Stop();
			if (didWork)
				Console.WriteLine($"tot [{map.Version}]: {totwatch.ElapsedMilliseconds}ms");
			return true;
		}

		/// <summary>
		/// Save map into xml. This is meant to save maps that are manually chosen
		/// </summary>
		public static void SaveMapToXML(Beatmap map) {
			if (map is null || string.IsNullOrEmpty(map.Title)) return;
			var set = new Mapset(map);
			if (map.IsAnalyzed)
				set.IsAnalyzed = true;

			// save new mapset to xml
			set.SaveToXML();
		}

		/// <summary>
		/// Save map into cache and xml. This is meant to save maps that are manually chosen
		/// </summary>
		public static Mapset SaveMapset(Mapset mapset, bool clearLists, bool saveToXml, Action<Action> onUIThread) {
			if (mapset is null) return null;
			return AnalyzeMapset(mapset, clearLists, saveToXml, onUIThread);
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

		private static Mapset AnalyzeMapset(Mapset set, bool clearLists, bool saveToXml, Action<Action> onUIThread) {
			if (set is null) return null;
			//Console.Write("analyzing set...");
			//check if the mapset has been analyzed
			if (!string.IsNullOrEmpty(set.FolderPath) && _allMapsets.TryGetValue(set.FolderPath, out var storedSet)) {
				//Console.Write("mapset has been analyzed...");
				//check for missing versions (difficulties)
				var (missingMaps, needsAnalyzeMaps) = GetMissingAnalyzedDiffs(set, storedSet);
				if (missingMaps.Any() || needsAnalyzeMaps.Any()) {
					//Console.Write("some maps are missing...");
					string prevFolderPath = storedSet.FolderPath;
					foreach (var map in missingMaps) {
						storedSet.Add(map);
					}
					
					if (storedSet.FolderPath != prevFolderPath)
						onUIThread(() => _allMapsets.Remove(prevFolderPath));
					if (needsAnalyzeMaps.Any())
						onUIThread(() => needsAnalyzeMaps.ForEach(map => map.DiffRating.ClearCachedSeries()));
					analyzeAndCacheMapset(storedSet);

					if (missingMaps.Any())
						Console.WriteLine($"{missingMaps.Count} missing maps analyzed");
					if (needsAnalyzeMaps.Any())
						Console.WriteLine($"{needsAnalyzeMaps.Count} maps analyzed");
				}
				else {
					Console.WriteLine("found cached result, no maps are missing");
				}
				if (!ReferenceEquals(storedSet, set)) {
					onUIThread(() => set.Dispose());
				}
				return storedSet;
			}
			else {
				//Console.WriteLine("mapset not analyzed...");
				analyzeAndCacheMapset(set);
				//Console.WriteLine("analyzed");
				return set;
			}
			
			void analyzeAndCacheMapset(Mapset set) {
				var errorsLock = new object();
				Parallel.ForEach(set, map => {
					try {
						AnalyzeMap(map, clearLists);
					}
					catch (Exception ex) {
						lock (errorsLock) {
							Console.WriteLine($"[ERROR] Failed to parse beatmap: \"{map.Artist} - {map.Title} [{map.Version}]\"");
							Console.WriteLine(ex.ToString());
						}
						map.IsAnalyzed = false;
					}
				});
				var toRemove = set.Where(map => !map.IsAnalyzed || !File.Exists(map.Filepath)).ToArray();
				
				onUIThread(() => {
					foreach (Beatmap map in toRemove) {
						set.Remove(map);
						map.Dispose();
					}
					set.IsAnalyzed = true;

					// add to cache
					if (!string.IsNullOrEmpty(set.FolderPath))
						_allMapsets[set.FolderPath] = set;
				});

				if (saveToXml) {
					//Console.Write("saving set...");
					if (set.SaveToXML()) { /*Console.WriteLine("set saved");*/ }
					else { /*Console.WriteLine("!! could not save");*/ }
				}
			}
		}

		private static (List<Beatmap> missing, List<Beatmap> needsAnalyze) GetMissingAnalyzedDiffs(Mapset set, Mapset targetSet) {
			if (set.Title == targetSet.Title && set.Artist == targetSet.Artist && set.Creator == targetSet.Creator) {
				var missing = new List<Beatmap>();
				var targetVersions = targetSet.Select(map => map.Version).ToHashSet();
				foreach (Beatmap map in set) {
					if (!targetVersions.Contains(map.Version))
						missing.Add(map);
				}
				var needsAnalyze = targetSet.Where(map => !map.IsAnalyzed).ToList();
				return (missing, needsAnalyze);
			}
			else
				return (new(), new());
		}

		#endregion
	}
}
