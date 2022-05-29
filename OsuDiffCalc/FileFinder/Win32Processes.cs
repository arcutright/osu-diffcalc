// see license at bottom for original version of this code
namespace OsuDiffCalc.FileFinder {
	using Microsoft.Win32.SafeHandles;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Runtime.ConstrainedExecution;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Security.Permissions;
	using System.Text;
	using System.Threading;

	// see https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke
	// from WinDef.h.                S/U is signed/unsigned
	// Type                        | S/U | x86    | x64
	// ----------------------------+-----+--------+-------
	// BYTE, BOOLEAN               | U   | 8 bit  | 8 bit
	// ----------------------------+-----+--------+-------
	// SHORT                       | S   | 16 bit | 16 bit
	// USHORT, WORD                | U   | 16 bit | 16 bit
	// ----------------------------+-----+--------+-------
	// INT, LONG                   | S   | 32 bit | 32 bit
	// UINT, ULONG, DWORD          | U   | 32 bit | 32 bit
	// ----------------------------+-----+--------+-------
	// INT_PTR, LONG_PTR, LPARAM   | S   | 32 bit | 64 bit
	// UINT_PTR, ULONG_PTR, WPARAM | U   | 32 bit | 64 bit
	// ----------------------------+-----+--------+-------
	// LONGLONG                    | S   | 64 bit | 64 bit
	// ULONGLONG, QWORD            | U   | 64 bit | 64 bit
	using HANDLE = System.IntPtr;
	using PVOID = System.IntPtr;
	using DWORD = System.UInt32;
	using WORD = System.UInt16;
	using ULONG = System.UInt32;
	using UINT = System.UInt32;
	using USHORT = System.UInt16;
	using LONG = System.Int32;
	using INT = System.Int32;
	using SHORT = System.Int16;
	using UCHAR = System.Byte;
	using CHAR = System.Byte;
	using CCHAR = System.Byte;
	using BOOL = System.Boolean;
	using BYTE = System.Byte;
	using BOOLEAN = System.Byte; // true = 1, false = 0

	public class Win32Processes {
		private static readonly int _intPtrSize = Marshal.SizeOf<IntPtr>(); // x86: 4, x64: 8
		private static readonly int _ustrSize = Marshal.SizeOf<UNICODE_STRING>(); // x86: 8, x64: 16
		private static readonly int _handleEntrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(); // x86: 16, x64: 24
		private static readonly int _objectTypeInfoSize = Marshal.SizeOf<OBJECT_TYPE_INFORMATION>(); // x86: 96, x64: 104

		#region Visible structure
		//[ComVisible(true), EventTrackingEnabled(true)] 
		public class DetectOpenFiles// : ServicedComponent 
		{
			private static Dictionary<string, string> _deviceMap;
			private const string _networkDevicePrefix = "\\Device\\LanmanRedirector\\";

			private const int MAX_PATH = 260;

			private enum SystemHandleType {
				OB_TYPE_UNKNOWN = 0,
				OB_TYPE_TYPE = 1,
				OB_TYPE_DIRECTORY,
				OB_TYPE_SYMBOLIC_LINK,
				OB_TYPE_TOKEN,
				OB_TYPE_PROCESS,
				OB_TYPE_THREAD,
				OB_TYPE_UNKNOWN_7,
				OB_TYPE_EVENT,
				OB_TYPE_EVENT_PAIR,
				OB_TYPE_MUTANT,
				OB_TYPE_UNKNOWN_11,
				OB_TYPE_SEMAPHORE,
				OB_TYPE_TIMER,
				OB_TYPE_PROFILE,
				OB_TYPE_WINDOW_STATION,
				OB_TYPE_DESKTOP,
				OB_TYPE_SECTION,
				OB_TYPE_KEY,
				OB_TYPE_PORT,
				OB_TYPE_WAITABLE_PORT,
				OB_TYPE_UNKNOWN_21,
				OB_TYPE_UNKNOWN_22,
				OB_TYPE_UNKNOWN_23,
				OB_TYPE_UNKNOWN_24,
				OB_TYPE_CONTROLLER,
				OB_TYPE_DEVICE,
				OB_TYPE_DRIVER,
				OB_TYPE_IO_COMPLETION,
				OB_TYPE_FILE
			};

