namespace OsuDiffCalc.Tests.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using NUnit.Framework;
	using OsuDiffCalc.FileProcessor;
	using OsuDiffCalc.FileProcessor.BeatmapObjects;

	[TestFixture]
	class ParserTests {
		[Test]
		public void InvalidMap() {

		}

		[Test]
		public void EmptyMap() {

		}

		[Test]
		public void MapWithOnlyCircles() {

		}

		[Test]
		public void MapWithOnlySliders() {

		}

		[Test]
		public void MapWithOnlySpinners() {

		}

		[Test]
		public void MapWithOnlyBreaks() {

		}

		[Test]
		public void VariedOldMaps() {

		}

		[Test]
		public void VariedV14Maps() {

		}

		[Test]
		public void TwoButtonMaps() {

		}

		[TestCase("v8", 57, 70, 1, 4, 6, 6, 7)]
		[TestCase("v4", 255, 1, 0, 5, 9, 9, 9)]
		[TestCase("v3", 270, 67, 4, 5, 4, 4, 4)]
		[TestCase("v6", 313, 78, 6, 4, 8, 8, 7)]
		[TestCase("v13", 409, 192, 1, 4, 8, 7, 6)]
		[TestCase("v5", 278, 76, 1, 6, 7, 7, 5)]
		[TestCase("v12", 575, 255, 3, 4, 9, 7, 7)]
		[TestCase("v11", 352, 247, 0, 4, 9, 7, 7)]
		[TestCase("v7", 544, 155, 2, 4, 8, 8, 6)]
		[TestCase("v10", 483, 276, 3, 4, 9, 7, 7)]
		[TestCase("v9", 1413, 245, 1, 4, 9, 8, 4)]
		[TestCase("v14", 924, 474, 1, 4.2, 9.3, 9, 6)]
		public void FileFormatVersions(string filename, int numCircles, int numSliders, int numSpinners, double cs, double ar, double od, double hp) {
			var map = LoadMap(Path.Combine($"FileFormatVersions", $"{filename}.osu"));
			Assert.AreEqual(numCircles, map.NumCircles, "Wrong number of hitcircles");
			Assert.AreEqual(numSliders, map.NumSliders, "Wrong number of sliders");
			Assert.AreEqual(numSpinners, map.NumSpinners, "Wrong number of spinners");
			Assert.That(map.CircleSize, Is.EqualTo(cs).Within(0.2).Percent, "Wrong CS");
			Assert.That(map.ApproachRate, Is.EqualTo(ar).Within(0.2).Percent, "Wrong AR");
			Assert.That(map.OverallDifficulty, Is.EqualTo(od).Within(0.2).Percent, "Wrong OD");
			Assert.That(map.HpDrain, Is.EqualTo(hp).Within(0.2).Percent, "Wrong HP");
			int idx = 0;
			foreach (var timingPoint in map.TimingPoints) {
				Assert.That(timingPoint.MsPerBeat, Is.GreaterThanOrEqualTo(0), $"Negative ms per beat for timing point {idx}");
				Assert.That(timingPoint.Bpm, Is.GreaterThanOrEqualTo(0), $"Negative bpm for timing point {idx}");
				Assert.That(timingPoint.EffectiveSliderBPM, Is.GreaterThanOrEqualTo(0), $"Negative slider bpm for timing point {idx}");
				++idx;
			}
			idx = 0;
			foreach (var obj in map.BeatmapObjects) {
				Assert.That(obj.EndTime, Is.GreaterThanOrEqualTo(obj.StartTime), $"BeatmapObject ends before it begins: {obj}");
				if (obj is Slider slider) {
					Assert.That(slider.PixelLength, Is.GreaterThanOrEqualTo(0), $"Slider has negative length: {slider}");
					Assert.That(slider.TotalLength, Is.GreaterThanOrEqualTo(0), $"Slider has negative length: {slider}");
					Assert.That(slider.NumSlides, Is.GreaterThanOrEqualTo(0), $"Slider has negative slides: {slider}");
					Assert.That(slider.EndTime, Is.GreaterThanOrEqualTo(slider.StartTime), $"Slider starts before it ends: {slider}");
					Assert.That(slider.PxPerSecond, Is.GreaterThanOrEqualTo(0), $"Slider has negative velocity: {slider}");
				}
				++idx;
			}
		}

		private static string _resourcesDir = null;

		private Beatmap LoadMap(string filePath) {
			if (!Directory.Exists(_resourcesDir)) {
				// get parent dir of this project
				var assembly = System.Reflection.Assembly.GetAssembly(typeof(ParserTests));
				string dirPath = Path.GetDirectoryName(assembly.Location);
				string baseName = Path.GetFileName(dirPath);
				while (baseName != "bin") {
					dirPath = Path.GetDirectoryName(dirPath);
					baseName = Path.GetFileName(dirPath);
				}
				dirPath = Path.GetDirectoryName(dirPath);
				_resourcesDir = Path.Combine(dirPath, "Resources");
			}
			filePath = !Path.IsPathRooted(filePath) ? Path.Combine(_resourcesDir, filePath) : filePath;
			Assert.IsTrue(Parser.TryParse(filePath, out var beatmap, out string failureMsg), $"Could not parse beatmap at '{filePath}': \nmsg: '{failureMsg}'");
			return beatmap;
		}

		private void AssertMapContains(Beatmap map, int numCircles, int numSliders, int numSpinners, int? numBreaks = null, int? numTimingPoints = null) {
			Assert.AreEqual(numCircles, map.NumCircles, "Wrong number of circles");
			Assert.AreEqual(numSliders, map.NumSliders, "Wrong number of sliders");
			Assert.AreEqual(numSpinners, map.NumSpinners, "Wrong number of spinners");
			if (numBreaks.HasValue)
				Assert.AreEqual(numBreaks.Value, map.NumBreakSections, "Wrong number of break sections");
			if (numTimingPoints.HasValue)
				Assert.AreEqual(numTimingPoints.Value, map.NumTimingPoints, "Wrong number of timing points");
		}
	}
}
