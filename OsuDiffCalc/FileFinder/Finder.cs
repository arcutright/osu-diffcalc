namespace OsuDiffCalc.FileFinder {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Text.RegularExpressions;
	using Utility;

	class Finder {
		private static string _osuDirectory = null;
		private static string _osuSongsDirectory = null;

		private static readonly HashSet<string> _osuOpenFileTypes = new(StringComparer.OrdinalIgnoreCase) {
			".osu", // osu map file
			".mp3", ".ogg", ".oga", ".mogg", ".wma", ".wav", ".flac", ".aac", ".alac", ".wv", // audio
			".avi", ".flv", ".mp4", ".mkv", ".mov", ".wmv", ".webm", ".gifv", ".vob", ".ogv",  // video
			".mpg", ".mpeg", ".m4v", ".3gp", ".mov", ".qt", ".flv", // video
		};
		//private static readonly HashSet<string> _osuOpenFileTypes = new(StringComparer.OrdinalIgnoreCase){
		//	".mp3", ".ogg", ".wav", ".aac", // audio
		//	".mp4", ".avi", ".mov", ".flv", ".webm", // video mentioned in osu!lazer source code
		//	".mkv", ".wmv", ".mpg", ".mpeg", ".mov", // other common video types
		//};

		/// <summary>
		/// Get path to osu! main folder, where the executable lives
		/// </summary>
		/// <param name="osuProcess">If we have a reference to the osu! process we can use it to be a little more predictable</param>
		public static string GetOsuDirectory(Process osuProcess = null) {
			if (_osuDirectory is not null)
				return _osuDirectory;

			// find the path to the main osu! directory (where osu!.exe lives)
			string osuDir = null;
			if (osuProcess is not null)
				osuDir = Path.GetDirectoryName(osuProcess.MainModule.FileName);

			if (!Directory.Exists(osuDir)) {
				// try the most common paths first
				osuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
				if (!Directory.Exists(osuDir)) {
					osuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "osu!");

					// fallback: look for handlers for osu file types
					if (!Directory.Exists(osuDir)) {
						var osuExePath = AssociatedApplicationHelper.FindAssociatedApplication(".osz") ?? AssociatedApplicationHelper.FindAssociatedApplication(".osk");
						osuDir = Path.GetDirectoryName(osuExePath);

						// fallback: look for osu! shortcut entry in start menu
						if (!Directory.Exists(osuDir)) {
							string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
							string shortcut = Directory.GetFiles(startMenuPath, "*osu*.lnk", SearchOption.AllDirectories)
																.OrderBy(s => s.Length + (s.ToLower().Contains("osu!") ? 0 : 1)) // poor man's edit distance
							                  .FirstOrDefault();
							if (shortcut is not null)
								osuDir = Path.Combine(Path.GetDirectoryName(ShortcutHelper.ResolveShortcut(shortcut)));
						}
					}
				}
			}

			if (Directory.Exists(osuDir)) {
				_osuDirectory = osuDir;
				return osuDir;
			}
			else
				return null;
		}

		/// <summary>
		/// Get path to osu! Songs folder, where the beatmaps are stored
		/// </summary>
		/// <param name="osuProcess">If we have a reference to the osu! process we can use it to be a little more predictable</param>
		public static string GetOsuSongsDirectory(Process osuProcess = null) {
			if (_osuSongsDirectory is not null)
				return _osuSongsDirectory;

			var osuDir = GetOsuDirectory(osuProcess);
			var songsDir = Path.Combine(osuDir, "Songs"); // default value

			// parse user config file in case they have a custom BeatmapDirectory
			try {
				var cfgFile = new FileInfo(Path.Combine(osuDir, $"osu!.{Environment.UserName}.cfg"));
				if (cfgFile.Exists) {
					var beatmapDirRegex = new Regex(@"^\s*BeatmapDirectory\s*[=:]\s*(.*)$");
					foreach (var line in File.ReadLines(cfgFile.FullName)) {
						var match = beatmapDirRegex.Match(line);
						if (match.Success) {
							var beatmapDirResult = match.Groups[1].Value.Trim().Trim('"');
							if (Path.IsPathRooted(beatmapDirResult))
								songsDir = beatmapDirResult;
							else
								songsDir = Path.Combine(osuDir, beatmapDirResult);
							break;
						}
					}
				}
			}
			catch { }

			if (Directory.Exists(songsDir)) {
				_osuSongsDirectory = songsDir;
				return songsDir;
			}
			else
				return null;
		}

		/// <summary>
		/// Gets current/active beatmap directory based on osu!'s open file hooks
		/// </summary>
		public static string GetActiveBeatmapDirectory(int? osuPid) {
			if (!osuPid.HasValue)
				return null;
			try {
				// filter to processes with open audio files
				int pid = osuPid.Value;
				foreach (var file in OpenFilesDetector.GetOpenFilesEnumerator(pid, _osuOpenFileTypes)) { // HOT PATH
					var dirPath = Path.GetDirectoryName(file);
					// filter to processes whose directory contains .osu files
					var beatmapFiles = Directory.EnumerateFiles(dirPath, "*.osu", SearchOption.TopDirectoryOnly);
					if (beatmapFiles.Any())
						return dirPath;
				}
				return null;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Get a reference to the osu! process. Returns <see langword="null"/> if it cannot be found.
		/// </summary>
		/// <param name="lastOsuProcess"> last process reference of osu!, if available </param>
		public static Process GetOsuProcess(int guiPid, Process lastOsuProcess) {
			lastOsuProcess?.Refresh();
			if (lastOsuProcess?.HasExited == false)
				return lastOsuProcess;
			else {
				lastOsuProcess?.Dispose();
				return GetOsuProcess(guiPid);
			}
		}

		/// <inheritdoc cref="GetOsuProcess(int, Process)"/>
		/// <param name="lastOsuPid"> last pid of osu!, if available </param>
		public static Process GetOsuProcess(int guiPid, int? lastOsuPid) {
			if (lastOsuPid.HasValue) {
				try {
					var process = Process.GetProcessById(lastOsuPid.Value);
					if (process is not null && !process.HasExited)
						return process;
				}
				catch (ArgumentException) {
					// The process specified by the processId parameter is not running. The identifier might be expired.
				}
				catch (InvalidOperationException) {
					// The process was not started by this object.
				}
			}
			return GetOsuProcess(guiPid);
		}

		/// <summary>
		/// Get a reference to the osu! process. Returns <see langword="null"/> if it cannot be found.
		/// </summary>
		static Process GetOsuProcess(int guiPid) {
			Process result = null;
			int consolePid = Program.ConsolePid;

			// filter list of running processes to those which may be osu processes (checks names, etc)
			var processesToInterrogate = new List<Process>();
			var processes = Process.GetProcesses(".");
			int i = 0;
			for (; i < processes.Length; ++i) {
				var p = processes[i];
				if (MayBeOsuProcess(p, consolePid, guiPid))
					processesToInterrogate.Add(p);
				else
					p.Dispose();
			}

			// interrogate "may be osu processes" to see which one is osu!
			int n = processesToInterrogate.Count;
			if (n == 0)
				return null;
			else if (n == 1) // if only 1, don't try to do open file handle checks
				return processesToInterrogate[0];
			else {
				// find first "may be osu process" that has open handles to music/video files with .osu files in the same dir
				for (i = 0; i < n; ++i) {
					var p = processesToInterrogate[i];
					if (HasOpenOsuFiles(p)) {
						result = p;
						++i;
						break;
					}
					else
						p.Dispose();
				}
				// dispose other processes
				for (; i < n; ++i) {
					processesToInterrogate[i].Dispose();
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool MayBeOsuProcess(Process p, int consolePid, int guiPid) {
			// pre-filter against known pid(s), or wrongly-named processes
			return p is not null
				&& p.Id != consolePid
				&& p.Id != guiPid
				&& string.Equals("osu!", p.ProcessName, StringComparison.OrdinalIgnoreCase)
				&& string.Equals("osu!", Path.GetFileName(p.MainModule.FileVersionInfo.ProductName), StringComparison.OrdinalIgnoreCase);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool HasOpenOsuFiles(Process p) {
			// filter to processes who have open files which osu might use
			if (p is not null) {
				foreach (var file in OpenFilesDetector.GetOpenFilesEnumerator(p.Id, _osuOpenFileTypes)) {
					// whose directory contains .osu files
					var dirPath = Path.GetDirectoryName(file);
					var beatmapFiles = Directory.EnumerateFiles(dirPath, "*.osu", SearchOption.TopDirectoryOnly);
					if (beatmapFiles.Any())
						return true;
				}
			}
			return false;
		}
	}
}
