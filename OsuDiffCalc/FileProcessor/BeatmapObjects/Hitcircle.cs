namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	/// <summary> A hit circle object </summary>
	/// <inheritdoc cref="HitObject"/>
	record Hitcircle(float X, float Y, double StartTime) : HitObject(X, Y, StartTime, StartTime) {
	}
}