			private static readonly Dictionary<string, SystemHandleType> _handleTypeTokens = new() {
				{ "", SystemHandleType.OB_TYPE_TYPE },
				{ "Directory", SystemHandleType.OB_TYPE_DIRECTORY },
				{ "SymbolicLink", SystemHandleType.OB_TYPE_SYMBOLIC_LINK },
				{ "Token", SystemHandleType.OB_TYPE_TOKEN },
				{ "Process",SystemHandleType.OB_TYPE_PROCESS },
				{ "Thread", SystemHandleType.OB_TYPE_THREAD },
				{ "Unknown7", SystemHandleType.OB_TYPE_UNKNOWN_7 },
				{ "Event", SystemHandleType.OB_TYPE_EVENT },
				{ "EventPair", SystemHandleType.OB_TYPE_EVENT_PAIR },
				{ "Mutant", SystemHandleType.OB_TYPE_MUTANT },
				{ "Unknown11", SystemHandleType.OB_TYPE_UNKNOWN_11 },
				{ "Semaphore", SystemHandleType.OB_TYPE_SEMAPHORE },
				{ "Timer", SystemHandleType.OB_TYPE_TIMER },
				{ "Profile", SystemHandleType.OB_TYPE_PROFILE },
				{ "WindowStation", SystemHandleType.OB_TYPE_WINDOW_STATION },
				{ "Desktop", SystemHandleType.OB_TYPE_DESKTOP },
				{ "Section", SystemHandleType.OB_TYPE_SECTION },
				{ "Key", SystemHandleType.OB_TYPE_KEY },
				{ "Port", SystemHandleType.OB_TYPE_PORT },
				{ "WaitablePort", SystemHandleType.OB_TYPE_WAITABLE_PORT },
				{ "Unknown21", SystemHandleType.OB_TYPE_UNKNOWN_21 },
				{ "Unknown22", SystemHandleType.OB_TYPE_UNKNOWN_22 },
				{ "Unknown23", SystemHandleType.OB_TYPE_UNKNOWN_23 },
				{ "Unknown24", SystemHandleType.OB_TYPE_UNKNOWN_24 },
				{ "IoCompletion", SystemHandleType.OB_TYPE_IO_COMPLETION },
				{ "File", SystemHandleType.OB_TYPE_FILE },
				{ "Controller", SystemHandleType.OB_TYPE_CONTROLLER },
				{ "Device", SystemHandleType.OB_TYPE_DEVICE },
				{ "Driver", SystemHandleType.OB_TYPE_DRIVER },
			};

			/// <summary> 
			/// Gets the open files enumerator. 
			/// </summary> 
			/// <param name="processId">The process id.</param> 
			/// <returns></returns> 
			public static IEnumerable<string> GetOpenFilesEnumerator(int processId, HashSet<string> fileExtensionFilter = null) {
				return new OpenFiles(processId, fileExtensionFilter);
			}

			public static List<Process> GetProcessesUsingFile(string fName, IEqualityComparer<string> fNameComparer = null) {
				fNameComparer ??= EqualityComparer<string>.Default;
				var result = new List<Process>();
				foreach (var p in Process.GetProcesses()) {
					try {
						if (GetOpenFilesEnumerator(p.Id).Contains(fName, fNameComparer))
							result.Add(p);
						else
							p.Dispose();
					}
					catch {
						// some processes will fail 
						p.Dispose();
					} 
				}
				return result;
			}

			private sealed class OpenFiles : IEnumerable<string> {
				private readonly int _processId;
				private readonly HashSet<string> _fileExtensionFilter;
				private readonly bool _useFileExtensionFilter;

