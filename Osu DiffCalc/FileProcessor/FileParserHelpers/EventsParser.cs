using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class EventsParser
    {
        public static void parse(Beatmap beatmap, ref StreamReader reader)
        {
            GeneralHelper.skipTo(ref reader, @"[Events]", false);

            string line;
            string[] data;
            BeatmapObjects.BreakSection breakSection;

            while ((line = reader.ReadLine()) != null && line.Length > 0 && !line.StartsWith("["))
            {
                line = line.Trim();
                data = line.Split(',');

                if (data.Length >= 3)
                {
                    if(data[0].Equals("2"))
                    {
                        int start = (int)double.Parse(data[1]);
                        int end = (int)double.Parse(data[2]);
                        breakSection = new BeatmapObjects.BreakSection(start, end);
                        beatmap.addBreak(breakSection);
                    }
                }
            }
        }

    }
}
