namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;
	using BeatmapObjects;

	class EventsParser : ParserBase {
		/// <summary>
		/// Try to process a line in the '[Events]' beatmap region (breaks) and populate the <paramref name="beatmap"/>
		/// </summary>
		/// <returns> <see langword="true"/> if the difficulty info was parsed, otherwise <see langword="false"/> </returns>
		public static bool TryProcessLine(int lineNumber, string line, Beatmap beatmap, out string failureMessage) {
			failureMessage = "";
			string[] data = line.Split(',');
			if (data.Length < 3) {
				return true;
			}
			// type may be a string or an integer
			// 0 => backgrounds : type,?,backgroundFileName (usually quoted),?,?  (last two added in v12, are optional)
			// 1, "Video" => videos : type,offset,videoFileName (usually quoted)
			// 2 => break : type,startTime,endTime
			// 3 => background color transformations : type,?,?,?,?
			string typeString = data[0].Trim().ToLowerInvariant();
			int type = -1;
			if (typeString.StartsWith("break", System.StringComparison.OrdinalIgnoreCase))
				type = 2;
			else if (!int.TryParse(data[0], out type)) {
				// Video, Sprite, Animation, or contained data. Example (note the 1-space indent for multiline data):
				/* 'Sprite,Background,Centre,"SB\1fKj.png",320,240'
				 * ' F,0,166620,0,1'
				 * ' F,0,176711,176715,1,0' */
				failureMessage = "";
				return true;
			}
			if (type == 0 && string.IsNullOrEmpty(beatmap.BackgroundImage)) {
				string background = data[2];
				// unquote file path if quoted (does not use escape chars)
				if (background[0] is '"' or '\'' && background[^1] is '"' or '\'')
					background = background[1..^1];
				beatmap.BackgroundImage = background;
			}
			else if (type == 2) {
				if (!int.TryParse(data[1], out int startTime)) {
					failureMessage = $"Cannot parse start time for break section at line {lineNumber}";
					return false;
				}
				if (!int.TryParse(data[2], out int endTime)) {
					failureMessage = $"Cannot parse end time for break section at line {lineNumber}";
					return false;
				}
				beatmap.AddBreak(new BreakSection(startTime, endTime));
			}
			return true;
		}
	}
}
