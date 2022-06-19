namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	// This is a mapset, a collection of different difficulties
	class Mapset : IEnumerable<Beatmap>, IDisposable {
		private readonly List<Beatmap> _beatmaps = new();
		private bool _isDisposed;

		public Mapset(string title, string artist, string creator) {
			Title = title;
			Artist = artist;
			Creator = creator;
		}
		public Mapset(Beatmap map) {
			Add(map);
		}
		public Mapset(IEnumerable<Beatmap> maps) {
			foreach (var map in maps) {
				Add(map);
			}
		}

		/// <summary>
		/// The common folder, if all maps are in the same folder. Otherwise null.
		/// </summary>
		public string FolderPath { get; private set; }
		public string Title { get; private set; }
		public string Artist { get; private set; }
		public string Creator { get; private set; }
		public int Count => _beatmaps.Count;
		public bool IsAnalyzed { get; set; } = false;

		public void Add(Beatmap map) {
			_beatmaps.Add(map);

			if (Count == 1) {
				Title = map.Title;
				Artist = map.Artist;
				Creator = map.Creator;
				FolderPath = Path.GetDirectoryName(map.Filepath);
			}
			else {
				string mapFolderPath = Path.GetDirectoryName(map.Filepath);
				if (FolderPath != mapFolderPath)
					FolderPath = null;
			}
			IsAnalyzed = false;
		}

		public bool Remove(Beatmap map) {
			if (_beatmaps.Remove(map)) {
				if (Count == 0)
					Clear();
				else if (string.IsNullOrEmpty(FolderPath)) {
					string folderPath = Path.GetDirectoryName(_beatmaps[0].Filepath);
					for (int i = 1; i < _beatmaps.Count; ++i) {
						var storedMapFolderPath = Path.GetDirectoryName(_beatmaps[i].Filepath);
						if (storedMapFolderPath != folderPath) {
							folderPath = null;
							break;
						}
					}
					FolderPath = folderPath;
				}
				return true;
			}
			else
				return false;
		}

		public void Sort(bool ascending = true) {
			if (ascending)
				_beatmaps.Sort((x, y) => x.DiffRating.TotalDifficulty.CompareTo(y.DiffRating.TotalDifficulty));
			else
				_beatmaps.Sort((x, y) => y.DiffRating.TotalDifficulty.CompareTo(x.DiffRating.TotalDifficulty));
		}

		public bool SaveToXML() {
			if (IsAnalyzed)
				return SavefileXMLManager.SaveMapset(this);
			else
				return false;
		}

		public Beatmap this[int index] {
			get => _beatmaps[index];
			set => _beatmaps[index] = value;
		}

		public IEnumerator<Beatmap> GetEnumerator() => _beatmaps.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _beatmaps.GetEnumerator();

		public int IndexOf(Beatmap map) => _beatmaps.IndexOf(map);

		public bool Contains(Beatmap item) => IndexOf(item) != -1;

		/// <summary>
		/// Warning: this does not dispose the beatmaps
		/// </summary>
		public void Clear() {
			_beatmaps.Clear();
			Title = null;
			Artist = null;
			Creator = null;
			FolderPath = null;
		}

		public override string ToString() => $"{Artist} - {Title} mapped by {Creator} [{_beatmaps.Count}]";

		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					foreach (var map in _beatmaps) {
						map?.Dispose();
					}
				}
				Clear();

				_isDisposed = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
