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
							 from file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id, filters)
							 select file;
			}
			catch { }
			return Array.Empty<string>();
		}

		private static readonly HashSet<string> _osuOpenFileTypes = new(StringComparer.OrdinalIgnoreCase){ 
			".mp3", ".ogg", ".oga", ".mogg", ".wma", ".wav", ".flac", ".aac", ".alac", ".wv", // audio
			".avi", ".flv", ".mp4", ".mkv", ".mov", ".wmv", ".webm", ".gifv", ".vob", ".ogv",  // video
			".mpg", ".mpeg", ".m4v", ".3gp", ".mov", ".qt", ".flv", // video
		};

		/// <summary>
		/// Gets current/active beatmap directory based on osu!'s open file hooks
		/// </summary>
		public static string GetOsuBeatmapDirectory(int? osuPid) {
			if (!osuPid.HasValue)
				return null;
			try {
				// filter to processes with open audio files
				int pid = osuPid.Value;
				foreach (var file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(pid, _osuOpenFileTypes)) { // HOT PATH
					var dirPath = Path.GetDirectoryName(file);
					// filter to processes whose directory contains .osu files
					var beatmapFiles = Directory.EnumerateFiles(dirPath, "*.osu", SearchOption.TopDirectoryOnly);
					if (beatmapFiles.Any())
						return dirPath;
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
			if (lastOsuProcess is not null && !lastOsuProcess.HasExited)
				return lastOsuProcess;
			else
				return GetOsuProcess(guiPid);
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
				catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) {
					int yy = 1;
				} 
			}
			try {
				// TODO: rewrite without linq (consider using win32 api funcs?) to reduce cpu/memory usage
				var consolePid = Program.ConsolePid;
				var processes =
					(from p in Process.GetProcessesByName("osu!")
					 where p.Id != consolePid && p.Id != guiPid && Path.GetFileName(p.MainModule.FileVersionInfo.ProductName).ToLower() == "osu!"
					 select p).ToList();

				int n = processes.Count;
				if (n == 0)
					return null;
				else if (n == 1)
					return processes[0];

				var processesUsingFiles =
					from p in processes
					// filter to processes with open audio files
					from file in Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id, _osuOpenFileTypes)
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
