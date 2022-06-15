// Most of the code in this file was lifted from https://github.com/ppy/osu-framework and https://github.com/ppy/osu
// See copyright notes at the bottom of this file.
#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.FileProcessor.BeatmapObjects.SliderPathHelpers {
	/// <summary>
	/// Utility class to compare <see cref="float"/> or <see cref="double"/> values for equality.
	/// </summary>
	public static class Precision {
		/// <summary>
		/// The default epsilon for all <see cref="float"/> values.
		/// </summary>
		public const float FLOAT_EPSILON = 1e-3f;

		/// <summary>
		/// The default epsilon for all <see cref="double"/> values.
		/// </summary>
		public const double DOUBLE_EPSILON = 1e-7;

		/// <summary>
		/// Computes whether a value is definitely greater than another given an acceptable difference.
		/// </summary>
		/// <param name="value1">The value to compare.</param>
		/// <param name="value2">The value to compare against.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> is definitely greater than <paramref name="value2"/>.</returns>
		public static bool DefinitelyBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => value1 - acceptableDifference > value2;

		/// <summary>
		/// Computes whether a value is definitely greater than another given an acceptable difference.
		/// </summary>
		/// <param name="value1">The value to compare.</param>
		/// <param name="value2">The value to compare against.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> is definitely greater than <paramref name="value2"/>.</returns>
		public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => value1 - acceptableDifference > value2;

		/// <summary>
		/// Computes whether a value is almost greater than another given an acceptable difference.
		/// </summary>
		/// <param name="value1">The value to compare.</param>
		/// <param name="value2">The value to compare against.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> is almost greater than <paramref name="value2"/>.</returns>
		public static bool AlmostBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => value1 > value2 - acceptableDifference;

		/// <summary>
		/// Computes whether a value is almost greater than another given an acceptable difference.
		/// </summary>
		/// <param name="value1">The value to compare.</param>
		/// <param name="value2">The value to compare against.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> is almost greater than <paramref name="value2"/>.</returns>
		public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => value1 > value2 - acceptableDifference;

		/// <summary>
		/// Computes whether two values are equal within an acceptable difference.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
		public static bool AlmostEquals(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => Math.Abs(value1 - value2) <= acceptableDifference;

		/// <summary>
		/// Computes whether two <see cref="Vector2"/>s are equal within an acceptable difference.
		/// </summary>
		/// <param name="value1">The first <see cref="Vector2"/>.</param>
		/// <param name="value2">The second <see cref="Vector2"/>.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
		public static bool AlmostEquals(Vector2 value1, Vector2 value2, float acceptableDifference = FLOAT_EPSILON) => AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);

		/// <summary>
		/// Computes whether two values are equal within an acceptable difference.
		/// </summary>
		/// <param name="value1">The first value.</param>
		/// <param name="value2">The second value.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
		public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => Math.Abs(value1 - value2) <= acceptableDifference;

		/// <summary>
		/// Computes whether two <see cref="RectangleF"/>s intersect within an acceptable difference.
		/// </summary>
		/// <param name="rect1">The first <see cref="RectangleF"/>.</param>
		/// <param name="rect2">The second <see cref="RectangleF"/>.</param>
		/// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
		/// <returns>Whether <paramref name="rect1"/> and <paramref name="rect2"/> intersect.</returns>
		public static bool AlmostIntersects(RectangleF rect1, RectangleF rect2, float acceptableDifference = FLOAT_EPSILON)
				=> rect1.X <= rect2.X + rect2.Width + acceptableDifference
					 && rect2.X <= rect1.X + rect1.Width + acceptableDifference
					 && rect1.Y <= rect2.Y + rect2.Height + acceptableDifference
					 && rect2.Y <= rect1.Y + rect1.Height + acceptableDifference;
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
