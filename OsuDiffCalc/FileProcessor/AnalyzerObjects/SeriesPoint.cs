namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;

	readonly record struct SeriesPoint(float X, float Y) : IComparable<SeriesPoint> {
		public int CompareTo(SeriesPoint other) => X.CompareTo(other.X);
	}
}
