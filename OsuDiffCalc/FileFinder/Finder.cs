namespace OsuDiffCalc.FileFinder {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	class Finder {
		public static IEnumerable<string> GetOpenFilesUsedByProcess(string processName, params string[] filetypeFilter) {
			var filters = filetypeFilter.Select(x => x.ToLower()).ToHashSet();
			try {
				return from p in Process.GetProcessesByName(processName)
							 // filter to processes with open audio files
							 from file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id)
							 where filters.Contains(Path.GetExtension(file).ToLower())
							 select file;
			}
			catch { }
			return Array.Empty<string>();
		}

		private static readonly HashSet<string> _osuOpenFileTypes = new(new []{ 
			".mp3", ".ogg", ".oga", ".mogg", ".wma", ".wav", ".flac", ".aac", ".alac", ".wv", // audio
			".avi", ".flv", ".mp4", ".mkv", ".mov", ".wmv", ".webm", ".gifv", ".vob", ".ogv",  // video
			".mpg", ".mpeg", ".m4v", ".3gp", ".mov", ".qt", ".flv", // video
		});

		/// <summary>
		/// Gets current/active beatmap directory based on osu!'s open file hooks
		/// </summary>
		public static string GetOsuBeatmapDirectory(int? pid) {
			if (!pid.HasValue)
				return null;
			try {
				var beatmapDirs =
					// filter to processes with open audio files
					from file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(pid.Value)
					where _osuOpenFileTypes.Contains(Path.GetExtension(file).ToLower())
					// filter to processes whose directory contains .osu files
					let dirPath = Path.GetDirectoryName(file)
					let beatmapFiles = Directory.EnumerateFiles(dirPath, "*.osu", SearchOption.TopDirectoryOnly)
					where beatmapFiles.Any()
					select dirPath;
				return beatmapDirs.FirstOrDefault();
			}
			catch { 
				return null;
			}
		}

		/// <summary>
		/// Get a reference to the osu! process. Returns <see langword="null"/> if it cannot be found.
		/// </summary>
		/// <param name="lastPid"> last pid of osu!, if available </param>
		public static Process GetOsuProcess(int? lastPid = null) {
			if (lastPid.HasValue) {
				try {
					return Process.GetProcessById(lastPid.Value);
				}
				catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) {
					int yy = 1;
				} 
			}
			try {
				var processes =
					(from p in Process.GetProcessesByName("osu!")
					 where Path.GetFileName(p.MainModule.FileName).ToLower() == "osu!.exe"
					 select p).ToList();
				if (processes.Count <= 1)
					return processes.FirstOrDefault();

				var processesUsingFiles =
					from p in processes
					// filter to processes with open audio files
					from file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id)
					where _osuOpenFileTypes.Contains(Path.GetExtension(file).ToLower())
					// filter to processes whose directory contains .osu files
					let dirPath = Path.GetDirectoryName(file)
					let beatmapFiles = Directory.EnumerateFiles(dirPath, "*.osu", SearchOption.TopDirectoryOnly)
					where beatmapFiles.Any()
					select p;
				return processesUsingFiles.FirstOrDefault() ?? processes.FirstOrDefault();
			}
			catch (Exception ex) {
				return null;
			}
		}
	}
}
