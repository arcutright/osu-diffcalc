namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	class Slider : HitObject {
		public string SliderType { get; }
		public int Repeat { get; }
		public double PixelLength { get; }
		public double TotalLength { get; }
		public double MsPerBeat { get; }
		public double PxPerSecond { get; }
		public int X2 { get; }
		public int Y2 { get; }
		public List<Point> Points { get; }

		public Slider(int x, int y, int startTime, string sliderType, double pixelLength, int repeat, 
			            List<Point> points, TimingPoint timingPoint, double sliderMultiplier)
			     : base(x, y, startTime, startTime) {
			Points = points;
			Repeat = repeat;
			SliderType = sliderType;
			PixelLength = pixelLength;
			TotalLength = PixelLength * Repeat;

			MsPerBeat = timingPoint.MsPerBeat;

			// calculate end time of slider
			double sliderVelocityMultiplier = timingPoint.EffectiveSliderBPM / timingPoint.Bpm;
			double calculatedEndTime = StartTime + (TotalLength / (100.0 * sliderMultiplier * sliderVelocityMultiplier) * timingPoint.MsPerBeat);
			EndTime = (int)(calculatedEndTime + 0.5);

			// calculate the speed in px/s
			double time = Math.Max(EndTime - StartTime, 1);
			PxPerSecond = TotalLength * 1000.0 / time;
			// get x2, y2
			if (Repeat % 2 == 0) {
				X2 = Points[^1].X; // TODO: these are approximations
				Y2 = Points[^1].Y;
			}
			else {
				X2 = Points[0].X; // TODO: these are approximations
				Y2 = Points[0].Y;
			}
		}
	}

	public struct Point {
		public readonly int X, Y;

		public Point(int x, int y) {
			X = x;
			Y = y;
		}
	}
}
