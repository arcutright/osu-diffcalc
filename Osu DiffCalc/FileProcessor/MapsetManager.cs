using Osu_DiffCalc.FileFinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osu_DiffCalc.FileProcessor
{
    class MapsetManager
    {
        static List<Mapset> allMapsets = new List<Mapset>();
        
        public static void clear()
        {
            allMapsets.Clear();
        }

        //gets mapset directory based on osu's hooks to audio files
        public static string getCurrentMapsetDirectory()
        {
            string mapsetDirectory = null;
            try
            {
                //get current mapset from process hooks
                string[] audioFormats = {".mp3", ".ogg", ".wav"};
                mapsetDirectory = Finder.GetOsuBeatmapDirectoriesFromProcessHooks("osu!").FirstOrDefault();
            }
            catch { }
            return mapsetDirectory;
        }

        //get mapset directory based on osu's window title (only works while user is playing a map)
        public static string getCurrentMapsetDirectory(string ingameWindowTitle, string prevMapsetDirectory)
        {
            string mapsetDirectory = null;
            try
            {
                //title info is organized: artist - song title [difficulty]
                string titleInfo = ingameWindowTitle.Substring(ingameWindowTitle.IndexOf('-')+1).Trim();
                string mapsetDirectoryTitle = titleInfo.Substring(0, titleInfo.LastIndexOf('[')).Trim();
                string difficulty = titleInfo.Substring(titleInfo.LastIndexOf('[')+1, titleInfo.LastIndexOf(']'));
                string songsDirectory = prevMapsetDirectory.Substring(0, prevMapsetDirectory.LastIndexOf('\\'));
                IEnumerable<string> possibleMapsetDirectories = Directory.EnumerateDirectories(songsDirectory, "*" + mapsetDirectoryTitle + "*", SearchOption.TopDirectoryOnly);
                foreach(string directory in possibleMapsetDirectories)
                {
                    if(Directory.EnumerateFiles(directory, "*"+difficulty+"*.osu", SearchOption.TopDirectoryOnly).Count() > 0)
                    {
                        mapsetDirectory = directory;
                        break;
                    }
                }
            }
            catch { }
            return mapsetDirectory;
        }

        //entry point from GUI.cs
        public static Mapset analyzeMapset(string directory, UserInterface.GUI gui, bool clearLists = true)
        {
            //timing
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (Directory.Exists(directory))
                {
                    //initalize allMapsets array from xml if needed
                    if (!SavefileXMLManager.initialized)
                        SavefileXMLManager.Parse(ref allMapsets);
                    Console.WriteLine("xml analyzed");

                    //parse the mapset by iterating on the directory's .osu files
                    IEnumerable<string> mapPaths = Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".osu"));
                    Console.WriteLine("got osu files");

                    Mapset set = buildSet(mapPaths);
                    Console.WriteLine("set built");

                    if (set.beatmaps.Count > 0)
                    {
                        set = analyzeMapset(set, clearLists);
                        Console.WriteLine("mapset analyzed");
                    }

                    //timing
                    watch.Stop();
                    if (gui != null)
                        gui.SetTime2(string.Format("{0} ms", watch.ElapsedMilliseconds));

                    return set;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!!-- Error: could not analyze set");
                Console.WriteLine(e.GetBaseException());
            }
            watch.Stop();
            return null;
        }

        //main analysis method - every path leads to this
        public static bool analyzeMap(Beatmap map, bool clearLists = true)
        {
            var totwatch = System.Diagnostics.Stopwatch.StartNew();
            var localwatch = System.Diagnostics.Stopwatch.StartNew();
            //parse map if needed
            if (!map.parsed)
                if (!Parser.parse(map))
                    return false;
            localwatch.Stop();
            Console.WriteLine("parse [{0}]: {1}ms", map.version, localwatch.ElapsedMilliseconds);
            //analyze map: streams, jumps, etc

            localwatch.Restart();
            if (!map.analyzed)
                Analyzer.analyze(map, clearLists);
            localwatch.Stop();
            Console.WriteLine("analyze [{0}]: {1}ms", map.version, localwatch.ElapsedMilliseconds);

            //timing
            totwatch.Stop();
            Console.WriteLine("tot [{0}]: {1}ms", map.version, totwatch.ElapsedMilliseconds);
            return true;
        }

        //this is meant to save maps that are manually chosen
        public static void saveMap(Beatmap map)
        {
            if (map != null && map.title != null)
            {
                Mapset set = new Mapset(map);
                if (map.analyzed)
                    set.analyzed = true;
                int index;
                //check if the mapset has been saved
                if ((index = checkForMapset(set)) >= 0)
                {
                    //check if the map has been saved
                    Mapset storedSet = allMapsets[index];
                    bool found = false;
                    foreach (Beatmap storedMap in storedSet.beatmaps)
                    {
                        if (storedMap.version == map.version)
                        {
                            found = true;
                            break;
                        }
                    }
                    //save map 
                    if (!found)
                    {
                        storedSet.add(map);
                        storedSet.saveToXML();
                    }
                }
                else
                {
                    allMapsets.Add(set);
                    set.saveToXML();
                }
            }
        }

        #region Private helpers

        private static Mapset buildSet(IEnumerable<string> mapPaths)
        {
            List<Beatmap> allMaps = new List<Beatmap>();
            Beatmap map = new Beatmap();
            foreach (string mapPath in mapPaths)
            {
                map = Parser.parseMapPath(mapPath);
                if (map != null)
                    allMaps.Add(map);
            }
            return new Mapset(allMaps);
        }

        private static Mapset analyzeMapset(Mapset set, bool clearLists = true)
        {
            bool save = true;
            int index = 0;
            //Console.Write("analyzing set...");
            //check if the mapset has been analyzed
            if ((index = checkForMapset(set)) >= 0)
            {
                //Console.Write("mapset has been analyzed...");
                //check for missing versions (difficulties)
                List<Beatmap> missingMaps = getMissingAnalyzedDiffs(set, index);
                if (missingMaps.Count() > 0)
                {
                    allMapsets[index].analyzed = false;
                    //Console.Write("some maps are missing...");
                    
                    foreach(Beatmap map in missingMaps)
                    {
                        if (analyzeMap(map, clearLists))
                            allMapsets[index].add(map);
                    }
                    //Console.WriteLine("missing maps analyzed");
                }
                else
                {
                    //Console.WriteLine("no maps are missing");
                    save = false;
                }
                set = allMapsets[index];
            }
            else
            {
                //Console.WriteLine("mapset not analyzed...");
                foreach (Beatmap map in set.beatmaps)
                    analyzeMap(map);
                allMapsets.Add(set);
                //Console.WriteLine("analyzed");
            }
            set.analyzed = true;
            if (save)
            {
                //Console.Write("saving set...");
                if (set.saveToXML())
                { /*Console.WriteLine("set saved");*/ }
                else
                { /*Console.WriteLine("!! could not save");*/ }
            }
            return set;
        }


        private static int checkForMapset(Mapset set, bool completeOnly=false)
        {
            int numMapsets = allMapsets.Count();
            if (numMapsets > 0)
            {
                Mapset stored = allMapsets[0];
                for (int i = 0; i < numMapsets; i++)
                {
                    stored = allMapsets[i];
                    if (stored.title == set.title && set.artist == stored.artist && set.creator == stored.creator)
                    {
                        if (completeOnly)
                        {
                            if (set.beatmaps.Count() == stored.beatmaps.Count())
                                return i;
                        }
                        else
                            return i;
                    }
                }
            }
            return -1;
        }

        private static List<Beatmap> getMissingAnalyzedDiffs(Mapset set, int indexForAllMapsetsSearch)
        {
            List<Beatmap> missing = new List<Beatmap>();
            if (indexForAllMapsetsSearch < allMapsets.Count)
            {
                Mapset searching = allMapsets[indexForAllMapsetsSearch];
                if (set.title == searching.title && set.artist == searching.artist && set.creator == searching.creator)
                {
                    bool found = false;
                    foreach (Beatmap toFind in set.beatmaps)
                    {
                        found = false;
                        foreach (Beatmap storedMap in searching.beatmaps)
                        {
                            if (storedMap.version == toFind.version)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            missing.Add(toFind);
                    }
                }
            }
            return missing;
        }

        #endregion
    }
}
