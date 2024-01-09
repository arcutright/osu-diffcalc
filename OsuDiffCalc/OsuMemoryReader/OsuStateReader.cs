using System;
using System.Diagnostics;
using System.IO;
using OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;
using OsuDiffCalc.Utility;

namespace OsuDiffCalc.OsuMemoryReader;

internal readonly record struct OsuMemoryState(
	OsuStatus Status,
	OsuGameMode GameMode,
	OsuMods Mods,
	int MapId,
	int SetId,
	string MapString,
	string FolderName,
	string OsuFileName,
	string MD5FileHash
) {
	public static OsuMemoryState Invalid { get; } = new(OsuStatus.Unknown, OsuGameMode.Unknown, OsuMods.Unknown, -1, -1, null, null, null, null);

	public bool IsInGame => Status is OsuStatus.Playing or OsuStatus.ResultsScreen or OsuStatus.MultiplayerResultsScreen;
	public bool IsInEditor => Status is OsuStatus.EditingMap;
}

internal class OsuStateReader {
	private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();
	private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

	// Can find needle in https://github.com/Piotrekol/ProcessMemoryDataFinder/blob/95030bba9c5e2de0667c4ae95e6e6b1fbbde3d5c/OsuMemoryDataProvider/StructuredOsuMemoryReader.cs#L53
	private const string _osuBaseAddressNeedleHexString = "F80174048365";
	private static readonly ProcessPropertyReader _memoryReader = new(_osuBaseAddressNeedleHexString);
	
	private static OsuStatus _prevStatus = OsuStatus.Unknown;
	private static OsuGameMode _prevGameMode = OsuGameMode.Unknown;
	private static OsuMods _prevMods = OsuMods.Unknown;
	private static string _prevMapString = "", _prevOsuFileName = "";

	/// <summary>
	/// Throw away references to the previously cached process used when reading
	/// the memory state and reset all the property address lookups. <br/>
	/// Future calls to <see cref="TryReadCurrentOsuState"/> will open a new handle
	/// and rebuild the property cache tree.
	/// </summary>
	public static void ClearCache() {
		_memoryReader.TargetProcess = null;
		_prevStatus = OsuStatus.Unknown;
		_prevGameMode = OsuGameMode.Unknown;
		_prevMods = OsuMods.Unknown;
		_prevMapString = "";
		_prevOsuFileName = "";
	}

	public static bool TryReadCurrentOsuState(Process osuProcess, out OsuMemoryState currentState) {
		if (osuProcess is not null && !osuProcess.HasExitedSafe()) {
			try {
				_memoryReader.TargetProcess = osuProcess;

				// read game state from memory
				var status = _memoryReader.ReadProperty<GeneralData, OsuStatus>(nameof(GeneralData.RawStatus), OsuStatus.Unknown);
				var gameMode = _memoryReader.ReadProperty<GeneralData, OsuGameMode>(nameof(GeneralData.GameMode), OsuGameMode.Unknown);
				var mods = _memoryReader.ReadProperty<GeneralData, OsuMods>(nameof(GeneralData.Mods), OsuMods.Unknown);
				var mapId = _memoryReader.ReadProperty<GeneralData, int>(nameof(CurrentBeatmap.Id), -1);
				var setId = _memoryReader.ReadProperty<GeneralData, int>(nameof(CurrentBeatmap.SetId), -1);
				var mapString = _memoryReader.ReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.MapString), "")?.Trim() ?? "";
				var folderName = _memoryReader.ReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.FolderName), "")?.Trim() ?? "";
				var osuFileName = _memoryReader.ReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.OsuFileName), "")?.Trim() ?? "";
				var md5FileHash = _memoryReader.ReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.MD5FileHash), "")?.Trim() ?? "";

				// empty out file names if they are invalid
				// this can happen when:
				//   1. the game state was changing while we were reading its memory
				//   2. the game was updated and the addresses are pointing at junk
				if (folderName.IndexOfAny(_invalidPathChars) != -1)
					folderName = "";
				if (osuFileName.IndexOfAny(_invalidFileNameChars) != -1)
					osuFileName = "";

				currentState = new OsuMemoryState(status, gameMode, mods, mapId, setId, mapString, folderName, osuFileName, md5FileHash);

				// debug info
				if (status != _prevStatus) {
					Console.WriteLine($"   memory current status: {status}");
					_prevStatus = status;
				}
				if (gameMode != _prevGameMode) {
					Console.WriteLine($"   memory current game mode: {gameMode}");
					_prevGameMode = gameMode;
				}
				if (mods != _prevMods) {
					Console.WriteLine($"   memory current mods: {mods}");
					_prevMods = mods;
				}
				if (mapString != _prevMapString) {
					Console.WriteLine($"   memory map string: {mapString}");
					_prevMapString = mapString;
				}
				if (osuFileName != _prevOsuFileName) {
					Console.WriteLine($"   memory osu file name: {osuFileName}");
					_prevOsuFileName = osuFileName;
				}

				// empty out file names if the strings are invalid
				if (currentState.FolderName.IndexOfAny(_invalidPathChars) != -1) {
					currentState = currentState with { FolderName = "" };
				}
				if (currentState.OsuFileName.IndexOfAny(_invalidFileNameChars) != -1) {
					currentState = currentState with { OsuFileName = "" };
				}

				// return [if state looks usable]
				return status != OsuStatus.Unknown
					&& gameMode != OsuGameMode.Unknown
					// sometimes 1 of 3 may be garbled, but usually the later fallbacks can handle this and still find the current map
					&& !(string.IsNullOrEmpty(mapString)
						&& string.IsNullOrEmpty(osuFileName)
						&& string.IsNullOrEmpty(folderName));
			}
			catch (Exception ex) {
				Console.WriteLine("Failed to find current beatmap for osu process");
				Console.WriteLine(ex);
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
			}
		}
		currentState = OsuMemoryState.Invalid;
		return false;
	}
}
