namespace OsuDiffCalc.FileProcessor {
	using System;
	using System.Collections.Generic;
	using AnalyzerObjects;
	using BeatmapObjects;

	class Beatmap : IDisposable {
		private bool _isDisposed;
		// https://osu.ppy.sh/wiki/en/Client/Beatmap_editor/osu%21_pixel
		// TODO: should this be 640x480?
		public const int MaxX = 512;
		public const int MaxY = 384;

		public string Title, Artist, Creator, Version;
		public string Filepath, Mp3FileName;
		public string BackgroundImage;
		public double ApproachRate = -1, CircleSize = -1, HpDrain = -1, OverallDifficulty = -1;
		/// <summary> Base slider velocity in hundreds of osupixels per beat </summary>
		public double SliderMultiplier = -1;
		/// <summary> Slider ticks per beat </summary>
		public double SliderTickRate = -1;
		public double MarginOfErrorMs300; // time window for a 300
		public double MarginOfErrorMs50; // time window for a 50
		public float CircleSizePx; // actual circle size
		public int Format = -1; // osu file format version
		public int Mode = -1; // osu!standard == 0, taiko == ?, ctb == ?, mania == ? etc.
		public bool IsAnalyzed;
		public bool IsParsed;
		public bool IsMetadataParsed;
		public bool IsOfficiallySupported;
		public bool IsOsuStandard => Mode == 0;

		public DifficultyRating DiffRating { get; init; } = new();

		public List<BeatmapObject> BeatmapObjects { get; } = new();
		public List<TimingPoint> TimingPoints { get; } = new();
		public List<BreakSection> BreakSections { get; } = new();

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

		public bool Add(BeatmapObject obj) {
			if (BeatmapObjects.Count != 0 && BeatmapObjects[^1] == obj)
				return false;
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
			return true;
		}

		public bool AddTiming(TimingPoint timingPoint) {
			if (TimingPoints.Count != 0 && TimingPoints[^1] == timingPoint)
				return false;
			TimingPoints.Add(timingPoint);
			NumTimingPoints++;
			return true;
		}

		public bool AddBreak(BreakSection breakSection) {
			if (BreakSections.Count != 0 && BreakSections[^1] == breakSection)
				return false;
			BreakSections.Add(breakSection);
			NumBreakSections++;
			return true;
		}

		public TimingPoint GetTiming(HitObject obj, bool startTime = true) {
			double time = startTime ? obj.StartTime : obj.EndTime;
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
			return $"{diff,5:f2} [{Version}]";
		}

		public string GetDiffDisplayString(double scalingFactor = 1) {
			var diff = DiffRating.TotalDifficulty / scalingFactor;
			return $"{diff,5:f2} [{Version}]";
		}

		public string GetFamiliarizedDetailString() {
			double scaling =  DiffRating.TotalDifficulty / DifficultyRating.FamiliarizeRating(DiffRating.TotalDifficulty);
			return GetDiffDetailString(scaling);
		}

		public string GetDiffDetailString(double scalingFactor = 1) {
			return string.Format("jumps:{0:0.###}  streams:{3:0.###}  \ndoubles:{1:0.###}  bursts:{2:0.###}  sliders:{4:0.###}",
					DiffRating.JumpsDifficulty / scalingFactor, 
					DiffRating.DoublesDifficulty / scalingFactor,
					DiffRating.BurstsDifficulty / scalingFactor, 
					DiffRating.StreamsDifficulty / scalingFactor,
					DiffRating.SlidersDifficulty / scalingFactor);
		}

		public string GetDiffDetailString(Func<double, double> diffFunction) {
			return string.Format("jumps:{0:0.###}  streams:{3:0.###}  \ndoubles:{1:0.###}  bursts:{2:0.###}  sliders:{4:0.###}",
					diffFunction(DiffRating.JumpsDifficulty), 
					diffFunction(DiffRating.DoublesDifficulty),
					diffFunction(DiffRating.BurstsDifficulty), 
					diffFunction(DiffRating.StreamsDifficulty),
					diffFunction(DiffRating.SlidersDifficulty));
		}

		public void PrintDebug() {
			Console.WriteLine("\n--------------Beatmap---------------");
			Console.WriteLine($"format: v{Format}  official support:{IsOfficiallySupported}");
			Console.WriteLine($"title: {Title} [{Version}]");
			Console.WriteLine($"artist: {Artist}");
			Console.WriteLine($"creator: {Creator}\n");

			Console.WriteLine($"ar: {ApproachRate}  hp: {HpDrain}  cs: {CircleSize}  od: {OverallDifficulty}");
			Console.WriteLine($"slidermult: {SliderMultiplier}  tickrate: {SliderTickRate}");
			Console.WriteLine($"difficulty rating: {DiffRating.TotalDifficulty:0.0}\n");

			Console.WriteLine($"hitObjects: {NumHitObjects}  circles: {NumCircles}  sliders: {NumSliders}  spinners: {NumSpinners}");
		}

		protected virtual void Dispose(bool disposing) {
			if (!_isDisposed) {
				if (disposing) {
					DiffRating.Dispose();
				}

				_isDisposed = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
