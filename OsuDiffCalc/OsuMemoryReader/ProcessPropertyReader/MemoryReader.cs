using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using OsuDiffCalc.Utility;
using PAR = OsuDiffCalc.NativeMethods.ProcessAccessRights;
using MBI_STATE = OsuDiffCalc.NativeMethods.MBI_STATE;
using MEM_PROTECT = OsuDiffCalc.NativeMethods.MEM_PROTECT;

namespace OsuDiffCalc.OsuMemoryReader;

partial class ProcessPropertyReader {
	/// <summary>
	/// Tool for reading memory chunks and locating them in a process
	/// </summary>
	internal class MemoryReader : IDisposable {
		static readonly nuint _memInfoSize = (nuint)Unsafe.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>();
		private readonly CancellationTokenSource cts = new();
		private SafeProcessHandle _hProcess = new();
		private Process _currentProcess = null;

		/// <summary>
		/// Size of pointers in process <see cref="CurrentProcess"/>
		/// </summary>
		public int IntPtrSize { get; private set; } = IntPtr.Size;

		public Process CurrentProcess {
			get => _currentProcess;
			internal set {
				if (ReferenceEquals(_currentProcess, value)) return;
				CloseHandle();
				_currentProcess = value;
				IntPtrSize = NativeMethods.Is64BitProcess(value) ? 8 : 4;
				OpenHandle();
			}
		}

		/// <summary>
		/// Find the starting address of <paramref name="needle"/> in the process <see cref="CurrentProcess"/> current writable memory pages
		/// </summary>
		/// <param name="needle">the sequence of bytes to look for ("needle in a haystack")</param>
		/// <returns>
		/// A valid address in the process memory if <paramref name="needle"/> is found, otherwise <see cref="IntPtr.Zero"/>.
		/// </returns>
		public IntPtr FindNeedle(Span<byte> needle, IntPtr startingAddress = default) {
			var pHandle = _hProcess;
			if (pHandle is null || pHandle.IsInvalid || pHandle.IsClosed)
				return IntPtr.Zero;

			// this substring search is an implementation of Knuth-Morris-Pratt
			// see https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm

			// build look up table for substring matches
			int needleSize = needle.Length;
			Span<int> lut = needleSize < 256 ? stackalloc int[needleSize] : new int[needleSize];
			lut[0] = -1;
			int lutIdx = 1;
			int cnd = 0; // index in needle of the next character of the candidate substring
			while (lutIdx < needleSize) {
				if (needle[lutIdx] == needle[cnd])
					lut[lutIdx] = lut[cnd];
				else {
					lut[lutIdx] = cnd;
					while (cnd >= 0 && needle[lutIdx] != needle[cnd]) {
						cnd = lut[cnd];
					}
				}
				++lutIdx;
				++cnd;
			}

			// try to find needle in process memory pages, looking at the memory in chunks to avoid huge allocations

			// first, find system page size (usually 4-32 kB)
			// and use a buffer of a couple pages at a time to make up for the overhead of type marshalling
			bool isWow64 = Environment.Is64BitOperatingSystem && NativeMethods.IsWow64Process(pHandle, out bool iswow) && iswow;
			NativeMethods.SYSTEM_INFO sysInfo;
			if (!isWow64)
				NativeMethods.GetSystemInfo(out sysInfo);
			else
				NativeMethods.GetNativeSystemInfo(out sysInfo);
			int bufSize = sysInfo.dwPageSize > 0 ? (int)sysInfo.dwPageSize * 2 : 1024 * 32;

			var bytePool = ArrayPool<byte>.Shared;
			byte[] buffer = bytePool.Rent(bufSize);

			// search through the memory regions of the process
			IntPtr queryAddress = (nint)startingAddress > sysInfo.lpMinimumApplicationAddress ? startingAddress : sysInfo.lpMinimumApplicationAddress;
			while (NativeMethods.VirtualQueryEx(pHandle, queryAddress, out var memInfo, _memInfoSize)) {
				if (!ShouldReadPage(memInfo.State, memInfo.Protect) || memInfo.RegionSize <= 0) {
					queryAddress = memInfo.BaseAddress + memInfo.RegionSize;
					continue;
				}

				// read memory region into buffer in chunks (in case of huge pages)
				var endAddress = memInfo.BaseAddress + memInfo.RegionSize;
				var maxChunkSize = memInfo.RegionSize < bufSize ? memInfo.RegionSize : bufSize;
				var maxChunkEndAddress = endAddress - maxChunkSize;

				for (var chunkBaseAddress = memInfo.BaseAddress; chunkBaseAddress < endAddress; chunkBaseAddress += maxChunkSize) {
					var chunkSize = chunkBaseAddress <= maxChunkEndAddress ? maxChunkSize : (endAddress - chunkBaseAddress);
					bool readOk = NativeMethods.ReadProcessMemory(pHandle, chunkBaseAddress, buffer, chunkSize, out var bytesRead);
					if (!readOk || bytesRead != chunkSize)
						break;

					// find first match in buffer
					int bIdx = 0; // index of current char in buffer
					int nIdx = 0; // index of current char in needle
					while (bIdx < chunkSize) {
						if (buffer[bIdx] == needle[nIdx]) {
							++bIdx;
							++nIdx;
							if (nIdx == needleSize) {
								// found match
								bytePool.Return(buffer);
								return chunkBaseAddress + (bIdx - needleSize);
							}
						}
						else {
							nIdx = lut[nIdx];
							if (nIdx < 0) {
								++bIdx;
								++nIdx;
							}
						}
					}
				}

				// next query address
				queryAddress = memInfo.BaseAddress + memInfo.RegionSize;
			}

			// no match in any of the memory pages
			bytePool.Return(buffer);
			return IntPtr.Zero;
		}

