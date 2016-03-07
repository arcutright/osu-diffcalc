using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class GeneralParser
    {
        public static bool parse(Beatmap beatmap, ref StreamReader reader)
        {
            GeneralHelper.skipTo(ref reader, @"[General]", false);
            beatmap.mp3FileName = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "AudioFile"), "AudioFile");
            if (beatmap.format > 5)
            {
                int mode = (int)GeneralHelper.getFloatFromLine(GeneralHelper.skipTo(ref reader, "Mode"), "Mode");
                if (mode == 0)
                    return true;
                else
                    return false;
            }
            else
                return true;
        }
    }
}