				internal OpenFiles(int processId, HashSet<string> fileExtensionFilter) {
					_processId = processId;
					_fileExtensionFilter = fileExtensionFilter;
					_useFileExtensionFilter = fileExtensionFilter is not null && fileExtensionFilter.Count != 0;
				}

				public IEnumerator<string> GetEnumerator() {
					NT_STATUS ret;
					int length = 0x10000; // 2^16
					// Loop, probing for required memory. 
					do {
						var ptr = IntPtr.Zero;
						RuntimeHelpers.PrepareConstrainedRegions();
						try {
							RuntimeHelpers.PrepareConstrainedRegions();
							try { }
							finally {
								// CER guarantees that the address of the allocated  
								// memory is actually assigned to ptr if an  
								// asynchronous exception occurs. 
								ptr = Marshal.AllocHGlobal(length);
							}
							ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out int returnLength); // HOT PATH
							if (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH) {
								// Round required memory up to the nearest 64KB boundary. 
								length = ((returnLength + 0xffff) & ~0xffff);
							}
							else if (ret == NT_STATUS.STATUS_SUCCESS) {
								// by using direct offests and avoiding Marshal.PtrToStructure<>(), this runs ~2x faster, at the cost of readability

								// SYSTEM_HANDLE_INFORMATION struct is { numberOfEntries; entry[] }
								int numHandleEntries = (int)Marshal.ReadIntPtr(ptr);
								int offset = _intPtrSize;

								for (int i = 0; i < numHandleEntries; i++) {
									var ownerPid = (uint)Marshal.ReadInt16(ptr + offset); // read the SYSTEM_HANDLE_TABLE_ENTRY_INFO.OwnerPid
									if (ownerPid == _processId) {
										var handle = (IntPtr)Marshal.ReadInt16(ptr + offset + 6); // read the SYSTEM_HANDLE_TABLE_ENTRY_INFO.HandleValue

										bool isFileHandle = GetHandleType(handle, ownerPid, out SystemHandleType handleType) // HOT PATH
											&& handleType == SystemHandleType.OB_TYPE_FILE;
										if (isFileHandle) {
											if (GetFileNameFromHandle(handle, ownerPid, out string devicePath)) {
												// only inspect paths to file types which match the filter
												if (!_useFileExtensionFilter || _fileExtensionFilter.Contains(Path.GetExtension(devicePath))) {
													if (ConvertDevicePathToDosPath(devicePath, out string dosPath)) {
														yield return dosPath;
														//if (File.Exists(dosPath)) {
														//	yield return new FileInfo(dosPath); 
														//}
														//else if (Directory.Exists(dosPath)) {
														//	yield return new DirectoryInfo(dosPath); 
														//}
													}
												}
											}
										}
									}
									offset += _handleEntrySize;
								}
							}
						}
						finally {
							// CER guarantees that the allocated memory is freed,  
							// if an asynchronous exception occurs.
							Marshal.FreeHGlobal(ptr);
						}
					} while (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH);
				}

				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
					return GetEnumerator();
				}
			}

