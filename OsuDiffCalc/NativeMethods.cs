namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	internal static class NativeMethods {
		internal static bool TryMoveToScreen(int pid, int screenId) {
			return TryMoveToScreen(Process.GetProcessById(pid), screenId);
		}

		internal static bool TryMoveToScreen(Process process, int screenId) {
			return TryMoveToScreen(process?.MainWindowHandle ?? IntPtr.Zero, screenId);
		}

		internal static bool TryMoveToScreen(IntPtr hWnd, int screenId) {
			if (screenId < 0 || screenId >= Screen.AllScreens.Length)
				return false;
			var screen = Screen.AllScreens[screenId];
			return TryMoveToScreen(hWnd, screen);
		}

		internal static bool TryMoveToScreen(int pid, Screen screen) {
			return TryMoveToScreen(Process.GetProcessById(pid), screen);
		}

		internal static bool TryMoveToScreen(Process process, Screen screen) {
			var hWnd = process?.MainWindowHandle ?? IntPtr.Zero;
			return TryMoveToScreen(hWnd, screen);
		}

		internal static bool TryMoveToScreen(IntPtr hWnd, Screen screen) {
			if (hWnd == IntPtr.Zero || screen is null)
				return false;
			var currentScreen = Screen.FromHandle(hWnd);
			if (currentScreen?.Bounds == screen.Bounds)
				return true;
			if (!GetWindowRect(hWnd, out RECT rect))
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
			return SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, x2, y2, width, height, flags);
		}

		/// <inheritdoc cref="SetWindowPos(IntPtr, IntPtr, int, int, int, int, SetWindowPosFlags)"/>
		internal static bool SetWindowPos(IntPtr hWnd, SpecialWindowHandles insertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags) {
			return SetWindowPos(hWnd, (IntPtr)insertAfter, x, y, cx, cy, uFlags);
		}

		internal static bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect) {
			if (GetWindowRect(hWnd, out RECT rect)) {
				lpRect = new Rectangle(
					x: rect.Left,
					y: rect.Top,
					width: rect.Right - rect.Left + 1,
					height: rect.Bottom - rect.Top + 1);
				return true;
			}
			else {
				lpRect = default;
				return false;
			}
		}

		internal static bool IsForegroundWindowFullScreen() {
			return IsFullScreen(GetForegroundWindow());
		}

		internal static bool IsFullScreen(Process process) {
			IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;
			return IsFullScreen(hWnd);
		}

		internal static bool IsFullScreen(IntPtr hWnd, Screen screen = null) {
			if (hWnd == IntPtr.Zero)
				return false;

			// ensure the target process is in the foreground
			var fgWindow = GetForegroundWindow();
			if (fgWindow != IntPtr.Zero && fgWindow != hWnd)
				return false;

			// find the screen it's on and get its position
			screen ??= Screen.FromHandle(hWnd);
			if (!GetWindowRect(hWnd, out RECT rect))
				return false;

			// in case you want the process name:

			// uint targetPid = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
			// var targetProcess = Process.GetProcessById((int)targetPid);
			// Console.WriteLine($"The process we are querying is [{targetPid}] '{targetProcess?.ProcessName}'");

			// when using "fullscreen borderless" or fullscreen without focus, it will be width+1 or height+1 or similar
			return (rect.Right - rect.Left) == screen.Bounds.Width
				&& (rect.Bottom - rect.Top) == screen.Bounds.Height;
		}

		internal static void ActivateApplication(string strAppName) {
			Process[] pList = Process.GetProcessesByName(strAppName);
			if (pList.Length > 0) {
				ShowWindow(pList[0].MainWindowHandle, ShowWindowCommand.SW_RESTORE);
				SetForegroundWindow(pList[0].MainWindowHandle);
			}
		}

		/// <summary>
		/// works but constantly captures mouse/kb
		/// </summary>
		internal static void MakeForegroundWindow(Process process) {
			var hWnd = process.MainWindowHandle;
			var flags = SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE
				| SetWindowPosFlags.SWP_SHOWWINDOW 
				//| SetWindowPosFlags.SWP_NOACTIVATE
				//| SetWindowPosFlags.SWP_NOZORDER
				| SetWindowPosFlags.SWP_ASYNCWINDOWPOS
				| SetWindowPosFlags.SWP_FRAMECHANGED;
			ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNOACTIVATE);
			SetForegroundWindow(hWnd);
			SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, flags);
		}

		/// <summary>
		/// "works" but glitches in and out (only shows for a fraction of a second)
		/// </summary>
		internal static void MakeForegroundWindow2(Process process) {
			var hWnd = process.MainWindowHandle;
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
		internal static void MakeForegroundWindow3(Process process) {
			var hWnd = process.MainWindowHandle;
			uint fgPid = GetWindowThreadProcessId(GetForegroundWindow());
			uint appPid = GetWindowThreadProcessId(hWnd);
			var swpFlags = SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE
				| SetWindowPosFlags.SWP_SHOWWINDOW 
				| SetWindowPosFlags.SWP_NOACTIVATE
				| SetWindowPosFlags.SWP_ASYNCWINDOWPOS
			;
			if (fgPid == appPid) {
				SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, swpFlags);
				return;
			}
			// ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
			SetForegroundWindow(hWnd);
			AttachThreadInput(fgPid, appPid, true);
			SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, swpFlags);
		}

		/// <summary>
		/// works but captures mouse/kb, can't find an easy way to forward inputs...
		/// </summary>
		internal static void ForceForegroundWindow(Process processToMove, Process processToDrawOver) {
			var hWnd = processToMove.MainWindowHandle;
			var actualFgWnd = GetForegroundWindow();
			var baseWnd = processToDrawOver?.MainWindowHandle ?? actualFgWnd;
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
				SetWindowPos(hWnd, SpecialWindowHandles.HWND_TOP, 0, 0, 0, 0, flags);
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

#pragma warning disable IDE1006 // Naming Styles
		// ReSharper disable InconsistentNaming

		/// <summary>
		///     Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is
		///     directed to the window, and various visual cues are changed for the user. The system assigns a slightly higher
		///     priority to the thread that created the foreground window than it does to other threads.
		///     <para>See for https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx more information.</para>
		/// </summary>
		/// <param name="hWnd">
		///     C++ ( hWnd [in]. Type: HWND )<br />A handle to the window that should be activated and brought to the foreground.
		/// </param>
		/// <returns>
		///     <c>true</c> or nonzero if the window was brought to the foreground, <c>false</c> or zero If the window was not
		///     brought to the foreground.
		/// </returns>
		/// <remarks>
		///     The system restricts which processes can set the foreground window. A process can set the foreground window only if
		///     one of the following conditions is true:
		///     <list type="bullet">
		///     <listheader>
		///         <term>Conditions</term><description></description>
		///     </listheader>
		///     <item>The process is the foreground process.</item>
		///     <item>The process was started by the foreground process.</item>
		///     <item>The process received the last input event.</item>
		///     <item>There is no foreground process.</item>
		///     <item>The process is being debugged.</item>
		///     <item>The foreground process is not a Modern Application or the Start Screen.</item>
		///     <item>The foreground is not locked (see LockSetForegroundWindow).</item>
		///     <item>The foreground lock time-out has expired (see SPI_GETFOREGROUNDLOCKTIMEOUT in SystemParametersInfo).</item>
		///     <item>No menus are active.</item>
		///     </list>
		///     <para>
		///     An application cannot force a window to the foreground while the user is working with another window.
		///     Instead, Windows flashes the taskbar button of the window to notify the user.
		///     </para>
		///     <para>
		///     A process that can set the foreground window can enable another process to set the foreground window by
		///     calling the AllowSetForegroundWindow function. The process specified by dwProcessId loses the ability to set
		///     the foreground window the next time the user generates input, unless the input is directed at that process, or
		///     the next time a process calls AllowSetForegroundWindow, unless that process is specified.
		///     </para>
		///     <para>
		///     The foreground process can disable calls to SetForegroundWindow by calling the LockSetForegroundWindow
		///     function.
		///     </para>
		///     http://pinvoke.net/default.aspx/user32/SetForegroundWindow.html
		/// </remarks> 
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int SetForegroundWindow([In] IntPtr hwnd);

		/// <summary>
		/// Enables the specified process to set the foreground window using the SetForegroundWindow function. 
		/// The calling process must already be able to set the foreground window. For more information, see Remarks later in this topic.
		/// </summary>
		/// <param name="dwProcessId">
		/// The identifier of the process that will be enabled to set the foreground window. 
		/// If this parameter is ASFW_ANY, all processes will be enabled to set the foreground window.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero. <br/>
		/// If the function fails, the return value is zero. The function will fail if the calling process cannot set 
		/// the foreground window.To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// The system restricts which processes can set the foreground window. A process can set the foreground 
		/// window only if one of the following conditions is true: <br />
		/// <list type="bullet">
		///		<item>The process is the foreground process.</item>
		///		<item>The process was started by the foreground process.</item>
		///		<item>The process received the last input event.</item>
		///		<item>There is no foreground process.</item>
		///		<item>The foreground process is being debugged.</item>
		///		<item>The foreground is not locked (see <see cref="LockSetForegroundWindow"/>).</item>
		///		<item>The foreground lock time-out has expired(see SPI_GETFOREGROUNDLOCKTIMEOUT in SystemParametersInfo).</item>
		///		<item>No menus are active.</item>
		/// </list>
		/// <para>
		/// A process that can set the foreground window can enable another process to set the foreground window by 
		/// calling AllowSetForegroundWindow.The process specified by dwProcessId loses the ability to set the
		/// foreground window the next time the user generates input, unless the input is directed at that process,
		/// or the next time a process calls AllowSetForegroundWindow, unless that process is specified.
		/// </para>
		/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AllowSetForegroundWindow([In] int dwProcessId);

		/// <summary>
		/// The foreground process can call the LockSetForegroundWindow function to disable calls to the SetForegroundWindow function.
		/// </summary>
		/// <param name="uLockCode">Specifies whether to enable or disable calls to SetForegroundWindow. </param>
		/// <remarks>
		/// The system automatically enables calls to SetForegroundWindow if the user presses the ALT key or takes some
		/// action that causes the system itself to change the foreground window (for example, clicking a background window).
		/// This function is provided so applications can prevent other applications from making a foreground change 
		/// that can interrupt its interaction with the user.
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool LockSetForegroundWindow([In] LSFWCode uLockCode);

		internal enum LSFWCode : uint {
			/// <summary> Disables calls to SetForegroundWindow </summary>
			LSFW_LOCK = 1u,
			/// <summary> Enables calls to SetForegroundWindow </summary>
			LSFW_UNLOCK = 2u
		}

		/// <summary>
		/// Sets the specified window's show state.
		/// </summary>
		/// <param name="hWnd">A handle to the window.</param>
		/// <param name="nCmdShow">
		/// See <see cref="ShowWindowCommand"/> <br/>
		/// Controls how the window is to be shown. This parameter is ignored the first time an application calls ShowWindow, 
		/// if the program that launched the application provides a STARTUPINFO structure. Otherwise, the first time 
		/// ShowWindow is called, the value should be the value obtained by the WinMain function in its <paramref name="nCmdShow"/> parameter. 
		/// In subsequent calls, this parameter can be one of the following values.</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>To perform certain special effects when showing or hiding a window, use AnimateWindow.</para>
		/// <para>The first time an application calls ShowWindow, it should use the WinMain function's <paramref name="nCmdShow"/> 
		/// parameter as its <paramref name="nCmdShow"/> parameter. Subsequent calls to ShowWindow must use one of the values in 
		/// the given list, instead of the one specified by the WinMain function's <paramref name="nCmdShow"/> parameter.</para>
		/// <para>As noted in the discussion of the <paramref name="nCmdShow"/> parameter, the <paramref name="nCmdShow"/> value 
		/// is ignored in the first call to ShowWindow if the program that launched the application specifies startup information 
		/// in the structure.In this case, ShowWindow uses the information specified in the STARTUPINFO structure to show the window.
		/// On subsequent calls, the application must call ShowWindow with <paramref name="nCmdShow"/> set to SW_SHOWDEFAULT to use 
		/// the startup information provided by the program that launched the application.</para>
		/// This behavior is designed for the following situations:<br />
		/// <list type="bullet">
		/// <item>Applications create their main window by calling CreateWindow with the WS_VISIBLE flag set.</item>
		/// <item>Applications create their main window by calling CreateWindow with the WS_VISIBLE flag cleared, 
		/// and later call ShowWindow with the SW_SHOW flag set to make it visible.</item>
		/// </list>
		/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
		/// </remarks>
		[DllImportAttribute("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow([In] IntPtr hWnd, [In] ShowWindowCommand nCmdShow);

		/// <summary>
		/// Sets the process-default DPI awareness to system-DPI awareness. 
		/// This is equivalent to calling SetProcessDpiAwarenessContext with a DPI_AWARENESS_CONTEXT value of DPI_AWARENESS_CONTEXT_SYSTEM_AWARE. 
		/// </summary>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetProcessDPIAware();

		/// <summary>
		/// Retrieves the thread identifier of the calling thread. <br />
		/// Alternative managed code: <c>Process.GetCurrentProcess().Threads[0].Id;</c>
		/// </summary>
		/// <remarks>
		/// Until the thread terminates, the thread identifier uniquely identifies the thread throughout the system. <br/>
		/// https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadid
		/// </remarks>
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern uint GetCurrentThreadId();

		/// <summary>
		/// Brings the specified window to the top of the Z order. If the window is a top-level window, it is activated. 
		/// If the window is a child window, the top-level parent window associated with the child window is activated.
		/// </summary>
		/// <param name="hWnd">A handle to the window to bring to the top of the Z order.</param>
		/// <returns>If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
		/// <remarks>
		/// Remarks:
		/// <para>Use the BringWindowToTop function to uncover any window that is partially or completely obscured by other windows.</para>
		/// <para>Calling this function is similar to calling the SetWindowPos function to change a window's position in the Z order. 
		/// BringWindowToTop does not make a window a top-level window.</para>
		/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-bringwindowtotop
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BringWindowToTop([In] IntPtr hWnd);

		/// <inheritdoc cref="BringWindowToTop(IntPtr)"/>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BringWindowToTop([In] HandleRef hWnd);

		/// <summary>
		/// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
		/// </summary>
		/// <param name="hWnd">A handle to the window. </param>
		/// <param name="lpdwProcessId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not. </param>
		/// <returns>The return value is the identifier of the thread that created the window. </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/en-us/library/ms633522%28v=vs.85%29.aspx <br/>
		/// http://pinvoke.net/default.aspx/user32/GetWindowThreadProcessId.html
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint GetWindowThreadProcessId([In] IntPtr hWnd, [Out] out uint lpdwProcessId);

		/// <summary> When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter </summary>
		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId([In] IntPtr hWnd, IntPtr ProcessId);

		private static uint GetWindowThreadProcessId([In] IntPtr hWnd) => GetWindowThreadProcessId(hWnd, IntPtr.Zero);

		/// <summary>
		///     Retrieves a handle to the foreground window (the window with which the user is currently working). The system
		///     assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
		///     <para>See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633505%28v=vs.85%29.aspx for more information.</para>
		/// </summary>
		/// <remarks>http://pinvoke.net/default.aspx/user32/GetForegroundWindow.html</remarks>
		/// <returns>
		///     C++ ( Type: Type: HWND )<br /> The return value is a handle to the foreground window. The foreground window
		///     can be NULL in certain circumstances, such as when a window is losing activation.
		/// </returns>
		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr GetForegroundWindow();

		/// <summary>
		/// Attaches or detaches the input processing mechanism of one thread to that of another thread.
		/// </summary>
		/// <param name="idAttach">The identifier of the thread to be attached to another thread. The thread to be attached cannot be a system thread.</param>
		/// <param name="idAttachTo">The identifier of the thread to which idAttach will be attached. This thread cannot be a system thread. <br/>
		/// A thread cannot attach to itself.Therefore, idAttachTo cannot equal idAttach.</param>
		/// <param name="fAttach">If this parameter is TRUE, the two threads are attached. If the parameter is FALSE, the threads are detached.</param>
		/// <returns>If the function succeeds, the return value is nonzero. <br/>
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
		/// <remarks>
		/// <para>By using the AttachThreadInput function, a thread can share its input states (such as keyboard states and the current focus window) 
		/// with another thread. Keyboard and mouse events received by both threads are processed in the order they were received until the threads are 
		/// detached by calling AttachThreadInput a second time and specifying FALSE for the fAttach parameter.</para>
		/// <para>The AttachThreadInput function fails if either of the specified threads does not have a message queue. 
		/// The system creates a thread's message queue when the thread makes its first call to one of the USER or GDI functions. 
		/// The AttachThreadInput function also fails if a journal record hook is installed. Journal record hooks attach all input queues together.</para>
		/// <para>Note that key state, which can be ascertained by calls to the GetKeyState or GetKeyboardState function, 
		/// is reset after a call to AttachThreadInput.You cannot attach a thread to a thread in another desktop.</para>
		/// <c>
		/// // To attach to current thread <br/>
		/// AttachThreadInput(idAttach, curThreadId, true); <br/>
		/// // To dettach from current thread <br/>
		/// AttachThreadInput(idAttach, curThreadId, false); <br/>
		/// </c>
		/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-attachthreadinput
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AttachThreadInput([In] uint idAttach, [In] uint idAttachTo, [In] bool fAttach);

		/// <summary>
		///     Changes the size, position, and Z order of a child, pop-up, or top-level window. These windows are ordered
		///     according to their appearance on the screen. The topmost window receives the highest rank and is the first window
		///     in the Z order.
		///     <para>See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545%28v=vs.85%29.aspx for more information.</para>
		/// </summary>
		/// <param name="hWnd">C++ ( hWnd [in]. Type: HWND )<br />A handle to the window.</param>
		/// <param name="hWndInsertAfter">
		///     C++ ( hWndInsertAfter [in, optional]. Type: HWND )<br />A handle to the window to precede the positioned window in
		///     the Z order. This parameter must be a window handle or one of the following values.
		///     <list type="table">
		///     <itemheader>
		///         <term>HWND placement</term><description>Window to precede placement</description>
		///     </itemheader>
		///     <item>
		///         <term>HWND_BOTTOM ((HWND)1)</term>
		///         <description>
		///         Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost
		///         window, the window loses its topmost status and is placed at the bottom of all other windows.
		///         </description>
		///     </item>
		///     <item>
		///         <term>HWND_NOTOPMOST ((HWND)-2)</term>
		///         <description>
		///         Places the window above all non-topmost windows (that is, behind all topmost windows). This
		///         flag has no effect if the window is already a non-topmost window.
		///         </description>
		///     </item>
		///     <item>
		///         <term>HWND_TOP ((HWND)0)</term><description>Places the window at the top of the Z order.</description>
		///     </item>
		///     <item>
		///         <term>HWND_TOPMOST ((HWND)-1)</term>
		///         <description>
		///         Places the window above all non-topmost windows. The window maintains its topmost position
		///         even when it is deactivated.
		///         </description>
		///     </item>
		///     </list>
		///     <para>For more information about how this parameter is used, see the following Remarks section.</para>
		/// </param>
		/// <param name="x">The new position of the left side of the window, in client coordinates.</param>
		/// <param name="y">The new position of the top of the window, in client coordinates.</param>
		/// <param name="cx">The new width of the window, in pixels.</param>
		/// <param name="cy">The new height of the window, in pixels.</param>
		/// <param name="uFlags">
		///     C++ ( uFlags [in]. Type: UINT )<br />The window sizing and positioning flags. This parameter can be a combination
		///     of the following values.
		///     <list type="table">
		///     <itemheader>
		///         <term>HWND sizing and positioning flags</term>
		///         <description>Where to place and size window. Can be a combination of any</description>
		///     </itemheader>
		///     <item>
		///         <term>SWP_ASYNCWINDOWPOS (0x4000)</term>
		///         <description>
		///         If the calling thread and the thread that owns the window are attached to different input
		///         queues, the system posts the request to the thread that owns the window. This prevents the calling
		///         thread from blocking its execution while other threads process the request.
		///         </description>
		///     </item>
		///     <item>
		///         <term>SWP_DEFERERASE (0x2000)</term>
		///         <description>Prevents generation of the WM_SYNCPAINT message. </description>
		///     </item>
		///     <item>
		///         <term>SWP_DRAWFRAME (0x0020)</term>
		///         <description>Draws a frame (defined in the window's class description) around the window.</description>
		///     </item>
		///     <item>
		///         <term>SWP_FRAMECHANGED (0x0020)</term>
		///         <description>
		///         Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message
		///         to the window, even if the window's size is not being changed. If this flag is not specified,
		///         WM_NCCALCSIZE is sent only when the window's size is being changed
		///         </description>
		///     </item>
		///     <item>
		///         <term>SWP_HIDEWINDOW (0x0080)</term><description>Hides the window.</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOACTIVATE (0x0010)</term>
		///         <description>
		///         Does not activate the window. If this flag is not set, the window is activated and moved to
		///         the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter
		///         parameter).
		///         </description>
		///     </item>
		///     <item>
		///         <term>SWP_NOCOPYBITS (0x0100)</term>
		///         <description>
		///         Discards the entire contents of the client area. If this flag is not specified, the valid
		///         contents of the client area are saved and copied back into the client area after the window is sized or
		///         repositioned.
		///         </description>
		///     </item>
		///     <item>
		///         <term>SWP_NOMOVE (0x0002)</term>
		///         <description>Retains the current position (ignores X and Y parameters).</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOOWNERZORDER (0x0200)</term>
		///         <description>Does not change the owner window's position in the Z order.</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOREDRAW (0x0008)</term>
		///         <description>
		///         Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies
		///         to the client area, the nonclient area (including the title bar and scroll bars), and any part of the
		///         parent window uncovered as a result of the window being moved. When this flag is set, the application
		///         must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
		///         </description>
		///     </item>
		///     <item>
		///         <term>SWP_NOREPOSITION (0x0200)</term><description>Same as the SWP_NOOWNERZORDER flag.</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOSENDCHANGING (0x0400)</term>
		///         <description>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOSIZE (0x0001)</term>
		///         <description>Retains the current size (ignores the cx and cy parameters).</description>
		///     </item>
		///     <item>
		///         <term>SWP_NOZORDER (0x0004)</term>
		///         <description>Retains the current Z order (ignores the hWndInsertAfter parameter).</description>
		///     </item>
		///     <item>
		///         <term>SWP_SHOWWINDOW (0x0040)</term><description>Displays the window.</description>
		///     </item>
		///     </list>
		/// </param>
		/// <returns><c>true</c> or nonzero if the function succeeds, <c>false</c> or zero otherwise or if function fails.</returns>
		/// <remarks>
		///     <para>
		///     As part of the Vista re-architecture, all services were moved off the interactive desktop into Session 0.
		///     hwnd and window manager operations are only effective inside a session and cross-session attempts to manipulate
		///     the hwnd will fail. For more information, see The Windows Vista Developer Story: Application Compatibility
		///     Cookbook.
		///     </para>
		///     <para>
		///     If you have changed certain window data using SetWindowLong, you must call SetWindowPos for the changes to
		///     take effect. Use the following combination for uFlags: SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER |
		///     SWP_FRAMECHANGED.
		///     </para>
		///     <para>
		///     A window can be made a topmost window either by setting the hWndInsertAfter parameter to HWND_TOPMOST and
		///     ensuring that the SWP_NOZORDER flag is not set, or by setting a window's position in the Z order so that it is
		///     above any existing topmost windows. When a non-topmost window is made topmost, its owned windows are also made
		///     topmost. Its owners, however, are not changed.
		///     </para>
		///     <para>
		///     If neither the SWP_NOACTIVATE nor SWP_NOZORDER flag is specified (that is, when the application requests that
		///     a window be simultaneously activated and its position in the Z order changed), the value specified in
		///     hWndInsertAfter is used only in the following circumstances.
		///     </para>
		///     <list type="bullet">
		///     <item>Neither the HWND_TOPMOST nor HWND_NOTOPMOST flag is specified in hWndInsertAfter. </item>
		///     <item>The window identified by hWnd is not the active window. </item>
		///     </list>
		///     <para>
		///     An application cannot activate an inactive window without also bringing it to the top of the Z order.
		///     Applications can change an activated window's position in the Z order without restrictions, or it can activate
		///     a window and then move it to the top of the topmost or non-topmost windows.
		///     </para>
		///     <para>
		///     If a topmost window is repositioned to the bottom (HWND_BOTTOM) of the Z order or after any non-topmost
		///     window, it is no longer topmost. When a topmost window is made non-topmost, its owners and its owned windows
		///     are also made non-topmost windows.
		///     </para>
		///     <para>
		///     A non-topmost window can own a topmost window, but the reverse cannot occur. Any window (for example, a
		///     dialog box) owned by a topmost window is itself made a topmost window, to ensure that all owned windows stay
		///     above their owner.
		///     </para>
		///     <para>
		///     If an application is not in the foreground, and should be in the foreground, it must call the
		///     SetForegroundWindow function.
		///     </para>
		///     <para>
		///     To use SetWindowPos to bring a window to the top, the process that owns the window must have
		///     SetForegroundWindow permission.
		///     </para>
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetWindowPos([In] IntPtr hWnd, [In] IntPtr hWndInsertAfter, [In] int x, [In] int y, [In] int cx, [In] int cy, [In] SetWindowPosFlags uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect([In] IntPtr hwnd, [Out] out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

		/// <summary>
		/// Win32 RECT
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT {
			/// <summary> x position of upper-left corner </summary>
			public int Left;
			/// <summary> y position of upper-left corner </summary>
			public int Top;
			/// <summary> x position of bottom-right corner </summary>
			public int Right;
			/// <summary> y position of bottom-right corner </summary>
			public int Bottom;

			public override string ToString() => $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
		}

		static int Width(this RECT rect) => rect.Right - rect.Left + 1;
		static int Height(this RECT rect) => rect.Bottom - rect.Top + 1;


		#region Window searching

		/// <summary>
		///   Find the first top-level window that matches the search criteria
		/// </summary>
		/// <param name="title"></param>
		/// <param name="className"></param>
		/// <param name="bufferLength">max number of characters for title/class name</param>
		/// <returns>A handle to the window, if found, otherwise IntPtr.Zero</returns>
		internal static IntPtr SearchForWindow(string title, string className = null, int bufferLength = 1024) {
			var sd = new EnumWindowsData { ClassName = className, Title = title, BufferLength = bufferLength };
			if (EnumWindows(EnumWindowsCallback, ref sd))
				return sd.hWnd;
			else
				return IntPtr.Zero;
		}

		/// <summary>
		///   Find the first top-level window associated with the specified desktop that matches the search criteria
		/// </summary>
		/// <param name="hDesktop">
		///   A handle to the desktop whose top-level windows are to be enumerated. This handle is returned by the CreateDesktop,
		///   GetThreadDesktop, OpenDesktop, or OpenInputDesktop function, and must have the DESKTOP_READOBJECTS access right.
		///   For more information, see Desktop Security and Access Rights. <br/>
		///   If this parameter is NULL, the current desktop is used.
		/// </param>
		/// <inheritdoc cref="SearchForWindow"/>
		internal static IntPtr SearchForDesktopWindow(IntPtr hDesktop, string title, string className = null, int bufferLength = 1024) {
			var sd = new EnumWindowsData { ClassName = className, Title = title, BufferLength = bufferLength };
			if (EnumWindows(EnumWindowsCallback, ref sd))
				return sd.hWnd;
			else
				return IntPtr.Zero;
		}

		/// <summary>
		///   Find many top-level windows that match the search criteria
		/// </summary>
		/// <param name="maxNumMatches">
		///   Max number of matches to accept, -1 for all matches (this function will stop enumerating windows once the max is reached)
		/// </param>
		/// <inheritdoc cref="SearchForWindow"/>
		internal static IList<IntPtr> SearchForWindows(string title, string className = null, int bufferLength = 1024, int maxNumMatches = -1) {
			var sd = new EnumWindowsDataMany(maxNumMatches) { ClassName = className, Title = title, BufferLength = bufferLength };
			if (EnumWindowsMany(EnumWindowsCallbackMany, ref sd))
				return sd.WindowHandles;
			else
				return Array.Empty<IntPtr>();
		}

		/// <summary>
		///   Find many top-level windows associated with the specified desktop that match the search criteria
		/// </summary>
		/// <inheritdoc cref="SearchForDesktopWindow"/>
		/// <inheritdoc cref="SearchForWindows"/>
		internal static IList<IntPtr> SearchForDesktopWindows(IntPtr hDesktop, string title, string className = null, int bufferLength = 1024, int maxNumMatches = -1) {
			var sd = new EnumWindowsDataMany(maxNumMatches) { ClassName = className, Title = title, BufferLength = bufferLength };
			if (EnumDesktopWindowsMany(hDesktop, EnumWindowsCallbackMany, ref sd))
				return sd.WindowHandles;
			else
				return Array.Empty<IntPtr>();
		}

		/// <summary>
		///   Retrieves a handle to the top-level window whose class name and window name match the specified strings.
		///   This function does not search child windows. This function does not perform a case-sensitive search. <br/>
		///   To search child windows, beginning with a specified child window, use the FindWindowEx function.
		/// </summary>
		/// <param name="lpClassName">
		///   The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function.
		///   The atom must be in the low-order word of lpClassName; the high-order word must be zero. <br/>
		///   - If lpClassName points to a string, it specifies the window class name. The class name can be any name
		///   registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names. <br/>
		///   - If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.
		/// </param>
		/// <param name="lpWindowName">
		///   The window name (the window's title). If this parameter is NULL, all window names match.
		/// </param>
		/// <returns>
		///   If the function succeeds, the return value is a handle to the window that has the specified class name and window name.
		///   If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// <para>
		///   If the lpWindowName parameter is not NULL, FindWindow calls the GetWindowText function to retrieve the
		///   window name for comparison. For a description of a potential problem that can arise, see the Remarks
		///   for GetWindowText.
		/// </para>
		/// <br/> https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-findwindowa
		/// <br/> https://www.pinvoke.net/default.aspx/user32/FindWindow.html
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr FindWindow([In] string lpClassName, [In] string lpWindowName);

		/// <inheritdoc cref="FindWindow(string, string)"/>
		internal static IntPtr FindWindow(string lpWindowName) => FindWindow(null, lpWindowName);

		/// <summary>
		///   Enumerates all top-level windows on the screen by passing the handle to each window, in turn, to an
		///   application-defined callback function. EnumWindows continues until the last top-level window is
		///   enumerated or the callback function returns FALSE.
		/// </summary>
		/// <param name="lpEnumFunc">
		///   A pointer to an application-defined callback function. For more information, see EnumWindowsProc.
		/// </param>
		/// <param name="lParam">An application-defined value to be passed to the callback function.</param>
		/// <returns>
		///   - If the function succeeds, the return value is nonzero. <br/>
		///   - If the function fails, the return value is zero. To get extended error information, call GetLastError. <br/>
		///   - If EnumWindowsProc returns zero, the return value is also zero. In this case, the callback function
		///     should call SetLastError to obtain a meaningful error code to be returned to the caller of EnumWindows.
		/// </returns>
		/// <remarks>
		///   <para>
		///     The EnumWindows function does not enumerate child windows, with the exception of a few top-level
		///     windows owned by the system that have the WS_CHILD style.
		///   </para><para>
		///     This function is more reliable than calling the GetWindow function in a loop.An application that calls 
		///     GetWindow to perform this task risks being caught in an infinite loop or referencing a handle to a window
		///     that has been destroyed.
		///   </para>
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows 
		///   <br/> https://www.pinvoke.net/default.aspx/user32/EnumWindows.html
		/// </remarks>
		[DllImport("user32.dll", EntryPoint = "EnumWindows", ExactSpelling = false, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, [MarshalAs(UnmanagedType.Struct)] ref EnumWindowsData lParam);

		/// <inheritdoc cref="EnumWindows(EnumWindowsProc, ref EnumWindowsData)"/>
		[DllImport("user32.dll", EntryPoint = "EnumWindows", ExactSpelling = false, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindowsMany(EnumWindowsProcMany lpEnumFunc, [MarshalAs(UnmanagedType.Struct)] ref EnumWindowsDataMany lParam);

		/// <summary>
		///   Enumerates all top-level windows associated with the specified desktop.
		///   It passes the handle to each window, in turn, to an application-defined callback function.
		/// </summary>
		/// <param name="hDesktop">
		///   A handle to the desktop whose top-level windows are to be enumerated. This handle is returned by the CreateDesktop,
		///   GetThreadDesktop, OpenDesktop, or OpenInputDesktop function, and must have the DESKTOP_READOBJECTS access right.
		///   For more information, see Desktop Security and Access Rights. <br/>
		///   If this parameter is NULL, the current desktop is used.
		/// </param>
		/// <param name="lpfn">A pointer to an application-defined EnumWindowsProc callback function.</param>
		/// <param name="lParam">An application-defined value to be passed to the callback function.</param>
		/// <returns>
		///   If the function fails or is unable to perform the enumeration, the return value is zero. <br/>
		///   To get extended error information, call GetLastError. <br/>
		///   You must ensure that the callback function sets SetLastError if it fails. <br/>
		///   Windows Server 2003 and Windows XP/2000: If there are no windows on the desktop, GetLastError returns ERROR_INVALID_HANDLE.
		/// </returns>
		/// <remarks>
		///   The EnumDesktopWindows function repeatedly invokes the lpfn callback function until the last top-level window 
		///   is enumerated or the callback function returns FALSE. 
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdesktopwindows
		/// </remarks>
		[DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc lpfn, [MarshalAs(UnmanagedType.Struct)] ref EnumWindowsData lParam);

		/// <inheritdoc cref="EnumDesktopWindows(IntPtr, EnumWindowsProc, ref EnumWindowsData)"/>
		[DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumDesktopWindowsMany(IntPtr hDesktop, EnumWindowsProcMany lpfn, [MarshalAs(UnmanagedType.Struct)] ref EnumWindowsDataMany lParam);

		/// <summary>
		///   An application-defined callback function used with the EnumWindows or EnumDesktopWindows function.
		///   It receives top-level window handles. The WNDENUMPROC type defines a pointer to this callback function. 
		///   EnumWindowsProc is a placeholder for the application-defined function name.
		/// </summary>
		/// <param name="hWnd">A handle to a top-level window.</param>
		/// <param name="lParam">The application-defined value given in EnumWindows or EnumDesktopWindows.</param>
		/// <returns>To continue enumeration, the callback function must return TRUE; to stop enumeration, it must return FALSE.</returns>
		/// <remarks>
		///   An application must register this callback function by passing its address to EnumWindows or EnumDesktopWindows.
		///   <br/>https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms633498(v=vs.85)
		/// </remarks>
		delegate bool EnumWindowsProc(IntPtr hWnd, ref EnumWindowsData lParam);

		/// <inheritdoc cref="EnumWindowsProc"/>
		delegate bool EnumWindowsProcMany(IntPtr hWnd, ref EnumWindowsDataMany lParam);

		/// <summary>
		/// Application-defined struct to be used by the EnumWindows and EnumDesktopWindows functions
		/// </summary>
		struct EnumWindowsData {
			public EnumWindowsData() { }

			// Can put anything in this application-defined struct
			/// <summary> Buffer length used for string matching </summary>
			public int BufferLength = 1024;
			/// <summary>
			///   The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function.
			///   The atom must be in the low-order word of lpClassName; the high-order word must be zero. <br/>
			///   - If lpClassName points to a string, it specifies the window class name. The class name can be any name
			///   registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names. <br/>
			///   - If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.
			/// </summary>
			public string ClassName = null;
			public string Title = null;

			public IntPtr hWnd = IntPtr.Zero;
		}

		/// <inheritdoc cref="EnumWindowsData"/>
		struct EnumWindowsDataMany {
			/// <param name="maxNumMatches"> -1 = return all </param>
			public EnumWindowsDataMany(int maxNumMatches) {
				MaxNumMatches = maxNumMatches;
				WindowHandles = maxNumMatches > 0 ? new(maxNumMatches) : new(64);
			}

			/// <summary> Buffer length used for string matching </summary>
			public int BufferLength = 1024;
			/// <summary>
			///   The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function.
			///   The atom must be in the low-order word of lpClassName; the high-order word must be zero. <br/>
			///   - If lpClassName points to a string, it specifies the window class name. The class name can be any name
			///   registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names. <br/>
			///   - If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.
			/// </summary>
			public string ClassName = null;
			public string Title = null;

			public int NumMatches = 0;
			/// <summary> -1 = return all </summary>
			public readonly int MaxNumMatches = -1;
			public readonly List<IntPtr> WindowHandles = new(64);
		}

		static bool EnumWindowsCallback(IntPtr hWnd, ref EnumWindowsData data) {
			// Check classname and title
			// This is different from FindWindow() in that the code below allows partial matches
			var sb = new StringBuilder(data.BufferLength);
			bool isMatch = true;
			if (!string.IsNullOrEmpty(data.ClassName)) {
				GetClassName(hWnd, sb, sb.Capacity);
				isMatch &= sb.ToString().StartsWith(data.ClassName);
				sb.Clear();
			}
			if (isMatch && !string.IsNullOrEmpty(data.Title)) {
				GetWindowText(hWnd, sb, sb.Capacity);
				isMatch &= sb.ToString().StartsWith(data.Title);
				sb.Clear();
			}
			if (isMatch)
				data.hWnd = hWnd;
			return !isMatch;
		}

		static bool EnumWindowsCallbackMany(IntPtr hWnd, ref EnumWindowsDataMany data) {
			// Check classname and title
			// This is different from FindWindow() in that the code below allows partial matches
			var sb = new StringBuilder(data.BufferLength);
			bool isMatch = true;
			if (!string.IsNullOrEmpty(data.ClassName)) {
				GetClassName(hWnd, sb, sb.Capacity);
				isMatch &= sb.ToString().StartsWith(data.ClassName);
				sb.Clear();
			}
			if (isMatch && !string.IsNullOrEmpty(data.Title)) {
				GetWindowText(hWnd, sb, sb.Capacity);
				isMatch &= sb.ToString().StartsWith(data.Title);
				sb.Clear();
			}
			if (isMatch) {
				data.WindowHandles.Add(hWnd);
				// only stop iteration if we exceed max number of matches
				++data.NumMatches;
				if (data.MaxNumMatches != -1 && data.NumMatches > data.MaxNumMatches)
					return false; 
			}
			return true;
		}

		/// <summary>
		///   Retrieves the name of the class to which the specified window belongs.
		/// </summary>
		/// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
		/// <param name="lpClassName">The class name string.</param>
		/// <param name="nMaxCount">
		///   The length of the lpClassName buffer, in characters. The buffer must be large enough to include the
		///   terminating null character; otherwise, the class name string is truncated to nMaxCount-1 characters.
		/// </param>
		/// <returns>
		///   If the function succeeds, the return value is the number of characters copied to the buffer, 
		///   not including the terminating null character. <br/>
		///   If the function fails, the return value is zero. <br/>
		///   To get extended error information, call GetLastError function.
		/// </returns>
		/// <remarks>
		///   https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclassname
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetClassName([In] IntPtr hWnd, [Out] StringBuilder lpClassName, [In] int nMaxCount);

		/// <summary>
		///     Copies the text of the specified window's title bar (if it has one) into a buffer. If the specified window is a
		///     control, the text of the control is copied. However, GetWindowText cannot retrieve the text of a control in another
		///     application.
		///     <para>
		///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633520%28v=vs.85%29.aspx  for more
		///     information
		///     </para>
		/// </summary>
		/// <param name="hWnd">
		///     C++ ( hWnd [in]. Type: HWND )<br />A <see cref="IntPtr" /> handle to the window or control containing the text.
		/// </param>
		/// <param name="lpString">
		///     C++ ( lpString [out]. Type: LPTSTR )<br />The <see cref="StringBuilder" /> buffer that will receive the text. If
		///     the string is as long or longer than the buffer, the string is truncated and terminated with a null character.
		/// </param>
		/// <param name="nMaxCount">
		///     C++ ( nMaxCount [in]. Type: int )<br /> Should be equivalent to
		///     <see cref="StringBuilder.Length" /> after call returns. The <see cref="int" /> maximum number of characters to copy
		///     to the buffer, including the null character. If the text exceeds this limit, it is truncated.
		/// </param>
		/// <returns>
		///     If the function succeeds, the return value is the length, in characters, of the copied string, not including
		///     the terminating null character. If the window has no title bar or text, if the title bar is empty, or if the window
		///     or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.<br />
		///     This function cannot retrieve the text of an edit control in another application.
		/// </returns>
		/// <remarks>
		///     If the target window is owned by the current process, GetWindowText causes a WM_GETTEXT message to be sent to the
		///     specified window or control. If the target window is owned by another process and has a caption, GetWindowText
		///     retrieves the window caption text. If the window does not have a caption, the return value is a null string. This
		///     behavior is by design. It allows applications to call GetWindowText without becoming unresponsive if the process
		///     that owns the target window is not responding. However, if the target window is not responding and it belongs to
		///     the calling application, GetWindowText will cause the calling application to become unresponsive. To retrieve the
		///     text of a control in another process, send a WM_GETTEXT message directly instead of calling GetWindowText.<br />For
		///     an example go to
		///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms644928%28v=vs.85%29.aspx#sending">
		///     Sending a
		///     Message.
		///     </see>
		///     <br/>
		///     https://www.pinvoke.net/default.aspx/user32/GetWindowText.html
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int GetWindowText([In] IntPtr hWnd, [Out] StringBuilder lpString, [In] int nMaxCount);

		/// <summary>
		///   Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar).
		///   If the specified window is a control, the function retrieves the length of the text within the control.
		///   However, GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
		/// </summary>
		/// <param name="hWnd">A handle to the window or control.</param>
		/// <returns>
		///   - If the function succeeds, the return value is the length, in characters, of the text. 
		///     Under certain conditions, this value might be greater than the length of the text (see Remarks).
		///   - If the window has no text, the return value is zero.
		///   - Function failure is indicated by a return value of zero and a GetLastError result that is nonzero.
		/// </returns>
		/// <remarks>
		///   <para>
		///     If the target window is owned by the current process, GetWindowTextLength causes a WM_GETTEXTLENGTH message
		///     to be sent to the specified window or control.
		///   </para><para>
		///     Under certain conditions, the GetWindowTextLength function may return a value that is larger than the actual 
		///     length of the text. This occurs with certain mixtures of ANSI and Unicode, and is due to the system allowing
		///     for the possible existence of double-byte character set (DBCS) characters within the text. The return value,
		///     however, will always be at least as large as the actual length of the text; you can thus always use it to
		///     guide buffer allocation. This behavior can occur when an application uses both ANSI functions and common
		///     dialogs, which use Unicode. It can also occur when an application uses the ANSI version of GetWindowTextLength
		///     with a window whose window procedure is Unicode, or the Unicode version of GetWindowTextLength with a window
		///     whose window procedure is ANSI. For more information on ANSI and ANSI functions, see Conventions for Function
		///     Prototypes.
		///   </para><para>
		///     To obtain the exact length of the text, use the WM_GETTEXT, LB_GETTEXT, or CB_GETLBTEXT messages,
		///     or the GetWindowText function.
		///   </para>
		///   https://www.pinvoke.net/default.aspx/user32/GetWindowTextLength.html <br/>
		///   https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtextlengtha
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int GetWindowTextLength(IntPtr hWnd);

		/// <summary>
		///   Determines the visibility state of the specified window.
		/// </summary>
		/// <param name="hWnd">A handle to the window to be tested.</param>
		/// <returns>
		///   If the specified window, its parent window, its parent's parent window, and so forth, have the
		///   WS_VISIBLE style, the return value is nonzero. Otherwise, the return value is zero. <br/>
		///   Because the return value specifies whether the window has the WS_VISIBLE style, it may be nonzero
		///   even if the window is totally obscured by other windows.
		/// </returns>
		/// <remarks>
		///   <para>
		///     The visibility state of a window is indicated by the WS_VISIBLE style bit. When WS_VISIBLE is set,
		///     the window is displayed and subsequent drawing into it is displayed as long as the window has the
		///     WS_VISIBLE style. 
		///   </para><para>
		///     Any drawing to a window with the WS_VISIBLE style will not be displayed if the window is obscured
		///     by other windows or is clipped by its parent window.
		///   </para>
		///   https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible
		/// </remarks>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowVisible(IntPtr hWnd);

		#endregion

		#region Console related

		/// <summary>
		///   Retrieves the window handle used by the console associated with the calling process.
		/// </summary>
		/// <returns>
		///   The return value is a handle to the window used by the console associated with the calling process
		///   or NULL if there is no such associated console.
		/// </returns>
		/// <remarks>
		///   <para>
		///	    For an application that is hosted inside a pseudoconsole session, this function returns a window
		///	    handle for message queue purposes only. The associated window is not displayed locally as the
		///	    pseudoconsole is serializing all actions to a stream for presentation on another terminal window
		///	    elsewhere.
		///   </para>
		///   <para>
		///     This API is not recommended and does not have a virtual terminal equivalent. This decision intentionally
		///     aligns the Windows platform with other operating systems. This state is only relevant to the local user,
		///     session, and privilege context. Applications remoting via cross-platform utilities and transports like
		///     SSH may not work as expected if using this API.
		///   </para>
		///   https://docs.microsoft.com/en-us/windows/console/getconsolewindow
		/// </remarks>
		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetConsoleWindow();

		// https://stackoverflow.com/a/28616832
		// https://docs.microsoft.com/en-us/windows/console/console-functions

		//[DllImport("kernel32.dll", SetLastError = true)]
		//private static extern bool AttachConsole(uint dwProcessId);

		//[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		//private static extern bool FreeConsole();

		//[DllImport("kernel32.dll")]
		//static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

		//delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);
		//enum CtrlTypes : uint {
		//	CTRL_C_EVENT = 0,
		//	CTRL_BREAK_EVENT,
		//	CTRL_CLOSE_EVENT,
		//	CTRL_LOGOFF_EVENT = 5,
		//	CTRL_SHUTDOWN_EVENT
		//}
		//bool is_attached=false;
		//ConsoleCtrlDelegate ConsoleCtrlDelegateDetach = delegate(CtrlType) {
		//	if (is_attached = !FreeConsole())
		//		Trace.Error('FreeConsole on ' + CtrlType + ': ' + new Win32Exception());
		//	return true;
		//};

		#endregion

		#region PInvoke enums

		/// <summary>
		///     Special window handles
		/// </summary>
		internal enum SpecialWindowHandles : int {
			/// <summary>
			///     Places the window at the top of the Z order.
			/// </summary>
			HWND_TOP = 0,
			/// <summary>
			///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
			/// </summary>
			HWND_BOTTOM = 1,
			/// <summary>
			///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
			/// </summary>
			HWND_TOPMOST = -1,
			/// <summary>
			///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
			/// </summary>
			HWND_NOTOPMOST = -2
		}

		/// <summary>
		/// Command options for User32 ShowWindow function
		/// </summary>
		/// <remarks>https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow</remarks>
		internal enum ShowWindowCommand : int {
			SW_HIDE = 0,
			/// <summary>
			/// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
			/// </summary>
			SW_SHOWNORMAL = 1,
			/// <inheritdoc cref="SW_SHOWNORMAL"/>
			SW_NORMAL = SW_SHOWNORMAL,
			/// <summary>
			/// Activates the window and displays it as a minimized window.
			/// </summary>
			SW_SHOWMINIMIZED = 2,
			/// <summary>
			/// Activates the window and displays it as a maximized window.
			/// </summary>
			SW_SHOWMAXIMIZED = 3,
			/// <inheritdoc cref="SW_SHOWMAXIMIZED"/>
			SW_MAXIMIZE = SW_SHOWMAXIMIZED,
			/// <summary>
			/// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
			/// </summary>
			SW_SHOWNOACTIVATE = 4,
			/// <summary>
			/// Activates the window and displays it in its current size and position.
			/// </summary>
			SW_SHOW = 5,
			/// <summary>
			/// Minimizes the specified window and activates the next top-level window in the Z order.
			/// </summary>
			SW_MINIMIZE = 6,
			/// <summary>
			/// Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
			/// </summary>
			SW_SHOWMINNOACTIVE = 7,
			/// <summary>
			/// Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
			/// </summary>
			SW_SHOWNA = 8,
			/// <summary>
			/// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
			/// </summary>
			SW_RESTORE = 9,
			/// <summary>
			/// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
			/// </summary>
			SW_SHOWDEFAULT = 10,
			/// <summary>
			/// Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
			/// </summary>
			SW_FORCEMINIMIZE = 11
		}

		[Flags]
		internal enum SetWindowPosFlags : uint {
			/// <summary>
			///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
			/// </summary>
			SWP_ASYNCWINDOWPOS = 0x4000,

			/// <summary>
			///     Prevents generation of the WM_SYNCPAINT message.
			/// </summary>
			SWP_DEFERERASE = 0x2000,

			/// <summary>
			///     Draws a frame (defined in the window's class description) around the window.
			/// </summary>
			SWP_DRAWFRAME = 0x0020,

			/// <summary>
			///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
			/// </summary>
			SWP_FRAMECHANGED = 0x0020,

			/// <summary>
			///     Hides the window.
			/// </summary>
			SWP_HIDEWINDOW = 0x0080,

			/// <summary>
			///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
			/// </summary>
			SWP_NOACTIVATE = 0x0010,

			/// <summary>
			///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
			/// </summary>
			SWP_NOCOPYBITS = 0x0100,

			/// <summary>
			///     Retains the current position (ignores X and Y parameters).
			/// </summary>
			SWP_NOMOVE = 0x0002,

			/// <summary>
			///     Does not change the owner window's position in the Z order.
			/// </summary>
			SWP_NOOWNERZORDER = 0x0200,

			/// <summary>
			///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
			/// </summary>
			SWP_NOREDRAW = 0x0008,

			/// <summary>
			///     Same as the SWP_NOOWNERZORDER flag.
			/// </summary>
			SWP_NOREPOSITION = 0x0200,

			/// <summary>
			///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
			/// </summary>
			SWP_NOSENDCHANGING = 0x0400,

			/// <summary>
			///     Retains the current size (ignores the cx and cy parameters).
			/// </summary>
			SWP_NOSIZE = 0x0001,

			/// <summary>
			///     Retains the current Z order (ignores the hWndInsertAfter parameter).
			/// </summary>
			SWP_NOZORDER = 0x0004,

			/// <summary>
			///     Displays the window.
			/// </summary>
			SWP_SHOWWINDOW = 0x0040,
		}

		// ReSharper restore InconsistentNaming
		#endregion
	}
}
