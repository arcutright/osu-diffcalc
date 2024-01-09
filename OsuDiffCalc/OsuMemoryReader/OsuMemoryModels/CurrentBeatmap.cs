namespace OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;

// See https://github.com/Piotrekol/ProcessMemoryDataFinder/tree/f6db3a1dea0abc179aea7f3aeb43ec47863f6e5a/OsuMemoryDataProvider/OsuMemoryModels/Direct
// See https://github.com/ppy/osu/blob/4bc26dbb487241e2bbae73751dbe9e93a4e427da/osu.Game/Beatmaps/BeatmapInfo.cs#L27

[MemoryAddressInfo(Offset = -0xC)]
public class CurrentBeatmap {
	[MemoryAddressInfo(Offset = 0xC8)]
	public int Id { get; set; }

	[MemoryAddressInfo(Offset = 0xCC)]
	public int SetId { get; set; }

	[MemoryAddressInfo(Offset = 0x80)]
	public string MapString { get; set; }

	[MemoryAddressInfo(Offset = 0x78)]
	public string FolderName { get; set; }

	/// <summary>
	/// File name of currently loaded .osu file (including extension)
	/// </summary>
	[MemoryAddressInfo(Offset = 0x90)]
	public string OsuFileName { get; set; }

	/// <summary>
	/// MapString with unicode chars if there are any (eg "{Song name} [{diff name}]")
	/// </summary>
	[MemoryAddressInfo(Offset = 0x84)]
	public string MapStringUnicode { get; set; }

	/// <summary>
	/// MapString after artist name (eg "{Song name} [{diff name}]")
	/// </summary>
	[MemoryAddressInfo(Offset = 0x88)]
	public string ShortMapString { get; set; }

	/// <summary>
	/// MD5 hash of currently loaded .osu file (including extension)
	/// </summary>
	[MemoryAddressInfo(Offset = 0x6C)]
	public string MD5FileHash { get; set; }

	[MemoryAddressInfo(Offset = 0x2C)]
	public float Ar { get; set; }

	[MemoryAddressInfo(Offset = 0x30)]
	public float Cs { get; set; }

	[MemoryAddressInfo(Offset = 0x34)]
	public float Hp { get; set; }

	[MemoryAddressInfo(Offset = 0x38)]
	public float Od { get; set; }
}
