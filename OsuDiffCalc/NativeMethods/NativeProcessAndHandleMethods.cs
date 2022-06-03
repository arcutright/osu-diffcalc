/*****************************************************************************
 * 
 * This file contains native methods used when dealing with processes or handles
 * 
 * These are defined by Windows and documentation has been added if available.
 * Several structs were reverse engineered by myself or others because
 * Microsoft does not document everything in the Winows kernel.
 * 
 *****************************************************************************/
#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Runtime.ConstrainedExecution;
	using System.Security;
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

		/// <summary>
		/// [NtQuerySystemInformation may be altered or unavailable in future versions of Windows.
		/// Applications should use the alternate functions listed in this topic.] 
		/// </summary>
		/// <param name="SystemInformationClass">The kind of system information to be retrieved</param>
		/// <param name="SystemInformation">
		///   (void ptr) A pointer to a buffer that receives the requested information. The size and structure of this
		///   information varies depending on the value of the SystemInformationClass parameter: <br/>
		///   SYSTEM_BASIC_INFORMATION, SYSTEM_CODEINTEGRITY_INFORMATION, SYSTEM_EXCEPTION_INFORMATION, SYSTEM_INTERRUPT_INFORMATION,
		///   SYSTEM_KERNEL_VA_SHADOW_INFORMATION, SYSTEM_LEAP_SECOND_INFORMATION, ...
		/// </param>
		/// <param name="SystemInformationLength">The size of the buffer pointed to by the SystemInformation parameter, in bytes. </param>
		/// <param name="ReturnLength">
		///   An optional pointer to a location where the function writes the actual size of the information requested.
		///   If that size is less than or equal to the SystemInformationLength parameter, the function copies the information
		///   into the SystemInformation buffer; otherwise, it returns an NTSTATUS error code and returns in ReturnLength the
		///   size of buffer required to receive the requested information.
		/// </param>
		/// <returns> An NTSTATUS success or error code </returns>
		/// <remarks>
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation
		///   <br/> http://www.pinvoke.net/default.aspx/ntdll/NtQuerySystemInformation.html
		/// </remarks>
		[DllImport("ntdll.dll", SetLastError = SetLastError)]
		public static extern NT_STATUS NtQuerySystemInformation(
			[In] SYSTEM_INFORMATION_CLASS SystemInformationClass,
			[In, Out] IntPtr SystemInformation,
			[In] int SystemInformationLength,
			[Out] out int ReturnLength
		);

		/// <summary>
		///   [This function may be changed or removed from Windows without further notice.] <br/>
		///   Retrieves various kinds of object information.
		/// </summary>
		/// <param name="Handle">The handle of the object for which information is being queried.</param>
		/// <param name="ObjectInformationClass">Indicates the kind of object information to be retrieved.</param>
		/// <param name="ObjectInformation">
		///   An optional pointer to a buffer where the requested information is to be returned. The size and
		///   structure of this information varies depending on the value of the ObjectInformationClass parameter.
		/// </param>
		/// <param name="ObjectInformationLength">
		///	  The size of the buffer pointed to by the ObjectInformation parameter, in bytes.
		/// </param>
		/// <param name="ReturnLength">
		///   An optional pointer to a location where the function writes the actual size of the information requested.
		///   If that size is less than or equal to the ObjectInformationLength parameter, the function copies the
		///   information into the ObjectInformation buffer; otherwise, it returns an NTSTATUS error code and returns
		///   in ReturnLength the size of the buffer required to receive the requested information.
		/// </param>
		/// <returns>An NTSTATUS or error code.</returns>
		/// <remarks>
		///   This function has no associated header file or import library. You must use the LoadLibrary or
		///   GetProcAddress function to dynamically link to Ntdll.dll. <br/>
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryobject
		/// </remarks>
		[DllImport("ntdll.dll", SetLastError = SetLastError)]
		public static extern NT_STATUS NtQueryObject(
			[In] HANDLE Handle,
			[In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
			[Out] PVOID ObjectInformation,
			[In] int ObjectInformationLength, // ULONG
			[Out] out int ReturnLength // PULONG, optional
		);

		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		public static extern int QueryDosDevice(
			[In] string lpDeviceName,
			[Out] StringBuilder lpTargetPath,
			[In] int ucchMax
		);

		#region Process related methods

		[DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
		public static extern HANDLE GetCurrentProcess();

		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		public static extern int GetProcessId([In] HANDLE Process);

		/// <summary>
		/// Retrieves the thread identifier of the calling thread. <br />
		/// Alternative managed code: <c>Process.GetCurrentProcess().Threads[0].Id;</c>
		/// </summary>
		/// <remarks>
		/// Until the thread terminates, the thread identifier uniquely identifies the thread throughout the system. <br/>
		/// https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadid
		/// </remarks>
		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		public static extern uint GetCurrentThreadId();

		/// <summary>
		/// Sets the process-default DPI awareness to system-DPI awareness. 
		/// This is equivalent to calling SetProcessDpiAwarenessContext with a DPI_AWARENESS_CONTEXT value of DPI_AWARENESS_CONTEXT_SYSTEM_AWARE. 
		/// </summary>
		[DllImport("user32.dll", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetProcessDPIAware();

		/// <summary>
		///   Opens an existing local process object.
		/// </summary>
		/// <param name="dwDesiredAccess">
		///   The access to the process object. This access right is checked against the security descriptor for the process.
		///   This parameter can be one or more of the process access rights. <br/>
		///   If the caller has enabled the SeDebugPrivilege privilege, the requested access is granted regardless of the
		///   contents of the security descriptor.
		/// </param>
		/// <param name="bInheritHandle">
		///   If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes
		///   do not inherit this handle.
		/// </param>
		/// <param name="dwProcessId">
		///		The identifier of the local process to be opened. <br/>
		///		If the specified process is the System Idle Process(0x00000000), the function fails and the last error code is
		///		ERROR_INVALID_PARAMETER. If the specified process is the System process or one of the Client Server Run-Time
		///		Subsystem(CSRSS) processes, this function fails and the last error code is ERROR_ACCESS_DENIED because their
		///		access restrictions prevent user-level code from opening them. <br/>
		///		If you are using GetCurrentProcessId as an argument to this function, consider using GetCurrentProcess instead
		///		of OpenProcess, for improved performance.
		/// </param>
		/// <returns>
		///		If the function succeeds, the return value is an open handle to the specified process. <br/>
		///		If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		///			To open a handle to another local process and obtain full access rights, you must enable the SeDebugPrivilege privilege.
		///			For more information, see Changing Privileges in a Token. <br/>
		///			The handle returned by the OpenProcess function can be used in any function that requires a handle to a process, such
		///			as the wait functions, provided the appropriate access rights were requested. <br/>
		///			When you are finished with the handle, be sure to close it using the CloseHandle function. <br/>
		///		<br/> https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess
		///   <br/> https://www.pinvoke.net/default.aspx/kernel32/OpenProcess.html
		/// </remarks>
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = SetLastError)]
		public static extern HANDLE OpenProcessUnsafe(
			[In] ProcessAccessRights dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] DWORD dwProcessId
		);

		/// <inheritdoc cref="OpenProcessUnsafe(ProcessAccessRights, BOOL, DWORD)"/>
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = SetLastError)]
		public static extern SafeProcessHandle OpenProcess(
			[In] ProcessAccessRights dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] DWORD dwProcessId
		);

		/// <summary>
		///   Closes an open object handle.
		/// </summary>
		/// <param name="hObject">A valid handle to an open object.</param>
		/// <returns>
		///   <para>
		///   If the function succeeds, the return value is nonzero.
		///   </para><para>
		///   If the function fails, the return value is zero. To get extended error information, call GetLastError.
		///   </para><para>
		///   If the application is running under a debugger, the function will throw an exception if it receives either a
		///   handle value that is not valid or a pseudo-handle value. This can happen if you close a handle twice, or if
		///   you call CloseHandle on a handle returned by the FindFirstFile function instead of calling the FindClose function.
		///   </para>
		/// </returns>
		/// <remarks>
		///   <para>
		///   The documentation for the functions that create these objects indicates that CloseHandle should be used when you are
		///   finished with the object, and what happens to pending operations on the object after the handle is closed. In general,
		///   CloseHandle invalidates the specified object handle, decrements the object's handle count, and performs object retention
		///   checks. After the last handle to an object is closed, the object is removed from the system. For a summary of the creator
		///   functions for these objects, see Kernel Objects.
		///   </para><para>
		///   Generally, an application should call CloseHandle once for each handle it opens. It is usually not necessary to call
		///   CloseHandle if a function that uses a handle fails with ERROR_INVALID_HANDLE, because this error usually indicates that
		///   the handle is already invalidated. However, some functions use ERROR_INVALID_HANDLE to indicate that the object itself is
		///   no longer valid. For example, a function that attempts to use a handle to a file on a network might fail with
		///   ERROR_INVALID_HANDLE if the network connection is severed, because the file object is no longer available. In this case,
		///   the application should close the handle.
		///   </para><para>
		///   If a handle is transacted, all handles bound to a transaction should be closed before the transaction is committed. If a
		///   transacted handle was opened by calling CreateFileTransacted with the FILE_FLAG_DELETE_ON_CLOSE flag, the file is not
		///   deleted until the application closes the handle and calls CommitTransaction. For more information about transacted objects,
		///   see Working With Transactions.
		///   </para><para>
		///   Closing a thread handle does not terminate the associated thread or remove the thread object. Closing a process handle does
		///   not terminate the associated process or remove the process object. To remove a thread object, you must terminate the thread,
		///   then close all handles to the thread. For more information, see Terminating a Thread. To remove a process object, you must
		///   terminate the process, then close all handles to the process. For more information, see Terminating a Process.
		///   </para><para>
		///   Closing a handle to a file mapping can succeed even when there are file views that are still open. For more information,
		///   see Closing a File Mapping Object.
		///   </para><para>
		///   Do not use the CloseHandle function to close a socket. Instead, use the closesocket function, which releases all resources
		///   associated with the socket including the handle to the socket object. For more information, see Socket Closure.
		///   </para><para>
		///   Do not use the CloseHandle function to close a handle to an open registry key. Instead, use the RegCloseKey function. 
		///   CloseHandle does not close the handle to the registry key, but does not return an error to indicate this failure.
		///   </para>
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle
		///   <br/> https://www.pinvoke.net/default.aspx/kernel32/CloseHandle.html
		/// </remarks>
		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle([In] HANDLE hObject);

		/// <summary>
		/// Marks the handle for freeing and releasing resources
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if handle was valid and closed, or already closed. <br/>
		/// <see langword="false"/> if handle was invalid.
		/// </returns>
		public static bool CloseHandle(SafeProcessHandle hObject) {
			if (hObject is null || hObject.IsInvalid)
				return false;
			else if (!hObject.IsClosed)
				hObject.Close();
			return true;
		}

		[DllImport("kernel32.dll", EntryPoint = "DuplicateHandle", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(
			[In] HANDLE hSourceProcessHandle,
			[In] HANDLE hSourceHandle,
			[In] HANDLE hTargetProcessHandle,
			[Out] out SafeObjectHandle lpTargetHandle,
			[In] DWORD dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] DuplicateHandleOptions dwOptions
		);

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
		/// <code>
		/// // To attach to current thread
		/// AttachThreadInput(idAttach, curThreadId, true);
		/// // To dettach from current thread
		/// AttachThreadInput(idAttach, curThreadId, false);
		/// </code>
		/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-attachthreadinput
		/// </remarks>
		[DllImport("user32.dll", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachThreadInput(
			[In] DWORD idAttach,
			[In] DWORD idAttachTo,
			[In] BOOL fAttach
		);

		#endregion
	}
}
