namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using BeatmapObjects;

	class HitObjectsParser : ParserBase {
		/// <summary>
		/// Try to parse the '[HitObjects]' beatmap region from the <paramref name="reader"/> and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if any HitObjects were parsed, otherwise <see langword="false"/> </returns>
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[HitObjects]"))
				return false;
			if (beatmap.NumTimingPoints <= 0)
				return false;

			int breakSectionIndex = 0;
			bool breakSectionsUsed = false;
			int timingPointIndex = 0;
			bool timingPointsUsed = false;
			TimingPoint curTiming = beatmap.TimingPoints[0];

			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				string[] data = line.Split(',');
				if (data.Length < 4) 
					continue;
				int x = int.Parse(data[0]);
				int y = int.Parse(data[1]);
				int time = int.Parse(data[2]);
				int type = int.Parse(data[3]);

				// check if we are in a break section
				if (beatmap.NumBreakSections > 0) {
					while (!breakSectionsUsed && time >= beatmap.BreakSections[breakSectionIndex].StartTime) {
						beatmap.Add(beatmap.BreakSections[breakSectionIndex]);
						if (breakSectionIndex >= beatmap.NumBreakSections - 1)
							breakSectionsUsed = true;
						breakSectionIndex++;
					}
				}
				// find current timing point
				if (!timingPointsUsed && time >= beatmap.TimingPoints[timingPointIndex].Offset) {
					while (timingPointIndex < beatmap.NumTimingPoints - 1 && time >= beatmap.TimingPoints[timingPointIndex].Offset)
						timingPointIndex++;
					if (time < beatmap.TimingPoints[timingPointIndex].Offset)
						timingPointIndex--; // TODO: ???
					curTiming = beatmap.TimingPoints[timingPointIndex];
					if (timingPointIndex >= beatmap.NumTimingPoints - 1)
						timingPointsUsed = true;
					timingPointIndex++;
				}

				if ((type & 1) == 1) {
					// circle : x,y,time,type,hit-sound,addition
					beatmap.Add(new Hitcircle(x, y, time));
				}
				else if ((type & 2) == 2) {
					if (data.Length < 6) {
						Console.WriteLine("[Warn] Cannot parse slider; data is incomplete");
						continue;
					}
					// slider : x,y,time,type,hit-sound,slidertype|points,repeat,length,edge_hitsound,edge_addition (v10 addition param),addition
					// slidertype: b/c/l/p: [B]ezier/[C]atmull/[L]inear/[P]erfect circle
					string[] pointsArr = data[5].Split('|');
					string sliderType = pointsArr[0];
					var points = new List<Point>();
					for (int i = 1; i < pointsArr.Length; ++i) {
						string[] pointArr = pointsArr[i].Split(':');
						if (pointArr.Length == 2) {
							var px = int.Parse(pointArr[0]);
							var py = int.Parse(pointArr[1]);
							points.Add(new Point(px, py));
						}
					}
					int numSlides = int.Parse(data[6]);
					double pixelLength = double.Parse(data[7]);
					beatmap.Add(new Slider(x, y, time, sliderType, pixelLength, numSlides, points, curTiming, beatmap.SliderMultiplier));
				}
				else if ((type & 8) == 8) {
					// spinner : x,y,time,type,hit-sound,end_spinner,addition
					int endTime = int.Parse(data[5]);
					beatmap.Add(new Spinner(x, y, time, endTime));
				}
				else {
					// ???
					System.Console.WriteLine($"Not sure what this HitObject entry is :: {TimingParser.GetTimeStamp(time)}:  xy({x} {y})  type:{type}");
				}
				if (!reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			if (beatmap.NumHitObjects > 0)
				return true;
			else
				return false;
		}
	}
}
