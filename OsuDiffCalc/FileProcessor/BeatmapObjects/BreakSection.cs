namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	/// <summary> A break in gameplay </summary>
	/// <inheritdoc cref="BeatmapObject"/>
	record BreakSection(int StartTime, int EndTime) : BeatmapObject(StartTime, EndTime) {
	}
}
