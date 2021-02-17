namespace OsuDiffCalc.UserInterface {
	using System;
	using System.IO;
	using System.Windows.Forms;

	class UX {
		private static readonly string OsuBeatmapDirectory =
#if DEBUG
			Path.Combine(
				Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)))), // .sln directory
				$"OsuDiffCalc.Tests", "Resources");
#else
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "osu!", "Songs");
#endif

		public static string[] GetFilenamesFromDialog() {
			using var dialog = new OpenFileDialog {
				Title = "Open osu! Beatmap File",
				Filter = "osu! files|*.osu",
				InitialDirectory = OsuBeatmapDirectory,
				Multiselect = true,
				DereferenceLinks = true,
			};
			try {
				if (dialog.ShowDialog() == DialogResult.OK) {
					if (Directory.Exists(dialog.FileName))
						return Directory.GetFiles(dialog.FileName, "*.osu", SearchOption.TopDirectoryOnly);
					else
						return dialog.FileNames;
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
