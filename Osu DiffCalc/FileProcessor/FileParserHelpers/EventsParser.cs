namespace OsuDiffCalc.FileProcessor.FileParserHelpers {
	using System.IO;
	using BeatmapObjects;

	class EventsParser : ParserBase {
		public static bool TryParse(Beatmap beatmap, ref StreamReader reader) {
			if (!TrySkipTo(ref reader, "[Events]"))
				return false;

			string line;
			while ((line = reader.ReadLine()) is not null) {
				string[] data = line.Split(',');
				if (data.Length >= 3 && data[0].Equals("2")) {
					int start = (int)double.Parse(data[1]);
					int end = (int)double.Parse(data[2]);
					beatmap.AddBreak(new BreakSection(start, end));
				}
				if (!reader.EndOfStream && reader.Peek() == '[')
					break;
			}
			return true;
		}

	}
}