			private static bool GetFileNameFromHandle(IntPtr handle, uint processId, out string fileName) {
				IntPtr currentProcess = NativeMethods.GetCurrentProcess();
				bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
				SafeProcessHandle processHandle = null;
				SafeObjectHandle objectHandle = null;
				try {
					if (remote) {
						processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
						if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess,
								out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS)) {
							handle = objectHandle.DangerousGetHandle();
						}
					}
					return GetFileNameFromHandle(handle, out fileName, 200);
				}
				finally {
					if (remote) {
						processHandle?.Close();
						objectHandle?.Close();
					}
				}
			}

			private static bool GetFileNameFromHandle(IntPtr handle, out string fileName, int wait) {
				using var f = new FileNameFromHandleState(handle);
				ThreadPool.QueueUserWorkItem(GetFileNameFromHandle, f);
				if (f.WaitOne(wait)) {
					fileName = f.FileName;
					return f.RetValue;
				}
				else {
					fileName = string.Empty;
					return false;
				}
			}

			private class FileNameFromHandleState : IDisposable {
				private readonly ManualResetEvent _mr;

				public IntPtr Handle { get; }
				public string FileName { get; set; }
				public bool RetValue { get; set; }

				public FileNameFromHandleState(IntPtr handle) {
					_mr = new ManualResetEvent(false);
					this.Handle = handle;
				}

				public bool WaitOne(int wait) {
					return _mr.WaitOne(wait, false);
				}

				public void Set() {
					try {
						_mr.Set();
					}
					catch { }
				}

				public void Dispose() {
					_mr?.Close();
				}
			}

			private static void GetFileNameFromHandle(object state) {
				if (state is FileNameFromHandleState s) {
					s.RetValue = GetFileNameFromHandle(s.Handle, out string fileName);
					s.FileName = fileName;
					s.Set();
				}
			}

			private static bool GetFileNameFromHandle(IntPtr handle, out string fileName) {
				IntPtr ptr = IntPtr.Zero;
				RuntimeHelpers.PrepareConstrainedRegions();
				try {
					int length = 0x200;  // 512 bytes 
					RuntimeHelpers.PrepareConstrainedRegions();
					try { }
					finally {
						// CER guarantees the assignment of the allocated  
						// memory address to ptr, if an ansynchronous exception  
						// occurs. 
						ptr = Marshal.AllocHGlobal(length);
					}
					NT_STATUS ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
					if (ret == NT_STATUS.STATUS_BUFFER_OVERFLOW) {
						RuntimeHelpers.PrepareConstrainedRegions();
						try { }
						finally {
							// CER guarantees that the previous allocation is freed, 
							// and that the newly allocated memory address is  
							// assigned to ptr if an asynchronous exception occurs. 
							Marshal.FreeHGlobal(ptr);
							ptr = Marshal.AllocHGlobal(length);
						}
						ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
					}
					if (ret == NT_STATUS.STATUS_SUCCESS) {
						fileName = Marshal.PtrToStringUni(ptr + _ustrSize, (length - _ustrSize - 1) / 2);
						return fileName.Length != 0;
					}
				}
				finally {
					// CER guarantees that the allocated memory is freed,  
					// if an asynchronous exception occurs. 
					Marshal.FreeHGlobal(ptr);
				}

				fileName = string.Empty;
				return false;
			}

			private static bool GetHandleType(IntPtr handle, uint processId, out SystemHandleType handleType) {
				string token = GetHandleTypeToken(handle, processId); // HOT PATH: 16.2%
				return GetHandleTypeFromToken(token, out handleType); // HOT PATH: 0.4%
			}

			private static bool GetHandleType(IntPtr handle, out SystemHandleType handleType) {
				string token = GetHandleTypeToken(handle);
				return GetHandleTypeFromToken(token, out handleType);
			}

			private static bool GetHandleTypeFromToken(string token, out SystemHandleType handleType) {
				if (_handleTypeTokens.TryGetValue(token, out handleType))
					return true;
				else {
					handleType = SystemHandleType.OB_TYPE_UNKNOWN;
					return false;
				}
			}

			private static string GetHandleTypeToken(IntPtr handle, uint processId) {
				IntPtr currentProcess = NativeMethods.GetCurrentProcess(); // HOT PATH: 0.4%
				bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
				SafeProcessHandle processHandle = null;
				SafeObjectHandle objectHandle = null;
				try {
					if (remote) {
						processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId); // HOT PATH: 9.4%
						if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS)) { // HOT PATH: 2.6%
							handle = objectHandle.DangerousGetHandle();
						}
					}
					return GetHandleTypeToken(handle); // HOT PATH: 1.9%
				}
				finally {
					if (remote) {
						// HOT PATH: 1.9% (combined)
						processHandle?.Close();
						objectHandle?.Close();
					}
				}
			}

			private static string GetHandleTypeToken(IntPtr handle) {
				NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, IntPtr.Zero, 0, out var length);
				IntPtr ptr = IntPtr.Zero;
				RuntimeHelpers.PrepareConstrainedRegions();
				if (length > 0) {
					try {
						RuntimeHelpers.PrepareConstrainedRegions();
						try { }
						finally {
							ptr = Marshal.AllocHGlobal(length);
						}
						if (NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, ptr, length, out var length2) == NT_STATUS.STATUS_SUCCESS) {
							// length2 in x86: 108, x64: 120
							return Marshal.PtrToStringUni(ptr + _objectTypeInfoSize);
						}
					}
					finally {
						Marshal.FreeHGlobal(ptr);
					}
				}
				return string.Empty;
			}

			private static bool ConvertDevicePathToDosPath(string devicePath, out string dosPath) {
				EnsureDeviceMap();
				int i = devicePath.Length;
				while (i > 0 && (i = devicePath.LastIndexOf('\\', i - 1)) != -1) {
					if (_deviceMap.TryGetValue(devicePath[..i], out var drive)) {
						dosPath = drive + devicePath[i..];
						return dosPath.Length != 0;
					}
				}
				dosPath = string.Empty;
				return false;
			}

			private static void EnsureDeviceMap() {
				if (_deviceMap is null) {
					var localDeviceMap = BuildDeviceMap();
					Interlocked.CompareExchange(ref _deviceMap, localDeviceMap, null);
				}
			}

			private static Dictionary<string, string> BuildDeviceMap() {
				string[] logicalDrives = Environment.GetLogicalDrives();
				var localDeviceMap = new Dictionary<string, string>(logicalDrives.Length);
				var lpTargetPath = new StringBuilder(MAX_PATH);
				foreach (string drive in logicalDrives) {
					string lpDeviceName = drive.Substring(0, 2);
					NativeMethods.QueryDosDevice(lpDeviceName, lpTargetPath, MAX_PATH);
					localDeviceMap.Add(NormalizeDeviceName(lpTargetPath.ToString()), lpDeviceName);
				}
				localDeviceMap.Add(_networkDevicePrefix[..^1], "\\");
				return localDeviceMap;
			}

			private static string NormalizeDeviceName(string deviceName) {
				if (string.Compare(deviceName, 0, _networkDevicePrefix, 0, _networkDevicePrefix.Length, StringComparison.InvariantCulture) == 0) {
					string shareName = deviceName[(deviceName.IndexOf('\\', _networkDevicePrefix.Length) + 1)..];
					return _networkDevicePrefix + shareName;
				}
				return deviceName;
			}
		}

