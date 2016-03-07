using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Osu_DiffCalc.FileProcessor
{
    class SavefileXMLManager
    {
        static string xmlSaveFilePath = Path.Combine(Directory.GetCurrentDirectory(), "analyzedmaps.xml");
        static XDocument document;
        public static bool initialized = false;

        public static void ClearXML()
        {
            document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("root"));
            document.Save(@xmlSaveFilePath);
        }

        public static bool Parse(ref List<Mapset> allMapsets)
        {
            try
            {
                //find and load xml
                LoadXML();
                //parse xml
                var mapsets = from set in document.Descendants("mapset")
                              select new
                              {
                                  title = set.Attribute("title").Value.Trim(),
                                  artist = set.Attribute("artist").Value.Trim(),
                                  creator = set.Attribute("creator").Value.Trim(),
                                  maps = set.Descendants("map"),
                              };
                foreach (var mapset in mapsets)
                {
                    Mapset set = new Mapset(mapset.title, mapset.artist, mapset.creator);
                    foreach (var map in mapset.maps)
                    {
                        Beatmap beatmap = new Beatmap(ref set, map.Attribute("version").Value.Trim());
                        string diffstring = map.Attribute("totalDiff").Value.Trim();
                        beatmap.diffRating.totalDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        diffstring = map.Attribute("jumpDiff").Value.Trim();
                        beatmap.diffRating.jumpDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        diffstring = map.Attribute("streamDiff").Value.Trim();
                        beatmap.diffRating.streamDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        diffstring = map.Attribute("burstDiff").Value.Trim();
                        beatmap.diffRating.burstDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        diffstring = map.Attribute("coupletDiff").Value.Trim();
                        beatmap.diffRating.coupletDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        diffstring = map.Attribute("sliderDiff").Value.Trim();
                        beatmap.diffRating.sliderDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

                        set.add(beatmap);
                    }
                    allMapsets.Add(set);
                }
                initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SaveMapset(Mapset set)
        {
            Console.Write("saving to xml...");
            try
            {
                LoadXML();

                //check if a matching mapset exists
                IEnumerable<XElement> validMapsets = GetValidMapsets(set);
                if (validMapsets.Count() > 0)
                {
                    //if so, check if all difficulties are present, add if needed
                    IEnumerable<XElement> validDiffs = from el in validMapsets.Elements("map")
                                                       select el;
                    if (validDiffs.Count() != set.beatmaps.Count())
                    {
                        //check which difficulty is missing
                        foreach (Beatmap map in set.beatmaps)
                        {
                            bool found = false;
                            foreach (XElement toFind in validDiffs)
                            {
                                if ((string)toFind.Attribute("version") == map.version)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                Console.WriteLine("adding missing diffs to xml");
                                //add missing difficulty
                                validDiffs.Last().AddAfterSelf(new XElement("map",
                                    new XAttribute("version", map.version),
                                    new XAttribute("totalDiff", ((decimal)map.diffRating.totalDifficulty).ToString()),
                                    new XAttribute("jumpDiff", ((decimal)map.diffRating.jumpDifficulty).ToString()),
                                    new XAttribute("streamDiff", ((decimal)map.diffRating.streamDifficulty).ToString()),
                                    new XAttribute("burstDiff", ((decimal)map.diffRating.burstDifficulty).ToString()),
                                    new XAttribute("coupletDiff", ((decimal)map.diffRating.coupletDifficulty).ToString()),
                                    new XAttribute("sliderDiff", ((decimal)map.diffRating.sliderDifficulty).ToString()) ));
                            }
                        }
                    }
                    else
                    {
                        //do nothing, xml is fine
                        Console.WriteLine("mapset in xml");
                    }
                }
                //no matching mapset in xml
                else
                {
                    Console.WriteLine("adding new set to xml");
                    XElement mapsetNode = new XElement("mapset",
                        new XAttribute("title", set.title),
                        new XAttribute("artist", set.artist),
                        new XAttribute("creator", set.creator) );
                    foreach(Beatmap map in set.beatmaps)
                    {
                        mapsetNode.Add(new XElement("map",
                            new XAttribute("version", map.version),
                            new XAttribute("totalDiff", ((decimal)map.diffRating.totalDifficulty).ToString()),
                            new XAttribute("jumpDiff", ((decimal)map.diffRating.jumpDifficulty).ToString()),
                            new XAttribute("streamDiff", ((decimal)map.diffRating.streamDifficulty).ToString()),
                            new XAttribute("burstDiff", ((decimal)map.diffRating.burstDifficulty).ToString()),
                            new XAttribute("coupletDiff", ((decimal)map.diffRating.coupletDifficulty).ToString()),
                            new XAttribute("sliderDiff", ((decimal)map.diffRating.sliderDifficulty).ToString()) ));
                    }
                    if(document.Root.Elements().Count() > 0)
                        document.Root.LastNode.AddAfterSelf(mapsetNode);
                    else
                        document.Root.Add(mapsetNode);
                }
                document.Save(@xmlSaveFilePath);
                return true;
            }
            catch
            {
                Console.WriteLine("!!- Could not add to file - trying to create");
                try
                {
                    //create entire file
                    XmlWriter writer = XmlWriter.Create(@xmlSaveFilePath);
                    writer.WriteStartDocument();
                    writer.WriteWhitespace("\n\n");

                    //create root node
                    writer.WriteStartElement("root");
                    writer.WriteWhitespace("\n\t");

                    //create mapset branch
                    writer.WriteStartElement("mapset");
                    writer.WriteAttributeString("title", set.title);
                    writer.WriteAttributeString("artist", set.artist);
                    writer.WriteAttributeString("creator", set.creator);

                    foreach (Beatmap map in set.beatmaps)
                    {
                        writer.WriteWhitespace("\n\t\t");
                        //create version branch
                        writer.WriteStartElement("map");
                        //populate beatmap specific data
                        writer.WriteAttributeString("version", map.version);
                        writer.WriteAttributeString("totalDiff", ((decimal)map.diffRating.totalDifficulty).ToString());
                        writer.WriteAttributeString("jumpDiff", ((decimal)map.diffRating.jumpDifficulty).ToString());
                        writer.WriteAttributeString("streamDiff", ((decimal)map.diffRating.streamDifficulty).ToString());
                        writer.WriteAttributeString("burstDiff", ((decimal)map.diffRating.burstDifficulty).ToString());
                        writer.WriteAttributeString("coupletDiff", ((decimal)map.diffRating.coupletDifficulty).ToString());
                        writer.WriteAttributeString("sliderDiff", ((decimal)map.diffRating.sliderDifficulty).ToString());
                        writer.WriteWhitespace("\n\t\t");
                        writer.WriteEndElement();
                    }
                    writer.WriteWhitespace("\n\t");
                    writer.WriteEndElement(); //end mapset
                    writer.WriteWhitespace("\n");
                    writer.WriteEndElement(); //end root
                    writer.WriteEndDocument();
                    writer.Close();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("!!-- Error creating XML");
                    Console.WriteLine(e.GetBaseException());
                    return false;
                }
            }
        }

        #region Private helpers

        static void LoadXML()
        {
            document = new XDocument();
            try
            {
                document = XDocument.Load(@xmlSaveFilePath);
            }
            catch (FileNotFoundException)
            {
                document.Save(@xmlSaveFilePath);
                Console.WriteLine("!!-- Could not load xml, created new: " + xmlSaveFilePath);
            }
        }

        static IEnumerable<XElement> GetValidMapsets(Mapset set)
        {
            return (from el in document.Root.Elements("mapset")
                    where (string)el.Attribute("title") == set.title && (string)el.Attribute("artist") == set.artist && (string)el.Attribute("creator") == set.creator
                    select el);
        }

        static IEnumerable<XElement> GetValidMapsets(Beatmap map)
        {
            return (from el in document.Root.Elements("mapset")
                    where (string)el.Attribute("title") == map.title && (string)el.Attribute("artist") == map.artist && (string)el.Attribute("creator") == map.creator
                    select el);
        }

        #endregion
    }
}
