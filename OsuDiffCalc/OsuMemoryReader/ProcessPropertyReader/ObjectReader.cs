using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.OsuMemoryReader;

/*
 * Some background reading to understand the layout of types in .NET
 * 
 * https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/object.h
 * strings are a special case:
 * https://referencesource.microsoft.com/#mscorlib/system/string.cs
 * https://github.com/dotnet/runtime/blob/75e65e99af5b3c8a2fd0abd5e887f8b14bd45362/src/coreclr/vm/object.h#L863
 */

partial class ProcessPropertyReader {
	/// <summary>
	/// Memory reader for reading objects from a process (strings, arrays, etc)
	/// </summary>
	class ObjectReader : IDisposable {
		private static readonly Dictionary<Type, int> _typeSizeCache = new();
		static ObjectReader() {
			static void add(Type t1, int size) => _typeSizeCache[t1] = size;
			static void add2(Type t1, Type t2, int size) => _typeSizeCache[t1] = _typeSizeCache[t2] = size;

			add(typeof(string), 0);
			add2(typeof(byte),    typeof(byte?),    sizeof(byte));
			add2(typeof(sbyte),   typeof(sbyte?),   sizeof(sbyte));
			add2(typeof(bool),    typeof(bool?),    sizeof(bool));
			add2(typeof(char),    typeof(char?),    sizeof(char));
			add2(typeof(short),   typeof(short?),   sizeof(short));
			add2(typeof(ushort),  typeof(ushort?),  sizeof(ushort));
			add2(typeof(int),     typeof(int?),     sizeof(int));
			add2(typeof(uint),    typeof(uint?),    sizeof(uint));
			add2(typeof(long),    typeof(long?),    sizeof(long));
			add2(typeof(ulong),   typeof(ulong?),   sizeof(ulong));
			add2(typeof(float),   typeof(float?),   sizeof(float));
			add2(typeof(double),  typeof(double?),  sizeof(double));
			add2(typeof(decimal), typeof(decimal?), sizeof(decimal));
			add2(typeof(IntPtr),  typeof(IntPtr?),  Unsafe.SizeOf<IntPtr>());
			add2(typeof(UIntPtr), typeof(UIntPtr?), Unsafe.SizeOf<UIntPtr>());
		}

		private readonly MemoryReader _memoryReader;

		public ObjectReader(MemoryReader memoryReader) {
			_memoryReader = memoryReader;
			UpdateIntPtrSize();
		}

		/// <summary>
		/// Re-scan the process memory to find the ptr size
		/// </summary>
		public void UpdateIntPtrSize() {
			lock (_typeSizeCache) {
				int intPtrSize = _memoryReader?.IntPtrSize ?? 4;
				_typeSizeCache[typeof(IntPtr)]   = intPtrSize;
				_typeSizeCache[typeof(IntPtr?)]  = intPtrSize;
				_typeSizeCache[typeof(UIntPtr)]  = intPtrSize;
				_typeSizeCache[typeof(UIntPtr?)] = intPtrSize;
			}
		}

