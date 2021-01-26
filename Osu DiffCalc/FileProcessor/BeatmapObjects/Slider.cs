namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	class Slider : HitObject {
		/// <summary> Type of curve used to construct this slider. Is used to determine the speed and length of the slider. </summary>
		public SliderType Type { get; }
		/// <summary>
		/// Amount of times the player has to follow the slider's curve back-and-forth before the slider is complete.
		/// It can also be interpreted as the repeat count plus one.
		/// </summary>
		public int NumSlides { get; }
		/// <summary> Visual length of the slider in osupixels </summary>
		public double PixelLength { get; }
		/// <summary> Total length of the slider in osupixels, accounting for repeats </summary>
		public double TotalLength { get; }
		/// <summary> Speed of the slider in in osupixels/second </summary>
		public double PxPerSecond { get; }
		/// <summary> X end position in osupixels </summary>
		public int X2 { get; }
		/// <summary> Y end position in osupixels </summary>
		public int Y2 { get; }
		/// <summary> Control points of the slider measured in osupixels </summary>
		public List<Point> Points { get; }

		public Slider(int x, int y, int startTime, string sliderType, double pixelLength, int repeat, 
			            List<Point> points, TimingPoint timingPoint, double sliderMultiplier)
			     : base(x, y, startTime, startTime) {
			Points = points;
			NumSlides = repeat;
			PixelLength = pixelLength;

			// slider type
			sliderType = sliderType?.Trim().ToLower();
			char sliderChar = !string.IsNullOrEmpty(sliderType) ? sliderType[0] : ' ';
			Type = sliderChar switch {
				'B' => SliderType.Bezier,
				'C' => SliderType.CentripetalCatmullRom,
				'L' => SliderType.Linear,
				'P' => SliderType.PerfectCircle,
				_ => SliderType.Bezier, // TODO: is this the default for old osu maps?
			};
			if (points.Count == 2)
				Type = SliderType.Linear;
			else if (Type == SliderType.PerfectCircle && points.Count != 3) 
				Type = SliderType.Bezier;

			/* From the updated osu!wiki, 2021-01-25:
			 * If the slider's length is longer than the defined curve, the slider will extend until it reaches the target length:
			 *   For bézier, catmull, and linear curves, it continues in a straight line from the end of the curve.
			 *   For perfect circle curves, it continues the circular arc.
			 * Notice: The slider's `length` can be used to determine the time it takes to complete the slider. 
			 *   `length / (SliderMultiplier * 100) * beatLength` tells how many milliseconds it takes to complete one slide of the 
			 *   slider (assuming beatLength has been adjusted for inherited timing points). */

			// TODO: true calculation of the slider's length by reconstructing the curve
			TotalLength = PixelLength * NumSlides;

			// calculate end time of slider
			double sliderVelocityMultiplier = timingPoint.EffectiveSliderBPM / timingPoint.Bpm;
			double calculatedEndTime = StartTime + (TotalLength / (100.0 * sliderMultiplier * sliderVelocityMultiplier) * timingPoint.MsPerBeat);
			EndTime = (int)(calculatedEndTime + 0.5);

			// calculate the speed in px/s
			double time = Math.Max(EndTime - StartTime, 1);
			PxPerSecond = TotalLength * 1000.0 / time;
			// get x2, y2
			if (NumSlides % 2 == 0) {
				X2 = Points[^1].X; // TODO: these are approximations
				Y2 = Points[^1].Y;
			}
			else {
				X2 = Points[0].X; // TODO: these are approximations
				Y2 = Points[0].Y;
			}
		}

		public enum SliderType {
			Bezier,
			CentripetalCatmullRom,
			Linear,
			PerfectCircle,
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
