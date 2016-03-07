using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class DifficultyParser
    {
        public static void parse(Beatmap beatmap, ref StreamReader reader)
        {
            GeneralHelper.skipTo(ref reader, "[Difficulty]", false);
            beatmap.hpDrain = GeneralHelper.getFloatFromNextLine(ref reader, "HPDrain");
            beatmap.circleSize = GeneralHelper.getFloatFromNextLine(ref reader, "CircleSize");
            beatmap.accuracy = GeneralHelper.getFloatFromNextLine(ref reader, "OverallDiff");

            beatmap.circleSizePx = (float)(-8.28127 * beatmap.circleSize + 100.597);
            beatmap.marginOfErrorMs300 = (float)(-6 * beatmap.accuracy + 79.5);
            beatmap.marginOfErrorMs50 = (float)(-10 * beatmap.accuracy + 199.5);

            if (beatmap.format > 7)
                beatmap.approachRate = GeneralHelper.getFloatFromNextLine(ref reader, "Approach");
            else
                beatmap.approachRate = beatmap.accuracy;
            beatmap.sliderMultiplier = GeneralHelper.getFloatFromNextLine(ref reader, "SliderMult");
            beatmap.sliderTickRate = GeneralHelper.getFloatFromNextLine(ref reader, "SliderTick");
        }

    }
}