		/// <param name="needle">
		/// the sequence of bytes to look for ("needle in a haystack"). <br/>
		/// A "no value" is treated as a wildcard (byte? with no value, not \0).
		/// </param>
		/// <inheritdoc cref="FindNeedle(Span{byte}, IntPtr)"/>
		public IntPtr FindNeedle(Span<byte?> needle, IntPtr startingAddress = default) {
			var pHandle = _hProcess;
			if (pHandle is null || pHandle.IsInvalid || pHandle.IsClosed)
				return IntPtr.Zero;

			// this substring search is an implementation of Knuth-Morris-Pratt
			// see https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm

			// build look up table for substring matches
			int needleSize = needle.Length;
			Span<int> lut = needleSize < 256 ? stackalloc int[needleSize] : new int[needleSize];
			lut[0] = -1;
			int lutIdx = 1;
			int cnd = 0; // index in needle of the next character of the candidate substring
			while (lutIdx < needleSize) {
				if (needle[lutIdx] == needle[cnd])
					lut[lutIdx] = lut[cnd];
				else {
					lut[lutIdx] = cnd;
					while (cnd >= 0 && needle[lutIdx] != needle[cnd]) {
						cnd = lut[cnd];
					}
				}
				++lutIdx;
				++cnd;
			}

			// try to find needle in process memory pages, looking at the memory in chunks to avoid huge allocations

			// first, find system page size (usually 4-32 kB)
			// and use a buffer of a couple pages at a time to make up for the overhead of type marshalling
			bool isWow64 = Environment.Is64BitOperatingSystem && NativeMethods.IsWow64Process(pHandle, out bool iswow) && iswow;
			NativeMethods.SYSTEM_INFO sysInfo;
			if (!isWow64)
				NativeMethods.GetSystemInfo(out sysInfo);
			else
				NativeMethods.GetNativeSystemInfo(out sysInfo);
			int bufSize = sysInfo.dwPageSize > 0 ? (int)sysInfo.dwPageSize * 2 : 1024 * 32;

			var bytePool = ArrayPool<byte>.Shared;
			byte[] buffer = bytePool.Rent(bufSize);

			// search through the memory regions of the process
			IntPtr queryAddress = (nint)startingAddress > sysInfo.lpMinimumApplicationAddress ? startingAddress : sysInfo.lpMinimumApplicationAddress;
			while (NativeMethods.VirtualQueryEx(pHandle, queryAddress, out var memInfo, _memInfoSize)) {
				if (!ShouldReadPage(memInfo.State, memInfo.Protect) || memInfo.RegionSize <= 0) {
					queryAddress = memInfo.BaseAddress + memInfo.RegionSize;
					continue;
				}

				// read memory region into buffer in chunks (in case of huge pages)
				var endAddress = memInfo.BaseAddress + memInfo.RegionSize;
				var maxChunkSize = memInfo.RegionSize < bufSize ? memInfo.RegionSize : bufSize;
				var maxChunkEndAddress = endAddress - maxChunkSize;

				for (var chunkBaseAddress = memInfo.BaseAddress; chunkBaseAddress < endAddress; chunkBaseAddress += maxChunkSize) {
					var chunkSize = chunkBaseAddress <= maxChunkEndAddress ? maxChunkSize : (endAddress - chunkBaseAddress);
					bool readOk = NativeMethods.ReadProcessMemory(pHandle, chunkBaseAddress, buffer, chunkSize, out var bytesRead);
					if (!readOk || bytesRead != chunkSize)
						break;

					// find first match in buffer
					int bIdx = 0; // index of current char in buffer
					int nIdx = 0; // index of current char in needle
					while (bIdx < chunkSize) {
						if (needle[nIdx] is not byte nByte || buffer[bIdx] == nByte) {
							++bIdx;
							++nIdx;
							if (nIdx == needleSize) {
								// found match
								bytePool.Return(buffer);
								return chunkBaseAddress + (bIdx - needleSize);
							}
						}
						else {
							nIdx = lut[nIdx];
							if (nIdx < 0) {
								++bIdx;
								++nIdx;
							}
						}
					}
				}

				// next query address
				queryAddress = memInfo.BaseAddress + memInfo.RegionSize;
			}

			// no match in any of the memory pages
			bytePool.Return(buffer);
			return IntPtr.Zero;
		}

