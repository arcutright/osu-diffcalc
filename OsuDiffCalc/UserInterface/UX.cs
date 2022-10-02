namespace OsuDiffCalc.UserInterface {
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;

	class UX {
		private static string _searchDirectory = null;

		static UX() {
#if DEBUG || RELEASE_TESTING
			var slnDir = Path.GetDirectoryName(Program.ExecutablePath);
			while (!string.IsNullOrEmpty(slnDir) && !Directory.EnumerateFiles(slnDir, "*.sln").Any()) {
				slnDir = Path.GetDirectoryName(slnDir);
			}
			_searchDirectory = Path.Combine(slnDir, "OsuDiffCalc.Tests", "Resources");
#endif
		}

		/// <summary>
		/// Launch an OpenFileDialog for the user to pick some osu! files.
		/// </summary>
		/// <param name="searchDirectory">if null, will re-use last search directory</param>
		/// <returns> The list of selected file paths </returns>
		/// <remarks> Warning: this needs to be run from an STAThread. </remarks>
		[STAThread]
		public static string[] GetFilenamesFromDialog(string searchDirectory = null, string title = "Open osu! Beatmap File", string filter = "osu! files|*.osu") {
			searchDirectory ??= _searchDirectory;
			if (!Directory.Exists(searchDirectory))
				searchDirectory = FileFinder.Finder.GetOsuSongsDirectory();

			using var dialog = new OpenFileDialog {
				Title = title,
				Filter = filter,
				InitialDirectory = searchDirectory,
				Multiselect = true,
				DereferenceLinks = true,
			};
			try {
				if (dialog.ShowDialog() == DialogResult.OK) {
					searchDirectory = Path.GetDirectoryName(dialog.FileName);
					return dialog.FileNames;
				}
			}
			catch (Exception e) {
				Console.WriteLine(e.GetBaseException());
			}
			finally {
				_searchDirectory = searchDirectory;
			}
			return Array.Empty<string>();
		}
	}
}
