namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	/// <summary> A spinner (always appears in the center of the play field) </summary>
	/// <inheritdoc cref="BeatmapObject"/>
	record Spinner(double StartTime, double EndTime) : BeatmapObject(StartTime, EndTime) {
	}
}
