using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OsuDiffCalc.Utility;

namespace OsuDiffCalc.OsuMemoryReader;

/// <summary>
/// Tool for reading the memory of a .NET process in a semi-structured way. <br/>
/// Currently only supports reading things as complicated as <c>[ptr + offset] -&gt; [structPtr + offset2] = structData</c>. <br/>
/// To use this, you create it with a "base address" (or something it can use to find the base)
/// and then read things relative to that base. <br/>
/// See <see cref="MemoryAddressInfoAttribute"/> examples for usage.
/// </summary>
partial class ProcessPropertyReader : IDisposable {
	private readonly MemoryReader _memoryReader;
	private readonly AddressFinder _addressFinder;
	private readonly ObjectReader _objectReader;
	private readonly Dictionary<(Type targetType, string propertyName), PropertyCacheEntry> _propertyCache = new();

	private record PropertyCacheEntry(
		MemoryAddressInfoAttribute ClassAttribute,
		MemoryAddressInfoAttribute PropertyAttribute,
		bool IsValid,
		bool IsConstantAddress,
		IntPtr ClassAddress,
		IntPtr PropertyAddress
		//, PropertyCacheEntry Parent // (if we want to support longer chains of [ptr1]->[ptr2]->...)
	);

	/// <inheritdoc cref="AddressFinder(MemoryReader, ObjectReader, string)"/>
	public ProcessPropertyReader(string baseAddressNeedleHexString) {
		_memoryReader = new();
		_objectReader = new ObjectReader(_memoryReader);
		_addressFinder = new AddressFinder(_memoryReader, _objectReader, baseAddressNeedleHexString);
	}

	/// <inheritdoc cref="AddressFinder(MemoryReader, ObjectReader, ReadOnlySpan{byte})"/>
	public ProcessPropertyReader(ReadOnlySpan<byte> baseAddressNeedle) {
		_memoryReader = new();
		_objectReader = new ObjectReader(_memoryReader);
		_addressFinder = new AddressFinder(_memoryReader, _objectReader, baseAddressNeedle);
	}

	public bool CanRead => TargetProcess is not null && !TargetProcess.HasExitedSafe();

	public Process TargetProcess {
		get => _memoryReader.CurrentProcess;
		set {
			if (ReferenceEquals(_memoryReader.CurrentProcess, value)) return;
			_memoryReader.CurrentProcess = value;
			_propertyCache.Clear();
			_addressFinder.UpdateBaseAddress();
			_objectReader.UpdateIntPtrSize();
		}
	}

