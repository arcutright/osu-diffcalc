using System;
using System.Collections.Generic;

namespace Osu_DiffCalc.FileProcessor
{
    class Mapset
    {
        // This is a mapset, a collection of different difficulties
        public string title, artist, creator;
        public List<Beatmap> beatmaps = new List<Beatmap>();
        public bool analyzed = false;

        public Mapset(string title, string artist, string creator)
        {
            this.title = title;
            this.artist = artist;
            this.creator = creator;
        }

        public Mapset(List<Beatmap> maps)
        {
            if (maps.Count > 0)
            {
                beatmaps = maps;
                title = maps[0].title;
                artist = maps[0].artist;
                creator = maps[0].creator;
            }
        }

        public Mapset(Beatmap map)
        {
            add(map);
        }

        public void add(Beatmap map)
        {
            beatmaps.Add(map);
            if(title == null && artist == null && creator == null)
            {
                title = map.title;
                artist = map.artist;
                creator = map.creator;
            }
        }

        public void sort(bool ascending=true)
        {
            if(ascending)
                beatmaps.Sort((x, y) => x.diffRating.totalDifficulty.CompareTo(y.diffRating.totalDifficulty));
            else
                beatmaps.Sort((x, y) => y.diffRating.totalDifficulty.CompareTo(x.diffRating.totalDifficulty));
        }

        public bool saveToXML()
        {
            if (analyzed)
                return SavefileXMLManager.SaveMapset(this);
            else
                return false;
        }

    }
}