		/// <summary>
		/// Get the runtime memory size of a type. <br/>
		/// WARNING: for nullables, this returns the underlying type size, not including the `hasValue` or padding due to the Nullable wrapper. <br/>
		/// For pointers/nint this will return <see cref="MemoryReader.IntPtrSize"/> (from the target process)
		/// </summary>
		/// <returns>
		/// The number of bytes the type will take up in dotnet runtime memory,
		/// or 0 if it is unknown or varies (strings, classes, etc)
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int SizeOf<T>() {
			// note: this sucks to read but roslyn will evaulate all the typeof(T) checks and sizeof(T) ahead of time
			if      (typeof(T) == typeof(string))                                   return 0;               // varies
			else if (typeof(T) == typeof(byte)    || typeof(T) == typeof(byte?))    return sizeof(byte);    // 1
			else if (typeof(T) == typeof(sbyte)   || typeof(T) == typeof(sbyte?))   return sizeof(sbyte);   // 1
			else if (typeof(T) == typeof(bool)    || typeof(T) == typeof(bool?))    return sizeof(bool);    // 1
			else if (typeof(T) == typeof(char)    || typeof(T) == typeof(char?))    return sizeof(char);    // 2
			else if (typeof(T) == typeof(short)   || typeof(T) == typeof(short?))   return sizeof(short);   // 2
			else if (typeof(T) == typeof(ushort)  || typeof(T) == typeof(ushort?))  return sizeof(ushort);  // 2
			else if (typeof(T) == typeof(int)     || typeof(T) == typeof(int?))     return sizeof(int);     // 4
			else if (typeof(T) == typeof(uint)    || typeof(T) == typeof(uint?))    return sizeof(uint);    // 4
			else if (typeof(T) == typeof(float)   || typeof(T) == typeof(float?))   return sizeof(float);   // 4
			else if (typeof(T) == typeof(double)  || typeof(T) == typeof(double?))  return sizeof(double);  // 8
			else if (typeof(T) == typeof(long)    || typeof(T) == typeof(long?))    return sizeof(long);    // 8
			else if (typeof(T) == typeof(ulong)   || typeof(T) == typeof(ulong?))   return sizeof(ulong);   // 8
			else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?)) return sizeof(decimal); // 16
			else if (typeof(T) == typeof(IntPtr)  || typeof(T) == typeof(IntPtr?)
				    || typeof(T) == typeof(UIntPtr) || typeof(T) == typeof(UIntPtr?))
				return _memoryReader.IntPtrSize;
			else
				return UnknownSizeOf<T>();
		}

		private static int UnknownSizeOf<T>() {
			lock (_typeSizeCache) {
				if (_typeSizeCache.TryGetValue(typeof(T), out var size))
					return size;

				int sizeT;
				if (typeof(T).IsEnum)
					sizeT = Unsafe.SizeOf<T>();
				else if (typeof(T).IsValueType) {
					var underlyingType = Nullable.GetUnderlyingType(typeof(T));
					if (underlyingType is not null) {
						// TODO: using SizeOf(Type) is not safe for AOT in net7+ since it requires reflection
						sizeT = SizeOf(underlyingType);
					}
					else
						sizeT = Unsafe.SizeOf<T>();
				}
				else
					sizeT = 0;

				return _typeSizeCache[typeof(T)] = sizeT;
			}
		}

		/// <summary>
		/// Sketchy way to get the runtime memory size of a type. <br/>
		/// WARNING: for nullables, this returns the underlying type size, not including the `hasValue` or padding due to the Nullable wrapper. <br/>
		/// Prefer using <see cref="SizeOf{T}"/> whenever possible, it is faster and more likely to be correct.
		/// </summary>
		/// <inheritdoc cref="SizeOf{T}"/>
		public static int SizeOf(Type type) {
			if (type is null)
				return 0;
			lock (_typeSizeCache) {
				if (_typeSizeCache.TryGetValue(type, out var size))
					return size;

				int sizeT;
				if (type.IsEnum) {
					var tc = Type.GetTypeCode(type);
					sizeT = tc switch {
						TypeCode.Boolean => sizeof(bool),
						TypeCode.Char => sizeof(char),
						TypeCode.Byte or TypeCode.SByte => sizeof(byte),
						TypeCode.Int16 or TypeCode.UInt16 => sizeof(short),
						TypeCode.Int32 or TypeCode.UInt32 => sizeof(int),
						TypeCode.Int64 or TypeCode.UInt64 => sizeof(long),
						// TODO: using SizeOf(Type) is not safe for AOT in net7+ since it requires reflection
						_ => SizeOf(Enum.GetUnderlyingType(type)),
					};
				}
				else if (type.IsValueType) {
					var underlyingType = Nullable.GetUnderlyingType(type);
					if (underlyingType is not null)
						sizeT = SizeOf(underlyingType);
					else
						sizeT = Marshal.SizeOf(type);
				}
				else
					sizeT = 0;

				return _typeSizeCache[type] = sizeT;
			}
		}

		/// <summary>
		/// Read a pointer starting at <paramref name="baseAddr"/>
		/// </summary>
		/// <param name="baseAddr"></param>
		/// <returns>the value of the pointer, or IntPtr.Zero if it failed</returns>
		public IntPtr ReadPointer(IntPtr baseAddr) {
			if (baseAddr == IntPtr.Zero)
				return IntPtr.Zero;

			var intPtrSize = _memoryReader.IntPtrSize;
			var data = _memoryReader.ReadBytes(baseAddr, intPtrSize);
			if (data is null)
				return IntPtr.Zero;

			if (intPtrSize == 8)
				return new IntPtr(BitConverter.ToInt64(data, 0));

			var rawPtr = BitConverter.ToInt32(data, 0);
			if (rawPtr > 0 && rawPtr <= int.MaxValue)
				return new IntPtr(rawPtr);
			else
				return IntPtr.Zero;
		}

		/// <summary>
		/// Try to read a pointer starting at address <paramref name="baseAddr"/>
		/// </summary>
		/// <param name="baseAddr"></param>
		/// <param name="result"></param>
		/// <returns>
		/// <see langword="true"/> if could read pointer and <paramref name="result"/> != <see cref="IntPtr.Zero"/>,
		/// otherwise <see langword="false"/>
		/// </returns>
		public bool TryReadPointer(IntPtr baseAddr, out IntPtr result) {
			return (result = ReadPointer(baseAddr)) != IntPtr.Zero;
		}

		/// <summary>
		/// Try to read a string stored at a given address in the current process. <br/>
		/// Note that this will assume there's an error if the string length &gt; <paramref name="maxStringLength"/>
		/// </summary>
		/// <param name="baseAddress"> starting address to read from </param>
		/// <param name="bytesPerCharacter"> bytes per char. Generally 1 in ASCII/UTF8, 2 in Unicode/UTF16, etc.</param>
		/// <param name="encoding"> If you don't specify encoding, it will make a guess </param>
		/// <param name="maxStringLength"> Some unreasonable length to avoid reading dumb amounts of memory when there's clearly a problem </param>
		/// <returns>
		/// Some string or <see langword="null"/> if unable to read (or max length exceeded)
		/// </returns>
		public string ReadString(IntPtr baseAddress, int bytesPerCharacter = 2, Encoding encoding = null, uint maxStringLength = 16384) {
			if (baseAddress == IntPtr.Zero || bytesPerCharacter == 0) return null;
			try {
				var (length, firstElementPtr) = ReadArrayHeader(baseAddress, isString: true);
				if (length == 0) return string.Empty;
				if (maxStringLength > 0 && length > maxStringLength) return null; // unwilling to read, return nothing

				var bytePool = ArrayPool<byte>.Shared;
				byte[] rentedBuffer = null;
				var numBytes = (int)(bytesPerCharacter * length);
				var byteBuffer = numBytes < 1024 ? new byte[numBytes] : (rentedBuffer = bytePool.Rent(numBytes));

				if (!_memoryReader.TryReadBytes(firstElementPtr, byteBuffer, numBytes)) {
					if (rentedBuffer is not null) bytePool.Return(rentedBuffer);
					return null;
				}

				var enc = encoding ?? bytesPerCharacter switch {
					1 => Encoding.UTF8,
					2 => Encoding.Unicode,
					3 | 4 => Encoding.UTF32,
					_ => Encoding.Unicode,
				};
				string result = enc.GetString(byteBuffer, 0, numBytes);

				if (rentedBuffer is not null) bytePool.Return(rentedBuffer);
				return result;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Read an array starting at address <paramref name="baseAddress"/>. <br/>
		/// This will not work if the underlying data is a <see cref="List{T}"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="baseAddress"></param>
		/// <returns>
		/// Some array or <see langword="null"/> if unable to read
		/// </returns>
		public T[] ReadArray<T>(IntPtr baseAddress) where T : struct {
			if (baseAddress == IntPtr.Zero) return null;
			try {
				var (length, firstElementPtr) = ReadArrayHeader(baseAddress, isString: false);
				if (length < 0) return null;
				if (length == 0) return Array.Empty<T>();

				var bytePool = ArrayPool<byte>.Shared;
				byte[] rentedBuffer = null;
				int elementSize = SizeOf<T>();
				var numBytes = (int)(elementSize * length);
				var byteBuffer = numBytes < 1024 ? new byte[numBytes] : (rentedBuffer = bytePool.Rent(numBytes));

				if (!_memoryReader.TryReadBytes(firstElementPtr, byteBuffer, numBytes)) {
					if (rentedBuffer is not null) bytePool.Return(byteBuffer);
					return null;
				}
				var byteSpan = byteBuffer.AsSpan();

				var arr = new T[length];
				for (var (i, offset) = (0, 0); i < length; ++i, offset += elementSize) {
					arr[i] = MemoryMarshal.Read<T>(byteSpan.Slice(offset, elementSize));
				}

				if (rentedBuffer is not null) bytePool.Return(byteBuffer);
				return arr;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Read a list starting at address <paramref name="baseAddress"/>. <br/>
		/// This will not work if the underlying data is an array.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="baseAddress"></param>
		/// <returns>
		/// Some <see cref="List{T}"/> or <see langword="null"/> if unable to read
		/// </returns>
		public List<T> ReadList<T>(IntPtr baseAddress) where T : struct {
			if (baseAddress == IntPtr.Zero) return null;
			try {
				var (count, firstElementPtr) = ReadListHeader(baseAddress);
				if (count < 0) return null;
				if (count == 0) return new();

				int elementSize = SizeOf<T>();
				var numBytes = (int)(elementSize * count);
				var byteBuffer = _memoryReader.ReadBytes(firstElementPtr, numBytes);
				if (byteBuffer is null || byteBuffer.Length != numBytes) return null;
				var byteSpan = byteBuffer.AsSpan();

				var arr = new T[count];
				for (var (i, offset) = (0, 0); i < count; ++i, offset += elementSize) {
					arr[i] = MemoryMarshal.Read<T>(byteSpan.Slice(offset, elementSize));
				}
				return new List<T>(arr);
			}
			catch {
				return null;
			}
		}



		#region Read array headers

		// Finds number of elements and a pointer to the first element of an array like structure
		// 
		// string/array are similar besides the size of the 'length' field

		private (uint length, IntPtr firstElementPtr) ReadArrayHeader(IntPtr baseAddress, bool isString) {
			var address = ReadPointer(baseAddress);
			if (address == IntPtr.Zero)
				return (0, IntPtr.Zero);

			var intPtrSize = _memoryReader.IntPtrSize;

			// first thing after VTable is the array length
			var lengthFieldAddress = address + intPtrSize;
			uint length;

			if (isString) {
				// strings always use a 4 byte length field
				var lengthFieldBytes = _memoryReader.ReadBytes(lengthFieldAddress, 4);
				if (lengthFieldBytes == null || lengthFieldBytes.Length != 4)
					return (0, IntPtr.Zero);
				length = BitConverter.ToUInt32(lengthFieldBytes, 0);

				var firstElementPtr = lengthFieldAddress + 4;
				return (length, firstElementPtr);
			}
			else {
				// array length is native int size
				var lengthFieldBytes = _memoryReader.ReadBytes(lengthFieldAddress, intPtrSize);
				if (lengthFieldBytes == null || lengthFieldBytes.Length != intPtrSize)
					return (0, IntPtr.Zero);

				if (intPtrSize == 4)
					length = BitConverter.ToUInt32(lengthFieldBytes, 0);
				else if (intPtrSize == 8) {
					var numberOfElementsLong = BitConverter.ToUInt64(lengthFieldBytes, 0);
					if (numberOfElementsLong > uint.MaxValue)
						return (0, IntPtr.Zero); // unable to read, return nothing
					length = (uint)numberOfElementsLong;
				}
				else
					throw new NotImplementedException($"Read array with intptr size {intPtrSize}");

				var firstElementPtr = lengthFieldAddress + intPtrSize;
				return (length, firstElementPtr);
			}
		}

		// A List<T> object has a class VTable and an internal array, where the internal array size is not always equal to the list size
		private (uint count, IntPtr firstElementPtr) ReadListHeader(IntPtr baseAddress) {
			var address = ReadPointer(baseAddress);
			if (address == IntPtr.Zero)
				return (0, IntPtr.Zero);

			var intPtrSize =  _memoryReader.IntPtrSize;

			// in a list, _size is a field attached to the class
			// skip VTable + _items ptr + ?
			// note the .NET JIT may break this hack in the future, this was validated in .NET Framework 4
			var sizeFieldAddress = address + (3 * intPtrSize);

			// in a list, the _size is always 4 bytes
			var sizeFieldBytes = _memoryReader.ReadBytes(sizeFieldAddress, 4);
			if (sizeFieldBytes == null || sizeFieldBytes.Length != 4)
				return (0, IntPtr.Zero);
			var count = BitConverter.ToUInt32(sizeFieldBytes, 0);

			// skip VTable of list structure, resolve pointer to internal array
			var internalArrayPtr = ReadPointer(address + intPtrSize);
			// skip VTable of internal array + number of elements, the rest will be its data
			var firstElementPtr = internalArrayPtr + (2 * intPtrSize);
			
			return (count, firstElementPtr);
		}

		#endregion

		private bool _isDisposed = false;
		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					// dispose managed state (managed objects)
					_memoryReader?.Dispose();
				}
				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
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