	/// <summary>
	/// <para>
	/// Try to read the value of the property named <paramref name="propertyName"/>
	/// in class <typeparamref name="TClass"/> in the process <see cref="TargetProcess"/>. <br/>
	/// Currently works for <typeparamref name="TProperty"/>: <see langword="string"/>s,
	/// base <see langword="struct"/>s (<see langword="int"/>, <see langword="double"/>, <see langword="enum"/>, etc + nullable variants),
	/// simple <see langword="struct"/>s (non-nullable).
	/// </para><para>
	/// WARNING: encoding options currently only apply to strings, not to chars. Chars are currently always read as UTF16. <br/>
	/// eg. if you need to read a UTF8 char, use <typeparamref name="TProperty"/>=<see cref="byte"/> and <typeparamref name="TReturnType"/>=<see cref="char"/>
	/// </para><para>
	/// If you want to return a different type here, you can, using the <typeparamref name="TReturnType"/>. <br/>
	/// For example, <typeparamref name="TProperty"/>=`<see cref="int"/>` and <typeparamref name="TReturnType"/>=`<see cref="int"/>?` is perfectly valid,
	/// as long as an explicit cast succeeds (<c>(<typeparamref name="TReturnType"/>)(object)value</c>)
	/// </para>
	/// </summary>
	/// <typeparam name="TClass">
	/// </typeparam>
	/// <typeparam name="TProperty">
	/// Underlying type for the property.
	/// </typeparam>
	/// <typeparam name="TReturnType">
	/// Return type for the property. Offered for convenience, to avoid requiring casts from <see cref="object"/> on every call. <br/>
	/// Supports using <see cref="object"/>, the actual underlying type for property <paramref name="propertyName"/>, or nullable variants for <see langword="struct"/>s. <br/>
	/// For example, if the property is an <see langword="enum"/> `MyEnum`, you can use `MyEnum?` here
	/// </typeparam>
	/// <param name="propertyName"></param>
	/// <param name="result"></param>
	/// <param name="defaultValue"> Default value for <paramref name="result"/> when the property read wasn't successful </param>
	/// <returns> <see langword="true"/> when a read was successful and <paramref name="result"/> was populated, otherwise <see langword="false"/> </returns>
	// Note: ugly annotation is for Trimming compatibility in .net6+
	public bool TryReadProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	                            TClass,
		                          TProperty,
															TReturnType>
		                         (string propertyName, out TReturnType result, TReturnType defaultValue = default) {
		if (!CanRead) {
			result = defaultValue;
			return false;
		}
		try {
			IntPtr classAddress = IntPtr.Zero;
			IntPtr propAddress = IntPtr.Zero;
			var cacheKey = (typeof(TClass), propertyName);
			if (!_propertyCache.TryGetValue(cacheKey, out var entry)) {
				// lookup memory layout info for property and store in cache
				MemoryAddressInfoAttribute propAttr = null;

				var classAttr = typeof(TClass).GetCustomAttribute<MemoryAddressInfoAttribute>();
				var propInfo = typeof(TClass).GetProperty(propertyName);
				bool isValid = false;
				bool isConstantAddress = false;

				if (propInfo is not null) {
					propAttr = propInfo.GetCustomAttribute<MemoryAddressInfoAttribute>();
					if (propAttr is not null) {
						// lookup address for property to see whether it is a constant
						isValid = _addressFinder.TryFindPropertyAddress(classAttr, propAttr, out classAddress, out propAddress);
						isValid |= propAddress == IntPtr.Zero;
						isConstantAddress =
							isValid
							&& (classAddress == IntPtr.Zero || classAttr?.ShouldFollowClassPointer == false)
							&& propAttr.IsConstantPath;
					}
					else {
#if DEBUG
						Console.Error.WriteLine($"ERROR: Missing {nameof(MemoryAddressInfoAttribute)} on property '{propertyName}' in class {typeof(TClass).FullName}, cannot read prop without it");
						System.Diagnostics.Debugger.Break();
#endif
					}
				}
				else {
#if DEBUG
					Console.Error.WriteLine($"ERROR: Cannot find property '{propertyName}' in class {typeof(TClass).FullName}. Did you mistype something?");
					System.Diagnostics.Debugger.Break();
#endif
				}
				entry = new(classAttr, propAttr, isValid, isConstantAddress, classAddress, propAddress);
				_propertyCache[cacheKey] = entry;
			}

			// lookup address for property
			if (!entry.IsValid) {
				result = defaultValue;
				return false;
			}
			else if (entry.IsConstantAddress) {
				propAddress = entry.PropertyAddress;
			}
			else if (!_addressFinder.TryFindPropertyAddress(entry.ClassAttribute, entry.PropertyAttribute, out classAddress, out propAddress)) {
				result = defaultValue;
				return false;
			}

			// read underlying property value
			int bytesPerChar = 2;
			Encoding encoding = Encoding.Unicode;
			if (entry.PropertyAttribute is not null) {
				bytesPerChar = entry.PropertyAttribute.BytesPerChar;
				encoding = entry.PropertyAttribute.Encoding;
			}

			bool readOk = TryReadValue<TProperty, TReturnType>(propAddress, bytesPerChar, encoding, out result) 
					          && (typeof(TProperty).IsValueType || result is not null);
			if (readOk)
				return true;
			//else
			//	result = defaultValue;
		}
		catch {
		}
		result = defaultValue;
		return false;
	}

