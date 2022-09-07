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

		/// <summary>
		/// Launch an OpenFileDialog for the user to pick some osu! files.
		/// </summary>
		/// <returns> The list of selected file paths </returns>
		/// <remarks> Warning: this needs to be run from an STAThread. </remarks>
		[STAThread]
		public static string[] GetFilenamesFromDialog(string title = "Open osu! Beatmap File", string filter = "osu! files|*.osu") {
			if (!Directory.Exists(_searchDirectory))
				_searchDirectory = FileFinder.Finder.GetOsuSongsDirectory();

			using var dialog = new OpenFileDialog {
				Title = title,
				Filter = filter,
				InitialDirectory = _searchDirectory,
				Multiselect = true,
				DereferenceLinks = true,
			};
			try {
				if (dialog.ShowDialog() == DialogResult.OK) {
					_searchDirectory = Path.GetDirectoryName(dialog.FileName);
					return dialog.FileNames;
				}
			}
			catch (Exception e) {
				Console.WriteLine(e.GetBaseException());
			}
			return Array.Empty<string>();
		}
	}
}
