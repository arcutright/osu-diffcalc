namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;
	using BeatmapObjects;

	class EventsParser : ParserBase {
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[Events]"))
				return false;

			string line;
			while ((line = reader.ReadLine()?.Trim()) is not null) {
				if (line.Length == 0 || line.StartsWith("//"))
					continue;
				string[] data = line.Split(',');
				if (data.Length >= 3) {
					int type = int.Parse(data[0]);
					// type 0: backgrounds
					// type 1: videos
					// type 2: breaks
					if (type == 2) {
						int start = int.Parse(data[1]);
						int end = int.Parse(data[2]);
						beatmap.AddBreak(new BreakSection(start, end));
					}
				}
				if (!reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			return true;
		}

	}
}