	/// <summary>
	/// <para>
	/// Try to read the value of the property named <paramref name="propertyName"/>
	/// in class <typeparamref name="TClass"/> in the process <see cref="TargetProcess"/>. <br/>
	/// Currently works for <typeparamref name="TProperty"/>: <see langword="string"/>s,
	/// base <see langword="struct"/>s (<see langword="int"/>, <see langword="double"/>, <see langword="enum"/>, etc + nullable variants),
	/// simple <see langword="struct"/>s (non-nullable).
	/// </para><para>
	/// WARNING: if you want to return a different type than the underlying property,
	/// eg. if you want to return T? and the actual type T is MyEnum, you must use <see cref="TryReadProperty{TClass, TProperty, TReturnType}"/> instead. <br/>
	/// Otherwise it will read T bytes from the memory, and in-memory, <see cref="System.Nullable{T}"/> is larger than T.
	/// </para><para>
	/// WARNING: encoding options only applies to strings, not to chars. Chars are currently always read as UTF16. <br/>
	/// If you need to read a different encoding, either cast the result yourself or use the overload.
	/// </para>
	/// </summary>
	/// <typeparam name="TProperty"> 
	/// Underlying type and return type for the property.<br/>
	/// Supports using <see cref="object"/>, the actual underlying type for property <paramref name="propertyName"/>, or nullable variants for <see langword="struct"/>s. <br/>
	/// However, if this does not match the property (MyEnum? vs MyEnum) it will lead to a bad time. <br/>
	/// Use <see cref="TryReadProperty{TClass, TProperty, TReturnType}(string, out TReturnType, TReturnType)"/> instead.
	/// </typeparam>
	/// <inheritdoc cref="TryReadProperty{TClass, TProperty, TReturnType}(string, out TReturnType, TReturnType)"/>
	// Note: ugly annotation is for Trimming compatibility in .net6+
	public bool TryReadProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	                            TClass,
		                          TProperty>
		                         (string propertyName, out TProperty result, TProperty defaultValue = default)
		=> TryReadProperty<TClass, TProperty, TProperty>(propertyName, out result, defaultValue);

	/// <inheritdoc cref="TryReadProperty{TClass, TProperty}(string, out TProperty, TProperty)"/>
	// Note: ugly annotation is for Trimming compatibility in .net6+
	public bool TryReadProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	                            TClass>
		                         (string propertyName, out object result, object defaultValue = null)
		=> TryReadProperty<TClass, object>(propertyName, out result, defaultValue);

	/// <returns> the value of the property <paramref name="propertyName"/>, or <paramref name="defaultValue"/> if the read failed </returns>
	/// <inheritdoc cref="TryReadProperty{TClass, TProperty, TReturnType}(string, out TReturnType, TReturnType)"/>
	// Note: ugly annotation is for Trimming compatibility in .net6+
	public TReturnType ReadProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	                                TClass,
		                              TProperty,
																  TReturnType>
		                             (string propertyName, TReturnType defaultValue = default)
		=> TryReadProperty<TClass, TProperty, TReturnType>(propertyName, out var result, defaultValue) ? result : defaultValue;

	/// <returns> the value of the property <paramref name="propertyName"/>, or <paramref name="defaultValue"/> if the read failed </returns>
	/// <inheritdoc cref="TryReadProperty{TClass, TProperty}(string, out TProperty, TProperty)"/>
	// Note: ugly annotation is for Trimming compatibility in .net6+
	public TProperty ReadProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	                              TClass,
		                            TProperty>
		                           (string propertyName, TProperty defaultValue = default)
		=> TryReadProperty<TClass, TProperty>(propertyName, out var result, defaultValue) ? result : defaultValue;

