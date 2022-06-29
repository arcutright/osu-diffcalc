namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	/// <summary> A break in gameplay </summary>
	/// <inheritdoc cref="BeatmapObject"/>
	record BreakSection(double StartTime, double EndTime) : BeatmapObject(StartTime, EndTime) {
	}
}
