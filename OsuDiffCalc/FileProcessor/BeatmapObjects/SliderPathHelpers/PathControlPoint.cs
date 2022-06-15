﻿// Most of the code in this file was lifted from https://github.com/ppy/osu-framework and https://github.com/ppy/osu
// See copyright notes at the bottom of this file.
#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.FileProcessor.BeatmapObjects.SliderPathHelpers {
	public record struct PathControlPoint(Vector2 Position, PathType? Type = null) {
		/// <summary>
		/// The position of this <see cref="PathControlPoint"/>.
		/// </summary>
		public Vector2 Position { get; set; } = Position;

		/// <summary>
		/// The type of path segment starting at this <see cref="PathControlPoint"/>.
		/// If null, this <see cref="PathControlPoint"/> will be a part of the previous path segment.
		/// </summary>
		public PathType? Type { get; set; } = Type;
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