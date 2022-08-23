namespace OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;

// See https://github.com/Piotrekol/ProcessMemoryDataFinder/tree/95030bba9c5e2de0667c4ae95e6e6b1fbbde3d5c/OsuMemoryDataProvider/OsuMemoryModels/Direct
// or https://github.com/Piotrekol/ProcessMemoryDataFinder/blob/95030bba9c5e2de0667c4ae95e6e6b1fbbde3d5c/OsuMemoryDataProvider/StructuredOsuMemoryReader.cs#L61
public class GeneralData {
	[MemoryAddressInfo(Offset = -0x3C)]
	public OsuStatus RawStatus { get; set; }

	[MemoryAddressInfo(Offset = -0x33)]
	public OsuGameMode GameMode { get; set; }
}
