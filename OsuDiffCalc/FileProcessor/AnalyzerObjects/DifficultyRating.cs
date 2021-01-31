namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Windows.Forms.DataVisualization.Charting;

	class DifficultyRating {
		public const SeriesChartType DEFAULT_CHART_TYPE = SeriesChartType.Column;

		public double JumpDifficulty, StreamDifficulty, BurstDifficulty, CoupletDifficulty, SliderDifficulty;
		public double TotalDifficulty;

		public Series Jumps { get; } = BuildSeries("Jumps");
		public Series Streams { get; } = BuildSeries("Streams");
		public Series Bursts { get; } = BuildSeries("Bursts");
		public Series Couplets { get; } = BuildSeries("Couplets");
		public Series Sliders { get; } = BuildSeries("Sliders");

		public static double FamiliarizeRating(double rating) {
			//return 1.1 * Math.Pow(rating, 0.25);
			return 0.5 * Math.Pow(rating, 0.4);
		}

		public void AddJump(double time, double difficulty) => Add(time, difficulty, Jumps);
		public void AddStream(double time, double difficulty) => Add(time, difficulty, Streams);
		public void AddBurst(double time, double difficulty) => Add(time, difficulty, Bursts);
		public void AddCouplet(double time, double difficulty) => Add(time, difficulty, Couplets);
		public void AddSlider(double time, double difficulty) => Add(time, difficulty, Sliders);

		private void Add(double ms, double diff, Series dest) {
			dest.Points.AddXY(ms / 1000, diff);
		}

		private static Series BuildSeries(string legend) {
			return new Series {
				LegendText = legend,
				Name = legend,
				ChartType = DEFAULT_CHART_TYPE
			};
		}
	}
}
