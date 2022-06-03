// see license at bottom for original version of this code
namespace OsuDiffCalc.FileFinder {
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
	using static OsuDiffCalc.NativeMethods;

	//[ComVisible(true), EventTrackingEnabled(true)] 
	public class OpenFilesDetector {
		private static readonly int _intPtrSize = Marshal.SizeOf<IntPtr>(); // x86: 4, x64: 8
		private static readonly int _ustrSize = Marshal.SizeOf<UNICODE_STRING>(); // x86: 8, x64: 16
		private static readonly int _handleEntrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(); // x86: 16, x64: 24
		private static readonly int _objectTypeInfoSize = Marshal.SizeOf<OBJECT_TYPE_INFORMATION>(); // x86: 96, x64: 104

		private const string _networkDevicePrefix = "\\Device\\LanmanRedirector\\";
		private const int MAX_PATH = 260;

		private static Dictionary<string, string> _deviceMap;

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
						ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out int returnLength); // HOT PATH: 23.8%
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

									bool isFileHandle = GetHandleType(handle, ownerPid, out SystemHandleType handleType) // HOT PATH: 20%
										&& handleType == SystemHandleType.OB_TYPE_FILE;
									if (isFileHandle) {
										if (GetFileNameFromHandle(handle, ownerPid, out string devicePath)) { // HOT PATH: 1.6%
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
			IntPtr currentProcess = GetCurrentProcess();
			bool remote = (processId != GetProcessId(currentProcess));
			SafeProcessHandle processHandle = null;
			SafeObjectHandle objectHandle = null;
			try {
				if (remote) {
					processHandle = OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
					if (DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess,
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
			string token = GetHandleTypeToken(handle, processId); // HOT PATH: 19.8%
			return GetHandleTypeFromToken(token, out handleType);
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
			IntPtr currentProcess = NativeMethods.GetCurrentProcess();
			bool remote = (processId != NativeMethods.GetProcessId(currentProcess)); // HOT PATH: 0.6%
			SafeProcessHandle processHandle = null;
			SafeObjectHandle objectHandle = null;
			try {
				if (remote) {
					processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId); // HOT PATH: 12.6%
					if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS)) { // HOT PATH: 1.8%
						handle = objectHandle.DangerousGetHandle();
					}
				}
				return GetHandleTypeToken(handle); // HOT PATH: 2.5%
			}
			finally {
				if (remote) {
					// HOT PATH: 2% (combined)
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
