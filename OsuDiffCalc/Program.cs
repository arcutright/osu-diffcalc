namespace OsuDiffCalc {
	using System;
	using System.IO;
	using System.Diagnostics;
	using System.Threading;
	using System.Windows.Forms;
	using UserInterface;

	static class Program {
		/// <summary> System-wide pid for the console window thread </summary>
		internal static int ConsolePid { get; private set; }

		/// <summary> Win32 window HANDLE for the console thread which called Main() </summary>
		internal static IntPtr ConsoleWindowHandle { get; private set; }

		internal static string ExecutableName { get; private set; } = nameof(OsuDiffCalc);
		internal static string ExecutablePath { get; private set; } = string.Empty;
		internal static string ExecutableDir { get; private set; } = string.Empty;

		//the STAThread is needed to call Ux.getFileFromDialog()->.openFileDialog()
		[STAThread]
		static void Main(string[] args) {
			// grab the name of, and the path to, the main executable for this program
			// several fallbacks since this can behave differently under .net48, .net6, .net6+ with AOT, etc.
			string[] envArgs = Environment.GetCommandLineArgs();
			envArgs = envArgs?.Length >= 1 ? envArgs : args;
#if NET6_0_OR_GREATER
			ExecutablePath = Environment.ProcessPath;
#endif
			if (string.IsNullOrEmpty(ExecutablePath))
				ExecutablePath = envArgs?.Length >= 1 ? envArgs[0] : Process.GetCurrentProcess()?.MainModule?.FileName ?? string.Empty;
			if (!string.IsNullOrEmpty(ExecutablePath))
				ExecutableDir = Path.GetDirectoryName(ExecutablePath);
			if (string.IsNullOrEmpty(ExecutableDir))
				ExecutableDir = AppContext.BaseDirectory;
			if (string.IsNullOrEmpty(ExecutableDir))
				ExecutableDir = Directory.GetCurrentDirectory();
			if (string.IsNullOrEmpty(ExecutablePath))
				ExecutablePath = Path.Combine(ExecutableDir, Path.GetFileName(Process.GetCurrentProcess()?.MainModule?.FileName) ?? $"{nameof(OsuDiffCalc)}.exe");
			ExecutableName = Path.GetFileNameWithoutExtension(ExecutablePath);

#if NET5_0_OR_GREATER && PUBLISH_TRIMMED
			// support trimming for WinForms apps using nuget package WinFormsComInterop
			// currently does not support net7
			System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
#endif
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "Main.Thread";

			// ensure fonts look OK on monitors with non-default scaling set
#if NET5_0_OR_GREATER
			System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
			if (Environment.OSVersion.Version.Major >= 6)
				NativeMethods.SetProcessDPIAware();

			// TODO: these settings don't want to load when using net6+ with AOT compilation -- maybe switch to json + custom settings class
			// configure settings
			var settings = Properties.Settings.Default;
#if DEBUG
			settings.Reset();
#endif
			settings.Upgrade();

			IntPtr hWndConsole = NativeMethods.GetConsoleWindow();
			int consolePid = -1;
			if (hWndConsole != IntPtr.Zero) {
				NativeMethods.GetWindowThreadProcessId(hWndConsole, out uint nativePid);
				consolePid = (int)nativePid;
			}
			ConsoleWindowHandle = hWndConsole;
			ConsolePid = consolePid != -1 ? consolePid : (int)NativeMethods.GetCurrentThreadId();

			// TODO: use argument as initialization point for map analysis
			//       check if argument is a valid path to .osu file or directory containing osu files

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.ThreadException += Application_ThreadException;
			try {
				Application.Run(new GUI());
			}
			catch (Exception ex) {
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
			}
			finally {
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
			}

			//Finder.debugAllProcesses();

			/*
			Beatmap test = new Beatmap(Ux.getFilenameFromDialog());
			MapsetManager.analyzeMap(test, false);
			test.printDebug();
			*/
			//MapsetManager.analyzeCurrentMapset();
		}

		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			Console.WriteLine($"Unhandled exception!");
			Console.WriteLine($"  Sender [{sender?.GetType()}]: '{sender}', e: '{e}'");
			Console.WriteLine($"  ex: '{e?.Exception}'");
#if DEBUG
			System.Diagnostics.Debugger.Break();
#endif
		}
	}
}
