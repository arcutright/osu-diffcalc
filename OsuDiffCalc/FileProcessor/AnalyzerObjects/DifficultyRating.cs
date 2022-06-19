namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Forms.DataVisualization.Charting;

	class DifficultyRating : IDisposable {
		private bool _isDisposed;

		private readonly SeriesPointCollection
			_jumps   = new("Jumps",   64),
			_streams = new("Streams", 64),
			_bursts  = new("Bursts",  64),
			_doubles = new("Sliders", 64),
			_sliders = new("Doubles", 64) { IsEnabled = false };

		/// <summary>
		/// List of (raw list of individual points) <br/>
		/// where each point is (X: start time in seconds, Y: difficulty)
		/// </summary>
		private readonly SeriesPointCollection[] _allSeriesPoints;
		private readonly object _pointsLock = new();

		private readonly HashSet<float> _allSeriesXValues = new(1024);
		private bool _areSeriesPrepared;
		private bool _arePointsPrepared;

		public DifficultyRating() { 
			_allSeriesPoints = new[] {
				_jumps,
				_streams,
				_bursts,
				_sliders,
				_doubles,
			};
		}

		public DifficultyRating(double jumpsDifficulty, double streamsDifficulty, double burstsDifficulty, double doublesDifficulty, double slidersDifficulty, double totalDifficulty) : this() {
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

		public double JumpsDifficulty { get; set; }
		public double StreamsDifficulty { get; set; }
		public double BurstsDifficulty { get; set; }
		public double DoublesDifficulty { get; set; }
		public double SlidersDifficulty { get; set; }
		public double TotalDifficulty { get; set; }

		/// <summary> Max BPM of streams for 1/4 notes </summary>
		public double StreamsMaxBPM { get; set; }

		/// <summary> Average BPM of streams/bursts for 1/4 notes </summary>
		public double StreamsAverageBPM { get; set; }

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

		public Series[] GetAllSeries() {
			// Note: this controls which order the series will show up (when using Column display, inverted for StackedColumn)
			lock (_pointsLock) {
				return new[] {
					GetNormalizedSeries(_jumps),
					GetNormalizedSeries(_streams),
					GetNormalizedSeries(_bursts),
					GetNormalizedSeries(_sliders),
					GetNormalizedSeries(_doubles),
				};
			}
		}

		/// <summary>
		/// Removes the havier 'Series' objects but keeps the underlying points.
		/// Will rebuild the Series the next time they are requested. <br/>
		/// This is intended to be used to reduce memory size for caches when switching active maps.
		/// </summary>
		public void ClearCachedSeries() {
			lock (_pointsLock) {
				foreach (var points in _allSeriesPoints) {
					if (points.Series is not null) {
						points.Series.Points.Dispose();
						points.Series.Points.Clear();
						points.Series.Dispose();
						points.Series = null;
					}
				}
				_areSeriesPrepared = false;
			}
		}

		private Series GetNormalizedSeries(SeriesPointCollection points) {
			if (points is null)
				return null;
			if (!_areSeriesPrepared || points.Series is null)
				PrepareAllSeries();
			return points.Series;
		}

		public Series GetSeriesByName(string name) {
			var points = _allSeriesPoints.Where(points =>
				string.Compare(points.Name, name, StringComparison.OrdinalIgnoreCase) == 0
			).FirstOrDefault();
			return GetNormalizedSeries(points);
		}

		public void AddJump(int timeMs, float difficulty) => Add(timeMs, difficulty, _jumps);
		public void AddStream(int timeMs, float difficulty) => Add(timeMs, difficulty, _streams);
		public void AddBurst(int timeMs, float difficulty) => Add(timeMs, difficulty, _bursts);
		public void AddDouble(int timeMs, float difficulty) => Add(timeMs, difficulty, _doubles);
		public void AddSlider(int timeMs, float difficulty) => Add(timeMs, difficulty, _sliders);

		private void Add(int timeMs, float diff, SeriesPointCollection dest) {
			lock (_pointsLock) {
				float xValue = (float)(timeMs / 1000.0);
				_allSeriesXValues.Add(xValue);
				dest.Add(new SeriesPoint(xValue, diff));

				if (_areSeriesPrepared)
					ClearCachedSeries();
				_areSeriesPrepared = false;
				_arePointsPrepared = false;
			}
		}

		/// <summary>
		/// Sorts by x and adds a point (x, 0) to any series which is missing a point for all x in all other series. <br/>
		/// In this way, all series will have the same number of points. This is needed for some charts to be valid.
		/// </summary>
		private void PrepareAllSeries() {
			lock (_pointsLock) {
				if (_arePointsPrepared && _areSeriesPrepared) return;
				SortAndAccumulatePointCollections();
				BuildSeriesFromPointCollections();
				_areSeriesPrepared = true;
			}
		}

		private void SortAndAccumulatePointCollections() {
			lock (_pointsLock) {
				if (_arePointsPrepared) return;
				foreach (var points in _allSeriesPoints) {
					sortAndAccumulate(points);
				}
				_arePointsPrepared = true;
				_areSeriesPrepared = false;
			}

			//int n = _jumps.Count;
			//bool countsEqual = _streams.Count == n && _bursts.Count == n && _doubles.Count == n && _sliders.Count == n;

			// Sort + accumulate points which have the same X value by doing pt.Y = pt1.Y + pt2.Y
			static void sortAndAccumulate(SeriesPointCollection points) {
				if (points.Count <= 1) return;

				points.Sort();
				var points2 = new List<SeriesPoint>(points.Count);
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

				points.Clear();
				points.AddRange(points2);
			}
		}

		private void BuildSeriesFromPointCollections() {
			if (_areSeriesPrepared) return;

			// for each series, add a point (x, 0) for all times that appear in any of the series
			// this is needed for certain chart styles (each series having a point at each x value)
			var allSeriesXValues = _allSeriesXValues.ToList();
			allSeriesXValues.Sort();

			// populate the series & add dummy points as needed
			foreach (var points in _allSeriesPoints) {
				if (points.Series is not null) continue;
				int nPoints = points.Count;
				var series = new Series(points.Name);
				int i = 0;
				foreach (var x in allSeriesXValues) {
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
				if (i < nPoints)
					throw new Exception($"Point collections became desynchronized. This indicates a bug in our code for this map");
				points.Series = series;
			}
			_areSeriesPrepared = true;
		}

		private static Series BuildSeries(string name, bool enabled = true) {
			return new Series {
				Name = name,
				LegendText = name,
				IsValueShownAsLabel = false,
				Enabled = enabled,
				ChartType = Properties.Settings.Default.SeriesChartType
			};
		}

		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
				}
				ClearCachedSeries();
				foreach (var points in _allSeriesPoints) {
					points.Clear();
				}
				_allSeriesXValues.Clear();

				_isDisposed = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
