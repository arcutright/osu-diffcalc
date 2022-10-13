// Most of the code in this file was lifted from https://github.com/ppy/osu-framework and https://github.com/ppy/osu
// See copyright notes at the bottom of this file.
#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using OsuDiffCalc.Utility;

namespace OsuDiffCalc.FileProcessor.BeatmapObjects.SliderPathHelpers {
	public class SliderPath {
		/// <summary>
		/// The current version of this <see cref="SliderPath"/>. Updated when any change to the path occurs.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		/// The user-set distance of the path. If non-null, <see cref="Distance"/> will match this value,
		/// and the path will be shortened/lengthened to match this length.
		/// </summary>
		public double? ExpectedDistance {
			get => expectedDistance;
			set {
				invalidate();
				expectedDistance = value;
			}
		}

		public bool HasValidLength => Distance > 0;

		/// <summary>
		/// The control points of the path.
		/// </summary>
		public IReadOnlyList<PathControlPoint> ControlPoints => controlPoints;

		/// <summary>
		/// The caclulated path
		/// </summary>
		public IReadOnlyList<Vector2> CalculatedPath {
			get {
				ensureValid();
				return calculatedPath;
			}
		}
		
		private readonly List<PathControlPoint> controlPoints = new();
		private readonly List<Vector2> calculatedPath = new();
		private readonly List<double> cumulativeLength = new();
		private double? expectedDistance = null;
		private int cachedNumberOfControlPoints = -1;

		private double calculatedLength;

		/// <summary>
		/// Creates a new <see cref="SliderPath"/>.
		/// </summary>
		public SliderPath() {
		}

		/// <summary>
		/// Creates a new <see cref="SliderPath"/> initialised with a list of control points.
		/// </summary>
		/// <param name="controlPoints">An optional set of <see cref="PathControlPoint"/>s to initialise the path with.</param>
		/// <param name="expectedDistance">A user-set distance of the path that may be shorter or longer than the true distance between all control points.
		/// The path will be shortened/lengthened to match this length. If null, the path will use the true distance between all control points.</param>
		public SliderPath(PathControlPoint[] controlPoints, double? expectedDistance = null)
				: this() {
			this.controlPoints.AddRange(controlPoints);
			ExpectedDistance = expectedDistance;
		}

		public SliderPath(PathType type, Vector2[] controlPoints, double? expectedDistance = null)
				: this(controlPoints.Select((c, i) => new PathControlPoint(c, i == 0 ? (PathType?)type : null)).ToArray(), expectedDistance) {
		}

		/// <summary>
		/// The distance of the path after lengthening/shortening to account for <see cref="ExpectedDistance"/>.
		/// </summary>
		public double Distance {
			get {
				ensureValid();
				return cumulativeLength.Count == 0 ? 0 : cumulativeLength[^1];
			}
		}

		/// <summary>
		/// The distance of the path prior to lengthening/shortening to account for <see cref="ExpectedDistance"/>.
		/// </summary>
		public double CalculatedDistance {
			get {
				ensureValid();
				return calculatedLength;
			}
		}

		/// <summary>
		/// Computes the slider path until a given progress that ranges from 0 (beginning of the slider)
		/// to 1 (end of the slider) and stores the generated path in the given list.
		/// </summary>
		/// <param name="path">The list to be filled with the computed path.</param>
		/// <param name="p0">Start progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
		/// <param name="p1">End progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
		public void GetPathToProgress(List<Vector2> path, double p0, double p1) {
			ensureValid();

			double d0 = progressToDistance(p0);
			double d1 = progressToDistance(p1);

			path.Clear();

			int i = 0;

			for (; i < calculatedPath.Count && cumulativeLength[i] < d0; ++i) {
			}

			path.Add(interpolateVertices(i, d0));

			for (; i < calculatedPath.Count && cumulativeLength[i] <= d1; ++i)
				path.Add(calculatedPath[i]);

			path.Add(interpolateVertices(i, d1));
		}

		/// <summary>
		/// Computes the position on the slider at a given progress that ranges from 0 (beginning of the path)
		/// to 1 (end of the path).
		/// </summary>
		/// <param name="progress">Ranges from 0 (beginning of the path) to 1 (end of the path).</param>
		public Vector2 PositionAt(double progress) {
			ensureValid();

			double d = progressToDistance(progress);
			return interpolateVertices(indexOfDistance(d), d);
		}

		/// <summary>
		/// Returns the control points belonging to the same segment as the one given.
		/// The first point has a PathType which all other points inherit.
		/// </summary>
		/// <param name="controlPoint">One of the control points in the segment.</param>
		public List<PathControlPoint> PointsInSegment(PathControlPoint controlPoint) {
			bool found = false;
			var pointsInCurrentSegment = new List<PathControlPoint>();

			foreach (PathControlPoint point in ControlPoints) {
				if (point.Type != null) {
					if (!found)
						pointsInCurrentSegment.Clear();
					else {
						pointsInCurrentSegment.Add(point);
						break;
					}
				}

				pointsInCurrentSegment.Add(point);

				if (point == controlPoint)
					found = true;
			}

			return pointsInCurrentSegment;
		}

