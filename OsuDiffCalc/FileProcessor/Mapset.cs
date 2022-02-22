namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	// This is a mapset, a collection of different difficulties
	class Mapset : IEnumerable<Beatmap> {
		public Mapset(string title, string artist, string creator) {
			Title = title;
			Artist = artist;
			Creator = creator;
		}

		public Mapset(IEnumerable<Beatmap> maps) {
			if (maps is not null) {
				foreach (var map in maps) {
					if (map is not null)
						Add(map);
				}
			}
		}

		public Mapset(Beatmap map) {
			Add(map);
		}

		public string Title { get; private set; }
		public string Artist { get; private set; }
		public string Creator { get; private set; }
		public List<Beatmap> Beatmaps { get; } = new();
		public int Count => Beatmaps.Count;
		public bool IsAnalyzed { get; set; } = false;

		public void Add(Beatmap map) {
			Beatmaps.Add(map);
			if (Title is null && Artist is null && Creator is null) {
				Title = map.Title;
				Artist = map.Artist;
				Creator = map.Creator;
			}
		}

		public void Sort(bool ascending = true) {
			if (ascending)
				Beatmaps.Sort((x, y) => x.DiffRating.TotalDifficulty.CompareTo(y.DiffRating.TotalDifficulty));
			else
				Beatmaps.Sort((x, y) => y.DiffRating.TotalDifficulty.CompareTo(x.DiffRating.TotalDifficulty));
		}

		public bool SaveToXML() {
			if (IsAnalyzed)
				return SavefileXMLManager.SaveMapset(this);
			else
				return false;
		}

		public Beatmap this[int index] {
			get => Beatmaps[index];
			set => Beatmaps[index] = value;
		}

		public IEnumerator<Beatmap> GetEnumerator() => Beatmaps.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Beatmaps.GetEnumerator();

		public int IndexOf(Beatmap map) => Beatmaps.IndexOf(map);

		public bool Contains(Beatmap item) => IndexOf(item) != -1;

		public void Clear() {
			Beatmaps.Clear();
			Title = null;
			Artist = null;
			Creator = null;
		}
	}
}
