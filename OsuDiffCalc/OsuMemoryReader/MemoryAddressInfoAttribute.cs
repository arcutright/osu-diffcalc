using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.OsuMemoryReader;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
[DebuggerDisplay("Path: '{Path}', Offset: {Offset}, Encoding: {Encoding?.BodyName}")]
class MemoryAddressInfoAttribute : Attribute {
	private Encoding _encoding = null;

	/// <summary> Base memory path. Default = Base. If this is a class, all properties will be based off of this </summary>
	public string Path { get; init; } = null;
	public int Offset { get; init; } = 0;
	public bool IndirectClassPointer { get; init; } = true;
	public int BytesPerChar { get; init; } = 2;
	public string EncodingName { get; init; } = "Unicode";
	public Encoding Encoding {
		get => _encoding ??= GetEncoding(EncodingName);
		set => _encoding = value;
	}

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

	private Encoding GetEncoding(string name) {
		return name?.ToLower() switch {
			null or "" => null,
			"ascii" => Encoding.ASCII,
			"utf7" or "utf-7" => Encoding.UTF7,
			"utf8" or "utf-8" => Encoding.UTF8,
			"utf32" or "utf-32" => Encoding.UTF32,
			"utf16" or "utf-16" or "unicode" or "unicode-little" or "little-unicode" or "littleendianunicode" => Encoding.Unicode,
			"bigendianunicode" or "unicode-big" or "big-unicode" => Encoding.BigEndianUnicode,
			_ => Encoding.GetEncoding(name),
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
		if (encoding is null) return 0;
		else if (encoding.BodyName == "ascii")          return 1;
		else if (encoding.BodyName == "utf-7")          return 1;
		else if (encoding.BodyName == "utf-8")          return 1;
		else if (encoding.BodyName == "utf-16")         return 2;
		else if (encoding.BodyName == "utf-32")         return 4;
		else if (encoding == Encoding.ASCII)            return 1;
		else if (encoding == Encoding.UTF7)             return 1;
		else if (encoding == Encoding.UTF8)             return 1;
		else if (encoding == Encoding.Unicode)          return 2;
		else if (encoding == Encoding.BigEndianUnicode) return 2;
		else if (encoding.IsSingleByte)                 return 1;
		else return Encoding.GetMaxByteCount(1);
	}
}
