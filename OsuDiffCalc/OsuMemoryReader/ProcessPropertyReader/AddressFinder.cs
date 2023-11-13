using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CSharpPolyfills; // needed for netframework

namespace OsuDiffCalc.OsuMemoryReader;

partial class ProcessPropertyReader {
	class AddressFinder : IDisposable {
		private readonly MemoryReader _memoryReader;
		private readonly ObjectReader _objectReader;
		private readonly byte?[] _baseAddressBytes;
		private IntPtr _baseAddress;

		/// <param name="baseAddressNeedleHexString">
		/// The needle to look for in the process memory to mark the "base" address for properties. <br/>
		/// This should be a hex string (eg. 'F003652A', optional leading 0x, where 2 chars map to 1 byte and '??' will translate to a wildcard)
		/// </param>
		public AddressFinder(MemoryReader memoryReader, ObjectReader objectReader, string baseAddressNeedleHexString) {
			_memoryReader = memoryReader;
			_objectReader = objectReader;
			_baseAddressBytes = HexStringToByteArray(baseAddressNeedleHexString);
		}

		/// <param name="baseAddressNeedle"> The needle to look for in the process memory to mark the "base" address for properties. </param>
		public AddressFinder(MemoryReader memoryReader, ObjectReader objectReader, ReadOnlySpan<byte> baseAddressNeedle) {
			_memoryReader = memoryReader;
			_objectReader = objectReader;
			_baseAddressBytes = baseAddressNeedle.ToArray().Cast<byte?>().ToArray();
		}

		/// <param name="baseAddressNeedle">
		/// The needle to look for in the process memory to mark the "base" address for properties. <br/>
		/// A 'none' (empty 'byte?', not \0) counts as a wildcard, eg. any byte will match.
		/// </param>
		public AddressFinder(MemoryReader memoryReader, ObjectReader objectReader, ReadOnlySpan<byte?> baseAddressNeedle) {
			_memoryReader = memoryReader;
			_objectReader = objectReader;
			_baseAddressBytes = baseAddressNeedle.ToArray();
		}

		/// <summary>
		/// Re-scan the process memory to find the base address of the needle this class was created with
		/// </summary>
		public void UpdateBaseAddress() {
			_baseAddress = _memoryReader.FindNeedle(_baseAddressBytes);
		}

		/// <summary>
		/// Try to find the address of the property that is a member of some class
		/// (or just the property based on its attribute info)
		/// in the address space of the current process
		/// </summary>
		/// <param name="classAttr"> Attribute attached to the class, or <see langword="null"/> </param>
		/// <param name="propAttr"> Attribute attached to the property, or <see langword="null"/> </param>
		/// <param name="classAddress"> Starting address of the class, or <see cref="IntPtr.Zero"/> </param>
		/// <param name="propertyAddress"> Starting address of the property, or <see cref="IntPtr.Zero"/> </param>
		/// <returns>
		/// <see langword="true"/> if the property's starting address could be found and is not <see cref="IntPtr.Zero"/>,
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool TryFindPropertyAddress(MemoryAddressInfoAttribute classAttr,
		                                   MemoryAddressInfoAttribute propAttr,
		                                   out IntPtr classAddress,
		                                   out IntPtr propertyAddress) {
			try {
				if (_baseAddress == IntPtr.Zero)
					UpdateBaseAddress();

				var (classBaseAddress, classPtrOffset) = FindAttributeTarget(_baseAddress, classAttr);
				var (targetBaseAddress, propertyPtrOffset) = FindAttributeTarget(classBaseAddress, propAttr);

				if (classAttr is null || !classAttr.ShouldFollowClassPointer) {
					classAddress = IntPtr.Zero;
					propertyAddress = _objectReader.ReadPointer(targetBaseAddress + propertyPtrOffset); // [Base-0x3C]
				}
				else {
					var classAddressPtr = _objectReader.ReadPointer(targetBaseAddress + classPtrOffset); // [Base-0xC]
					if (!_objectReader.TryReadPointer(classAddressPtr, out classAddress)) { // [CurrentBeatmap]
						propertyAddress = IntPtr.Zero;
						return false;
					}
					else
						propertyAddress = classAddress + propertyPtrOffset; // +0x80
				}

				return propertyAddress != IntPtr.Zero;
			}
			catch (Exception ex) {
#if DEBUG
				Console.WriteLine(ex);
				System.Diagnostics.Debugger.Break();
#endif
				classAddress = IntPtr.Zero;
				propertyAddress = IntPtr.Zero;
				return false;
			}
		}

