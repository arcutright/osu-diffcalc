namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;

	class Mapset {
		// This is a mapset, a collection of different difficulties
		public string Title, Artist, Creator;
		public List<Beatmap> Beatmaps = new List<Beatmap>();
		public bool IsAnalyzed = false;

		public Mapset(string title, string artist, string creator) {
			Title = title;
			Artist = artist;
			Creator = creator;
		}

		public Mapset(List<Beatmap> maps) {
			if (maps.Count != 0) {
				Beatmaps = maps;
				Title = maps[0].Title;
				Artist = maps[0].Artist;
				Creator = maps[0].Creator;
			}
		}

		public Mapset(Beatmap map) {
			Add(map);
		}

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

	}
}
