namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using OsuDiffCalc.Utility;
	using SliderPathHelpers;

	record Slider : HitObject {
		public Slider(float x, float y, double startTime, string sliderType, double pixelLength, int numSlides, 
			            List<Vector2> controlPoints)
			     : base(x, y, startTime, startTime) {
			ControlPoints = new(controlPoints);
			NumSlides = numSlides;
			PixelLength = pixelLength;
			TotalLength = pixelLength * numSlides;

			// slider type
			sliderType = sliderType?.Trim().ToUpper() ?? " ";
			char sliderChar = sliderType.Length != 0 ? sliderType[0] : ' ';
			PathType = sliderChar switch {
				'B' => PathType.Bezier,
				'C' => PathType.Catmull,
				'L' => PathType.Linear,
				'P' => PathType.PerfectCircle,
				_ => PathType.Bezier, // TODO: is this the default for old osu maps?
			};
			if (controlPoints.Count == 2) // 2 points can only define a line
				PathType = PathType.Linear;
			else if (PathType == PathType.PerfectCircle && controlPoints.Count != 3) // perfect circle must be 3 points
				PathType = PathType.Bezier;
		}

		/// <summary>
		/// Amount of times the player has to follow the slider's curve back-and-forth before the slider is complete.
		/// It can also be interpreted as the repeat count plus one.
		/// </summary>
		public int NumSlides { get; }
		/// <summary> Visual length of the slider in osupixels </summary>
		public double PixelLength { get; }
		/// <summary> Total length of the slider in osupixels, accounting for repeats </summary>
		public double TotalLength { get; }
		/// <summary> Control points of the slider measured in osupixels. Note that [0] is the start position (X, Y) </summary>
		public EquatableReadOnlyList<Vector2> ControlPoints { get; }
		/// <summary> Type of path used to construct this slider. </summary>
		public PathType PathType { get; }
		/// <summary> Speed of the slider in in osupixels/second </summary>
		public double PxPerSecond { get; private set; }
		/// <inheritdoc cref="TimingPoint.MsPerBeat"/>
		public double MsPerBeat { get; private set; }
		/// <summary> X end position in osupixels </summary>
		public float X2 { get; private set; }
		/// <summary> Y end position in osupixels </summary>
		public float Y2 { get; private set; }

		/// <summary>
		/// Find the end point and end time for the slider (requires timing information)
		/// </summary>
		/// <param name="timingPoint"> Timing point for this slider </param>
		internal void AnalyzeShape(TimingPoint timingPoint) {
			/* From the updated osu!wiki, 2021-01-25:
			 * If the slider's length is longer than the defined curve, the slider will extend until it reaches the target length:
			 *   For bézier, catmull, and linear curves, it continues in a straight line from the end of the curve.
			 *   For perfect circle curves, it continues the circular arc.
			 * Notice: The slider's `length` can be used to determine the time it takes to complete the slider. 
			 *   `length / (SliderMultiplier * 100) * beatLength` tells how many milliseconds it takes to complete one slide of the 
			 *   slider (assuming beatLength has been adjusted for inherited timing points). */

			/* Example sliders as of 2022-06-14
			 * 
			 * Vertical line up:      '255,248,0,2,0,L|257:105,1,112.500000447034'
			 * 3 point arc:           '119,189,600,6,0,P|147:118|193:111,1,112.500000447034'
			 * big circle from 3 pts: '398,220,1200,6,0,P|415:269|385:207,1,450.000001788138'
			 * 3 straight sections:   '125,258,2400,6,0,B|123:159|123:159|242:190|242:190|242:68,1,337.500001341103'
			 * straight then arc:     '165,265,3300,2,0,B|164:136|164:136|262:90|324:178,1,300.000001192092'
			 * basically spline:      '204,144,4125,6,0,B|261:58|261:58|226:214|226:214|319:80|319:80|320:248|320:248|383:84|383:84|386:272|386:272|462:128,1,1087.50000432133'
			 * same spline(*):        '204,144,6525,2,0,B|261:58|261:58|226:214|226:214|319:80|319:80|320:248|320:248|383:84|383:84|386:272|386:272|448:159,1,1087.50000432133'
			 * (*) moved the requested end point but barely affected the position of the end circle (which was 'rounded up' past the requested end point)
			 */
			
			int n = ControlPoints.Count;
			SliderPath path = null;
			var pathPoints = new PathControlPoint[n];
			if (n > 0) {
				pathPoints[0] = new PathControlPoint(ControlPoints[0], PathType);
				for (int i = 1; i < n; i++) {
					PathType? pathType = ControlPoints[i-1] == ControlPoints[i] ? PathType : null;
					pathPoints[i] = new PathControlPoint(ControlPoints[i], pathType);
				}
			}
			try {
				path = new SliderPath(pathPoints, TotalLength / NumSlides);
			}
			catch (Exception ex) {
				Console.WriteLine("Exception in SliderPath calculation:");
				Console.WriteLine(ex.ToString());
			}

			// calculate end time of slider
			MsPerBeat = timingPoint.MsPerBeat;
			double slideTime = TotalLength / (100.0 * timingPoint.SliderVelocityMultiplier) * timingPoint.MsPerBeat;
			EndTime = (int)Math.Round(StartTime + slideTime, MidpointRounding.AwayFromZero);

			// calculate the speed in px/s
			var time = Math.Max(EndTime - StartTime, 1);
			PxPerSecond = TotalLength * 1000.0 / time;
			// get x2, y2
			if (NumSlides % 2 == 0) {
				var lastPoint = path?.CalculatedPath.LastOrDefaultS() ?? ControlPoints[^1];
				(X2, Y2) = (lastPoint.X, lastPoint.Y);
			}
			else {
				var firstPoint = path?.CalculatedPath.FirstOrDefaultS() ?? ControlPoints[0];
				(X2, Y2) = (firstPoint.X, firstPoint.Y);
			}
		}
	}
}
