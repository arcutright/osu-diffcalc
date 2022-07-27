namespace OsuDiffCalc.FileProcessor.AnalyzerObjects {
	using System;
	using System.Collections.Generic;
	using System.Windows.Forms.DataVisualization.Charting;

	class SeriesPointCollection : List<SeriesPoint>, IDisposable {
		private bool _isDisposed;

		public SeriesPointCollection(string name) : base() { Name = name; }
		public SeriesPointCollection(string name, int capacity) : base(capacity) { Name = name; }

		/// <summary>
		/// Name of this series
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Whether or not this series should be shown by default
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		public Series Series { get; set; } = null;

		public override string ToString() => $"'{Name}' Count={Count}, IsEnabled={IsEnabled}";

		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					// dispose managed state (managed objects)
					try { 
						Series?.Dispose(); 
						Clear();
					} catch { }
				}
				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				Series = null;

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
