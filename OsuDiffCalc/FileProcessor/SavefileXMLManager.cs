namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;
	using Utility;

	class SavefileXMLManager {
		private static readonly string _xmlSaveFilePath = Path.Combine(Directory.GetCurrentDirectory(), "analyzedmaps.xml");
		private static XDocument _document;

		public static bool IsInitialized = false;

		public static void ClearXML() {
			_document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("root"));
			_document.Save(_xmlSaveFilePath);
		}

		public static List<Mapset> ParseXML() {
			var parsedMaps = new List<Mapset>();
			try {
				//find and load xml
				LoadXML();
				//parse xml
				var mapsets = _document.Descendants("mapset").Select(set => (
					title: set.Attribute("title").Value?.Trim() ?? string.Empty,
					artist: set.Attribute("artist").Value?.Trim() ?? string.Empty,
					creator: set.Attribute("creator").Value?.Trim() ?? string.Empty,
					maps: set.Descendants("map")
				));
				foreach (var (title, artist, creator, maps) in mapsets) {
					var set = new Mapset(title, artist, creator);
					foreach (var map in maps) {
						string version = map.Attribute("version")?.Value.Trim();
						if (string.IsNullOrEmpty(version)) continue;

						var beatmap = new Beatmap(set, version) {
							Title = title,
							Artist = artist,
							Creator = creator,
							DiffRating = new(
								jumpsDifficulty: TryParseDouble(map.Attribute("jumpDiff")?.Value),
								streamsDifficulty: TryParseDouble(map.Attribute("streamDiff")?.Value),
								burstsDifficulty: TryParseDouble(map.Attribute("burstDiff")?.Value),
								doublesDifficulty: TryParseDouble(map.Attribute("doubleDiff")?.Value ?? map.Attribute("coupletDiff")?.Value),
								slidersDifficulty: TryParseDouble(map.Attribute("sliderDiff")?.Value),
								totalDifficulty: TryParseDouble(map.Attribute("totalDiff")?.Value)
							)
						};
						set.Add(beatmap);
					}
					parsedMaps.Add(set);
				}
				IsInitialized = true;
			}
			catch {
			}
			return parsedMaps;
		}

		private static double TryParseDouble(string valueString) {
			if (!string.IsNullOrEmpty(valueString) && double.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
				return parsedValue;
			else
				return default;
		}

		public static bool SaveMapset(Mapset set) {
			if (_document is null) return false;
			Console.Write("saving to xml...");
			try {
				LoadXML();

				//check if a matching mapset exists
				IEnumerable<XElement> validMapsets = GetValidMapsets(set);
				if (validMapsets.Any()) {
					//if so, check if all difficulties are present, add if needed
					var validDiffs = validMapsets.Elements("map").ToList();
					if (validDiffs.Count != set.Count) {
						//check which difficulty is missing
						foreach (Beatmap map in set) {
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
										new XAttribute("jumpDiff", dstr(map.DiffRating.JumpsDifficulty)),
										new XAttribute("streamDiff", dstr(map.DiffRating.StreamsDifficulty)),
										new XAttribute("burstDiff", dstr(map.DiffRating.BurstsDifficulty)),
										new XAttribute("doubleDiff", dstr(map.DiffRating.DoublesDifficulty)),
										new XAttribute("sliderDiff", dstr(map.DiffRating.SlidersDifficulty))));
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
						new XAttribute("creator", set.Creator)
					);
					foreach (Beatmap map in set) {
						mapsetNode.Add(new XElement("map",
							new XAttribute("version", map.Version),
							new XAttribute("totalDiff", dstr(map.DiffRating.TotalDifficulty)),
							new XAttribute("jumpDiff", dstr(map.DiffRating.JumpsDifficulty)),
							new XAttribute("streamDiff", dstr(map.DiffRating.StreamsDifficulty)),
							new XAttribute("burstDiff", dstr(map.DiffRating.BurstsDifficulty)),
							new XAttribute("doubleDiff", dstr(map.DiffRating.DoublesDifficulty)),
							new XAttribute("sliderDiff", dstr(map.DiffRating.SlidersDifficulty))
						));
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

					foreach (Beatmap map in set) {
						writer.WriteWhitespace("\n\t\t");
						//create version branch
						writer.WriteStartElement("map");
						//populate beatmap specific data
						writer.WriteAttributeString("version", map.Version);
						writer.WriteAttributeString("totalDiff", dstr(map.DiffRating.TotalDifficulty));
						writer.WriteAttributeString("jumpDiff", dstr(map.DiffRating.JumpsDifficulty));
						writer.WriteAttributeString("streamDiff", dstr(map.DiffRating.StreamsDifficulty));
						writer.WriteAttributeString("burstDiff", dstr(map.DiffRating.BurstsDifficulty));
						writer.WriteAttributeString("doubleDiff", dstr(map.DiffRating.DoublesDifficulty));
						writer.WriteAttributeString("sliderDiff", dstr(map.DiffRating.SlidersDifficulty));
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
			return _document?.Root?.Elements("mapset").Where(el =>
				(string)el.Attribute("title") == set.Title &&
				(string)el.Attribute("artist") == set.Artist &&
				(string)el.Attribute("creator") == set.Creator)
				?? Enumerable.Empty<XElement>();
		}

		static IEnumerable<XElement> GetValidMapsets(Beatmap map) {
			return _document?.Root?.Elements("mapset").Where(el =>
				(string)el.Attribute("title") == map.Title && 
				(string)el.Attribute("artist") == map.Artist &&
				(string)el.Attribute("creator") == map.Creator)
				?? Enumerable.Empty<XElement>();
		}

		#endregion
	}
}
