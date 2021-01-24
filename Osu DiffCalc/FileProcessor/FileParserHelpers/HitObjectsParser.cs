namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
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
			while ((line = reader.ReadLine()) is not null) {
				string[] data = line.Split(',');
				if (data.Length < 4) 
					throw new IOException($"Cannot parse HitObjects line \"{line}\" in beatmap \"{beatmap.Title}\"");
				int x = (int)double.Parse(data[0]);
				int y = (int)double.Parse(data[1]);
				int time = (int)double.Parse(data[2]);
				int type = (int)double.Parse(data[3]) % 4;

				if (beatmap.NumBreakSections > 0) {
					if (!breakSectionsUsed && time >= beatmap.BreakSections[breakSectionIndex].StartTime) {
						beatmap.Add(beatmap.BreakSections[breakSectionIndex]);
						if (breakSectionIndex >= beatmap.NumBreakSections - 1)
							breakSectionsUsed = true;
						breakSectionIndex++;
					}
				}

				if (!timingPointsUsed && time >= beatmap.TimingPoints[timingPointIndex].Offset) {
					while (timingPointIndex < beatmap.NumTimingPoints - 1 && time >= beatmap.TimingPoints[timingPointIndex].Offset)
						timingPointIndex++;
					if (time < beatmap.TimingPoints[timingPointIndex].Offset)
						timingPointIndex--;
					curTiming = beatmap.TimingPoints[timingPointIndex];
					if (timingPointIndex >= beatmap.NumTimingPoints - 1)
						timingPointsUsed = true;
					timingPointIndex++;
				}

				//spinner
				if (type == 0) {
					//spinner : x,y,time,type,hit-sound,end_spinner,addition
					int endTime = (int)double.Parse(data[5]);
					beatmap.Add(new Spinner(time, endTime));
				}
				//circle
				else if (type == 1) {
					//circle : x,y,time,type,hit-sound,addition
					beatmap.Add(new Hitcircle(x, y, time));
				}
				//slider
				else if (type == 2) {
					/* slider : x,y,time,type,hit-sound,slidertype|points,repeat,length,edge_hitsound,edge_addition (v10 addition param),addition
					 * slidertype: b/c/l/p: [B]ezier/[C]atmull/[L]inear/ [P] is a mystery ?[P]olynomial ?[P]assthrough */
					var pointChunkArray = data[5].Split('|');
					var points = new List<Point>();
					foreach (string pointChunk in pointChunkArray) {
						string[] pointArr = pointChunk.Split(':');
						if (pointArr.Length == 2) {
							var px = (int)double.Parse(pointArr[0]);
							var py = (int)double.Parse(pointArr[1]);
							points.Add(new Point(px, py));
						}
					}
					string sliderType = pointChunkArray[0];
					int repeat = (int)double.Parse(data[6]);
					float pixelLength = float.Parse(data[7]);
					beatmap.Add(new Slider(x, y, time, sliderType, pixelLength, repeat, points, curTiming, beatmap.SliderMultiplier));
				}
				//???
				else {
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
