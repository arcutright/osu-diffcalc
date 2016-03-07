using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class FormatParser
    {
        static int[] officiallySupportedFormats = { 8, 9, 10, 11, 12, 13, 14 };

        public static bool parse(Beatmap beatmap, ref StreamReader reader)
        {
            beatmap.isOfficiallySupported = false;
            try
            {
                string formatString = reader.ReadLine();
                if (formatString.Length > 0)
                {
                    formatString = Regex.Replace(formatString, "[^0-9]+", string.Empty);
                    if (formatString.Length > 0)
                    {
                        beatmap.format = (int)double.Parse(formatString);
                        if (officiallySupportedFormats.Contains(beatmap.format))
                            beatmap.isOfficiallySupported = true;
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("unknown or missing file format");
                Console.WriteLine(e.GetBaseException());
            }
            return false;
        }

    }
}
