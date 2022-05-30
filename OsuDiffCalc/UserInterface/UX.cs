namespace OsuDiffCalc.UserInterface {
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;

	class UX {
		private static string _searchDirectory
#if DEBUG
			= Path.Combine(
					Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)))), // .sln directory
					"OsuDiffCalc.Tests",
					"Resources"
				);
#else
			= null;
#endif

		public static string[] GetFilenamesFromDialog() {
			if (!Directory.Exists(_searchDirectory))
				_searchDirectory = FileFinder.Finder.GetOsuSongsDirectory();

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
	}
}
