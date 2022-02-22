namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Forms.DataVisualization.Charting;

	class DifficultyRating {
		private readonly Series
			_jumpsSeries = BuildSeries("Jumps"),
			_streamsSeries = BuildSeries("Streams"),
			_burstsSeries = BuildSeries("Bursts"),
			_doublesSeries = BuildSeries("Doubles"),
			_slidersSeries = BuildSeries("Sliders");

		private SeriesPointCollection
			_jumps = new(64),
			_streams = new(64),
			_bursts = new(64),
			_doubles = new(64),
			_sliders = new(64);

		private List<double> _allSeriesXValues = new(1024);

		public DifficultyRating() { }
		public DifficultyRating(double jumpsDifficulty, double streamsDifficulty, double burstsDifficulty, double doublesDifficulty, double slidersDifficulty, double totalDifficulty) {
			JumpsDifficulty = jumpsDifficulty;
			StreamsDifficulty = streamsDifficulty;
			BurstsDifficulty = burstsDifficulty;
			DoublesDifficulty = doublesDifficulty;
			SlidersDifficulty = slidersDifficulty;
			TotalDifficulty = totalDifficulty;
		}

		public static double FamiliarizeRating(double rating) {
			//return 1.1 * Math.Pow(rating, 0.25);
			return 0.5 * Math.Pow(rating, 0.4);
		}

		public bool IsNormalized { get; private set; }

		public double JumpsDifficulty { get; set; }
		public double StreamsDifficulty { get; set; }
		public double BurstsDifficulty { get; set; }
		public double DoublesDifficulty { get; set; }
		public double SlidersDifficulty { get; set; }
		public double TotalDifficulty { get; set; }


		/// <summary> Raw list of individual jump difficulties (X: start time in seconds, Y: difficulty). May be different length than other raw lists. </summary>
		public SeriesPointCollection Jumps => _jumps;
		/// <summary> Raw list of individual stream difficulties (X: start time in seconds, Y: difficulty). May be different length than other raw lists. </summary>
		public SeriesPointCollection Streams => _streams;
		/// <summary> Raw list of individual burst difficulties (X: start time in seconds, Y: difficulty). May be different length than other raw lists. </summary>
		public SeriesPointCollection Bursts => _bursts;
		/// <summary> Raw list of individual double difficulties (X: start time in seconds, Y: difficulty). May be different length than other raw lists. </summary>
		public SeriesPointCollection Doubles => _doubles;
		/// <summary> Raw list of individual slider difficulties (X: start time in seconds, Y: difficulty). May be different length than other raw lists. </summary>
		public SeriesPointCollection Sliders => _sliders;

		/// <summary> Noramlized series of jump difficulties (X: start time in seconds, Y: difficulty). Will be the same length as other *Series (an (x, 0) point is added where there was none). </summary>
		public Series JumpsSeries => GetNormalizedSeries(_jumpsSeries);
		/// <summary> Noramlized series of stream difficulties (X: start time in seconds, Y: difficulty). Will be the same length as other *Series (an (x, 0) point is added where there was none). </summary>
		public Series StreamsSeries => GetNormalizedSeries(_streamsSeries);
		/// <summary> Noramlized series of burst difficulties (X: start time in seconds, Y: difficulty). Will be the same length as other *Series (an (x, 0) point is added where there was none). </summary>
		public Series BurstsSeries => GetNormalizedSeries(_burstsSeries);
		/// <summary> Noramlized series of doubles difficulties (X: start time in seconds, Y: difficulty). Will be the same length as other *Series (an (x, 0) point is added where there was none). </summary>
		public Series DoublesSeries => GetNormalizedSeries(_doublesSeries);
		/// <summary> Noramlized series of slider difficulties (X: start time in seconds, Y: difficulty). Will be the same length as other *Series (an (x, 0) point is added where there was none). </summary>
		public Series SlidersSeries => GetNormalizedSeries(_slidersSeries);

		private Series GetNormalizedSeries(Series series) {
			if (!IsNormalized)
				NormalizeSeries();
			return series;
		}

		public Series GetSeriesByName(string name) {
			return name switch {
				"Jumps" => JumpsSeries,
				"Streams" => StreamsSeries,
				"Bursts" => BurstsSeries,
				"Doubles" => DoublesSeries,
				"Sliders" => SlidersSeries,
				_ => null
			};
		}

		public void AddJump(double timeMs, double difficulty) => Add(timeMs, difficulty, _jumps);
		public void AddStream(double timeMs, double difficulty) => Add(timeMs, difficulty, _streams);
		public void AddBurst(double timeMs, double difficulty) => Add(timeMs, difficulty, _bursts);
		public void AddDouble(double timeMs, double difficulty) => Add(timeMs, difficulty, _doubles);
		public void AddSlider(double timeMs, double difficulty) => Add(timeMs, difficulty, _sliders);

		private void Add(double ms, double diff, SeriesPointCollection dest) {
			double time = ms / 1000;
			_allSeriesXValues.Add(time);
			dest.Add(new SeriesPoint(time, diff));
			dest.IsSeriesSynchronized = false;
			IsNormalized = false;
		}

		/// <summary>
		/// Sorts by x and adds a point (x, 0) to any series which is missing a point for all x in all other series. <br/>
		/// In this way, all series will have the same number of points. This is needed for some charts to be valid.
		/// </summary>
		public void NormalizeSeries() {
			if (IsNormalized) return;
			SortAndAccumulatePointCollections();
			BuildSeriesFromPointCollections();
			IsNormalized = true;
		}

		private void SortAndAccumulatePointCollections() {
			if (IsNormalized) return;
			sortAndAccumulate(ref _jumps);
			sortAndAccumulate(ref _streams);
			sortAndAccumulate(ref _bursts);
			sortAndAccumulate(ref _doubles);
			sortAndAccumulate(ref _sliders);

			bool countsEqual = Enumerable.All(new[] { _jumps, _streams, _bursts, _doubles, _sliders }, col => col.Count == _jumps.Count);
			int yyyzs = 1;

			// Sort + accumulate points which have the same X value by doing pt.Y = pt1.Y + pt2.Y
			static void sortAndAccumulate(ref SeriesPointCollection points) {
				if (points.IsSeriesSynchronized || points.Count <= 1) return;

				points.Sort();
				var points2 = new SeriesPointCollection(points.Count);
				var prevPoint = points[0];
				for (int i = 1; i < points.Count; i++) {
					if (points[i].X != prevPoint.X) {
						points2.Add(prevPoint);
						prevPoint = points[i];
					}
					else {
						//points2.Add(points[i]);
						// next point is at same starting time, accumulate the Y values
						prevPoint = prevPoint with { Y = prevPoint.Y + points[i].Y };
					}
				}
				if (points2[^1] != prevPoint)
					points2.Add(prevPoint);

				points = points2;
			}
		}

		private void BuildSeriesFromPointCollections() {
			if (IsNormalized) return;

			// for each series, add a point (x, 0) for all times that appear in any of the series
			// this is needed for certain chart styles (each series having a point at each x value)
			_allSeriesXValues = _allSeriesXValues.Distinct().ToList();
			_allSeriesXValues.Sort();

			var seriesCollection = new[] {
				(_jumps, _jumpsSeries),
				(_streams, _streamsSeries),
				(_bursts, _burstsSeries),
				(_doubles, _doublesSeries),
				(_sliders, _slidersSeries) 
			};

			// populate the series & add dummy points as needed
			foreach (var (points, series) in seriesCollection) {
				if (points.IsSeriesSynchronized) continue;
				series.Points.Clear();
				int nPoints = points.Count;
				int i = 0;
				foreach (var x in _allSeriesXValues) {
					if (i < nPoints && points[i].X == x) {
						// this series already has a point at x
						series.Points.AddXY(x, points[i].Y);
						++i;
					}
					else {
						// this series has no point at x, so we add (x, 0)
						series.Points.AddXY(x, 0.0);
					}
				}
				points.IsSeriesSynchronized = true;
			}
			IsNormalized = true;
		}

		private static Series BuildSeries(string name) {
			return new Series {
				Name = name,
				LegendText = name,
				IsValueShownAsLabel = false,
				Enabled = true,
				ChartType = Properties.Settings.Default.SeriesChartType
			};
		}

		internal class SeriesPointCollection : List<SeriesPoint> {
			public SeriesPointCollection() : base() { }
			public SeriesPointCollection(int capacity) : base(capacity) { }
			public SeriesPointCollection(IEnumerable<SeriesPoint> points) : base(points) { }

			/// <summary>
			/// <see langword="true"/> if the corresponding <see cref="Series"/> is synchronized from this point collection; otherwise, <see langword="false"/>
			/// </summary>
			public bool IsSeriesSynchronized { get; set; } = false;

			/// <summary>
			/// Returns the first index where <c>point.X == <paramref name="x"/></c>. <br/>
			/// If <paramref name="ascending"/>, assumes this list is sorted and will break out once <c>point.X > <paramref name="x"/></c>
			/// </summary>
			/// <returns>index if point was found, otherwise -1</returns>
			public int FindIndexOfX(double x, bool ascending = false) {
				int n = Count;
				if (ascending) {
					for (int i = 0; i < n; ++i) {
						if (this[i].X == x)
							return i;
						else if (this[i].X > x)
							return -1;
					}
				}
				else {
					for (int i = 0; i < n; ++i) {
						if (this[i].X == x)
							return i;
					}
				}
				return -1;
			}
		}

		internal readonly record struct SeriesPoint(double X, double Y) : IComparable<SeriesPoint> {
			public int CompareTo(SeriesPoint other) => X.CompareTo(other.X);
		}
	}
}
