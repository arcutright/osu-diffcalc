using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.OsuMemoryReader;

/// <summary>
/// Attribute describing the location in process memory where this class/property can be found <br/>
/// Some examples of usage:
/// <code>
/// // the base of properties in MyClass will be at *[ProcessBase-0xC] instead of simply ProcessBase
/// [MemoryAddressInfo(Offset = -0xC)]
/// public class MyClass {
///   [MemoryAddressInfo(Offset = 0xCC)] 
///   public int Id { get; set; } // Id is at *[ProcessBase-0xC]+0xCC
///   
///   // Base for HardToFindStruct is wherever this needle is, instead of the class base
///   [MemoryAddressInfo(PathNeedle = "C8FF??????????810D????????00080000", Offset = +0x9)] 
///   public MyStruct HardToFindStruct { get; set; } // struct starts at *[needleLocation]+0x9
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
[DebuggerDisplay("Path: '{Path}', Offset: {Offset}, Encoding: {Encoding?.BodyName}")]
class MemoryAddressInfoAttribute : Attribute {
	private Encoding _encoding = null;

	public MemoryAddressInfoAttribute() {
		if (BytesPerChar <= 0)
			BytesPerChar = GetBytesPerChar(Encoding);
		if (Encoding is null) {
			if (!string.IsNullOrEmpty(EncodingName))
				Encoding = GetEncoding(EncodingName);
		}
		if (BytesPerChar <= 0)
			BytesPerChar = GetBytesPerChar(Encoding);
		if (Encoding is null)
			Encoding = GetEncoding(BytesPerChar);
	}

	/// <summary>
	/// Base memory address path. null/"base" = process base. <br/>
	/// Currently only supports null/"base" or constant hex string (no wildcards),
	/// where the hex string is a valid address in the process' memory space. <br/>
	/// If this is attribute is attached to a class, all properties will be based off of this.
	/// </summary>
	public string Path { get; init; } = null;

	/// <summary>
	/// Search for this needle in the memory to act as the base path. <br/>
	/// Supports hex string needle ("??" for wildcard), eg "12FD348A" or "????A6B4C3". <br/>
	/// If this is attribute is attached to a class, all properties will be based off of this. <br/>
	/// <br/>
	/// Note that Path will override this if it is non-empty.
	/// </summary>
	public string PathNeedle { get; init; } = null;

	/// <summary>
	/// Offset from the Path (or from the start of the address of the PathNeedle)
	/// </summary>
	public int Offset { get; init; } = 0;

	/// <summary>
	/// If <see langword="true"/>, will cache the property path calculations (only when possible, otherwise this is ignored). <br/>
	/// If <see langword="false"/>, may end up scanning the process' memory constantly
	/// (if we need to scan for a PathNeedle, for example).
	/// </summary>
	public bool IsConstantPath { get; init; } = true;

	/// <summary>
	/// If <see langword="true"/>, dereference the pointer at [ClassAttr.Path + ClassAttr.Offset] before adding [PropAttr.Offset]. <br/>
	/// This only has meaning when this attribute is attached to a class and while evaluating the class' property values.
	/// </summary>
	public bool ShouldFollowClassPointer { get; init; } = true;

	/// <inheritdoc cref="Encoding"/>
	public int BytesPerChar { get; init; } = 2;

	/// <inheritdoc cref="Encoding"/>
	public string EncodingName { get; init; } = "utf-16";

	/// <summary>
	/// String encoding (if this is attached to a string property, otherwise this is ignored). <br/>
	/// Note that dotnet processes always store strings in UTF16 (aka Unicode) 2-byte-per-char. <br/>
	/// This only applies to strings, not to chars. Chars are currently always read as UTF16
	/// </summary>
	public Encoding Encoding {
		get => _encoding ??= GetEncoding(EncodingName);
		set => _encoding = value;
	}

	private Encoding GetEncoding(string name) {
		return name?.ToLower() switch {
			null or ""
				=> null,
			"ascii"
				=> Encoding.ASCII,
			"utf7" or "utf-7" or "utf8" or "utf-8"
				=> Encoding.UTF8,
			"utf16" or "utf-16" or "unicode" or "utf16le" or "utf-16le"
				=> Encoding.Unicode,
			"utf16be" or "utf-16be"
				=> Encoding.BigEndianUnicode,
			"utf32" or "utf-32"
				=> Encoding.UTF32,
			"utf32be" or "utf-32be"
				=> new UTF32Encoding(true, true),
			_
				=> Encoding.GetEncoding(name),
		};
	}

	private Encoding GetEncoding(int bytesPerChar) {
		return bytesPerChar switch {
			<= 0 => null,
			1 => Encoding.UTF8,
			2 => Encoding.Unicode,
			>= 3 => Encoding.UTF32,
		};
	}

	private int GetBytesPerChar(Encoding encoding) {
		// Encoding.GetEncodings().Select(e => e.GetEncoding()).Select(e2 => (e2.BodyName, e2.CodePage)).ToList()
		// see https://docs.microsoft.com/en-us/windows/win32/intl/code-page-identifiers?redirectedfrom=MSDN
		if (encoding is null)
			return 0;
		else if (encoding.BodyName is "ascii")                return 1;
		else if (encoding.BodyName is "utf-7"  or "utf-8")    return 1;
		else if (encoding.BodyName is "utf-16" or "utf-16BE") return 2;
		else if (encoding.BodyName is "utf-32" or "utf-32BE") return 4;
		else if (encoding.CodePage is 20127)          return 1; // ascii
		else if (encoding.CodePage is 65000 or 65001) return 1; // utf-7,  utf-8
		else if (encoding.CodePage is 1200  or 1201)  return 2; // utf-16, utf-16be
		else if (encoding.CodePage is 12000 or 12001) return 4; // utf-32, utf-32be
		else if (encoding.IsSingleByte)
			return 1;
		else 
			return Encoding.GetMaxByteCount(1);
	}
}
