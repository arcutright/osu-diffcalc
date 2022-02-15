namespace OsuDiffCalc {
	using System;
	using System.Windows.Forms;
	using System.Threading;
	using UserInterface;

	class Program {
		/// <summary>
		/// Program-wide settings
		/// </summary>
		internal static Properties.Settings Settings { get; private set; } = Properties.Settings.Default;

		//the STAThread is needed to call Ux.getFileFromDialog()->.openFileDialog()
		[STAThread]
		static void Main(string[] args) {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "Main.Thread";

			// configure settings
			Settings = Properties.Settings.Default;
#if DEBUG
			Settings.Reset();
#endif
			Settings.Upgrade();

			// TODO: use argument as initialization point for map analysis
			//       check if argument is a valid path to .osu file or directory containing osu files

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new GUI());

			//Finder.debugAllProcesses();

			/*
			Beatmap test = new Beatmap(Ux.getFilenameFromDialog());
			MapsetManager.analyzeMap(test, false);
			test.printDebug();
			*/
			//MapsetManager.analyzeCurrentMapset();
			try {
				Console.WriteLine("-----------program finished-----------");
				Console.ReadKey();
			}
			catch { }
		}
	}
}
