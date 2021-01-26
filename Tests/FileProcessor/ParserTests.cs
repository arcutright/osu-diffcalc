namespace Tests.FileProcessor {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using NUnit.Framework;
	using OsuDiffCalc.FileProcessor;

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
		public void VariedOldMaps() {

		}

		[Test]
		public void VariedV14Maps() {

		}

		[Test]
		public void TwoButtonMaps() {

		}

		private Beatmap LoadMap(string relativePath) {
			// get path
			Assert.IsTrue(Parser.TryParse(path, out var beatmap), $"Could not parse beatmap at {path}");
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
