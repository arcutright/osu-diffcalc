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
	using System.ComponentModel;
	using System.Diagnostics;
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
		public static extern int GetCurrentThreadId();

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
		public static extern HANDLE OpenProcessUnsafe(
			[In] ProcessAccessRights dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] int dwProcessId
		);

		/// <inheritdoc cref="OpenProcessUnsafe(ProcessAccessRights, BOOL, DWORD)"/>
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = SetLastError)]
		public static extern SafeProcessHandle OpenProcess(
			[In] ProcessAccessRights dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] DWORD dwProcessId
		);

		/// <inheritdoc cref="OpenProcessUnsafe(ProcessAccessRights, BOOL, DWORD)"/>
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = SetLastError)]
		public static extern SafeProcessHandle OpenProcess(
			[In] ProcessAccessRights dwDesiredAccess,
			[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			[In] int dwProcessId
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
#if !NET5_0_OR_GREATER
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
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

		/// <inheritdoc cref="AttachThreadInput(DWORD, DWORD, BOOL)"/>
		[DllImport("user32.dll", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachThreadInput(
			[In] int idAttach,
			[In] int idAttachTo,
			[In] BOOL fAttach
		);

		/// <summary>
		/// Retrieves information about a range of pages within the virtual address space of a specified process.
		/// </summary>
		/// <param name="hProcess">
		/// A handle to the process whose memory information is queried. The handle must have been opened with the
		/// PROCESS_QUERY_INFORMATION access right, which enables using the handle to read information from the 
		/// process object. For more information, see Process Security and Access Rights.
		/// </param>
		/// <param name="lpAddress">
		/// A pointer to the base address of the region of pages to be queried. This value is rounded down to the next
		/// page boundary. To determine the size of a page on the host computer, use the GetSystemInfo function. <br/>
		/// If lpAddress specifies an address above the highest memory address accessible to the process, the function
		/// fails with ERROR_INVALID_PARAMETER.
		/// </param>
		/// <param name="lpBuffer">A structure in which information about the specified page range is returned.</param>
		/// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
		/// <returns>
		/// The return value is the actual number of bytes returned in the information buffer. <br/>
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// Possible error values include ERROR_INVALID_PARAMETER.
		/// </returns>
		/// <remarks>
		/// <br/> See https://www.pinvoke.net/default.aspx/kernel32/VirtualQueryEx.html
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualqueryex
		/// </remarks>
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualQueryEx(
			[In] HANDLE hProcess,
			[In] PVOID lpAddress,
			[Out] out MEMORY_BASIC_INFORMATION lpBuffer,
			[In] nuint dwLength
		);

		/// <inheritdoc cref="VirtualQueryEx(HANDLE, PVOID, out MEMORY_BASIC_INFORMATION, nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualQueryEx(
			[In] SafeHandle hProcess,
			[In] PVOID lpAddress,
			[Out] out MEMORY_BASIC_INFORMATION lpBuffer,
			[In] nuint dwLength
		);

		/// <inheritdoc cref="VirtualQueryEx(HANDLE, PVOID, out MEMORY_BASIC_INFORMATION, nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualQueryEx(
			[In] HANDLE hProcess,
			[In] nuint lpAddress,
			[Out] out MEMORY_BASIC_INFORMATION lpBuffer,
			[In] nuint dwLength
		);

		/// <inheritdoc cref="VirtualQueryEx(HANDLE, PVOID, out MEMORY_BASIC_INFORMATION, nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualQueryEx(
			[In] SafeHandle hProcess,
			[In] nuint lpAddress,
			[Out] out MEMORY_BASIC_INFORMATION lpBuffer,
			[In] nuint dwLength
		);

		/// <summary>
		/// Retrieves information about the current system. <br/>
		/// To retrieve accurate information for an application running on WOW64, call the GetNativeSystemInfo function.
		/// </summary>
		/// <param name="Info"> A pointer to a SYSTEM_INFO structure that receives the information. </param>
		/// <remarks>
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsysteminfo
		/// <br/> See https://www.pinvoke.net/default.aspx/kernel32.getsysteminfo
		/// </remarks>
		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		public static extern void GetSystemInfo([Out] out SYSTEM_INFO Info);

		/// <summary>
		/// Retrieves information about the current system to an application running under WOW64. <br/>
		/// If the function is called from a 64-bit application, it is equivalent to the GetSystemInfo function.  <br/>
		/// If the function is called from an x86 or x64 application running on a 64-bit system that does not have
		/// an Intel64 or x64 processor (such as ARM64), it will return information as if the system is x86 only
		/// if x86 emulation is supported (or x64 if x64 emulation is also supported).
		/// </summary>
		/// <param name="Info">A pointer to a SYSTEM_INFO structure that receives the information.</param>
		[DllImport("kernel32.dll", SetLastError = SetLastError)]
		public static extern void GetNativeSystemInfo([Out] out SYSTEM_INFO Info);

		/// <summary>
		/// Read the memory of some process
		/// </summary>
		/// <param name="hProcess">
		/// A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.
		/// </param>
		/// <param name="lpBaseAddress">
		/// A pointer to the base address in the specified process from which to read. Before any data transfer occurs,
		/// the system verifies that all data in the base address and memory of the specified size is accessible for
		/// read access, and if it is not accessible the function fails.
		/// </param>
		/// <param name="lpBuffer">
		/// A pointer to a buffer that receives the contents from the address space of the specified process.
		/// </param>
		/// <param name="nSize">
		/// The number of bytes to be read from the specified process.
		/// </param>
		/// <param name="lpNumberOfBytesRead">
		/// A pointer to a variable that receives the number of bytes transferred into the specified buffer.
		/// If lpNumberOfBytesRead is NULL, the parameter is ignored.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero. <br/>
		/// If the function fails, the return value is 0 (zero). To get extended error information, call GetLastError. <br/>
		/// The function fails if the requested read operation crosses into an area of the process that is inaccessible.
		/// </returns>
		/// <remarks>
		/// ReadProcessMemory copies the data in the specified address range from the address space of the specified
		/// process into the specified buffer of the current process. Any process that has a handle with PROCESS_VM_READ
		/// access can call the function. <br/>
		/// The entire area to be read must be accessible, and if it is not accessible, the function fails.
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory
		/// </remarks>
		[DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(
			[In] HANDLE hProcess,
			[In] PVOID lpBaseAddress,
			[In][Out] byte[] lpBuffer,
			[In] nuint nSize,
			[Out] out nuint lpNumberOfBytesRead
		);

		/// <inheritdoc cref="ReadProcessMemory(HANDLE, HANDLE, UCHAR[], nuint, out nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(
			[In] SafeHandle hProcess,
			[In] PVOID lpBaseAddress,
			[In][Out] byte[] lpBuffer,
			[In] nuint nSize,
			[Out] out nuint lpNumberOfBytesRead
		);

		/// <inheritdoc cref="ReadProcessMemory(HANDLE, HANDLE, UCHAR[], nuint, out nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(
			[In] HANDLE hProcess,
			[In] PVOID lpBaseAddress,
			[In][Out] byte[] lpBuffer,
			[In] nint nSize,
			[Out] out nint lpNumberOfBytesRead
		);

		/// <inheritdoc cref="ReadProcessMemory(HANDLE, HANDLE, UCHAR[], nuint, out nuint)"/>
		[DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = SetLastError)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(
			[In] SafeHandle hProcess,
			[In] PVOID lpBaseAddress,
			[In][Out] byte[] lpBuffer,
			[In] nint nSize,
			[Out] out nint lpNumberOfBytesRead
		);

		/// <summary>
		/// Determines whether the specified process is running under WOW64 or an Intel64 of x64 processor. <br/>
		/// It will return true if the process is a 32-bit process running on a 64-bit operating system
		/// in a compatibility layer called Wow64 (Windows-32-on-Windows-64).
		/// <code>
		/// 32-bit on 32-bit Windows => false
		/// 32-bit on 64-bit Windows => true
		/// 64-bit on 64-bit Windows => false
		/// </code>
		/// </summary>
		/// <param name="processHandle">
		/// A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or
		/// PROCESS_QUERY_LIMITED_INFORMATION access right. For more information, see Process Security
		/// and Access Rights. <br/>
		/// Windows Server 2003 and Windows XP: The handle must have the PROCESS_QUERY_INFORMATION access right.</param>
		/// <param name="wow64Process">
		/// A pointer to a value that is set to TRUE if the process is running under WOW64 on an Intel64 or
		/// x64 processor. If the process is running under 32-bit Windows, the value is set to FALSE.
		/// If the process is a 32-bit application running under 64-bit Windows 10 on ARM, the value
		/// is set to FALSE. If the process is a 64-bit application running under 64-bit Windows, the value
		/// is also set to FALSE.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is a nonzero value. <br/>
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// Applications should use IsWow64Process2 instead of IsWow64Process to determine if a process is running under WOW. <br/>
		/// IsWow64Process2 removes the ambiguity inherent to multiple WOW environments by explicitly returning both the
		/// architecture of the host and guest for a given process. Applications can use this information to reliably
		/// identify situations such as running under emulation on ARM64. To compile an application that uses this function,
		/// define _WIN32_WINNT as 0x0501 or later. For more information, see Using the Windows Headers. 
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/api/wow64apiset/nf-wow64apiset-iswow64process
		/// </remarks>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(
			[In] HANDLE processHandle,
			[Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
		);

		/// <inheritdoc cref="IsWow64Process(HANDLE, out BOOL)"/>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(
			[In] SafeHandle processHandle,
			[Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
		);

		/// <summary>
		/// Determines whether the specified process is running under WOW64; also returns additional
		/// machine process and architecture information.
		/// </summary>
		/// <param name="hProcess">
		/// A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION
		/// access right. For more information, see Process Security and Access Rights.
		/// </param>
		/// <param name="pProccessMachine">
		/// On success, returns a pointer to an IMAGE_FILE_MACHINE_* value. The value will be IMAGE_FILE_MACHINE_UNKNOWN
		/// if the target process is not a WOW64 process; otherwise, it will identify the type of WoW process.
		/// </param>
		/// <param name="pNativeMachine">
		/// On success, returns a pointer to a possible IMAGE_FILE_MACHINE_* value identifying the native architecture of host system.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is a nonzero value. <br/>
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// IsWow64Process2 provides an improved direct replacement for IsWow64Process. In addition to determining
		/// if the specified process is running under WOW64, IsWow64Process2 returns the following information: <br/>
		/// - Whether the target process, specified by hProcess, is running under Wow or not. <br/>
		/// - The architecture of the target process. <br/>
		/// - Optionally, the architecture of the host system. <br/>
		/// <br/> See https://docs.microsoft.com/en-us/windows/win32/api/wow64apiset/nf-wow64apiset-iswow64process2
		/// </remarks>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process2(
			[In] HANDLE hProcess,
			[Out] out IMAGE_FILE_MACHINE pProccessMachine,
			[Out] out IMAGE_FILE_MACHINE pNativeMachine
		);

		/// <inheritdoc cref="IsWow64Process2(HANDLE, out IMAGE_FILE_MACHINE, out IMAGE_FILE_MACHINE)"/>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process2(
			[In] SafeHandle hProcess,
			[Out] out IMAGE_FILE_MACHINE pProccessMachine,
			[Out] out IMAGE_FILE_MACHINE pNativeMachine
		);

		/// <inheritdoc cref="IsWow64Process2(HANDLE, out IMAGE_FILE_MACHINE, out IMAGE_FILE_MACHINE)"/>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process2(
			[In] HANDLE hProcess,
			[Out] out IMAGE_FILE_MACHINE pProccessMachine
		);

		/// <inheritdoc cref="IsWow64Process2(HANDLE, out IMAGE_FILE_MACHINE, out IMAGE_FILE_MACHINE)"/>
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process2(
			[In] SafeHandle hProcess,
			[Out] out IMAGE_FILE_MACHINE pProccessMachine
		);

		/// <summary>
		/// Test whether a process is using 64 bit address pointers
		/// </summary>
		/// <exception cref="Win32Exception"></exception>
		public static bool Is64BitProcess(int pid) {
			if (pid == 0 || !Environment.Is64BitOperatingSystem)
				return false;

			using var pHandle = OpenProcess(ProcessAccessRights.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
			if (!IsWow64Process(pHandle, out bool isWow64Emulated))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			return !isWow64Emulated;
		}

		/// <inheritdoc cref="Is64BitProcess(int)"/>
		public static bool Is64BitProcess(this Process process) {
			if (process is null)
				return false;
			else
				return Is64BitProcess(process.Id);
		}

		#endregion
	}
}
