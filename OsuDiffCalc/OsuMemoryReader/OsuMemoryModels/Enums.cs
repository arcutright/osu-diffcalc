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
// osu.OsuModes
public enum OsuStatus : int {
	/// <summary>
	/// Indicates that status read in osu memory is not defined in <see cref="OsuStatus"/>
	/// </summary>
	Unknown                  = -2,
	NotRunning               = -1,
	MainMenu                 = 0,  // Menu
	EditingMap               = 1,  // Edit
	Playing                  = 2,  // Play
	GameShutdownAnimation    = 3,  // Exit
	SongSelectEdit           = 4,  // SelectEdit
	SongSelect               = 5,  // SelectPlay
	SelectDrawings           = 6,  // ?
	ResultsScreen            = 7,  // Rank
	Update                   = 8,  // ?
	Busy                     = 9,  // ?
	GameStartupAnimation     = 10, // Unknown (in osu stable)
	MultiplayerRooms         = 11, // Lobby
	MultiplayerRoom          = 12, // MatchSetup
	MultiplayerSongSelect    = 13, // SelectMulti
	MultiplayerResultsScreen = 14, // RankingVs
	OsuDirect                = 15, // OnlineSelection
	OptionsOffsetWizard      = 16,
	RankingTagCoop           = 17,
	RankingTeam              = 18,
	ProcessingBeatmaps       = 19, // BeatmapImport
	PackageUpdater           = 20, // ?
	Benchmark                = 21, // ?
	Tourney                  = 22,
	Charts                   = 23, // ?
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
	Key4        = 1 << 15,
	Key5        = 1 << 16,
	Key6        = 1 << 17,
	Key7        = 1 << 18,
	Key8        = 1 << 19,
	FadeIn      = 1 << 20,
	Random      = 1 << 21,
	/// <summary> Just listen to the song + see background. The thing after clicking Auto twice </summary>
	Cinema      = 1 << 22, // when Cinema chosen in game, ModFlags = 1<<22 | AutoPlay
	Target      = 1 << 23,
	Key9        = 1 << 24,
	KeyCoop     = 1 << 25,
	Key1        = 1 << 26,
	Key3        = 1 << 27, // not a typo
	Key2        = 1 << 28,
	ScoreV2     = 1 << 29,
	Mirror      = 1 << 30,
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

// osu stable enum "#=zcSbSZOlOrQ0l3zB8Dg=="
public enum OsuPlayerState : int {
	Idle = 0,
	Afk,
	Playing,
	Editing,
	Modding,
	Multiplayer,
	Watching,
	Unknown,
	Testing,
	Submitting,
	Paused,
	Lobby,
	Multiplaying,
	OsuDirect // = 13
}

public enum SlotStatus {
	Open = 1,
	Locked = 2,
	NotReady = 4,
	Ready = 8,
	NoMap = 0x10,
	Playing = 0x20,
	Complete = 0x40,
	HasPlayer = 0x7C,
	Quit = 0x80
}
