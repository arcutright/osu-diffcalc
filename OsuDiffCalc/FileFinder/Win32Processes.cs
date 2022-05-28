/* MODIFIED BY ME

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
	using System.Security.Permissions;
	using System.Text;
	using System.Threading;

	public class Win32Processes {
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
				//OB_TYPE_CONTROLLER, 
				//OB_TYPE_DEVICE, 
				//OB_TYPE_DRIVER, 
				OB_TYPE_IO_COMPLETION,
				OB_TYPE_FILE
			};

			private const int handleTypeTokenCount = 27;
			private static readonly string[] handleTypeTokens = new string[] {
						"", "", "Directory", "SymbolicLink", "Token",
						"Process", "Thread", "Unknown7", "Event", "EventPair", "Mutant",
				"Unknown11", "Semaphore", "Timer", "Profile", "WindowStation",
				"Desktop", "Section", "Key", "Port", "WaitablePort",
				"Unknown21", "Unknown22", "Unknown23", "Unknown24",
				"IoCompletion", "File"
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
						IntPtr ptr = IntPtr.Zero;
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
							ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out int returnLength);
							if (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH) {
								// Round required memory up to the nearest 64KB boundary. 
								length = ((returnLength + 0xffff) & ~0xffff);
							}
							else if (ret == NT_STATUS.STATUS_SUCCESS) {
								// SYSTEM_HANDLE_INFORMATION struct is { numberOfEntries; entry[] }
								int numHandleEntries = Marshal.ReadInt32(ptr);
								int offset = sizeof(int);
								int handleEntrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>();
								for (int i = 0; i < numHandleEntries; i++) {
									var handleEntry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(ptr + offset);
									if (handleEntry.OwnerPid == _processId) {
										var handle = (IntPtr)handleEntry.HandleValue;

										if (GetHandleType(handle, handleEntry.OwnerPid, out SystemHandleType handleType) &&
												handleType == SystemHandleType.OB_TYPE_FILE) {
											if (GetFileNameFromHandle(handle, handleEntry.OwnerPid, out string devicePath)) {
												if (ConvertDevicePathToDosPath(devicePath, out string dosPath)) {
													// only return paths to files which match the filter
													if (!_useFileExtensionFilter || _fileExtensionFilter.Contains(Path.GetExtension(dosPath)))
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
									offset += handleEntrySize;
								}
							}
						}
						finally {
							// CER guarantees that the allocated memory is freed,  
							// if an asynchronous exception occurs.
							Marshal.FreeHGlobal(ptr);
							//sw.Flush(); 
							//sw.Close(); 
						}
					} while (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH);
				}

				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
					return GetEnumerator();
				}
			}

			private static bool GetFileNameFromHandle(IntPtr handle, int processId, out string fileName) {
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
				ThreadPool.QueueUserWorkItem(new WaitCallback(GetFileNameFromHandle), f);
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
				var s = (FileNameFromHandleState)state;
					s.RetValue = GetFileNameFromHandle(s.Handle, out string fileName);
					s.FileName = fileName;
				s.Set();
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
						fileName = Marshal.PtrToStringUni((IntPtr)((int)ptr + 8), (length - 9) / 2);
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

			private static bool GetHandleType(IntPtr handle, int processId, out SystemHandleType handleType) {
				string token = GetHandleTypeToken(handle, processId);
				return GetHandleTypeFromToken(token, out handleType);
			}

			private static bool GetHandleType(IntPtr handle, out SystemHandleType handleType) {
				string token = GetHandleTypeToken(handle);
				return GetHandleTypeFromToken(token, out handleType);
			}

			private static bool GetHandleTypeFromToken(string token, out SystemHandleType handleType) {
				for (int i = 1; i < handleTypeTokenCount; i++) {
					if (handleTypeTokens[i] == token) {
						handleType = (SystemHandleType)i;
						return true;
					}
				}
				handleType = SystemHandleType.OB_TYPE_UNKNOWN;
				return false;
			}

			private static string GetHandleTypeToken(IntPtr handle, int processId) {
				IntPtr currentProcess = NativeMethods.GetCurrentProcess();
				bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
				SafeProcessHandle processHandle = null;
				SafeObjectHandle objectHandle = null;
				try {
					if (remote) {
						processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
						if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS)) {
							handle = objectHandle.DangerousGetHandle();
						}
					}
					return GetHandleTypeToken(handle);
				}
				finally {
					if (remote) {
						if (processHandle is not null) {
							processHandle.Close();
						}
						if (objectHandle is not null) {
							objectHandle.Close();
						}
					}
				}
			}

			private static string GetHandleTypeToken(IntPtr handle) {
				NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, IntPtr.Zero, 0, out int length);
				IntPtr ptr = IntPtr.Zero;
				RuntimeHelpers.PrepareConstrainedRegions();
				if (length > 0) {
					try {
						RuntimeHelpers.PrepareConstrainedRegions();
						try { }
						finally {
							ptr = Marshal.AllocHGlobal(length);
						}
						if (NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, ptr, length, out length) == NT_STATUS.STATUS_SUCCESS) {
							return Marshal.PtrToStringUni((IntPtr)((int)ptr + 0x60));
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
					if (_deviceMap.TryGetValue(devicePath.Substring(0, i), out var drive)) {
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

		/// <summary>
		/// Undocumented win32 type optionally returned by NtQuerySystemInfo
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/handle_table_entry.htm
		/// <br/> https://www.codeproject.com/Articles/18975/Listing-Used-Files
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_HANDLE_INFORMATION {
			public uint NumberOfHandles;
			public SYSTEM_HANDLE_TABLE_ENTRY_INFO[] Handles;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO {
			public uint OwnerPid;
			public byte ObjectType;
			public byte HandleFlags;
			public ushort HandleValue;
			public IntPtr ObjectPointer;
			public uint AccessMask;
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

		internal enum SYSTEM_INFORMATION_CLASS {
			SystemBasicInformation = 0,
			SystemPerformanceInformation = 2,
			SystemTimeOfDayInformation = 3,
			SystemProcessInformation = 5,
			SystemProcessorPerformanceInformation = 8,
			SystemHandleInformation = 16,
			SystemInterruptInformation = 23,
			SystemExceptionInformation = 33,
			SystemRegistryQuotaInformation = 37,
			SystemLookasideInformation = 45
		}

		internal enum OBJECT_INFORMATION_CLASS {
			ObjectBasicInformation = 0,
			ObjectNameInformation = 1,
			ObjectTypeInformation = 2,
			ObjectAllTypesInformation = 3,
			ObjectHandleInformation = 4
		}

		[Flags]
		internal enum ProcessAccessRights {
			PROCESS_DUP_HANDLE = 0x00000040
		}

		[Flags]
		internal enum DuplicateHandleOptions {
			DUPLICATE_CLOSE_SOURCE = 0x1,
			DUPLICATE_SAME_ACCESS = 0x2
		}


		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		internal sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private SafeObjectHandle()
					: base(true) { }

			internal SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle)
					: base(ownsHandle) {
				base.SetHandle(preexistingHandle);
			}

			protected override bool ReleaseHandle() {
				return NativeMethods.CloseHandle(base.handle);
			}
		}

		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
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

		internal static class NativeMethods {
			[DllImport("ntdll.dll")]
			internal static extern NT_STATUS NtQuerySystemInformation(
					[In] SYSTEM_INFORMATION_CLASS SystemInformationClass,
					[In] IntPtr SystemInformation,
					[In] int SystemInformationLength,
					[Out] out int ReturnLength);

			[DllImport("ntdll.dll")]
			internal static extern NT_STATUS NtQueryObject(
					[In] IntPtr Handle,
					[In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
					[In] IntPtr ObjectInformation,
					[In] int ObjectInformationLength,
					[Out] out int ReturnLength);

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern SafeProcessHandle OpenProcess(
					[In] ProcessAccessRights dwDesiredAccess,
					[In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
					[In] int dwProcessId);

			[DllImport("kernel32.dll", SetLastError = true)]
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

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern int GetProcessId(
					[In] IntPtr Process);

			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool CloseHandle(
					[In] IntPtr hObject);

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern int QueryDosDevice(
					[In] string lpDeviceName,
					[Out] StringBuilder lpTargetPath,
					[In] int ucchMax);
		}

		#endregion
	}
}
