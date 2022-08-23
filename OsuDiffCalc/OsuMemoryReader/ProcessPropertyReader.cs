using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OsuDiffCalc.Utility;

namespace OsuDiffCalc.OsuMemoryReader;

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
	/// Try to read the value of the property named <paramref name="propertyName"/>
	/// in class <typeparamref name="TClass"/> in the process <see cref="TargetProcess"/>. <br/>
	/// Currently works for <typeparamref name="TProperty"/>: <see langword="string"/>s,
	/// base <see langword="struct"/>s (<see langword="int"/>, <see langword="double"/>, <see langword="enum"/>, etc + nullable variants),
	/// simple <see langword="struct"/>s (non-nullable)
	/// </summary>
	/// <typeparam name="TClass">
	/// </typeparam>
	/// <typeparam name="TProperty">
	/// Return type for the property. Offered for convenience, to avoid requiring casts from <see cref="object"/> on every call. <br/>
	/// Supports using <see cref="object"/>, the actual underlying type for property <paramref name="propertyName"/>, or nullable variants for <see langword="struct"/>s. <br/>
	/// For example, if the property is an <see langword="enum"/> `MyEnum`, you can use `MyEnum?` here
	/// </typeparam>
	/// <param name="propertyName"></param>
	/// <param name="result"></param>
	/// <param name="defaultValue"> Default value for <paramref name="result"/> when the property read wasn't successful </param>
	/// <returns> <see langword="true"/> when a read was successful and <paramref name="result"/> was populated, otherwise <see langword="false"/> </returns>
	public bool TryReadProperty<TClass, TProperty>(string propertyName, out TProperty result, TProperty defaultValue = default) {
		result = defaultValue;
		try {
			IntPtr classAddress, propAddress;
			var cacheKey = (typeof(TClass), propertyName);
			if (!_propertyCache.TryGetValue(cacheKey, out var entry)) {
				// lookup memory layout info for property and store in cache
				MemoryAddressInfoAttribute propAttr;

				var classAttr = typeof(TClass).GetCustomAttribute<MemoryAddressInfoAttribute>();
				var propInfo = typeof(TClass).GetProperty(propertyName);
				bool isValid;
				bool isConstantAddress;

				if (propInfo is not null) {
					propAttr = propInfo.GetCustomAttribute<MemoryAddressInfoAttribute>();

					// lookup address for property to see whether it is a constant
					isValid = _addressFinder.TryFindPropertyAddress(classAttr, propAttr, out classAddress, out propAddress);
					isValid |= propAddress == IntPtr.Zero;
					isConstantAddress = classAddress == IntPtr.Zero;
				}
				else {
					propAttr = null;
					classAddress = IntPtr.Zero;
					propAddress = IntPtr.Zero;
					isValid = false;
					isConstantAddress = false;
				}
				entry = new(classAttr, propAttr, isValid, isConstantAddress, classAddress, propAddress);
				_propertyCache[cacheKey] = entry;
			}

			// lookup address for property
			if (!entry.IsValid)
				return false;
			else if (entry.IsConstantAddress)
				propAddress = entry.PropertyAddress;
			else if (!_addressFinder.TryFindPropertyAddress(entry.ClassAttribute, entry.PropertyAttribute, out classAddress, out propAddress))
				return false;

			// read underlying property value
			int bytesPerChar = 2;
			Encoding encoding = Encoding.Unicode;
			if (entry.PropertyAttribute is not null) {
				bytesPerChar = entry.PropertyAttribute.BytesPerChar;
				encoding = entry.PropertyAttribute.Encoding;
			}

			bool readOk = TryReadValue(propAddress, bytesPerChar, encoding, out result) 
					          && (typeof(TProperty).IsValueType || result is not null);
			if (readOk)
				return true;
			else
				result = defaultValue;
		}
		catch {
		}
		return false;
	}

	/// <inheritdoc cref="TryReadProperty{TClass, TProperty}(string, out TProperty, TProperty)"/>
	public bool TryReadProperty<TClass>(string propertyName, out object result, object defaultValue = null) {
		return TryReadProperty<TClass, object>(propertyName, out result, defaultValue);
	}

	/// <returns> the value of the property <paramref name="propertyName"/>, or <paramref name="defaultValue"/> if the read failed </returns>
	/// <inheritdoc cref="TryReadProperty{TClass, TProperty}(string, out TProperty, TProperty)"/>
	public TProperty ReadProperty<TClass, TProperty>(string propertyName, TProperty defaultValue = default) {
		return TryReadProperty<TClass, TProperty>(propertyName, out var result, defaultValue) ? result : defaultValue;
	}

	/// <inheritdoc cref="ReadProperty{TClass, TProperty}(string, TProperty)"/>
	public object ReadProperty<TClass>(string propertyName, object defaultValue = default) {
		return TryReadProperty<TClass, object>(propertyName, out var result, defaultValue) ? result : defaultValue;
	}

	private bool TryReadValue<T>(IntPtr address, int bytesPerChar, Encoding enc, out T result) {
		// note: this sucks to read but roslyn will evaulate all the typeof(T) checks ahead of time
		if      (address == IntPtr.Zero)            { result = default; return false; }
		else if (typeof(T) == typeof(string))       { result = cast(_objectReader.ReadString(address, bytesPerChar, enc)); return true; }
		else if (typeof(T) == typeof(int[]))        { result = cast(_objectReader.ReadArray<int>(address));                return true; }
		else if (typeof(T) == typeof(uint[]))       { result = cast(_objectReader.ReadArray<uint>(address));               return true; }
		else if (typeof(T) == typeof(long[]))       { result = cast(_objectReader.ReadArray<long>(address));               return true; }
		else if (typeof(T) == typeof(ulong[]))      { result = cast(_objectReader.ReadArray<ulong>(address));              return true; }
		else if (typeof(T) == typeof(short[]))      { result = cast(_objectReader.ReadArray<short>(address));              return true; }
		else if (typeof(T) == typeof(ushort[]))     { result = cast(_objectReader.ReadArray<ushort>(address));             return true; }
		else if (typeof(T) == typeof(float[]))      { result = cast(_objectReader.ReadArray<float>(address));              return true; }
		else if (typeof(T) == typeof(double[]))     { result = cast(_objectReader.ReadArray<double>(address));             return true; }
		else if (typeof(T) == typeof(char[]))       { result = cast(_objectReader.ReadArray<char>(address));               return true; }
		else if (typeof(T) == typeof(byte[]))       { result = cast(_objectReader.ReadArray<byte>(address));               return true; }
		else if (typeof(T) == typeof(sbyte[]))      { result = cast(_objectReader.ReadArray<sbyte>(address));              return true; }
		else if (typeof(T) == typeof(bool[]))       { result = cast(_objectReader.ReadArray<bool>(address));               return true; }
		else if (typeof(T) == typeof(List<int>))    { result = cast(_objectReader.ReadList<int>(address));                 return true; }
		else if (typeof(T) == typeof(List<uint>))   { result = cast(_objectReader.ReadList<uint>(address));                return true; }
		else if (typeof(T) == typeof(List<long>))   { result = cast(_objectReader.ReadList<long>(address));                return true; }
		else if (typeof(T) == typeof(List<ulong>))  { result = cast(_objectReader.ReadList<ulong>(address));               return true; }
		else if (typeof(T) == typeof(List<short>))  { result = cast(_objectReader.ReadList<short>(address));               return true; }
		else if (typeof(T) == typeof(List<ushort>)) { result = cast(_objectReader.ReadList<ushort>(address));              return true; }
		else if (typeof(T) == typeof(List<float>))  { result = cast(_objectReader.ReadList<float>(address));               return true; }
		else if (typeof(T) == typeof(List<double>)) { result = cast(_objectReader.ReadList<double>(address));              return true; }
		else if (typeof(T) == typeof(List<char>))   { result = cast(_objectReader.ReadList<char>(address));                return true; }
		else if (typeof(T) == typeof(List<byte>))   { result = cast(_objectReader.ReadList<byte>(address));                return true; }
		else if (typeof(T) == typeof(List<sbyte>))  { result = cast(_objectReader.ReadList<sbyte>(address));               return true; }
		else if (typeof(T) == typeof(List<bool>))   { result = cast(_objectReader.ReadList<bool>(address));                return true; }
		else {
			int size = _objectReader.SizeOf<T>();
			var bytes = size > 0 ? _memoryReader.ReadBytes(address, size) : null;
			if      (bytes is null)                                               { result = default; return false; }
			else if (typeof(T) == typeof(int)    || typeof(T) == typeof(int?))    { result = cast(BitConverter.ToInt32(bytes, 0));   return true; }
			else if (typeof(T) == typeof(uint)   || typeof(T) == typeof(uint?))   { result = cast(BitConverter.ToUInt32(bytes, 0));  return true; }
			else if (typeof(T) == typeof(long)   || typeof(T) == typeof(long?))   { result = cast(BitConverter.ToInt64(bytes, 0));   return true; }
			else if (typeof(T) == typeof(ulong)  || typeof(T) == typeof(ulong?))  { result = cast(BitConverter.ToUInt64(bytes, 0));  return true; }
			else if (typeof(T) == typeof(short)  || typeof(T) == typeof(short?))  { result = cast(BitConverter.ToInt16(bytes, 0));   return true; }
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?)) { result = cast(BitConverter.ToUInt16(bytes, 0));  return true; }
			else if (typeof(T) == typeof(float)  || typeof(T) == typeof(float?))  { result = cast(BitConverter.ToSingle(bytes, 0));  return true; }
			else if (typeof(T) == typeof(double) || typeof(T) == typeof(double?)) { result = cast(BitConverter.ToDouble(bytes, 0));  return true; }
			else if (typeof(T) == typeof(char)   || typeof(T) == typeof(char?))   { result = cast(BitConverter.ToChar(bytes, 0));    return true; }
			else if (typeof(T) == typeof(bool)   || typeof(T) == typeof(bool?))   { result = cast(BitConverter.ToBoolean(bytes, 0)); return true; }
			else if (typeof(T) == typeof(byte)   || typeof(T) == typeof(byte?))   { result = cast(bytes[0]);                         return true; }
			else if (typeof(T) == typeof(sbyte)  || typeof(T) == typeof(sbyte?))  { result = cast(bytes[0]);                         return true; }
			else 
				return TryReadUnaligned(bytes, size, out result);
		}

		static T cast<TIn>(TIn obj) => obj is T objOut ? objOut : (T)(object)obj;
	}

	private static bool TryReadUnaligned<T>(ReadOnlySpan<byte> buffer, int size, out T result) {
		try {
			if (size == 1)
				result = (T)(object)buffer[0];
			else {
				ref var source = ref MemoryMarshal.GetReference(buffer);
				result = size switch {
					2  => castRead<Int16>(ref source),
					4  => castRead<Int32>(ref source),
					8  => castRead<Int64>(ref source),
					_  => Unsafe.ReadUnaligned<T>(ref source)
				};
			}
			return true;
		}
		catch {
			result = default;
			return false;
		}

		// shenanigans to support returning struct `T` as `T?`
		// for example, if actual property is `MyEnum` but a user calls `TryReadValue<MyEnum?>`
		static T castRead<T2>(ref byte source) {
			var temp = Unsafe.ReadUnaligned<T2>(ref source);
			if (temp is T tResult)
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
