using System;

namespace OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;


public enum OsuGameMode : int {
	Unknown  = -1,
	Standard = 0,
	Taiko    = 1,
	Catch    = 2,
	Mania    = 3,
}

// See https://github.com/Piotrekol/ProcessMemoryDataFinder/blob/95030bba9c5e2de0667c4ae95e6e6b1fbbde3d5c/OsuMemoryDataProvider/OsuMemoryStatus.cs
public enum OsuStatus : int {
	/// <summary>
	/// Indicates that status read in osu memory is not defined in <see cref="OsuStatus"/>
	/// </summary>
	Unknown                  = -2,
	NotRunning               = -1,
	MainMenu                 = 0,
	EditingMap               = 1,
	Playing                  = 2,
	GameShutdownAnimation    = 3,
	SongSelectEdit           = 4,
	SongSelect               = 5,
	ResultsScreen            = 7,
	GameStartupAnimation     = 10,
	MultiplayerRooms         = 11,
	MultiplayerRoom          = 12,
	MultiplayerSongSelect    = 13,
	MultiplayerResultsScreen = 14,
	OsuDirect                = 15,
	RankingTagCoop           = 17,
	RankingTeam              = 18,
	ProcessingBeatmaps       = 19,
	Tourney                  = 22,
}

/// <summary>
/// Currently selected mods are a combination of flags. Lots of unknown values.
/// </summary>
[Flags]
public enum OsuMods : int {
	Unknown     = -1,
	None        = 0,
	NoFail      = 1,
	Easy        = 1 << 1,
	// ?        = 1 << 2,
	Hidden      = 1 << 3,
	HardRock    = 1 << 4,
	SuddenDeath = 1 << 5,
	DoubleTime  = 1 << 6,
	/// <summary> Automatic clicks but you aim </summary>
	Relax       = 1 << 7,
	HalfTime    = 1 << 8,
	NightCore   = 1 << 9, // when NC chosen in game, ModFlags = 1<<9 | DT
	Flashlight  = 1 << 10,
	/// <summary> osu! plays perfectly for you </summary>
	AutoPlay    = 1 << 11,
	SpunOut     = 1 << 12,
	/// <summary> Automatic aim but you click </summary>
	AutoPilot   = 1 << 13,
	Perfect     = 1 << 14, // when PF chosen in game, ModFlags = 1<<14 | SD
	// ?        = 1 << 15 .. 1 << 21
	/// <summary> Just listen to the song + see background. The thing after clicking Auto twice </summary>
	Cinema      = 1 << 22, // when Cinema chosen in game, ModFlags = 1<<22 | AutoPlay
	// ?        = 1 << 23 .. 1 << 28
	ScoreV2     = 1 << 29,
	// common shortened names
	NF = NoFail,
	EZ = Easy,
	HD = Hidden,
	HR = HardRock,
	SD = SuddenDeath,
	DT = DoubleTime,
	RX = Relax,
	HT = HalfTime,
	NC = NightCore,
	FL = Flashlight,
	SO = SpunOut,
	PF = Perfect,
}
