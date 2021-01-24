namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;

	class SavefileXMLManager {
		private static readonly string _xmlSaveFilePath = Path.Combine(Directory.GetCurrentDirectory(), "analyzedmaps.xml");
		private static XDocument _document;

		public static bool IsInitialized = false;

		public static void ClearXML() {
			_document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("root"));
			_document.Save(_xmlSaveFilePath);
		}

		public static bool Parse(ref List<Mapset> allMapsets) {
			try {
				//find and load xml
				LoadXML();
				//parse xml
				var mapsets = _document.Descendants("mapset").Select(set => new {
					title = set.Attribute("title").Value.Trim(),
					artist = set.Attribute("artist").Value.Trim(),
					creator = set.Attribute("creator").Value.Trim(),
					maps = set.Descendants("map"),
				});
				foreach (var mapset in mapsets) {
					var set = new Mapset(mapset.title, mapset.artist, mapset.creator);
					foreach (var map in mapset.maps) {
						var beatmap = new Beatmap(set, map.Attribute("version").Value.Trim());
						string diffstring = map.Attribute("totalDiff").Value.Trim();
						beatmap.DiffRating.TotalDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						diffstring = map.Attribute("jumpDiff").Value.Trim();
						beatmap.DiffRating.JumpDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						diffstring = map.Attribute("streamDiff").Value.Trim();
						beatmap.DiffRating.StreamDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						diffstring = map.Attribute("burstDiff").Value.Trim();
						beatmap.DiffRating.BurstDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						diffstring = map.Attribute("coupletDiff").Value.Trim();
						beatmap.DiffRating.CoupletDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						diffstring = map.Attribute("sliderDiff").Value.Trim();
						beatmap.DiffRating.SliderDifficulty = double.Parse(diffstring, CultureInfo.InvariantCulture);

						set.Add(beatmap);
					}
					allMapsets.Add(set);
				}
				IsInitialized = true;
				return true;
			}
			catch {
				return false;
			}
		}

		public static bool SaveMapset(Mapset set) {
			Console.Write("saving to xml...");
			try {
				LoadXML();

				//check if a matching mapset exists
				IEnumerable<XElement> validMapsets = GetValidMapsets(set);
				if (validMapsets.Any()) {
					//if so, check if all difficulties are present, add if needed
					var validDiffs = validMapsets.Elements("map").ToList();
					if (validDiffs.Count != set.Beatmaps.Count) {
						//check which difficulty is missing
						foreach (Beatmap map in set.Beatmaps) {
							bool found = false;
							foreach (XElement toFind in validDiffs) {
								if ((string)toFind.Attribute("version") == map.Version) {
									found = true;
									break;
								}
							}
							if (!found) {
								Console.WriteLine("adding missing diffs to xml");
								//add missing difficulty
								validDiffs[^1].AddAfterSelf(new XElement("map",
										new XAttribute("version", map.Version),
										new XAttribute("totalDiff", dstr(map.DiffRating.TotalDifficulty)),
										new XAttribute("jumpDiff", dstr(map.DiffRating.JumpDifficulty)),
										new XAttribute("streamDiff", dstr(map.DiffRating.StreamDifficulty)),
										new XAttribute("burstDiff", dstr(map.DiffRating.BurstDifficulty)),
										new XAttribute("coupletDiff", dstr(map.DiffRating.CoupletDifficulty)),
										new XAttribute("sliderDiff", dstr(map.DiffRating.SliderDifficulty))));
							}
						}
					}
					else {
						//do nothing, xml is fine
						Console.WriteLine("mapset in xml");
					}
				}
				//no matching mapset in xml
				else {
					Console.WriteLine("adding new set to xml");
					var mapsetNode = new XElement("mapset",
						new XAttribute("title", set.Title),
						new XAttribute("artist", set.Artist),
						new XAttribute("creator", set.Creator) );
					foreach (Beatmap map in set.Beatmaps) {
						mapsetNode.Add(new XElement("map",
							new XAttribute("version", map.Version),
							new XAttribute("totalDiff", dstr(map.DiffRating.TotalDifficulty)),
							new XAttribute("jumpDiff", dstr(map.DiffRating.JumpDifficulty)),
							new XAttribute("streamDiff", dstr(map.DiffRating.StreamDifficulty)),
							new XAttribute("burstDiff", dstr(map.DiffRating.BurstDifficulty)),
							new XAttribute("coupletDiff", dstr(map.DiffRating.CoupletDifficulty)),
							new XAttribute("sliderDiff", dstr(map.DiffRating.SliderDifficulty))));
					}
					if (_document.Root.Elements().Count() > 0)
						_document.Root.LastNode.AddAfterSelf(mapsetNode);
					else
						_document.Root.Add(mapsetNode);
				}
				_document.Save(_xmlSaveFilePath);
				return true;
			}
			catch {
				Console.WriteLine("!!- Could not add to file - trying to create");
				try {
					//create entire file
					var writer = XmlWriter.Create(_xmlSaveFilePath);
					writer.WriteStartDocument();
					writer.WriteWhitespace("\n\n");

					//create root node
					writer.WriteStartElement("root");
					writer.WriteWhitespace("\n\t");

					//create mapset branch
					writer.WriteStartElement("mapset");
					writer.WriteAttributeString("title", set.Title);
					writer.WriteAttributeString("artist", set.Artist);
					writer.WriteAttributeString("creator", set.Creator);

					foreach (Beatmap map in set.Beatmaps) {
						writer.WriteWhitespace("\n\t\t");
						//create version branch
						writer.WriteStartElement("map");
						//populate beatmap specific data
						writer.WriteAttributeString("version", map.Version);
						writer.WriteAttributeString("totalDiff", dstr(map.DiffRating.TotalDifficulty));
						writer.WriteAttributeString("jumpDiff", dstr(map.DiffRating.JumpDifficulty));
						writer.WriteAttributeString("streamDiff", dstr(map.DiffRating.StreamDifficulty));
						writer.WriteAttributeString("burstDiff", dstr(map.DiffRating.BurstDifficulty));
						writer.WriteAttributeString("coupletDiff", dstr(map.DiffRating.CoupletDifficulty));
						writer.WriteAttributeString("sliderDiff", dstr(map.DiffRating.SliderDifficulty));
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
				catch (Exception e) {
					Console.WriteLine("!!-- Error creating XML");
					Console.WriteLine(e.GetBaseException());
					return false;
				}
			}
			static string dstr(double val) => val.ToString(CultureInfo.InvariantCulture);
		}

		#region Private helpers

		static void LoadXML() {
			_document = new XDocument();
			try {
				_document = XDocument.Load(_xmlSaveFilePath);
			}
			catch (FileNotFoundException) {
				_document.Save(_xmlSaveFilePath);
				Console.WriteLine("!!-- Could not load xml, created new: " + _xmlSaveFilePath);
			}
		}

		static IEnumerable<XElement> GetValidMapsets(Mapset set) {
			return _document.Root.Elements("mapset").Where(el =>
				(string)el.Attribute("title") == set.Title &&
				(string)el.Attribute("artist") == set.Artist &&
				(string)el.Attribute("creator") == set.Creator);
		}

		static IEnumerable<XElement> GetValidMapsets(Beatmap map) {
			return _document.Root.Elements("mapset").Where(el =>
				(string)el.Attribute("title") == map.Title && 
				(string)el.Attribute("artist") == map.Artist &&
				(string)el.Attribute("creator") == map.Creator);
		}

		#endregion
	}
}
