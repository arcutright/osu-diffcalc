namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	/// <summary> A hit circle object </summary>
	/// <inheritdoc cref="HitObject"/>
	record Hitcircle(int X, int Y, int StartTime) : HitObject(X, Y, StartTime, StartTime) {
	}
}
