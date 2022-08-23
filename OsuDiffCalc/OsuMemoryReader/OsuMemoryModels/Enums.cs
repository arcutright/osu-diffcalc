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
