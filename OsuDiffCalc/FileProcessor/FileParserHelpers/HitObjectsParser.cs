namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using BeatmapObjects;

	class HitObjectsParser : ParserBase {
		/// <summary>
		/// Try to parse a line from the '[HitObjects]' beatmap region and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if all the metadata was read, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			string[] data = line.Split(',');
			if (data.Length < 4) {
				failureMessage = $"Incomplete HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[0], out int x)) {
				failureMessage = $"Cannot parse x for HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[1], out int y)) {
				failureMessage = $"Cannot parse y for HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[2], out int time)) {
				failureMessage = $"Cannot parse time for HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[3], out int type)) {
				failureMessage = $"Cannot parse type for HitObject at line {lineNumber}";
				return false;
			}

			if ((type & 1) == 1) {
				// circle : x,y,time,type,hit-sound,addition
				beatmap.Add(new Hitcircle(x, y, time));
			}
			else if ((type & 2) == 2) {
				// slider : x,y,time,type,hit-sound,slidertype|points,repeat,length,edge_hitsound,edge_addition (v10 addition param),addition
				if (data.Length < 8) {
					failureMessage = $"Incomplete slider at line {lineNumber}";
					return false;
				}
				// pointsArr : slidertype|points
				string[] pointsArr = data[5].Split('|');
				if (pointsArr.Length < 2) {
					failureMessage = $"Empty slider at line {lineNumber}";
					return false;
				}
				string sliderType = pointsArr[0];
				// points : pipe-separated list of strings in the format 'x:y'
				var points = new List<Point>();
				for (int i = 1; i < pointsArr.Length; ++i) {
					string[] pointArr = pointsArr[i].Split(':');
					if (pointArr.Length >= 2) {
						if (int.TryParse(pointArr[0], out int px) && int.TryParse(pointArr[1], out int py))
							points.Add(new Point(px, py));
					}
				}
				if (points.Count == 0) {
					failureMessage = $"Empty slider at line {lineNumber}";
					return false;
				}
				if (!int.TryParse(data[6], out int numSlides)) {
					failureMessage = $"Could not parse num slides for slider at line {lineNumber}";
					return false;
				}
				if (!double.TryParse(data[7], out double pixelLength)) {
					failureMessage = $"Could not parse length for slider at line {lineNumber}";
					return false;
				}
				beatmap.Add(new Slider(x, y, time, sliderType, pixelLength, numSlides, points));
			}
			else if ((type & 8) == 8) {
				// spinner : x,y,time,type,hit-sound,end_spinner,addition
				if (data.Length < 6) {
					failureMessage = $"Incomplete spinner at line {lineNumber}";
					return false;
				}
				if (!int.TryParse(data[5], out int endTime)) {
					failureMessage = $"Could not parse end time for spinner at line {lineNumber}";
					return false;
				}
				beatmap.Add(new Spinner(x, y, time, endTime));
			}
			else {
				failureMessage = $"Unsupported HitObject at line {lineNumber} :: {TimingParser.GetTimeStamp(time)}:  xy({x} {y})  type:{type}";
				return false;
			}
			return true;
		}

	}
}
