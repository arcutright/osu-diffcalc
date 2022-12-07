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

namespace OsuDiffCalc.FileProcessor.BeatmapObjects.SliderPathHelpers {
	public class SliderPath {
		/// <summary>
		/// Calculates a slider path from a list of control points.
		/// </summary>
		/// <param name="controlPoints">The control points for the path</param>
		/// <param name="pathTypes">The path type for each control point</param>
		/// <param name="numControlPoints">The number of control points</param>
		/// <param name="expectedDistance">A user-set distance of the path that may be shorter or longer than the true distance between all control points.
		/// The path will be shortened/lengthened to match this length. If null, the path will use the true distance between all control points.</param>
		public SliderPath(scoped ReadOnlySpan<Vector2> controlPoints, scoped ReadOnlySpan<PathType> pathTypes, int numControlPoints, double? expectedDistance = null) {
			// TODO: better estimate of path length
			List<Vector2> calculatedPath = new(numControlPoints);
			List<double> cumulativeLength = new(numControlPoints);

			calculatePath(controlPoints, pathTypes, numControlPoints, calculatedPath);
			CalculatedDistance = calculateLength(controlPoints, numControlPoints, calculatedPath, cumulativeLength, expectedDistance);
			int numLengthPoints = cumulativeLength.Count;
			Distance = numLengthPoints == 0 ? 0 : cumulativeLength[numLengthPoints - 1];

			int numCalculatedPoints = calculatedPath.Count;
			if (numCalculatedPoints == 0) {
				StartPoint = EndPoint = null;
			}
			else {
				StartPoint = calculatedPath[0];
				EndPoint = calculatedPath[numCalculatedPoints - 1];
			}
		}

		/// <summary>
		/// The user-set distance of the path. If non-null, <see cref="Distance"/> will match this value,
		/// and the path will be shortened/lengthened to match this length.
		/// </summary>
		public double? ExpectedDistance { get; }

		/// <summary>
		/// The distance of the path after lengthening/shortening to account for <see cref="ExpectedDistance"/>.
		/// </summary>
		public double Distance { get; }

		/// <summary>
		/// The distance of the path prior to lengthening/shortening to account for <see cref="ExpectedDistance"/>.
		/// </summary>
		public double CalculatedDistance { get; }

		public Vector2? StartPoint { get; }

		public Vector2? EndPoint { get; }

		private static void calculatePath(scoped ReadOnlySpan<Vector2> controlPoints, scoped ReadOnlySpan<PathType> pathTypes, int numControlPoints,
			                                List<Vector2> calculatedPath) {
			int n = numControlPoints;
			if (n == 0)
				return;

			int start = 0;
			for (int i = 0; i < n; i++) {
				if (pathTypes[i] == PathType.None && i < n - 1)
					continue;

				// The current vertex ends the segment
				var segmentVertices = controlPoints.Slice(start, i - start + 1);
				var segmentType = pathTypes[start];

				// calculate sub path from vertices
				if (segmentType is PathType.None or PathType.Linear) {
					for (int s = 0; s < segmentVertices.Length; s++) {
						if (calculatedPath.Count == 0 || calculatedPath[^1] != segmentVertices[s])
							calculatedPath.Add(segmentVertices[s]);
					}
				}
				else if (segmentType == PathType.Catmull) {
					var subPath = PathApproximator.ApproximateCatmull(segmentVertices);
					for (int s = 0; s < subPath.Count; s++) {
						if (calculatedPath.Count == 0 || calculatedPath[^1] != subPath[s])
							calculatedPath.Add(subPath[s]);
					}
				}
				else {
					// If for some reason a circular arc could not be fit to the 3 given points, fall back to a numerically stable bezier approximation.
					bool isBezier = true;
					if (segmentType == PathType.PerfectCurve && segmentVertices.Length == 3) {
						var subPath = PathApproximator.ApproximateCircularArc(segmentVertices);
						if (subPath is not null && subPath.Count != 0) {
							for (int s = 0; s < subPath.Count; s++) {
								if (calculatedPath.Count == 0 || calculatedPath[^1] != subPath[s])
									calculatedPath.Add(subPath[s]);
							}
							isBezier = false;
						}
					}
					if (isBezier) {
						var subPath = PathApproximator.ApproximateBezier(segmentVertices);
						for (int s = 0; s < subPath.Count; s++) {
							if (calculatedPath.Count == 0 || calculatedPath[^1] != subPath[s])
								calculatedPath.Add(subPath[s]);
						}
					}
				}

				// Start the new segment at the current vertex
				start = i;
			}
		}

		private static double calculateLength(scoped ReadOnlySpan<Vector2> controlPoints, int numControlPoints,
			                                    List<Vector2> calculatedPath, List<double> cumulativeLength, double? expectedDist) {
			double calculatedLength = 0;
			cumulativeLength.Add(0);

			for (int i = 0; i < calculatedPath.Count - 1; i++) {
				Vector2 diff = calculatedPath[i + 1] - calculatedPath[i];
				calculatedLength += diff.Length;
				cumulativeLength.Add(calculatedLength);
			}

			if (expectedDist is double expectedDistance && calculatedLength != expectedDistance) {
				// In osu-stable, if the last two control points of a slider are equal, extension is not performed.
				int n = numControlPoints;
				if (n >= 2 && controlPoints[n-1] == controlPoints[n-2] && expectedDistance > calculatedLength) {
					cumulativeLength.Add(calculatedLength);
					return calculatedLength;
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
					return calculatedLength;
				}

				// The direction of the segment to shorten or lengthen
				Vector2 dir = (calculatedPath[pathEndIndex] - calculatedPath[pathEndIndex - 1]).Normalized();

				calculatedPath[pathEndIndex] = calculatedPath[pathEndIndex - 1] + dir * (float)(expectedDistance - cumulativeLength[^1]);
				cumulativeLength.Add(expectedDistance);
			}
			return calculatedLength;
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