		private (IntPtr addr, int offset) FindAttributeTarget(IntPtr baseAddr, MemoryAddressInfoAttribute attr) {
			if (attr is not null) {
				if (string.Compare(attr.Path, "base", StringComparison.OrdinalIgnoreCase) == 0)
					return (baseAddr, attr.Offset);
				else if (!string.IsNullOrEmpty(attr.Path)) {
					// assume Path is a valid constant address in the process' memory space
					var path = attr.Path.AsSpan().Trim();
					if (path.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
						path = path[2..];

					int halfLen = path.Length / 2;
					Span<byte> buffer = halfLen < 1024 ? stackalloc byte[halfLen] : new byte[halfLen];
					HexStringToByteArray(path, buffer);

					if (halfLen >= 8) {
						//var addr = new IntPtr(BitConverter.ToInt64(buffer));
						var addr = new IntPtr(Unsafe.ReadUnaligned<Int64>(ref buffer[0])); // for netframework
						return (addr, attr.Offset);
					}
					else {
						//var addr = new IntPtr(BitConverter.ToInt32(buffer));
						var addr = new IntPtr(Unsafe.ReadUnaligned<Int32>(ref buffer[0])); // for netframework
						return (addr, attr.Offset);
					}
				}
				else if (!string.IsNullOrEmpty(attr.PathNeedle)) {
					// TODO: support for things besides needles
					var needle = attr.PathNeedle.AsSpan().Trim();
					if (needle.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
						needle = needle[2..];

					int halfLen = needle.Length / 2;
					IntPtr targetBaseAddress;
					bool hasWildcard = needle.Contains("??", StringComparison.OrdinalIgnoreCase);
					if (hasWildcard) {
						Span<byte?> buffer = halfLen < 1024 ? stackalloc byte?[halfLen] : new byte?[halfLen];
						HexStringToByteArray(needle, buffer);
						targetBaseAddress = _memoryReader.FindNeedle(buffer);
					}
					else {
						Span<byte> buffer = halfLen < 1024 ? stackalloc byte[halfLen] : new byte[halfLen];
						HexStringToByteArray(needle, buffer);
						targetBaseAddress = _memoryReader.FindNeedle(buffer);
					}
					return (targetBaseAddress, attr.Offset);
				}
				else if (string.IsNullOrEmpty(attr.Path))
					return (baseAddr, attr.Offset);
			}
			return (baseAddr, 0);
		}

		private static byte[] HexStringToByteArrayConstant(string hex) {
			if (hex is null) return null;
			var hexSpan = hex.AsSpan().Trim();
			if (hexSpan.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hexSpan = hexSpan[2..];

			int halfLen = hexSpan.Length / 2;
			var arr = new byte[halfLen];
			HexStringToByteArray(hexSpan, arr);
			return arr;
		}

		/// <inheritdoc cref="HexStringToByteArray(ReadOnlySpan{char}, Span{byte?})"/>
		private static byte?[] HexStringToByteArray(string hex) {
			if (hex is null) return null;
			var hexSpan = hex.AsSpan().Trim();
			if (hexSpan.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hexSpan = hexSpan[2..];

			int halfLen = hexSpan.Length / 2;
			var arr = new byte?[halfLen];
			HexStringToByteArray(hexSpan, arr);
			return arr;
		}

		private static void HexStringToByteArray(ReadOnlySpan<char> hex, Span<byte> buffer) {
			int halfLen = hex.Length / 2;
			for (int i = 0; i < halfLen; ++i) {
				int j = i * 2;
				buffer[i] = (byte)((GetHexVal(hex[j]) * 16) + GetHexVal(hex[j + 1]));
			}
		}

		/// <summary>
		/// For this one, "??" maps to null, which is then interpreted as a wildcard byte when looking for a match
		/// </summary>
		private static void HexStringToByteArray(ReadOnlySpan<char> hex, Span<byte?> buffer) {
			int halfLen = hex.Length / 2;
			for (int i = 0; i < halfLen; ++i) {
				int j = i * 2;
				if (hex[j] == '?' && hex[j + 1] == '?')
					buffer[i] = null;
				else
					buffer[i] = (byte)((GetHexVal(hex[j]) * 16) + GetHexVal(hex[j + 1]));
			}
		}

		static int GetHexVal(char hex) {
			//return hex - (hex < 58 ? 48 : 55); // uppercase
			//return hex - (hex < 58 ? 48 : 87); // lowercase
			return hex - (hex < 58 ? 48 : (hex < 97 ? 55 : 87)); // both
		}


		private bool _isDisposed = false;
		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					// dispose managed state (managed objects)
					_memoryReader.Dispose();
					_objectReader.Dispose();
				}
				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				_baseAddress = IntPtr.Zero;

				_isDisposed = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
