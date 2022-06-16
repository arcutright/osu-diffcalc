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
			// updated from the new osu! documentation: https://osu.ppy.sh/wiki/en/osu!_File_Formats/Osu_(file_format)#hit-objects
			// Hit object syntax : x,y,time,type,hitSound,<objectParams>,hitSample  (objectParams is a placeholder, each type has different params)
			string[] data = line.Split(',');
			if (data.Length < 4) {
				failureMessage = $"Incomplete HitObject at line {lineNumber}";
				return false;
			}
			if (!float.TryParse(data[0], out var x)) {
				failureMessage = $"Cannot parse x for HitObject at line {lineNumber}";
				return false;
			}
			if (!float.TryParse(data[1], out var y)) {
				failureMessage = $"Cannot parse y for HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[2], out var time)) {
				failureMessage = $"Cannot parse time for HitObject at line {lineNumber}";
				return false;
			}
			if (!int.TryParse(data[3], out var type)) {
				failureMessage = $"Cannot parse type for HitObject at line {lineNumber}";
				return false;
			}
			/* Hit object types are stored in an 8-bit integer where each bit is a flag with special meaning. The base hit object type is given by bits 0, 1, 3, and 7 (from least to most significant):
			 *  0: Hit circle
			 *  1: Slider
			 *  3: Spinner
			 *  7: osu!mania hold
			 * The remaining bits are used for distinguishing new combos and optionally skipping combo colours (commonly called "colour hax"):
			 *  2: New combo
			 *  4–6: A 3-bit integer specifying how many combo colours to skip, if this object starts a new combo. */
			if ((type & 1) == 1) {
				// circle : x,y,time,type,hitSound,hitSample
				beatmap.Add(new Hitcircle(x, y, time));
			}
			else if ((type & 2) == 2) {
				// slider : x,y,time,type,hitSound,curveType|curvePoints,slides,length,edgeSounds,edgeSets (v10 addition param),hitSample
				if (data.Length < 8) {
					failureMessage = $"Incomplete slider at line {lineNumber}";
					return false;
				}
				// pointsArr : curveType|curvePoints
				string[] pointsArr = data[5].Split('|');
				if (pointsArr.Length < 2) {
					failureMessage = $"Empty slider at line {lineNumber}";
					return false;
				}
				string sliderType = pointsArr[0];
				// points : pipe-separated list of strings in the format 'x:y' which are points not including the start (x,y)
				var points = new List<Vector2>(8) { new Vector2(x, y) };
				for (int i = 1; i < pointsArr.Length; ++i) {
					string[] curvePoints = pointsArr[i].Split(':');
					if (curvePoints.Length >= 2) {
						if (float.TryParse(curvePoints[0], out var px) && float.TryParse(curvePoints[1], out var py))
							points.Add(new Vector2(px, py));
					}
				}
				if (points.Count == 0) {
					failureMessage = $"Empty slider at line {lineNumber}";
					return false;
				}
				if (!int.TryParse(data[6], out var numSlides)) {
					failureMessage = $"Could not parse num slides for slider at line {lineNumber}";
					return false;
				}
				if (!float.TryParse(data[7], out var pixelLength)) {
					failureMessage = $"Could not parse length for slider at line {lineNumber}";
					return false;
				}
				beatmap.Add(new Slider(x, y, time, sliderType, pixelLength, numSlides, points));
			}
			else if ((type & 8) == 8) {
				// spinner : x,y,time,type,hitSound,endTime,hitSample
				if (data.Length < 6) {
					failureMessage = $"Incomplete spinner at line {lineNumber}";
					return false;
				}
				if (!int.TryParse(data[5], out var endTime)) {
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
