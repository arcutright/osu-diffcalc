using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class MetadataParser
    {
        public static bool parse(Beatmap beatmap, ref StreamReader reader)
        {
            GeneralHelper.skipTo(ref reader, @"[Metadata]", false);

            beatmap.title = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Title"), "Title");
            if (beatmap.title == null)
                return false;
            beatmap.artist = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Artist"), "Artist");
            if (beatmap.artist == null)
                return false;
            beatmap.creator = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Creator"), "Creator");
            if (beatmap.creator == null)
                return false;
            beatmap.version = GeneralHelper.getStringFromLine(GeneralHelper.skipTo(ref reader, "Version"), "Version");
            if (beatmap.version == null)
                return false;
            return true;
        }

    }
}
