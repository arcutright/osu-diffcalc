namespace OsuDiffCalc.FileFinder {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	class Finder {
		public static string GetWindowTitle(string processName) {
			string title = "";
			try {
				Process[] list = Process.GetProcessesByName(processName);
				if (list.Length > 0)
					title = list[0].MainWindowTitle;
			}
			catch { }
			return title;
		}

		public static string GetFirstFileUsedByProcess(string processName, params string[] filetypeFilter) {
			try {
				Process[] list = Process.GetProcessesByName(processName);
				//Win32Processes.DetectOpenFiles w32 = new Win32Processes.DetectOpenFiles();
				if (list.Length > 0) {
					//List<string> openFiles = Win32ProcessesRewrite2.GetFilesLockedBy(list[0]);
					IEnumerable<string> openFiles = Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(list[0].Id);
					foreach (string file in openFiles) {
						foreach (string filter in filetypeFilter) {
							if (file.EndsWith(filter, StringComparison.OrdinalIgnoreCase))
								return file;
						}
					}
				}
			}
			catch { }
			return null;
		}

		public static List<string> GetOpenFilesUsedByProcess(string processName, params string[] filetypeFilter) {
			var filePaths = new List<string>();
			try {
				Process[] list = Process.GetProcessesByName(processName);
				if (list.Length > 0) {
					foreach (Process p in list) {
						IEnumerable<string> openFiles = Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id);
						foreach (string file in openFiles) {
							foreach (string filter in filetypeFilter) {
								if (file.EndsWith(filter, StringComparison.OrdinalIgnoreCase)) {
									filePaths.Add(file);
								}
							}
						}
					}
				}
			}
			catch { }
			return filePaths;
		}

		public static List<string> GetOsuBeatmapDirectoriesFromProcessHooks(string processName) {
			string[] audioFileTypes = { ".mp3", ".ogg", ".wav" };

			var filePaths = new List<string>();
			var osuFileDirectories = new List<string>();
			try {
				Process[] list = Process.GetProcessesByName(processName);
				if (list.Length > 0) {
					foreach (Process p in list) {
						IEnumerable<string> openFiles = Win32Processes.DetectOpenFiles.GetOpenFilesEnumerator(p.Id);
						DirectoryInfo dirInfo;

						foreach (string file in openFiles) {
							foreach (string filter in audioFileTypes) {
								if (file.EndsWith(filter, StringComparison.OrdinalIgnoreCase)) {
									dirInfo = Directory.GetParent(file);
									if (dirInfo is not null) {
										if (Directory.EnumerateFiles(dirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly)
											  .Any(path => path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))) {
											osuFileDirectories.Add(dirInfo.FullName);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }

			return osuFileDirectories;
		}
	}
}
