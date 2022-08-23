namespace OsuDiffCalc.Utility {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.Linq;
	using System.Runtime.ConstrainedExecution;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using static OsuDiffCalc.NativeMethods;

	internal class WindowHelper {
		#region Wrappers around NativeMethods

		/// <inheritdoc cref="OsuDiffCalc.NativeMethods.SetWindowPos(IntPtr, IntPtr, int, int, int, int, SetWindowPosFlags)"/>
		public static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags)
			=> NativeMethods.SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);

		/// <inheritdoc cref="OsuDiffCalc.NativeMethods.GetForegroundWindow()"/>
		public static IntPtr GetForegroundWindow() => NativeMethods.GetForegroundWindow();

		/// <inheritdoc cref="OsuDiffCalc.NativeMethods.GetConsoleWindow()"/>
		public static IntPtr GetConsoleWindow() => NativeMethods.GetConsoleWindow();

		#endregion

		/// <inheritdoc cref="TrySetUseImmersiveDarkMode(IntPtr, bool)"/>
		public static bool TrySetUseImmersiveDarkMode(Process process, bool enabled)
			=> TrySetUseImmersiveDarkMode(process.MainWindowHandle, enabled);

		/// <summary>
		/// Try to set 'use immersive dark mode' on the window for recent versions of Windows.
		/// </summary>
		/// <param name="hWnd">handle to the window</param>
		/// <param name="enabled">value to set</param>
		/// <returns>true if it could be set, otherwise false</returns>
		public static bool TrySetUseImmersiveDarkMode(IntPtr hWnd, bool enabled) {
			if (IsWindows10(17763)) {
				var attribute = DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
				if (IsWindows10(18985))
					attribute = DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE;

				bool wasSet = DwmSetWindowAttribute(hWnd, attribute, enabled);
				if (wasSet) {
					// refresh the window title bar
					const SetWindowPosFlags flags =
						SetWindowPosFlags.SWP_DRAWFRAME
						| SetWindowPosFlags.SWP_NOACTIVATE
						| SetWindowPosFlags.SWP_NOMOVE
						| SetWindowPosFlags.SWP_NOSIZE
						| SetWindowPosFlags.SWP_NOZORDER;
					return SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, flags);
				}
				else
					return false;
			}
			return false;
		}

		public static bool TryMoveToScreen(int pid, int screenId)
			=> TryMoveToScreen(Process.GetProcessById(pid), screenId);

		public static bool TryMoveToScreen(Process process, int screenId)
			=> TryMoveToScreen(process?.MainWindowHandle ?? IntPtr.Zero, screenId);

		public static bool TryMoveToScreen(int pid, Screen screen)
			=> TryMoveToScreen(Process.GetProcessById(pid), screen);

		public static bool TryMoveToScreen(IntPtr hWnd, int screenId) {
			if (screenId < 0 || screenId >= Screen.AllScreens.Length)
				return false;
			var screen = Screen.AllScreens[screenId];
			return TryMoveToScreen(hWnd, screen);
		}

		public static bool TryMoveToScreen(Process process, Screen screen) {
			IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;
			return TryMoveToScreen(hWnd, screen);
		}

		public static bool TryMoveToScreen(IntPtr hWnd, Screen screen) {
			if (hWnd == IntPtr.Zero || screen is null)
				return false;
			var currentScreen = Screen.FromHandle(hWnd);
			if (currentScreen?.Bounds == screen.Bounds)
				return true;
			if (!NativeMethods.GetWindowRect(hWnd, out RECT rect))
				return false;
			// TODO: ensure new target pos is valid, rescale if monitors are different res / scaling / etc.

			int width = rect.Right - rect.Left;
			int height = rect.Bottom - rect.Top;

			int screenLeft = screen.WorkingArea.Left;
			int screenTop = screen.WorkingArea.Top;
			int screenRight = screen.WorkingArea.Right;
			int screenBottom = screen.WorkingArea.Bottom;

			int x2 = screenLeft;
			int y2 = screenTop;
			if (currentScreen != null) {
				var bounds = currentScreen.Bounds;
				double xRatio = rect.Left * 1.0 / bounds.Width;
				double yRatio = rect.Top * 1.0 / bounds.Height;

				// note: cannot easily simplify checks into Min(), Max() since bounds could be negative in multi-monitor arrays
				x2 = screenLeft + (int)Math.Round(xRatio * screen.Bounds.Width, MidpointRounding.AwayFromZero);
				if (x2 + width > screenRight)
					x2 = screenRight - width;
				if (x2 < screenLeft)
					x2 = screenLeft;

				y2 = screenTop + (int)Math.Round(yRatio * screen.Bounds.Height, MidpointRounding.AwayFromZero);
				if (y2 + height > screenBottom)
					y2 = screenBottom - height;
				if (y2 < screenTop)
					y2 = screenTop;
			}
			var flags = SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOACTIVATE;
			return NativeMethods.SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, x2, y2, width, height, flags);
		}

		public static bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect) {
			if (NativeMethods.GetWindowRect(hWnd, out RECT rect)) {
				lpRect = new Rectangle(
					x: rect.Left,
					y: rect.Top,
					width: rect.Width(),
					height: rect.Height());
				return true;
			}
			else {
				lpRect = default;
				return false;
			}
		}

		/// <summary>
		/// Adds the TOPMOST flag to the window, placing it above all non-topmost windows in the z-order. <br/>
		/// The window maintains this flag even when deactivated. See <see cref="SpecialWindowHandles.HWND_TOPMOST"/>.
		/// </summary>
		/// <param name="hWnd">handle to the window</param>
		/// <param name="topMost"> <para>
		/// If <see langword="true"/>, adds the TOPMOST flag to the window, placing it above all non-topmost windows in the z-order. <br/>
		/// The window maintains this flag even when deactivated. See <see cref="SpecialWindowHandles.HWND_TOPMOST"/>.
		/// </para><para>
		/// If <see langword="false"/>, removes the TOPMOST flag from the window, placing it behind all topmost windows in the z-order. <br/>
		/// See <see cref="SpecialWindowHandles.HWND_NOTOPMOST"/>.
		/// </para></param>
		/// <returns>
		/// <see langword="true"/> if the flag was successfully changed, otherwise <see langword="false"/>
		/// </returns>
		public static bool SetTopMost(IntPtr hWnd, bool topMost) {
			var flags = SetWindowPosFlags.SWP_NOMOVE
								| SetWindowPosFlags.SWP_NOSIZE
								| SetWindowPosFlags.SWP_NOACTIVATE;
			var pos = topMost ? SpecialWindowHandles.HWND_TOPMOST : SpecialWindowHandles.HWND_NOTOPMOST;
			return NativeMethods.SetWindowPos(hWnd, pos, 0, 0, 0, 0, flags);
		}

		public static bool IsForegroundWindowFullScreen()
			=> IsFullScreen(GetForegroundWindow());

		public static bool IsFullScreen(Process process) {
			IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;
			return IsFullScreen(hWnd);
		}

		public static bool IsFullScreen(IntPtr hWnd, Screen screen = null) {
			if (hWnd == IntPtr.Zero)
				return false;

			// ensure the target process is in the foreground
			IntPtr fgWindow = GetForegroundWindow();
			if (fgWindow != IntPtr.Zero && fgWindow != hWnd)
				return false;

			// find the screen it's on and get its position
			screen ??= Screen.FromHandle(hWnd);
			if (!NativeMethods.GetWindowRect(hWnd, out RECT rect))
				return false;

			// in case you want the process name:

			// uint targetPid = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
			// var targetProcess = Process.GetProcessById((int)targetPid);
			// Console.WriteLine($"The process we are querying is [{targetPid}] '{targetProcess?.ProcessName}'");

			// when using "fullscreen borderless" or fullscreen without focus, it will be width+1 or height+1 or similar
			return (rect.Right - rect.Left) == screen.Bounds.Width
				&& (rect.Bottom - rect.Top) == screen.Bounds.Height;
		}

		public static void ActivateApplication(string strAppName) {
			Process[] pList = Process.GetProcessesByName(strAppName);
			if (pList.Length > 0) {
				ShowWindow(pList[0].MainWindowHandle, ShowWindowCommand.SW_RESTORE);
				SetForegroundWindow(pList[0].MainWindowHandle);
			}
		}

		/// <inheritdoc cref="MakeForegroundWindow(IntPtr)"/>
		public static void MakeForegroundWindow(Process process) {
			if (process is not null) {
				process.Refresh();
				if (!process.HasExitedSafe())
					MakeForegroundWindow(process.MainWindowHandle);
			}
		}

		/// <summary>
		/// works but constantly captures mouse/kb
		/// </summary>
		public static void MakeForegroundWindow(IntPtr hWnd) {
			IntPtr fgWindow = GetForegroundWindow();
			if (hWnd == fgWindow) return;

			var flags = SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE
				| SetWindowPosFlags.SWP_SHOWWINDOW 
				//| SetWindowPosFlags.SWP_NOACTIVATE
				//| SetWindowPosFlags.SWP_NOZORDER
				| SetWindowPosFlags.SWP_ASYNCWINDOWPOS
				| SetWindowPosFlags.SWP_FRAMECHANGED;
			ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNOACTIVATE);
			SetForegroundWindow(hWnd);
			NativeMethods.SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, flags);
		}

		/// <summary>
		/// "works" but glitches in and out (only shows for a fraction of a second)
		/// </summary>
		public static void MakeForegroundWindow2(Process process) {
			IntPtr hWnd = process.MainWindowHandle;
			uint fgThread = GetWindowThreadProcessId(GetForegroundWindow());
			uint appThread = GetWindowThreadProcessId(hWnd);
			if (fgThread == appThread) return;
			ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
			SetForegroundWindow(hWnd);

			//AttachThreadInput(fgThread, appThread, false);
			AttachThreadInput(appThread, fgThread, true);
		}

		/*
		 * from https://www.pinvoke.net/default.aspx/user32.SetForegroundWindow
		 * 
		 * As mentioned above SetForegroundWindow might not always work as expected but it is actually very well documented why on https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setforegroundwindow
		 * as mentioned there the system restricts which processes can set the foreground window.
		 * One simple workaround would be to go for: A process can set the foreground window if the process received the last input event.
		 * So simply call keybd_event(0, 0, 0, 0); right in front of SetForegroundWindow(IntPtr hWnd);
		 * (Only problem with this approach might be that a key event might be triggered in the second application)
		 * 
		 * Second approach to gain full control would be to minimize and restore before SetForeground instead of using the keybd_event:
		 * if (MyWrapper.GetForegroundWindow() != targetHWnd) { //Check if the window isnt already in foreground
		 *   MyWrapper.ShowWindow(targetHWnd, 6); //Minimize (ShowWindowCommands.Minimize)
		 *   MyWrapper.ShowWindow(targetHWnd, 9); //Restore (ShowWindowCommands.Restore)
		 * }
		 */

		/// <summary>
		/// "works" but glitches in and out (only shows for a fraction of a second)
		/// </summary>
		public static void MakeForegroundWindow3(Process process) {
			IntPtr hWnd = process.MainWindowHandle;
			uint fgPid = GetWindowThreadProcessId(GetForegroundWindow());
			uint appPid = GetWindowThreadProcessId(hWnd);
			var swpFlags = SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE
				| SetWindowPosFlags.SWP_SHOWWINDOW 
				| SetWindowPosFlags.SWP_NOACTIVATE
				| SetWindowPosFlags.SWP_ASYNCWINDOWPOS
			;
			if (fgPid == appPid) {
				NativeMethods.SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, swpFlags);
				return;
			}
			// ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
			SetForegroundWindow(hWnd);
			AttachThreadInput(fgPid, appPid, true);
			NativeMethods.SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, swpFlags);
		}

		/// <summary>
		/// works but captures mouse/kb, can't find an easy way to forward inputs...
		/// </summary>
		public static void ForceForegroundWindow(Process processToMove, Process processToDrawOver) {
			IntPtr hWnd = processToMove.MainWindowHandle;
			IntPtr actualFgWnd = GetForegroundWindow();
			IntPtr baseWnd = processToDrawOver?.MainWindowHandle ?? actualFgWnd;
			uint fgPid = GetWindowThreadProcessId(actualFgWnd);
			uint basePid = GetWindowThreadProcessId(baseWnd);
			uint appPid = GetWindowThreadProcessId(hWnd);

			// works momentarily (screen flashes as it goes fullscreen/not fullscreen/...)
			if (basePid != fgPid) {
				BringWindowToTop(hWnd);
				ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNOACTIVATE);
				return;
			}
			else {
				var flags = SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE
					| SetWindowPosFlags.SWP_SHOWWINDOW 
					//| SetWindowPosFlags.SWP_NOACTIVATE
					//| SetWindowPosFlags.SWP_NOZORDER
					| SetWindowPosFlags.SWP_ASYNCWINDOWPOS
					//| SetWindowPosFlags.SWP_NOSENDCHANGING
					//| SetWindowPosFlags.SWP_NOREDRAW
				;
				//AttachThreadInput(targetPid, basePid, false);
				//AttachThreadInput(fgPid, appPid, true);
				AttachThreadInput(basePid, appPid, true);
				SetForegroundWindow(hWnd);
				//BringWindowToTop(hWnd);
				NativeMethods.SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, flags);
				//ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNOACTIVATE);
				AttachThreadInput(basePid, appPid, false); // this line makes diffcalc hold on to the foreground
				//AttachThreadInput(appPid, basePid, true); // does not work to pass inputs to osu!
			}
			// and capture input
			/*var hWnd = process.MainWindowHandle;
			uint fgThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
			uint appThread = GetCurrentThreadId();

			if (fgThread != appThread) {
				AttachThreadInput(fgThread, appThread, true);
				BringWindowToTop(hWnd);
				ShowWindow(hWnd, ShowWindowCommand.SW_SHOW);
				AttachThreadInput(fgThread, appThread, false);
			}
			else {
				BringWindowToTop(hWnd);
				ShowWindow(hWnd, ShowWindowCommand.SW_SHOW);
			}*/
		}

		private static bool IsWindows10(int build = -1)
			=> Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;

		private static bool IsWindows11(int build = -1)
			=> Environment.OSVersion.Version.Major >= 11 && Environment.OSVersion.Version.Build >= build;
	}
}
