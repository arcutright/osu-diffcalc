using System;
using System.IO;
using Osu_DiffCalc.FileProcessor.FileParserHelpers;
using System.Threading;

namespace Osu_DiffCalc.FileProcessor
{
    class Parser
    {
        public static bool parse(Beatmap beatmap)
        {
            try
            {
                //note: order matters, because it is parsing sequentially (to avoid polynomial time search)
                StreamReader reader = File.OpenText(beatmap.filepath);

                if (!FormatParser.parse(beatmap, ref reader))
                    return false;

                if (!GeneralParser.parse(beatmap, ref reader))
                    return false;

                if (!MetadataParser.parse(beatmap, ref reader))
                {
                    Console.WriteLine("\n\n!!!\nError parsing metadata\n!!!\n\n");
                    return false;
                }

                if (Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = string.Format("parse[{0}]", beatmap.version);

                DifficultyParser.parse(beatmap, ref reader);

                EventsParser.parse(beatmap, ref reader);

                if (!TimingParser.parse(beatmap, ref reader))
                    return false;

                if (!HitObjectsParser.parse(beatmap, ref reader))
                    return false;

                beatmap.parsed = true;
                reader.Close();
                //timing
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("!! -- Error parsing map");
                Console.WriteLine(e.GetBaseException());
            }
            return false;
        }

        //returns an absolute minimum amount of information about the map
        public static Beatmap parseMapPath(string mapPath)
        {
            try
            {
                StreamReader reader = File.OpenText(mapPath);
                Beatmap beatmap = new Beatmap();
                string formatString;
                while ((formatString = reader.ReadLine()) != null && !formatString.StartsWith("osu"));
                if (formatString.Length > 0)
                {
                    formatString = System.Text.RegularExpressions.Regex.Replace(formatString, "[^0-9]+", string.Empty);
                    if (formatString.Length > 0)
                        beatmap.format = (int)double.Parse(formatString);
                    else
                        return null;
                }
                GeneralHelper.skipTo(ref reader, @"[General]", false);
                beatmap.mp3FileName = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "AudioFile"), "AudioFile");

                if (beatmap.format > 5)
                {
                    int mode = (int)GeneralHelper.getFloatFromLine(GeneralHelper.skipTo(ref reader, "Mode"), "Mode");
                    if (mode != 0)
                        return null;
                }
                beatmap.title = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Title", false), "Title");
                beatmap.artist = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Artist"), "Artist");
                beatmap.creator = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Creator"), "Creator");
                beatmap.version = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Version"), "Version");
                beatmap.filepath = mapPath;
                return beatmap;
            }
            catch (Exception e)
            {
                Console.WriteLine("!! -- Error parsing map path: " + mapPath);
                Console.WriteLine(e.GetBaseException());
                return null;
            }
        }
    }
}
