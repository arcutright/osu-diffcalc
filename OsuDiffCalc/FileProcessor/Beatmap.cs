namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using AnalyzerObjects;
	using BeatmapObjects;

	class Beatmap {
		public const int MaxX = 512;
		public const int MaxY = 384;

		public string Title, Artist, Creator, Version;
		public string Filepath, Mp3FileName;
		public double ApproachRate, CircleSize, HpDrain, SliderMultiplier, SliderTickRate, Accuracy;
		public double MarginOfErrorMs300; //time window for a 300
		public double MarginOfErrorMs50; //time window for a 50
		public double CircleSizePx; //actual circle size
		public int Format = -1;
		public bool IsAnalyzed;
		public bool IsParsed;
		public bool IsMetadataParsed;
		public bool IsOfficiallySupported;

		public DifficultyRating DiffRating = new();

		public List<BeatmapObject> BeatmapObjects = new();
		public List<TimingPoint> TimingPoints = new();
		public List<BreakSection> BreakSections = new();

		public int NumHitObjects, NumBreakSections, NumTimingPoints;
		public int NumCircles, NumSliders, NumSpinners;

		public Beatmap(string filepath) {
			Filepath = filepath;
		}

		public Beatmap(Mapset set, string version) {
			Title = set?.Title;
			Artist = set?.Artist;
			Creator = set?.Creator;
			Version = version;
		}

		public void Add(BeatmapObject obj) {
			BeatmapObjects.Add(obj);
			if (obj is not BreakSection) {
				if (obj is Hitcircle)
					NumCircles++;
				else if (obj is Slider)
					NumSliders++;
				else if (obj is Spinner)
					NumSpinners++;
				NumHitObjects++;
			}
		}

		public void AddTiming(TimingPoint timingPoint) {
			TimingPoints.Add(timingPoint);
			NumTimingPoints++;
		}

		public void AddBreak(BreakSection breakSection) {
			BreakSections.Add(breakSection);
			NumBreakSections++;
		}

		public TimingPoint GetTiming(HitObject obj, bool startTime = true) {
			int time = startTime ? obj.StartTime : obj.EndTime;
			int maxIndex = -1;
			int numTimingPoints = TimingPoints.Count;
			for (int i = 0; i < numTimingPoints; i++) {
				if (time >= TimingPoints[i].Offset)
					maxIndex = i;
				else
					break;
			}
			if (maxIndex >= 0) {
				return TimingPoints[maxIndex];
			}
			else
				return null;
		}

		public string GetFamiliarizedDisplayString() {
			var diff = DifficultyRating.FamiliarizeRating(DiffRating.TotalDifficulty);
			return $"[{Version}]  {diff:0.###}";
		}

		public string GetDiffDisplayString(double scalingFactor = 1) {
			var diff = DiffRating.TotalDifficulty / scalingFactor;
			return $"[{Version}]  {diff:0.###}";
		}

		public string GetFamiliarizedDetailString() {
			double scaling =  DiffRating.TotalDifficulty / DifficultyRating.FamiliarizeRating(DiffRating.TotalDifficulty);
			return GetDiffDetailString(scaling);
		}

		public string GetDiffDetailString(double scalingFactor = 1) {
			return string.Format("jump:{0:0.####}  stream:{3:0.####}  \ncouplet:{1:0.####}  burst:{2:0.####}  slider:{4:0.####}",
					DiffRating.JumpDifficulty / scalingFactor, 
					DiffRating.CoupletDifficulty / scalingFactor,
					DiffRating.BurstDifficulty / scalingFactor, 
					DiffRating.StreamDifficulty / scalingFactor,
					DiffRating.SliderDifficulty / scalingFactor);
		}

		public string GetDiffDetailString(Func<double, double> diffFunction) {
			return string.Format("jump:{0:0.####}  stream:{3:0.####}  \ncouplet:{1:0.####}  burst:{2:0.####}  slider:{4:0.####}",
					diffFunction(DiffRating.JumpDifficulty), 
					diffFunction(DiffRating.CoupletDifficulty),
					diffFunction(DiffRating.BurstDifficulty), 
					diffFunction(DiffRating.StreamDifficulty),
					diffFunction(DiffRating.SliderDifficulty));
		}

		public void PrintDebug() {
			Console.WriteLine("\n--------------Beatmap---------------");
			Console.WriteLine($"format: v{Format}  official support:{IsOfficiallySupported}");
			Console.WriteLine($"title: {Title} [{Version}]");
			Console.WriteLine($"artist: {Artist}");
			Console.WriteLine($"creator: {Creator}\n");

			Console.WriteLine($"ar: {ApproachRate}  hp: {HpDrain}  cs: {CircleSize}  od: {Accuracy}");
			Console.WriteLine($"slidermult: {SliderMultiplier}  tickrate: {SliderTickRate}");
			Console.WriteLine($"difficulty rating: {DiffRating.TotalDifficulty:0.0}\n");

			Console.WriteLine($"hitObjects: {NumHitObjects}  circles: {NumCircles}  sliders: {NumSliders}  spinners: {NumSpinners}");
		}
	}
}