	/// <summary>
	/// Try to read an object of type <typeparamref name="T"/> (but cast and return as type <typeparamref name="TR"/>) 
	/// in the current process starting at address <paramref name="address"/>
	/// </summary>
	/// <returns> The object, cast to type <typeparamref name="TR"/>, or default if the read fails </returns>
	private bool TryReadValue<T, TR>(IntPtr address, int bytesPerChar, Encoding enc, out TR result) {
		// note: this sucks to read but roslyn will evaulate all the typeof(T) checks ahead of time
		if      (address == IntPtr.Zero)            { result = default; return false; }
		else if (typeof(T) == typeof(string))       return castKnown(_objectReader.ReadString(address, bytesPerChar, enc), out result);
		else if (typeof(T) == typeof(int[]))        return castKnown(_objectReader.ReadArray<int>(address), out result);
		else if (typeof(T) == typeof(uint[]))       return castKnown(_objectReader.ReadArray<uint>(address), out result);
		else if (typeof(T) == typeof(long[]))       return castKnown(_objectReader.ReadArray<long>(address), out result);
		else if (typeof(T) == typeof(ulong[]))      return castKnown(_objectReader.ReadArray<ulong>(address), out result);
		else if (typeof(T) == typeof(short[]))      return castKnown(_objectReader.ReadArray<short>(address), out result);
		else if (typeof(T) == typeof(ushort[]))     return castKnown(_objectReader.ReadArray<ushort>(address), out result);
		else if (typeof(T) == typeof(float[]))      return castKnown(_objectReader.ReadArray<float>(address), out result);
		else if (typeof(T) == typeof(double[]))     return castKnown(_objectReader.ReadArray<double>(address), out result);
		else if (typeof(T) == typeof(char[]))       return castKnown(_objectReader.ReadArray<char>(address), out result);
		else if (typeof(T) == typeof(byte[]))       return castKnown(_objectReader.ReadArray<byte>(address), out result);
		else if (typeof(T) == typeof(sbyte[]))      return castKnown(_objectReader.ReadArray<sbyte>(address), out result);
		else if (typeof(T) == typeof(bool[]))       return castKnown(_objectReader.ReadArray<bool>(address), out result);
		else if (typeof(T) == typeof(List<int>))    return castKnown(_objectReader.ReadList<int>(address), out result);
		else if (typeof(T) == typeof(List<uint>))   return castKnown(_objectReader.ReadList<uint>(address), out result);
		else if (typeof(T) == typeof(List<long>))   return castKnown(_objectReader.ReadList<long>(address), out result);
		else if (typeof(T) == typeof(List<ulong>))  return castKnown(_objectReader.ReadList<ulong>(address), out result);
		else if (typeof(T) == typeof(List<short>))  return castKnown(_objectReader.ReadList<short>(address), out result);
		else if (typeof(T) == typeof(List<ushort>)) return castKnown(_objectReader.ReadList<ushort>(address), out result);
		else if (typeof(T) == typeof(List<float>))  return castKnown(_objectReader.ReadList<float>(address), out result);
		else if (typeof(T) == typeof(List<double>)) return castKnown(_objectReader.ReadList<double>(address), out result);
		else if (typeof(T) == typeof(List<char>))   return castKnown(_objectReader.ReadList<char>(address), out result);
		else if (typeof(T) == typeof(List<byte>))   return castKnown(_objectReader.ReadList<byte>(address), out result);
		else if (typeof(T) == typeof(List<sbyte>))  return castKnown(_objectReader.ReadList<sbyte>(address), out result);
		else if (typeof(T) == typeof(List<bool>))   return castKnown(_objectReader.ReadList<bool>(address), out result);
		else {
			// read primitive type
			if (   typeof(T) == typeof(int)
					|| typeof(T) == typeof(uint)
					|| typeof(T) == typeof(long)
					|| typeof(T) == typeof(ulong)
					|| typeof(T) == typeof(short)
					|| typeof(T) == typeof(ushort)
					|| typeof(T) == typeof(float)
					|| typeof(T) == typeof(double)
					|| typeof(T) == typeof(char)
					|| typeof(T) == typeof(bool)
					|| typeof(T) == typeof(byte)
					|| typeof(T) == typeof(sbyte)
					|| typeof(T) == typeof(nint)
					|| typeof(T) == typeof(nuint)) {
				int size = _objectReader.SizeOf<T>();
				var bytes = size > 0 ? _memoryReader.ReadBytes(address, size) : null;
				if (bytes is null) {
					result = default;
					return false;
				}
				return tryReadKnown<T>(bytes, out result);
			}
			// read nullable type
			else if (   typeof(T) == typeof(int?)
							 || typeof(T) == typeof(uint?)
							 || typeof(T) == typeof(long?)
							 || typeof(T) == typeof(ulong?)
							 || typeof(T) == typeof(short?)
							 || typeof(T) == typeof(ushort?)
							 || typeof(T) == typeof(float?)
							 || typeof(T) == typeof(double?)
							 || typeof(T) == typeof(char?)
							 || typeof(T) == typeof(bool?)
							 || typeof(T) == typeof(byte?)
							 || typeof(T) == typeof(sbyte?)
							 || typeof(T) == typeof(nint?)
							 || typeof(T) == typeof(nuint?)
							 || Nullable.GetUnderlyingType(typeof(T)) is not null) {
				// this is the layout of a Nullable. ref https://referencesource.microsoft.com/#mscorlib/system/nullable.cs
				//
				// public struct Nullable<T> where T : struct {
				//   private bool hasValue; 
				//   internal T value;
				// }

				// read hasValue and return early if it doesn't have a value
				var hasValueBuffer = new byte[sizeof(bool)];
				if (!_memoryReader.TryReadBytes(address, hasValueBuffer, sizeof(bool))) {
					result = default;
					return false;
				}
				bool hasValue = BitConverter.ToBoolean(hasValueBuffer, 0);
				if (!hasValue) {
					result = default;
					return true;
				}

				// read the value
				int size = _objectReader.SizeOf<T>();
				var bytes = size > 0 ? _memoryReader.ReadBytes(address + sizeof(bool), size) : null;
				if (bytes is null) {
					result = default;
					return false;
				}
				else if (typeof(T) == typeof(int?))    return tryReadKnown<int>(bytes, out result);
				else if (typeof(T) == typeof(uint?))   return tryReadKnown<uint>(bytes, out result);
				else if (typeof(T) == typeof(long?))   return tryReadKnown<long>(bytes, out result);
				else if (typeof(T) == typeof(ulong?))  return tryReadKnown<ulong>(bytes, out result);
				else if (typeof(T) == typeof(short?))  return tryReadKnown<short>(bytes, out result);
				else if (typeof(T) == typeof(ushort?)) return tryReadKnown<ushort>(bytes, out result);
				else if (typeof(T) == typeof(float?))  return tryReadKnown<float>(bytes, out result);
				else if (typeof(T) == typeof(double?)) return tryReadKnown<double>(bytes, out result);
				else if (typeof(T) == typeof(char?))   return tryReadKnown<char>(bytes, out result);
				else if (typeof(T) == typeof(bool?))   return tryReadKnown<bool>(bytes, out result);
				else if (typeof(T) == typeof(byte?))   return tryReadKnown<byte>(bytes, out result);
				else if (typeof(T) == typeof(sbyte?))  return tryReadKnown<sbyte>(bytes, out result);
				else if (typeof(T) == typeof(nint?))   return tryReadKnown<nint>(bytes, out result);
				else if (typeof(T) == typeof(nuint?))  return tryReadKnown<nuint>(bytes, out result);
				else
					return TryReadUnaligned(bytes, size, out result);
			}
			// unknown, non-nullable type
			else {
				int size = _objectReader.SizeOf<T>();
				var bytes = size > 0 ? _memoryReader.ReadBytes(address, size) : null;
				if (bytes is null) {
					result = default;
					return false;
				}
				return TryReadUnaligned(bytes, size, out result);
			}
		}

		// try to read a known type from bytes
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool tryReadKnown<TIn>(byte[] bytes, out TR result) {
			// note: this sucks to read but roslyn will evaulate all the typeof(T) checks ahead of time
			if      (typeof(TIn) == typeof(int))    return castKnown(BitConverter.ToInt32(bytes, 0), out result);
			else if (typeof(TIn) == typeof(uint))   return castKnown(BitConverter.ToUInt32(bytes, 0), out result);
			else if (typeof(TIn) == typeof(long))   return castKnown(BitConverter.ToInt64(bytes, 0), out result);
			else if (typeof(TIn) == typeof(ulong))  return castKnown(BitConverter.ToUInt64(bytes, 0), out result);
			else if (typeof(TIn) == typeof(short))  return castKnown(BitConverter.ToInt16(bytes, 0), out result);
			else if (typeof(TIn) == typeof(ushort)) return castKnown(BitConverter.ToUInt16(bytes, 0), out result);
			else if (typeof(TIn) == typeof(float))  return castKnown(BitConverter.ToSingle(bytes, 0), out result);
			else if (typeof(TIn) == typeof(double)) return castKnown(BitConverter.ToDouble(bytes, 0), out result);
			else if (typeof(TIn) == typeof(char))   return castKnown(BitConverter.ToChar(bytes, 0), out result); // TODO: support non-utf16 chars
			else if (typeof(TIn) == typeof(bool))   return castKnown(BitConverter.ToBoolean(bytes, 0), out result);
			else if (typeof(TIn) == typeof(byte))   return castKnown(bytes[0], out result);
			else if (typeof(TIn) == typeof(sbyte))  return castKnown((sbyte)bytes[0], out result);
			else if (typeof(TIn) == typeof(nint)
				    || typeof(TIn) == typeof(nuint)) {
				// these refer to nint/nuint in the Process' memory space, so we need to use its bitness for size info
				if (_memoryReader.IntPtrSize == 8) {
					if (typeof(TIn) == typeof(nint))
						return castKnown(BitConverter.ToInt64(bytes, 0), out result);
					else
						return castKnown(BitConverter.ToUInt64(bytes, 0), out result);
				}
				else if (_memoryReader.IntPtrSize == 4) {
					if (typeof(TIn) == typeof(nint))
						return castKnown(BitConverter.ToInt32(bytes, 0), out result);
					else
						return castKnown(BitConverter.ToUInt32(bytes, 0), out result);
				}
			}
			result = default;
			return false;
		}

		// cast a generic value to a different generic type, and return true ("known" = we know this is safe, so always return true)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool castKnown<TIn>(TIn value, out TR result) {
			if (typeof(TIn) == typeof(TR)) {
				var refResult = value;
				result = Unsafe.As<TIn, TR>(ref refResult);
			}
			else {
				result = value is TR rVal ? rVal : (TR)(object)value;
			}
			return true;
		}
	}

	/// <summary>
	/// Read an object of type <typeparamref name="T"/> from the given bytes (reads n=<paramref name="size"/> bytes). <br/>
	/// This will probably work for sequential structs, but fail for objects. It doesn't examine method tables, etc. <br/>
	/// This is meant to be used as a fallback, not as a primary 'read this' function.
	/// </summary>
	private static bool TryReadUnaligned<T>(ReadOnlySpan<byte> buffer, int size, out T result) {
		try {
			if (size == 1) {
				var refResult = buffer[0];
				result = Unsafe.As<byte, T>(ref refResult);
			}
			else {
				ref var source = ref MemoryMarshal.GetReference(buffer);
				result = size switch {
					2 => castRead<Int16>(ref source),
					4 => castRead<Int32>(ref source),
					8 => castRead<Int64>(ref source),
					_ => Unsafe.ReadUnaligned<T>(ref source)
				};
			}
			return true;
		}
		catch {
			result = default;
			return false;
		}

		// read an object of type `TSource` starting at the pointer `source` and return as type `T`
		//   most of this is shenanigans to support returning struct `T` as `T?`
		//   for example, if actual property is `MyEnum` but a user calls `TryReadUnaligned<MyEnum?>`
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static T castRead<TSource>(ref byte source) {
			var temp = Unsafe.ReadUnaligned<TSource>(ref source);
			if (typeof(TSource) == typeof(T))
				return Unsafe.As<TSource, T>(ref temp);
			else if (temp is T tResult)
				return tResult;
			var underlyingType = Nullable.GetUnderlyingType(typeof(T));
			if (underlyingType is not null) {
				if (underlyingType.IsEnum)
					return (T)Enum.ToObject(underlyingType, temp);
				else
					return (T)Convert.ChangeType(temp, underlyingType);
			}
			else
				return (T)(object)temp;
		}
	}


	private bool _isDisposed = false;
	protected virtual void Dispose(bool disposing) {
		if (!_isDisposed) {
			if (disposing) {
				// dispose managed state (managed objects)
				_propertyCache.Clear();
				_memoryReader.Dispose();
				_addressFinder.Dispose();
				_objectReader.Dispose();
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
