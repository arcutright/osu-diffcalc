namespace OsuDiffCalc.UserInterface {
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;

	class UX {
		private static string _searchDirectory =
#if DEBUG
			Path.Combine(
				Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)))), // .sln directory
				$"OsuDiffCalc.Tests", "Resources");
#else
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "osu!", "Songs");
#endif

		public static string[] GetFilenamesFromDialog() {
			if (!Directory.Exists(_searchDirectory)) {
				// TODO: get osu! songs directory from process peeking if osu! is running

				// look for osu! entry in start menu
				string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
				string shortcut = Directory.GetFiles(startMenuPath, "*osu*.lnk", SearchOption.AllDirectories)
					.OrderBy(s => s.Length + (s.ToLower().Contains("osu!") ? 0 : 1)) // poor man's edit distance
					.FirstOrDefault();
				if (shortcut != null) {
					// TODO: support alternate songs directories by parsing 'osu!.<User>.cfg' for 'BeatmapDirectory = <Songs>' line
					_searchDirectory = Path.Combine(Path.GetDirectoryName(GetShortcutTargetFile(shortcut)), "Songs");
				}
			}
			using var dialog = new OpenFileDialog {
				Title = "Open osu! Beatmap File",
				Filter = "osu! files|*.osu",
				InitialDirectory = _searchDirectory,
				Multiselect = true,
				DereferenceLinks = true,
			};
			try {
				if (dialog.ShowDialog() == DialogResult.OK) {
					if (Directory.Exists(dialog.FileName)) {
						_searchDirectory = dialog.FileName;
						return Directory.GetFiles(dialog.FileName, "*.osu", SearchOption.TopDirectoryOnly);
					}
					else {
						_searchDirectory = Path.GetDirectoryName(dialog.FileName);
						return dialog.FileNames;
					}
				}
			}
			catch (Exception e) {
				Console.WriteLine(e.GetBaseException());
			}
			return Array.Empty<string>();
		}

		public static string[] GetFilenamesFromDialog(GUI gui) {
			string[] filenames = null;
			gui.Invoke((MethodInvoker)delegate {
				filenames = GetFilenamesFromDialog();
			});
			return filenames;
		}

		private static string GetShortcutTargetFile(string shortcutFilename) {
			string dir = Path.GetDirectoryName(shortcutFilename);
			string filename = Path.GetFileName(shortcutFilename);
			Shell32.Shell shell = new();
			Shell32.FolderItem folderItem = shell.NameSpace(dir).ParseName(filename);
			if (folderItem is not null) {
				var link = (Shell32.ShellLinkObject)folderItem.GetLink;
				return link?.Path ?? string.Empty;
			}
			return string.Empty;
		}
	}
}
