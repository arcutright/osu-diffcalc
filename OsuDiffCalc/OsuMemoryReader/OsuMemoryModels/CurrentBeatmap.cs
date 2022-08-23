namespace OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;

// See https://github.com/Piotrekol/ProcessMemoryDataFinder/tree/95030bba9c5e2de0667c4ae95e6e6b1fbbde3d5c/OsuMemoryDataProvider/OsuMemoryModels/Direct
// See https://github.com/ppy/osu/blob/4bc26dbb487241e2bbae73751dbe9e93a4e427da/osu.Game/Beatmaps/BeatmapInfo.cs#L27

[MemoryAddressInfo(Offset = -0xC)]
public class CurrentBeatmap {
	[MemoryAddressInfo(Offset = 0xCC)]
	public int Id { get; set; }

	[MemoryAddressInfo(Offset = 0xD0)]
	public int SetId { get; set; }

	[MemoryAddressInfo(Offset = 0x80)]
	public string MapString { get; set; }

	[MemoryAddressInfo(Offset = 0x78)]
	public string FolderName { get; set; }

	[MemoryAddressInfo(Offset = 0x94)]
	public string OsuFileName { get; set; }

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