#endregion

#region Internal structures

		// https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke\

		/// <summary>
		/// Undocumented win32 type optionally returned by NtQuerySystemInfo
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/handle_table_entry.htm
		/// <br/> https://www.codeproject.com/Articles/18975/Listing-Used-Files
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_HANDLE_INFORMATION {
			public IntPtr NumberOfHandles; // source says ULONG but I see 32 bit: 4 byte, 64 bit: 8 byte
			public SYSTEM_HANDLE_TABLE_ENTRY_INFO[] Handles;
		}

		/// <summary>
		/// Undocumented win32 type optionally returned by NtQuerySystemInfo
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/handle_table_entry.htm
		/// <br/> https://www.codeproject.com/Articles/18975/Listing-Used-Files
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO {
			public USHORT OwnerPid;
			public USHORT CreatorBackTraceIndex;
			public UCHAR ObjectType;
			public UCHAR HandleFlags;
			public USHORT HandleValue;
			public PVOID ObjectPointer;
			public ACCESS_MASK GrantedAccess;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/basic.htm?tx=135 </remarks>
		[StructLayout(LayoutKind.Sequential)]
		internal struct OBJECT_BASIC_INFORMATION {
			public uint Attributes;
			public uint GrantedAccess;
			public uint HandleCount;
			public uint PointerCount;
			public uint PagedPoolCharge;
			public uint NonPagedPoolCharge;
			[MarshalAs(UnmanagedType.U4, SizeConst = 3)]
			public uint[] Reserved;
			public uint TotalNumberOfHandles;
			public uint UnknownAt0x20;
			public uint NameInfoSize;
			public uint TypeInfoSize;
			public uint SecurityDescriptorSize;
			public long CreationTime;
		}

		/// <summary>
		/// win32 struct
		/// </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/winprog/windows-data-types#unicde_string </remarks>
		[StructLayout(LayoutKind.Sequential)]
		internal struct UNICODE_STRING {
			public USHORT Length;
			public USHORT MaximumLength;
			public IntPtr Buffer; // wchar_t*
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
		internal enum ACCESS_MASK : DWORD {
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

		/// <summary>
		/// The GENERIC_MAPPING structure defines the mapping of generic access rights to specific and standard
		/// access rights for an object. When a client application requests generic access to an object, that
		/// request is mapped to the access rights defined in this structure.
		/// </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-generic_mapping </remarks>
		[StructLayout(LayoutKind.Sequential)]
		internal struct GENERIC_MAPPING {
			/// <summary> Specifies an access mask defining read access to an object. </summary>
			public ACCESS_MASK GenericRead;
			/// <summary> Specifies an access mask defining write access to an object. </summary>
			public ACCESS_MASK GenericWrite;
			/// <summary> Specifies an access mask defining execute access to an object. </summary>
			public ACCESS_MASK GenericExecute;
			/// <summary> Specifies an access mask defining all possible types of access to an object. </summary>
			public ACCESS_MASK GenericAll;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/type.htm?tx=135
		/// <br/> https://processhacker.sourceforge.io/doc/struct___o_b_j_e_c_t___t_y_p_e___i_n_f_o_r_m_a_t_i_o_n.html
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		internal struct OBJECT_TYPE_INFORMATION {
			public UNICODE_STRING TypeName;
			public ULONG TotalNumberOfObjects;
			public ULONG TotalNumberOfHandles;
			public ULONG TotalPagedPoolUsage;
			public ULONG TotalNonPagedPoolUsage;
			public ULONG TotalNamePoolUsage;
			public ULONG TotalHandleTableUsage;
			public ULONG HighWaterNumberOfObjects;
			public ULONG HighWaterNumberOfHandles;
			public ULONG HighWaterPagedPoolUsage;
			public ULONG HighWaterNonPagedPoolUsage;
			public ULONG HighWaterNamePoolUsage;
			public ULONG HighWaterHandleTableUsage;
			public ULONG InvalidAttributes;
			public GENERIC_MAPPING GenericMapping;
			public ULONG ValidAccessMask;
			public BOOLEAN SecurityRequired;
			public BOOLEAN MaintainHandleCount;
			public UCHAR TypeIndex; // since WINBLUE
			public CHAR ReservedByte;
			public ULONG PoolType;
			public ULONG DefaultPagedPoolCharge;
			public ULONG DefaultNonPagedPoolCharge;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct OBJECT_NAME_INFORMATION {
			public UNICODE_STRING Name;
			public IntPtr NameBuffer; // wchar
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		public struct OBJECT_DATA_INFORMATION {
			public BOOLEAN InheritHandle;
			public BOOLEAN ProtectFromClose;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks>
		/// https://processhacker.sourceforge.io/doc/ntbasic_8h_source.html#l00186
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_ATTRIBUTES {
			public ULONG Length;
			public HANDLE RootDirectory;
			public IntPtr ObjectName; // PUNICODE_STRING
			public ULONG Attributes;
			public PVOID SecurityDescriptor; // PSECURITY_DESCRIPTOR;
			public PVOID SecurityQualityOfService; // PSECURITY_QUALITY_OF_SERVICE
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/handle_flag.htm?tx=135 </remarks>
		[StructLayout(LayoutKind.Sequential)]
		internal struct OBJECT_HANDLE_FLAG_INFORMATION {
			public bool Inherit;
			public bool ProtectFromClose;
		}

		internal enum NT_STATUS {
			STATUS_SUCCESS = 0x00000000,
			STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005L),
			STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004L)
		}

		/// <summary>
		/// From ntdll, documentation is hard to come by
		/// <br/> http://www.pinvoke.net/default.aspx/ntdll/SYSTEM_INFORMATION_CLASS.html
		/// </summary>
		internal enum SYSTEM_INFORMATION_CLASS {
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
		/// Undocumented win32 enum used by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/class.htm?tx=136 </remarks>
		internal enum OBJECT_INFORMATION_CLASS {
			ObjectBasicInformation         = 0,
			ObjectNameInformation          = 1,
			ObjectTypeInformation          = 2,
			ObjectAllTypesInformation      = 3,
			ObjectHandleFlagInformation    = 4,
			ObjectSessionInformation       = 5,
			ObjectSessionObjectInformation = 6,
			MaxObjectInfoClass             = 7,
		}

		/// <summary> Standard access rights used by all objects. </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights </remarks>
		[Flags]
		internal enum StandardAccessRights : uint {
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
		internal enum ProcessAccessRights : uint {
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

		[Flags]
		internal enum DuplicateHandleOptions {
			DUPLICATE_CLOSE_SOURCE = 0x1,
			DUPLICATE_SAME_ACCESS  = 0x2
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
		internal sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private SafeObjectHandle() : base(true) { }

			internal SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
				base.SetHandle(preexistingHandle);
			}

			protected override bool ReleaseHandle() {
				return NativeMethods.CloseHandle(base.handle);
			}
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
		internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private SafeProcessHandle() : base(true) { 
			}

			internal SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
				base.SetHandle(preexistingHandle);
			}

			protected override bool ReleaseHandle() {
				return NativeMethods.CloseHandle(base.handle);
			}
		}

#endregion

#region WinAPI calls

		/// <summary>
		/// [NtQuerySystemInformation may be altered or unavailable in future versions of Windows.
		/// Applications should use the alternate functions listed in this topic.] 
		/// </summary>
		/// <remarks>
		///   <br/> https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation
		/// </remarks>
		internal static class NativeMethods {
			const bool SetLastError
#if DEBUG
		= true;
#else
		= false;
#endif

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
			internal static extern NT_STATUS NtQuerySystemInformation(
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
			internal static extern NT_STATUS NtQueryObject(
				[In] HANDLE Handle,
				[In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
				[Out] PVOID ObjectInformation,
				[In] int ObjectInformationLength, // ULONG
				[Out] out int ReturnLength // PULONG, optional
			);

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
			/// </remarks>
			[DllImport("kernel32.dll", SetLastError = SetLastError)]
			internal static extern SafeProcessHandle OpenProcess(
				[In] ProcessAccessRights dwDesiredAccess,
				[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
				[In] uint dwProcessId
			);

			[DllImport("kernel32.dll", SetLastError = SetLastError)]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool DuplicateHandle(
					[In] IntPtr hSourceProcessHandle,
					[In] IntPtr hSourceHandle,
					[In] IntPtr hTargetProcessHandle,
					[Out] out SafeObjectHandle lpTargetHandle,
					[In] int dwDesiredAccess,
					[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
					[In] DuplicateHandleOptions dwOptions);

			[DllImport("kernel32.dll")]
			internal static extern IntPtr GetCurrentProcess();

			[DllImport("kernel32.dll", SetLastError = SetLastError)]
			internal static extern int GetProcessId([In] IntPtr Process);

			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[DllImport("kernel32.dll", SetLastError = SetLastError)]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool CloseHandle([In] IntPtr hObject);

			[DllImport("kernel32.dll", SetLastError = SetLastError)]
			internal static extern int QueryDosDevice(
					[In] string lpDeviceName,
					[Out] StringBuilder lpTargetPath,
					[In] int ucchMax);
		}

#endregion
	}
}

/*
Original version: 'https://vmccontroller.codeplex.com/SourceControl/latest#VmcController/VmcServices/DetectOpenFiles.cs'
--------------Original License--------------
New BSD License (BSD)

Copyright (c) 2008, Jonathan Bradshaw
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following 
conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Jonathan Bradshaw nor the names of its contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
