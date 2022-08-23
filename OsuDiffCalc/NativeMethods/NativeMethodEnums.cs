/*****************************************************************************
 * 
 * This file contains enums or flags used by native methods.
 * 
 * These are defined by Windows and documentation has been added if available.
 * Almost none of these had to be reverse engineered but they are not all complete.
 * 
 *****************************************************************************/
#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	// see https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke
	using HANDLE  = System.IntPtr;
	using PVOID   = System.IntPtr;
	using DWORD   = System.UInt32;
	using WORD    = System.UInt16;
	using ULONG   = System.UInt32;
	using UINT    = System.UInt32;
	using USHORT  = System.UInt16;
	using LONG    = System.Int32;
	using INT     = System.Int32;
	using SHORT   = System.Int16;
	using UCHAR   = System.Byte;
	using CHAR    = System.Byte;
	using CCHAR   = System.Byte;
	using BYTE    = System.Byte;
	using BOOL    = System.Boolean;
	using BOOLEAN = System.Byte; // true = 1, false = 0

	internal static partial class NativeMethods {
		const bool SetLastError
#if DEBUG
		= true;
#else
		= false;
#endif

		public enum NT_STATUS {
			STATUS_SUCCESS = 0x00000000,
			STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005L),
			STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004L)
		}

		[Flags]
		public enum DuplicateHandleOptions {
			DUPLICATE_CLOSE_SOURCE = 0x1,
			DUPLICATE_SAME_ACCESS  = 0x2
		}

		/// <summary>
		/// The ACCESS_MASK data type is a DWORD value that defines standard, specific, and generic rights.
		/// These rights are used in access control entries (ACEs) and are the primary means of specifying
		/// the requested or granted access to an object.
		/// </summary>
		/// <remarks> 
		/// The bits are allocated as follows:
		/// <code>
		/// 0-15  | Specific rights. Contains the access mask specific to the object type associated with the mask.
		/// 16-23 | Standard rights. Contains the object's standard access rights.
		/// 24    | Access system security (ACCESS_SYSTEM_SECURITY). It is used to indicate access to a system access control list (SACL).
		///         This type of access requires the calling process to have the SE_SECURITY_NAME (Manage auditing and security log) privilege.
		///         If this flag is set in the access mask of an audit access ACE (successful or unsuccessful access), the SACL access will be audited.
		/// 25    | Maximum allowed(MAXIMUM_ALLOWED)
		/// 26-27 | Reserved
		/// 28    | Generic all (GENERIC_ALL).
		/// 29    | Generic execute (GENERIC_EXECUTE).
		/// 30    | Generic write (GENERIC_WRITE).
		/// 31    | Generic read (GENERIC_READ).
		/// </code>
		/// Standard rights bits, 16-23, contain the object's standard access rights and can be a combination of the following flags:
		/// <code>
		/// 16 | DELETE: Delete access
		/// 17 | READ_CONTROL: Read access to the owner, group, and discretionary access control list (DACL) of the security descriptor.
		/// 18 | WRITE_DAC: Write access to the DACL.
		/// 19 | WRITE_OWNER: Write access to owner.
		/// 20 | SYNCHRONIZE: Sychronize access.
		/// </code>
		/// <br/> https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask
		/// </remarks>
		public enum ACCESS_MASK : DWORD {
			DELETE                   = 0x00010000,
			READ_CONTROL             = 0x00020000,
			WRITE_DAC                = 0x00040000,
			WRITE_OWNER              = 0x00080000,
			SYNCHRONIZE              = 0x00100000,
			STANDARD_RIGHTS_REQUIRED = 0x000F0000,
			STANDARD_RIGHTS_READ     = READ_CONTROL,
			STANDARD_RIGHTS_WRITE    = READ_CONTROL,
			STANDARD_RIGHTS_EXECUTE  = READ_CONTROL,
			STANDARD_RIGHTS_ALL      = 0x001F0000,
			SPECIFIC_RIGHTS_ALL      = 0x0000FFFF,
		}

		/// <summary> Standard access rights used by all objects. </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights </remarks>
		[Flags]
		public enum StandardAccessRights : DWORD {
			/// <summary> Required to delete the object. </summary>
			DELETE       = 0x00010000,
			/// <summary> 
			///   Required to read information in the security descriptor for the object, not including the information in the SACL.
			///   To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. <br/>
			///   For more information, see SACL Access Right.
			/// </summary>
			READ_CONTROL = 0x00020000,
			/// <summary> 
			///   The right to use the object for synchronization.
			///   This enables a thread to wait until the object is in the signaled state.
			/// </summary>
			SYNCHRONIZE  = 0x00100000,
			/// <summary> Required to modify the DACL in the security descriptor for the object. </summary>
			WRITE_DAC    = 0x00040000,
			/// <summary> Required to change the owner in the security descriptor for the object. </summary>
			WRITE_OWNER  = 0x00080000,
		}

		/// <summary> Process-specific access rights (superset of standard access rights) </summary>
		/// <remarks>
		///   <para>
		///     Windows Vista introduces protected processes to enhance support for Digital Rights Management.
		///     The system restricts access to protected processes and the threads of protected processes.
		///   </para>
		///   The following standard access rights are not allowed from a process to a protected process:
		///   <list type="bullet">
		///     <item> DELETE, READ_CONTROL, WRITE_DAC, WRITE_OWNER </item>
		///   </list>
		///   The following specific access rights are not allowed from a process to a protected process:
		///   <list type="bullet">
		///     <item> PROCESS_ALL_ACCESS </item>
		///     <item> PROCESS_CREATE_PROCESS </item>
		///     <item> PROCESS_CREATE_THREAD </item>
		///     <item> PROCESS_DUP_HANDLE </item>
		///     <item> PROCESS_QUERY_INFORMATION </item>
		///     <item> PROCESS_SET_INFORMATION </item>
		///     <item> PROCESS_SET_QUOTA </item>
		///     <item> PROCESS_VM_OPERATION </item>
		///     <item> PROCESS_VM_READ </item>
		///     <item> PROCESS_VM_WRITE </item>
		///   </list>
		///   The PROCESS_QUERY_LIMITED_INFORMATION right was introduced to provide access to a subset of the
		///   information available through PROCESS_QUERY_INFORMATION. <br/>
		///   <br/> See https://docs.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights 
		/// </remarks>
		[Flags]
		public enum ProcessAccessRights : DWORD {
			/// <summary> Required to use this process as the parent process with PROC_THREAD_ATTRIBUTE_PARENT_PROCESS. </summary>
			PROCESS_CREATE_PROCESS = 0x0080,
			/// <summary> Required to create a thread in the process. </summary>
			PROCESS_CREATE_THREAD = 0x0002,
			/// <summary> Required to duplicate a handle using DuplicateHandle. </summary>
			PROCESS_DUP_HANDLE = 0x0040,
			/// <summary> Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken). </summary>
			PROCESS_QUERY_INFORMATION = 0x0400,
			/// <summary> 
			///   Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName).
			///   A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION. <br/>
			///   Windows Server 2003 and Windows XP: This access right is not supported.
			/// </summary>
			PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
			/// <summary> Required to set certain information about a process, such as its priority class (see SetPriorityClass). </summary>
			PROCESS_SET_INFORMATION = 0x0200,
			/// <summary> Required to set memory limits using SetProcessWorkingSetSize. </summary>
			PROCESS_SET_QUOTA = 0x0100,
			/// <summary> Required to suspend or resume a process. </summary>
			PROCESS_SUSPEND_RESUME = 0x0800,
			/// <summary> Required to terminate a process using TerminateProcess. </summary>
			PROCESS_TERMINATE = 0x0001,
			/// <summary> Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory). </summary>
			PROCESS_VM_OPERATION = 0x0008,
			/// <summary> Required to read memory in a process using ReadProcessMemory. </summary>
			PROCESS_VM_READ = 0x0010,
			/// <summary> Required to write to memory in a process using WriteProcessMemory. </summary>
			PROCESS_VM_WRITE = 0x0020,
			/// <summary> Required to wait for the process to terminate using the wait functions. </summary>
			SYNCHRONIZE = 0x00100000,
			/// <summary>
			///   All possible access rights for a process object. <br/>
			///   Windows Server 2003 and Windows XP: The size of the PROCESS_ALL_ACCESS flag increased on Windows Server 2008 and Windows Vista.
			///   If an application compiled for Windows Server 2008 and Windows Vista is run on Windows Server 2003 or Windows XP, the
			///   PROCESS_ALL_ACCESS flag is too large and the function specifying this flag fails with ERROR_ACCESS_DENIED.
			///   To avoid this problem, specify the minimum set of access rights required for the operation. If PROCESS_ALL_ACCESS must be used,
			///   set _WIN32_WINNT to the minimum operating system targeted by your application (for example, #define _WIN32_WINNT _WIN32_WINNT_WINXP). <br/>
			///   For more information, see Using the Windows Headers. 
			/// </summary>
			PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE,
			/// <summary>
			/// ???
			/// </summary>
			STANDARD_RIGHTS_REQUIRED = 0x000F0000,
			// ----------------------------
			// The following are inherited from the standard object access rights
			// ----------------------------
			/// <summary> Required to delete the object. </summary>
			DELETE = 0x00010000,
			/// <summary> 
			///   Required to read information in the security descriptor for the object, not including the information in the SACL.
			///   To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. <br/>
			///   For more information, see SACL Access Right.
			/// </summary>
			READ_CONTROL = 0x00020000,
			/// <summary> Required to modify the DACL in the security descriptor for the object. </summary>
			WRITE_DAC = 0x00040000,
			/// <summary> Required to change the owner in the security descriptor for the object. </summary>
			WRITE_OWNER = 0x00080000,
		}


		#region Window show / position enums

		/// <summary>
		/// Option for LockSetForegroundWindow
		/// </summary>
		public enum LSFWCode : uint {
			/// <summary> Disables calls to SetForegroundWindow </summary>
			LSFW_LOCK = 1u,
			/// <summary> Enables calls to SetForegroundWindow </summary>
			LSFW_UNLOCK = 2u
		}

		/// <summary>
		/// Options for SetWindowPos
		/// </summary>
		public enum SpecialWindowHandles : int {
			/// <summary>
			/// Places the window at the top of the Z order.
			/// </summary>
			HWND_TOP = 0,
			/// <summary>
			/// Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
			/// </summary>
			HWND_BOTTOM = 1,
			/// <summary>
			/// Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
			/// </summary>
			HWND_TOPMOST = -1,
			/// <summary>
			/// Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
			/// </summary>
			HWND_NOTOPMOST = -2
		}

		/// <summary>
		/// Command options for User32 ShowWindow function
		/// </summary>
		/// <remarks>https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow</remarks>
		public enum ShowWindowCommand : int {
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
		public enum SetWindowPosFlags : DWORD {
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

		/// <summary>
		/// Options used by the DwmGetWindowAttribute and DwmSetWindowAttribute functions.
		/// </summary>
		/// <remarks>
		/// https://docs.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
		/// </remarks>
		[Flags]
		public enum DwmWindowAttribute : DWORD {
			/// <summary>
			/// Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL.
			/// TRUE if non-client rendering is enabled; otherwise, FALSE.
			/// </summary>
			DWMWA_NCRENDERING_ENABLED,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value
			/// from the DWMNCRENDERINGPOLICY enumeration.
			/// </summary>
			DWMWA_NCRENDERING_POLICY,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a
			/// value of type BOOL. TRUE to disable transitions, or FALSE to enable transitions.
			/// </summary>
			DWMWA_TRANSITIONS_FORCEDISABLED,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by DWM.
			/// The pvAttribute parameter points to a value of type BOOL. TRUE to enable content rendered in the non-client area to be
			/// visible on the frame; otherwise, FALSE.
			/// </summary>
			DWMWA_ALLOW_NCPAINT,
			/// <summary>
			/// Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space.
			/// The retrieved value is of type RECT. If the window is minimized or otherwise not visible to the user, then the value
			/// of the RECT retrieved is undefined. You should check whether the retrieved RECT contains a boundary that you can work
			/// with, and if it doesn't then you can conclude that the window is minimized or otherwise not visible.
			/// </summary>
			DWMWA_CAPTION_BUTTON_BOUNDS,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute
			/// parameter points to a value of type BOOL. TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, FALSE.
			/// </summary>
			DWMWA_NONCLIENT_RTL_LAYOUT,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static bitmap),
			/// even if a live or snapshot representation of the window is available. This value is normally set during a window's creation,
			/// and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time.
			/// The pvAttribute parameter points to a value of type BOOL. TRUE to require a iconic thumbnail or peek representation;
			/// otherwise, FALSE.
			/// </summary>
			DWMWA_FORCE_ICONIC_REPRESENTATION,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value
			/// from the DWMFLIP3DWINDOWPOLICY enumeration.
			/// </summary>
			DWMWA_FLIP3D_POLICY,
			/// <summary>
			/// Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of type RECT.
			/// </summary>
			DWMWA_EXTENDED_FRAME_BOUNDS,
			/// <summary>
			/// Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek representation
			/// (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with DWMWA_FORCE_ICONIC_REPRESENTATION.
			/// DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not changed throughout the window's lifetime.
			/// Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL.
			/// TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, FALSE. <br/>
			/// Windows Vista and earlier: This value is not supported.
			/// </summary>
			DWMWA_HAS_ICONIC_BITMAP,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview of the window
			/// when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over
			/// the window's thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter
			/// points to a value of type BOOL. TRUE to prevent peek functionality, or FALSE to allow it. <br/>
			/// Windows Vista and earlier: This value is not supported.
			/// </summary>
			DWMWA_DISALLOW_PEEK,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute parameter
			/// points to a value of type BOOL. TRUE to prevent the window from fading during another window's peek, or FALSE for normal behavior. <br/>
			/// Windows Vista and earlier: This value is not supported.
			/// </summary>
			DWMWA_EXCLUDED_FROM_PEEK,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM. <br/>
			/// Using with DirectComposition: Use the DWMWA_CLOAK flag to cloak the layered child window when animating a representation
			/// of the window's content via a DirectComposition visual that has been associated with the layered child window. For more details
			/// on this usage case, see How to animate the bitmap of a layered child window. <br/>
			/// Windows 7 and earlier: This value is not supported.
			/// </summary>
			DWMWA_CLOAK,
			/// <summary>
			/// Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why.
			/// <code>
			/// DWM_CLOAKED_APP       (0x0000001). The window was cloaked by its owner application.
			/// DWM_CLOAKED_SHELL     (0x0000002). The window was cloaked by the Shell.
			/// DWM_CLOAKED_INHERITED (0x0000004). The cloak value was inherited from its owner window.
			/// </code>
			/// Windows 7 and earlier: This value is not supported.
			/// </summary>
			DWMWA_CLOAKED,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates
			/// on the thumbnail image to match the window's contents. <br/>
			/// Windows 7 and earlier: This value is not supported.
			/// </summary>
			DWMWA_FREEZE_REPRESENTATION,
			/// <summary>
			/// 
			/// </summary>
			DWMWA_PASSIVE_UPDATE_MODE,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes. If this flag is set, then a Win32
			/// app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush
			/// (see Compositor.CreateHostBackdropBrush). The pvAttribute parameter points to a value of type BOOL. TRUE to enable host
			/// backdrop brushes for the window, or FALSE to disable it. <br/>
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_USE_HOSTBACKDROPBRUSH,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode
			/// system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting.
			/// The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. <br/>
			/// This value is supported starting with Windows 11 Build 22000, or Windows 10 Build 18985.
			/// </summary>
			DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
			/// <summary>
			/// USE_IMMERSIVE_DARK_MODE, supported starting with Windows 10 17763, before Windows 10 18985 (update 20H1). <br/>
			/// See <see cref="DWMWA_USE_IMMERSIVE_DARK_MODE"/>
			/// </summary>
			DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window.
			/// The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE. <br/>
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_WINDOW_CORNER_PREFERENCE = 33,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF.
			/// The app is responsible for changing the border color according to state changes, such as a change in window activation. <br/>
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_BORDER_COLOR,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF. <br/>
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_CAPTION_COLOR,
			/// <summary>
			/// Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF. <br/>
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_TEXT_COLOR,
			/// <summary>
			/// Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window.
			/// The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT.
			/// This value is supported starting with Windows 11 Build 22000.
			/// </summary>
			DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
			/// <summary>
			/// IMPORTANT. This value is available in pre-release versions of the Windows Insider Preview. <br/>
			/// Use with DwmGetWindowAttribute or DwmSetWindowAttribute. Retrieves or specifies the system-drawn backdrop material
			/// of a window, including behind the non-client area. The pvAttribute parameter points to a value of type DWM_SYSTEMBACKDROP_TYPE. <br/>
			/// This value is supported starting with Windows 11 Build 22621.
			/// </summary>
			DWMWA_SYSTEMBACKDROP_TYPE,
			/// <summary>
			/// The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
			/// </summary>
			DWMWA_LAST
		}

		#endregion

		/// <summary>
		/// Undocumented win32 enum used by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/class.htm?tx=136 </remarks>
		public enum OBJECT_INFORMATION_CLASS {
			ObjectBasicInformation         = 0,
			ObjectNameInformation          = 1,
			ObjectTypeInformation          = 2,
			ObjectAllTypesInformation      = 3,
			ObjectHandleFlagInformation    = 4,
			ObjectSessionInformation       = 5,
			ObjectSessionObjectInformation = 6,
			MaxObjectInfoClass             = 7,
		}

		/// <summary>
		/// From ntdll, documentation is hard to come by
		/// <br/> http://www.pinvoke.net/default.aspx/ntdll/SYSTEM_INFORMATION_CLASS.html
		/// </summary>
		public enum SYSTEM_INFORMATION_CLASS {
			SystemBasicInformation                                = 0x00,
			SystemProcessorInformation                            = 0x01,
			SystemPerformanceInformation                          = 0x02,
			SystemTimeOfDayInformation                            = 0x03,
			SystemPathInformation                                 = 0x04,
			SystemProcessInformation                              = 0x05,
			SystemCallCountInformation                            = 0x06,
			SystemDeviceInformation                               = 0x07,
			SystemProcessorPerformanceInformation                 = 0x08,
			SystemFlagsInformation                                = 0x09,
			SystemCallTimeInformation                             = 0x0A,
			SystemModuleInformation                               = 0x0B,
			SystemLocksInformation                                = 0x0C,
			SystemStackTraceInformation                           = 0x0D,
			SystemPagedPoolInformation                            = 0x0E,
			SystemNonPagedPoolInformation                         = 0x0F,
			SystemHandleInformation                               = 0x10,
			SystemObjectInformation                               = 0x11,
			SystemPageFileInformation                             = 0x12,
			SystemVdmInstemulInformation                          = 0x13,
			SystemVdmBopInformation                               = 0x14,
			SystemFileCacheInformation                            = 0x15,
			SystemPoolTagInformation                              = 0x16,
			SystemInterruptInformation                            = 0x17,
			SystemDpcBehaviorInformation                          = 0x18,
			SystemFullMemoryInformation                           = 0x19,
			SystemLoadGdiDriverInformation                        = 0x1A,
			SystemUnloadGdiDriverInformation                      = 0x1B,
			SystemTimeAdjustmentInformation                       = 0x1C,
			SystemSummaryMemoryInformation                        = 0x1D,
			SystemMirrorMemoryInformation                         = 0x1E,
			SystemPerformanceTraceInformation                     = 0x1F,
			SystemObsolete0                                       = 0x20,
			SystemExceptionInformation                            = 0x21,
			SystemCrashDumpStateInformation                       = 0x22,
			SystemKernelDebuggerInformation                       = 0x23,
			SystemContextSwitchInformation                        = 0x24,
			SystemRegistryQuotaInformation                        = 0x25,
			SystemExtendServiceTableInformation                   = 0x26,
			SystemPrioritySeperation                              = 0x27,
			SystemVerifierAddDriverInformation                    = 0x28,
			SystemVerifierRemoveDriverInformation                 = 0x29,
			SystemProcessorIdleInformation                        = 0x2A,
			SystemLegacyDriverInformation                         = 0x2B,
			SystemCurrentTimeZoneInformation                      = 0x2C,
			SystemLookasideInformation                            = 0x2D,
			SystemTimeSlipNotification                            = 0x2E,
			SystemSessionCreate                                   = 0x2F,
			SystemSessionDetach                                   = 0x30,
			SystemSessionInformation                              = 0x31,
			SystemRangeStartInformation                           = 0x32,
			SystemVerifierInformation                             = 0x33,
			SystemVerifierThunkExtend                             = 0x34,
			SystemSessionProcessInformation                       = 0x35,
			SystemLoadGdiDriverInSystemSpace                      = 0x36,
			SystemNumaProcessorMap                                = 0x37,
			SystemPrefetcherInformation                           = 0x38,
			SystemExtendedProcessInformation                      = 0x39,
			SystemRecommendedSharedDataAlignment                  = 0x3A,
			SystemComPlusPackage                                  = 0x3B,
			SystemNumaAvailableMemory                             = 0x3C,
			SystemProcessorPowerInformation                       = 0x3D,
			SystemEmulationBasicInformation                       = 0x3E,
			SystemEmulationProcessorInformation                   = 0x3F,
			SystemExtendedHandleInformation                       = 0x40,
			SystemLostDelayedWriteInformation                     = 0x41,
			SystemBigPoolInformation                              = 0x42,
			SystemSessionPoolTagInformation                       = 0x43,
			SystemSessionMappedViewInformation                    = 0x44,
			SystemHotpatchInformation                             = 0x45,
			SystemObjectSecurityMode                              = 0x46,
			SystemWatchdogTimerHandler                            = 0x47,
			SystemWatchdogTimerInformation                        = 0x48,
			SystemLogicalProcessorInformation                     = 0x49,
			SystemWow64SharedInformationObsolete                  = 0x4A,
			SystemRegisterFirmwareTableInformationHandler         = 0x4B,
			SystemFirmwareTableInformation                        = 0x4C,
			SystemModuleInformationEx                             = 0x4D,
			SystemVerifierTriageInformation                       = 0x4E,
			SystemSuperfetchInformation                           = 0x4F,
			SystemMemoryListInformation                           = 0x50,
			SystemFileCacheInformationEx                          = 0x51,
			SystemThreadPriorityClientIdInformation               = 0x52,
			SystemProcessorIdleCycleTimeInformation               = 0x53,
			SystemVerifierCancellationInformation                 = 0x54,
			SystemProcessorPowerInformationEx                     = 0x55,
			SystemRefTraceInformation                             = 0x56,
			SystemSpecialPoolInformation                          = 0x57,
			SystemProcessIdInformation                            = 0x58,
			SystemErrorPortInformation                            = 0x59,
			SystemBootEnvironmentInformation                      = 0x5A,
			SystemHypervisorInformation                           = 0x5B,
			SystemVerifierInformationEx                           = 0x5C,
			SystemTimeZoneInformation                             = 0x5D,
			SystemImageFileExecutionOptionsInformation            = 0x5E,
			SystemCoverageInformation                             = 0x5F,
			SystemPrefetchPatchInformation                        = 0x60,
			SystemVerifierFaultsInformation                       = 0x61,
			SystemSystemPartitionInformation                      = 0x62,
			SystemSystemDiskInformation                           = 0x63,
			SystemProcessorPerformanceDistribution                = 0x64,
			SystemNumaProximityNodeInformation                    = 0x65,
			SystemDynamicTimeZoneInformation                      = 0x66,
			SystemCodeIntegrityInformation                        = 0x67,
			SystemProcessorMicrocodeUpdateInformation             = 0x68,
			SystemProcessorBrandString                            = 0x69,
			SystemVirtualAddressInformation                       = 0x6A,
			SystemLogicalProcessorAndGroupInformation             = 0x6B,
			SystemProcessorCycleTimeInformation                   = 0x6C,
			SystemStoreInformation                                = 0x6D,
			SystemRegistryAppendString                            = 0x6E,
			SystemAitSamplingValue                                = 0x6F,
			SystemVhdBootInformation                              = 0x70,
			SystemCpuQuotaInformation                             = 0x71,
			SystemNativeBasicInformation                          = 0x72,
			SystemErrorPortTimeouts                               = 0x73,
			SystemLowPriorityIoInformation                        = 0x74,
			SystemBootEntropyInformation                          = 0x75,
			SystemVerifierCountersInformation                     = 0x76,
			SystemPagedPoolInformationEx                          = 0x77,
			SystemSystemPtesInformationEx                         = 0x78,
			SystemNodeDistanceInformation                         = 0x79,
			SystemAcpiAuditInformation                            = 0x7A,
			SystemBasicPerformanceInformation                     = 0x7B,
			SystemQueryPerformanceCounterInformation              = 0x7C,
			SystemSessionBigPoolInformation                       = 0x7D,
			SystemBootGraphicsInformation                         = 0x7E,
			SystemScrubPhysicalMemoryInformation                  = 0x7F,
			SystemBadPageInformation                              = 0x80,
			SystemProcessorProfileControlArea                     = 0x81,
			SystemCombinePhysicalMemoryInformation                = 0x82,
			SystemEntropyInterruptTimingInformation               = 0x83,
			SystemConsoleInformation                              = 0x84,
			SystemPlatformBinaryInformation                       = 0x85,
			SystemPolicyInformation                               = 0x86,
			SystemHypervisorProcessorCountInformation             = 0x87,
			SystemDeviceDataInformation                           = 0x88,
			SystemDeviceDataEnumerationInformation                = 0x89,
			SystemMemoryTopologyInformation                       = 0x8A,
			SystemMemoryChannelInformation                        = 0x8B,
			SystemBootLogoInformation                             = 0x8C,
			SystemProcessorPerformanceInformationEx               = 0x8D,
			SystemCriticalProcessErrorLogInformation              = 0x8E,
			SystemSecureBootPolicyInformation                     = 0x8F,
			SystemPageFileInformationEx                           = 0x90,
			SystemSecureBootInformation                           = 0x91,
			SystemEntropyInterruptTimingRawInformation            = 0x92,
			SystemPortableWorkspaceEfiLauncherInformation         = 0x93,
			SystemFullProcessInformation                          = 0x94,
			SystemKernelDebuggerInformationEx                     = 0x95,
			SystemBootMetadataInformation                         = 0x96,
			SystemSoftRebootInformation                           = 0x97,
			SystemElamCertificateInformation                      = 0x98,
			SystemOfflineDumpConfigInformation                    = 0x99,
			SystemProcessorFeaturesInformation                    = 0x9A,
			SystemRegistryReconciliationInformation               = 0x9B,
			SystemEdidInformation                                 = 0x9C,
			SystemManufacturingInformation                        = 0x9D,
			SystemEnergyEstimationConfigInformation               = 0x9E,
			SystemHypervisorDetailInformation                     = 0x9F,
			SystemProcessorCycleStatsInformation                  = 0xA0,
			SystemVmGenerationCountInformation                    = 0xA1,
			SystemTrustedPlatformModuleInformation                = 0xA2,
			SystemKernelDebuggerFlags                             = 0xA3,
			SystemCodeIntegrityPolicyInformation                  = 0xA4,
			SystemIsolatedUserModeInformation                     = 0xA5,
			SystemHardwareSecurityTestInterfaceResultsInformation = 0xA6,
			SystemSingleModuleInformation                         = 0xA7,
			SystemAllowedCpuSetsInformation                       = 0xA8,
			SystemDmaProtectionInformation                        = 0xA9,
			SystemInterruptCpuSetsInformation                     = 0xAA,
			SystemSecureBootPolicyFullInformation                 = 0xAB,
			SystemCodeIntegrityPolicyFullInformation              = 0xAC,
			SystemAffinitizedInterruptProcessorInformation        = 0xAD,
			SystemRootSiloInformation                             = 0xAE,
			SystemCpuSetInformation                               = 0xAF,
			SystemCpuSetTagInformation                            = 0xB0,
			SystemWin32WerStartCallout                            = 0xB1,
			SystemSecureKernelProfileInformation                  = 0xB2,
			SystemCodeIntegrityPlatformManifestInformation        = 0xB3,
			SystemInterruptSteeringInformation                    = 0xB4,
			SystemSuppportedProcessorArchitectures                = 0xB5,
			SystemMemoryUsageInformation                          = 0xB6,
			SystemCodeIntegrityCertificateInformation             = 0xB7,
			SystemPhysicalMemoryInformation                       = 0xB8,
			SystemControlFlowTransition                           = 0xB9,
			SystemKernelDebuggingAllowed                          = 0xBA,
			SystemActivityModerationExeState                      = 0xBB,
			SystemActivityModerationUserSettings                  = 0xBC,
			SystemCodeIntegrityPoliciesFullInformation            = 0xBD,
			SystemCodeIntegrityUnlockInformation                  = 0xBE,
			SystemIntegrityQuotaInformation                       = 0xBF,
			SystemFlushInformation                                = 0xC0,
			SystemProcessorIdleMaskInformation                    = 0xC1,
			SystemSecureDumpEncryptionInformation                 = 0xC2,
			SystemWriteConstraintInformation                      = 0xC3,
			SystemKernelVaShadowInformation                       = 0xC4,
			SystemHypervisorSharedPageInformation                 = 0xC5,
			SystemFirmwareBootPerformanceInformation              = 0xC6,
			SystemCodeIntegrityVerificationInformation            = 0xC7,
			SystemFirmwarePartitionInformation                    = 0xC8,
			SystemSpeculationControlInformation                   = 0xC9,
			SystemDmaGuardPolicyInformation                       = 0xCA,
			SystemEnclaveLaunchControlInformation                 = 0xCB,
			SystemWorkloadAllowedCpuSetsInformation               = 0xCC,
			SystemCodeIntegrityUnlockModeInformation              = 0xCD,
			SystemLeapSecondInformation                           = 0xCE,
			SystemFlags2Information                               = 0xCF,
			SystemSecurityModelInformation                        = 0xD0,
			SystemCodeIntegritySyntheticCacheInformation          = 0xD1,
			MaxSystemInfoClass                                    = 0xD2
		}

		/// <summary>
		/// Flags used by the SetWinEventHook function
		/// </summary>
		/// <remarks>
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/winauto/in-context-and-out-of-context-hook-functions
		/// </remarks>
		[Flags]
		public enum WinEventHookFlags : DWORD {
			/// <summary>
			/// The callback function is not mapped into the address space of the process that generates the event.
			/// Because the hook function is called across process boundaries, the system must queue events. Although
			/// this method is asynchronous, events are guaranteed to be in sequential order. For more information,
			/// see Out-of-Context Hook Functions. 
			/// </summary>
			OutOfContext   = 0x0000, // Events are ASYNC
			/// <summary>
			/// Prevents this instance of the hook from receiving the events that are generated by the thread
			/// that is registering this hook. 
			/// </summary>
			SkipOwnThread  = 0x0001, // Don't call back for events on installer's thread
			/// <summary>
			/// Prevents this instance of the hook from receiving the events that are generated by threads in this process.
			/// This flag does not prevent threads from generating events.
			/// </summary>
			SkipOwnProcess = 0x0002, // Don't call back for events on installer's process
			/// <summary>
			/// The DLL that contains the callback function is mapped into the address space of the process that generates
			/// the event. With this flag, the system sends event notifications to the callback function as they occur.
			/// The hook function must be in a DLL when this flag is specified. This flag has no effect when both the
			/// calling process and the generating process are not 32-bit or 64-bit processes, or when the generating
			/// process is a console application. For more information, see In-Context Hook Functions.
			/// </summary>
			InContext      = 0x0004
		}

		/// <summary>
		/// Event constants that are generated by the Windows operating system.
		/// </summary>
		/// <remarks>
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/winauto/event-constants
		/// <br/> See https://pub.dev/documentation/win32hooks/latest/const.win/EVENT_UIA_EVENTID_START-constant.html
		/// </remarks>
		public enum EventConstant : DWORD {
			EVENT_AIA_START                               = 0xA000,
			EVENT_AIA_END                                 = 0xAFFF,
			EVENT_MIN                                     = 0x00000001,
			EVENT_MAX                                     = 0x7FFFFFFF,
			EVENT_OBJECT_ACCELERATORCHANGE                = 0x8012,
			EVENT_OBJECT_CLOAKED                          = 0x8017,
			EVENT_OBJECT_CONTENTSCROLLED                  = 0x8015,
			EVENT_OBJECT_CREATE                           = 0x8000,
			EVENT_OBJECT_DEFACTIONCHANGE                  = 0x8011,
			EVENT_OBJECT_DESCRIPTIONCHANGE                = 0x800D,
			EVENT_OBJECT_DESTROY                          = 0x8001,
			EVENT_OBJECT_DRAGSTART                        = 0x8021,
			EVENT_OBJECT_DRAGCANCEL                       = 0x8022,
			EVENT_OBJECT_DRAGCOMPLETE                     = 0x8023,
			EVENT_OBJECT_DRAGENTER                        = 0x8024,
			EVENT_OBJECT_DRAGLEAVE                        = 0x8025,
			EVENT_OBJECT_DRAGDROPPED                      = 0x8026,
			EVENT_OBJECT_END                              = 0x80FF,
			EVENT_OBJECT_FOCUS                            = 0x8005,
			EVENT_OBJECT_HELPCHANGE                       = 0x8010,
			EVENT_OBJECT_HIDE                             = 0x8003,
			EVENT_OBJECT_HOSTEDOBJECTSINVALIDATED         = 0x8020,
			EVENT_OBJECT_IME_HIDE                         = 0x8028,
			EVENT_OBJECT_IME_SHOW                         = 0x8027,
			EVENT_OBJECT_IME_CHANGE                       = 0x8029,
			EVENT_OBJECT_INVOKED                          = 0x8013,
			EVENT_OBJECT_LIVEREGIONCHANGED                = 0x8019,
			EVENT_OBJECT_LOCATIONCHANGE                   = 0x800B,
			EVENT_OBJECT_NAMECHANGE                       = 0x800C,
			EVENT_OBJECT_PARENTCHANGE                     = 0x800F,
			EVENT_OBJECT_REORDER                          = 0x8004,
			EVENT_OBJECT_SELECTION                        = 0x8006,
			EVENT_OBJECT_SELECTIONADD                     = 0x8007,
			EVENT_OBJECT_SELECTIONREMOVE                  = 0x8008,
			EVENT_OBJECT_SELECTIONWITHIN                  = 0x8009,
			EVENT_OBJECT_SHOW                             = 0x8002,
			EVENT_OBJECT_STATECHANGE                      = 0x800A,
			EVENT_OBJECT_TEXTEDIT_CONVERSIONTARGETCHANGED = 0x8030,
			EVENT_OBJECT_TEXTSELECTIONCHANGED             = 0x8014,
			EVENT_OBJECT_UNCLOAKED                        = 0x8018,
			EVENT_OBJECT_VALUECHANGE                      = 0x800E,
			EVENT_OEM_DEFINED_START                       = 0x0101,
			EVENT_OEM_DEFINED_END                         = 0x01FF,
			EVENT_SYSTEM_ALERT                            = 0x0002,
			EVENT_SYSTEM_ARRANGMENTPREVIEW                = 0x8016,
			EVENT_SYSTEM_CAPTUREEND                       = 0x0009,
			EVENT_SYSTEM_CAPTURESTART                     = 0x0008,
			EVENT_SYSTEM_CONTEXTHELPSTART                 = 0x000C,
			EVENT_SYSTEM_DESKTOPSWITCH                    = 0x0020,
			EVENT_SYSTEM_DIALOGEND                        = 0x0011,
			EVENT_SYSTEM_DIALOGSTART                      = 0x0010,
			EVENT_SYSTEM_DRAGDROPEND                      = 0x000F,
			EVENT_SYSTEM_DRAGDROPSTART                    = 0x000E,
			EVENT_SYSTEM_END                              = 0x00FF,
			EVENT_SYSTEM_FOREGROUND                       = 0x0003,
			EVENT_SYSTEM_MENUPOPUPEND                     = 0x0007,
			EVENT_SYSTEM_MENUPOPUPSTART                   = 0x0006,
			EVENT_SYSTEM_MENUEND                          = 0x0005,
			EVENT_SYSTEM_MENUSTART                        = 0x0004,
			EVENT_SYSTEM_MINIMIZEEND                      = 0x0017,
			EVENT_SYSTEM_MINIMIZESTART                    = 0x0016,
			EVENT_SYSTEM_MOVESIZEEND                      = 0x000B,
			EVENT_SYSTEM_MOVESIZESTART                    = 0x000A,
			EVENT_SYSTEM_SCROLLINGEND                     = 0x0013,
			EVENT_SYSTEM_SOUND                            = 0x0001,
			EVENT_SYSTEM_SWITCHEND                        = 0x0015,
			EVENT_SYSTEM_SWITCHSTART                      = 0x0014,
			EVENT_UIA_EVENTID_START                       = 0x4E00,
			EVENT_UIA_EVENTID_END                         = 0x4EFF,
			EVENT_UIA_PROPID_START                        = 0x7500,
			EVENT_UIA_PROPID_END                          = 0x75FF,
		}
	}
}
