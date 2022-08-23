namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;

	readonly record struct SeriesPoint(double X, double Y) : IComparable<SeriesPoint> {
		public int CompareTo(SeriesPoint other) => X.CompareTo(other.X);
	}
}
