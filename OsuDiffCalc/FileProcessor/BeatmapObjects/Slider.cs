namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	class Slider : HitObject {
		public Slider(double x, double y, double startTime, string sliderType, double pixelLength, double numSlides, 
			            List<Point> points)
			     : base(x, y, startTime, startTime) {
			Points = points;
			NumSlides = numSlides;
			PixelLength = pixelLength;
			TotalLength = pixelLength * numSlides;

			// slider type
			sliderType = sliderType?.Trim().ToLower() ?? " ";
			char sliderChar = sliderType.Length != 0 ? sliderType[0] : ' ';
			CurveType = sliderChar switch {
				'B' => SliderCurveType.Bezier,
				'C' => SliderCurveType.CentripetalCatmullRom,
				'L' => SliderCurveType.Linear,
				'P' => SliderCurveType.PerfectCircle,
				_ => SliderCurveType.Bezier, // TODO: is this the default for old osu maps?
			};
			if (points.Count == 2) // 2 points can only define a line
				CurveType = SliderCurveType.Linear;
			else if (CurveType == SliderCurveType.PerfectCircle && points.Count != 3) // perfect circle must be 3 points
				CurveType = SliderCurveType.Bezier;
		}

		/// <summary>
		/// Amount of times the player has to follow the slider's curve back-and-forth before the slider is complete.
		/// It can also be interpreted as the repeat count plus one.
		/// </summary>
		public double NumSlides { get; }
		/// <summary> Visual length of the slider in osupixels </summary>
		public double PixelLength { get; }
		/// <summary> Total length of the slider in osupixels, accounting for repeats </summary>
		public double TotalLength { get; }
		/// <summary> Control points of the slider measured in osupixels </summary>
		public IReadOnlyList<Point> Points { get; }
		/// <summary> Type of curve used to construct this slider. Is used to determine the speed and length of the slider. </summary>
		public SliderCurveType CurveType { get; }
		/// <summary> Speed of the slider in in osupixels/second </summary>
		public double PxPerSecond { get; private set; }
		/// <summary> X end position in osupixels </summary>
		public double X2 { get; private set; }
		/// <summary> Y end position in osupixels </summary>
		public double Y2 { get; private set; }

		/// <summary>
		/// Find the end point and end time for the slider (requires timing information)
		/// </summary>
		/// <param name="timingPoint"> Timing point for this slider </param>
		/// <param name="sliderMultiplier"> Beatmap slider base multiplier (constant) </param>
		internal void AnalyzeShape(TimingPoint timingPoint, double sliderMultiplier) {
			/* From the updated osu!wiki, 2021-01-25:
			 * If the slider's length is longer than the defined curve, the slider will extend until it reaches the target length:
			 *   For bézier, catmull, and linear curves, it continues in a straight line from the end of the curve.
			 *   For perfect circle curves, it continues the circular arc.
			 * Notice: The slider's `length` can be used to determine the time it takes to complete the slider. 
			 *   `length / (SliderMultiplier * 100) * beatLength` tells how many milliseconds it takes to complete one slide of the 
			 *   slider (assuming beatLength has been adjusted for inherited timing points). */

			// TODO: true calculation of the slider's length by reconstructing the curve

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

		public enum SliderCurveType {
			Bezier,
			CentripetalCatmullRom,
			Linear,
			PerfectCircle,
		}
	}

	public struct Point {
		public readonly double X, Y;

		public Point(double x, double y) {
			X = x;
			Y = y;
		}
	}
}