		static bool ShouldReadPage(MBI_STATE state, MEM_PROTECT protect) {
			return state == MBI_STATE.MEM_COMMIT && (
				// if we are allowed to access the page
				(protect & MEM_PROTECT.PAGE_NOACCESS) |
				(protect & MEM_PROTECT.PAGE_GUARD) |
				(protect & MEM_PROTECT.PAGE_TARGETS_INVALID) |
				// and it's not read-only
				// (if it were read-only, how would the process of interest write anything there?)
				(protect & MEM_PROTECT.PAGE_EXECUTE) |
				(protect & MEM_PROTECT.PAGE_EXECUTE_READ) |
				(protect & MEM_PROTECT.PAGE_READONLY)
			) == 0;
		}

		/// <summary>
		/// Read bytes from the process
		/// </summary>
		/// <param name="address">Address to start reading from in process <see cref="CurrentProcess"/></param>
		/// <param name="numBytesToRead"></param>
		/// <returns>
		/// returns <see langword="null"/> if actual bytes read != <paramref name="numBytesToRead"/>
		/// </returns>
		public byte[] ReadBytes(IntPtr address, uint numBytesToRead) {
			if (address == IntPtr.Zero || _currentProcess is null)
				return null;

			var buffer = new byte[numBytesToRead];
			if (NativeMethods.ReadProcessMemory(_hProcess, address, buffer, numBytesToRead, out var numBytesRead) && numBytesRead == numBytesToRead)
				return buffer;
			else
				return null;
		}

		/// <inheritdoc cref="ReadBytes(IntPtr, uint)"/>
		public byte[] ReadBytes(IntPtr address, int numBytesToRead) {
			if (!CanReadBytes(address))
				return null;

			var buffer = new byte[numBytesToRead];
			if (NativeMethods.ReadProcessMemory(_hProcess, address, buffer, numBytesToRead, out var numBytesRead) && numBytesRead == numBytesToRead)
				return buffer;
			else
				return null;
		}

		/// <inheritdoc cref="ReadBytes(IntPtr, uint)"/>
		public byte[] ReadBytes(IntPtr address, long numBytesToRead) {
			if (!CanReadBytes(address))
				return null;

			nuint bytesToRead = (nuint)numBytesToRead;
			var buffer = new byte[numBytesToRead];
			if (NativeMethods.ReadProcessMemory(_hProcess, address, buffer, bytesToRead, out var numBytesRead) && numBytesRead == bytesToRead)
				return buffer;
			else
				return null;
		}

		/// <summary>
		/// Read bytes from the process
		/// </summary>
		/// <param name="address">Address to start reading from in process <see cref="CurrentProcess"/></param>
		/// <param name="numBytesToRead"></param>
		/// <returns>
		/// returns <see langword="null"/> if actual bytes read != <paramref name="numBytesToRead"/>
		/// </returns>
		public bool TryReadBytes(IntPtr address, byte[] buffer, uint numBytesToRead) {
			if (!CanReadBytes(address))
				return false;

			return NativeMethods.ReadProcessMemory(_hProcess, address, buffer, numBytesToRead, out var numBytesRead)
				&& numBytesRead == numBytesToRead;
		}

		/// <inheritdoc cref="TryReadBytes(IntPtr, byte[], uint)"/>
		public bool TryReadBytes(IntPtr address, byte[] buffer, int numBytesToRead) {
			if (!CanReadBytes(address))
				return false;

			return NativeMethods.ReadProcessMemory(_hProcess, address, buffer, numBytesToRead, out var numBytesRead)
				&& numBytesRead == numBytesToRead;
		}

		private bool CanReadBytes(IntPtr address) {
			if (address == IntPtr.Zero)
				return false;
			else
				return (_hProcess is not null) && !_hProcess.IsInvalid && !_hProcess.IsClosed;
		}

		private SafeProcessHandle OpenHandle() {
			try {
				CloseHandle();
				if (_currentProcess is not null && !_currentProcess.HasExitedSafe()) {
					const PAR access =
						PAR.PROCESS_QUERY_LIMITED_INFORMATION
						| PAR.PROCESS_VM_READ;
					return _hProcess = NativeMethods.OpenProcess(access, false, _currentProcess.Id);
				}
			}
			catch { }
			return _hProcess;
		}

		private void CloseHandle() {
			if (_hProcess is not null) {
				if (!_hProcess.IsInvalid)
					NativeMethods.CloseHandle(_hProcess);
				_hProcess.Dispose();
			}
			_hProcess = new();
		}

		private bool _isDisposed = false;
		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					// dispose managed state (managed objects)
					cts?.Cancel();
					_currentProcess?.Dispose();
					cts?.Dispose();
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				CloseHandle();

				// set large fields to null
				_currentProcess = null;
				_hProcess = null;

				_isDisposed = true;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
