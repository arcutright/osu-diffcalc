namespace OsuDiffCalc.UserInterface {
	using System;
	using System.Windows.Forms;

	class UX {
		public static string GetFilenameFromDialog() {
			string filename = null;
			var dialog = new OpenFileDialog {
				Title = "Open Osu Beatmap File",
				Filter = "OSU files|*.osu",
				InitialDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"osu file examples"),
				Multiselect = false
			};
			try {
				if (dialog.ShowDialog() == DialogResult.OK)
					filename = dialog.FileName;
			}
			catch (Exception e) {
				Console.WriteLine(e.GetBaseException());
			}
			dialog.Dispose();

			return filename;
		}

		public static string GetFilenameFromDialog(GUI gui) {
			string filename = null;
			gui.Invoke((MethodInvoker)delegate {
				filename = GetFilenameFromDialog();
			});
			return filename;
		}
	}
}