		private void invalidate() {
			cachedNumberOfControlPoints = -1;
			Version++;
		}

		private void ensureValid() {
			if (cachedNumberOfControlPoints == ControlPoints.Count)
				return;

			calculatePath();
			calculateLength();
			cachedNumberOfControlPoints = ControlPoints.Count;
		}

		private void calculatePath() {
			calculatedPath.Clear();

			int n = controlPoints.Count;
			if (n == 0)
				return;

			// Note: For large n (512+), consider using ArrayPool. Doubt it happens much in practice though
			Span<Vector2> vertices = n <= 128 ? stackalloc Vector2[n] : new Vector2[n];
			for (int i = 0; i < n; i++) {
				vertices[i] = controlPoints[i].Position;
			}

			int start = 0;

			for (int i = 0; i < n; i++) {
				if (controlPoints[i].Type == null && i < n - 1)
					continue;

				// The current vertex ends the segment
				var segmentVertices = vertices.Slice(start, i - start + 1);
				var segmentType = controlPoints[start].Type ?? PathType.Linear;

				var subPath = calculateSubPath(segmentVertices, segmentType);
				for (int s = 0; s < subPath.Count; s++) {
					if (calculatedPath.Count == 0 || calculatedPath[^1] != subPath[s])
						calculatedPath.Add(subPath[s]);
				}

				// Start the new segment at the current vertex
				start = i;
			}
		}

		private IList<Vector2> calculateSubPath(ReadOnlySpan<Vector2> subControlPoints, PathType type) {
			switch (type) {
				case PathType.Linear:
					return PathApproximator.ApproximateLinear(subControlPoints);

				case PathType.PerfectCurve:
					if (subControlPoints.Length != 3)
						break;

					var subPath = PathApproximator.ApproximateCircularArc(subControlPoints);

					// If for some reason a circular arc could not be fit to the 3 given points, fall back to a numerically stable bezier approximation.
					if (subPath.Count == 0)
						break;

					return subPath;

				case PathType.Catmull:
					return PathApproximator.ApproximateCatmull(subControlPoints);
			}

			return PathApproximator.ApproximateBezier(subControlPoints);
		}

		private void calculateLength() {
			calculatedLength = 0;
			cumulativeLength.Clear();
			cumulativeLength.Add(0);

			for (int i = 0; i < calculatedPath.Count - 1; i++) {
				Vector2 diff = calculatedPath[i + 1] - calculatedPath[i];
				calculatedLength += diff.Length;
				cumulativeLength.Add(calculatedLength);
			}

			if (ExpectedDistance is double expectedDistance && calculatedLength != expectedDistance) {
				// In osu-stable, if the last two control points of a slider are equal, extension is not performed.
				if (ControlPoints.Count >= 2 && ControlPoints[^1].Position == ControlPoints[^2].Position && expectedDistance > calculatedLength) {
					cumulativeLength.Add(calculatedLength);
					return;
				}

				// The last length is always incorrect
				cumulativeLength.RemoveAt(cumulativeLength.Count - 1);

				int pathEndIndex = calculatedPath.Count - 1;

				if (calculatedLength > expectedDistance) {
					// The path will be shortened further, in which case we should trim any more unnecessary lengths and their associated path segments
					while (cumulativeLength.Count > 0 && cumulativeLength[^1] >= expectedDistance) {
						cumulativeLength.RemoveAt(cumulativeLength.Count - 1);
						calculatedPath.RemoveAt(pathEndIndex--);
					}
				}

				if (pathEndIndex <= 0) {
					// The expected distance is negative or zero
					// TODO: Perhaps negative path lengths should be disallowed altogether
					cumulativeLength.Add(0);
					return;
				}

				// The direction of the segment to shorten or lengthen
				Vector2 dir = (calculatedPath[pathEndIndex] - calculatedPath[pathEndIndex - 1]).Normalized();

				calculatedPath[pathEndIndex] = calculatedPath[pathEndIndex - 1] + dir * (float)(expectedDistance - cumulativeLength[^1]);
				cumulativeLength.Add(expectedDistance);
			}
		}

		private int indexOfDistance(double d) {
			int i = cumulativeLength.BinarySearch(d);
			return i >= 0 ? i : ~i;
		}

		private double progressToDistance(double progress) {
			return progress.Clamp(0, 1) * Distance;
		}

		private Vector2 interpolateVertices(int i, double d) {
			int n = calculatedPath.Count;
			if (n == 0)
				return Vector2.Zero;

			if (i <= 0)
				return calculatedPath[0];
			if (i >= n)
				return calculatedPath[n - 1];

			Vector2 p0 = calculatedPath[i - 1];
			Vector2 p1 = calculatedPath[i];

			double d0 = cumulativeLength[i - 1];
			double d1 = cumulativeLength[i];

			// Avoid division by and almost-zero number in case two points are extremely close to each other.
			if (Precision.AlmostEquals(d0, d1))
				return p0;

			double w = (d - d0) / (d1 - d0);
			return p0 + (p1 - p0) * (float)w;
		}
	}
}

/*
Licensed under the MIT Licence.

Copyright (c) 2021 ppy Pty Ltd <contact@ppy.sh>.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
