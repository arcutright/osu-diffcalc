using System;
using System.Globalization;
using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class GeneralHelper
    {
        public static string skipTo(ref StreamReader reader, string keyword, bool stopAtHeadings=true)
        {
            string line;
            try
            {
                while ((line = reader.ReadLine()) != null)
                {
                    line.Trim();
                    if (line.Contains(keyword))
                        return line;
                    if (stopAtHeadings && reader.Peek() == '[')
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!! -- Error skipping");
                Console.WriteLine(e.GetBaseException());
            }
            return null;
        }

        public static float getFloatFromLine(string line, string startsWith, string delimiter=":")
        {
            int index;
            try
            {
                line = line.Trim();
                if ((index = line.IndexOf(delimiter)) >= 0)
                {
                    if (line.StartsWith(startsWith))
                    {
                        return (float)double.Parse(line.Substring(index + 1), CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!! -- Error pulling float from line");
                Console.WriteLine(e.GetBaseException());
            }
            return float.NaN;
        }

        public static float getFloatFromNextLine(ref StreamReader reader, string startsWith, string delimiter=":")
        {
            string line;
            try
            {
                if ((line = reader.ReadLine()) != null)
                {
                    return getFloatFromLine(line, startsWith, delimiter);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!! -- Error using reader to get float from next line");
                Console.WriteLine(e.GetBaseException());
            }
            return float.NaN;
        }

        public static string getStringFromLine(string line, string startsWith, string delimiter = ":")
        {
            int index;
            try
            {
                line = line.Trim();
                if ((index = line.IndexOf(delimiter)) >= 0)
                {
                    if (line.StartsWith(startsWith))
                    {
                        return line.Substring(index + 1);
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("!! -- Error pulling string from line");
                Console.WriteLine(e.GetBaseException());
            }
            return null;
        }

        public static string getStringFromNextLine(ref StreamReader reader, string startsWith, string delimiter = ":")
        {
            string line;
            try
            {
                if ((line = reader.ReadLine()) != null)
                {
                    return getStringFromLine(line, startsWith, delimiter);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!! -- Error using reader to get string from next line");
                Console.WriteLine(e.GetBaseException());
            }
            return null;
        }
    }
}
