using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if NETFRAMEWORK

// https://github.com/dotnet/runtime/blob/release/7.0/src/libraries/System.Private.CoreLib/src/System/MathF.cs
namespace System {
	/// <inheritdoc cref="System.Math"/>
	public static class MathF {
		/// <inheritdoc cref="Math.PI"/>
		public const float PI = (float)Math.PI;
		/// <inheritdoc cref="Math.E"/>
		public const float E = (float)Math.E;

		/// <inheritdoc cref="Math.Floor(double)"/>
		public static float Floor(float d) => (float)Math.Floor(d);
		/// <inheritdoc cref="Math.Ceiling(double)"/>
		public static float Ceiling(float d) => (float)Math.Ceiling(d);
		/// <inheritdoc cref="Math.Abs(float)"/>
		public static float Abs(float value) => (float)Math.Abs(value);
		/// <inheritdoc cref="Math.IEEERemainder(double, double)"/>
		public static float IEEERemainder(float x, float y) => (float)Math.IEEERemainder(x, y);

		/// <inheritdoc cref="Math.Sqrt(double)"/>
		public static float Sqrt(float d) => (float)Math.Sqrt(d);
		/// <summary>Returns the cube root of the specified number</summary>
		public static float Cbrt(float d) => (float)Math.Pow(d, 1.0/3.0);

		/// <inheritdoc cref="Math.Exp(double)"/>
		public static float Exp(float d) => (float)Math.Exp(d);
		/// <inheritdoc cref="Math.Pow(double, double)"/>
		public static float Pow(float x, float y) => (float)Math.Pow(x, y);

		/// <inheritdoc cref="Math.Log(double)"/>
		public static float Log(float d) => (float)Math.Log(d);
		/// <inheritdoc cref="Math.Log(double, double)"/>
		public static float Log(float d, float newBase) => (float)Math.Log(d, newBase);
		/// <inheritdoc cref="Math.Log2(double)"/>
		public static float Log2(float d) => (float)Math.Log(d, 2);
		/// <inheritdoc cref="Math.Log(double)"/>
		public static float Log10(float d) => (float)Math.Log10(d);

		/// <inheritdoc cref="Math.Sin(double)"/>
		public static float Sin(float a) => (float)Math.Sin(a);
		/// <inheritdoc cref="Math.Asin(double)"/>
		public static float Asin(float a) => (float)Math.Asin(a);
		/// <inheritdoc cref="Math.Sinh(double)"/>
		public static float Sinh(float a) => (float)Math.Sinh(a);

		/// <inheritdoc cref="Math.Cos(double)"/>
		public static float Cos(float a) => (float)Math.Cos(a);
		/// <inheritdoc cref="Math.Acos(double)"/>
		public static float Acos(float a) => (float)Math.Acos(a);
		/// <inheritdoc cref="Math.Cosh(double)"/>
		public static float Cosh(float a) => (float)Math.Cosh(a);

		/// <inheritdoc cref="Math.Tan(double)"/>
		public static float Tan(float a) => (float)Math.Tan(a);
		/// <inheritdoc cref="Math.Atan(double)"/>
		public static float Atan(float a) => (float)Math.Atan(a);
		/// <inheritdoc cref="Math.Atan2(double, double)"/>
		public static float Atan2(double x, double y) => (float)Math.Atan2(x, y);
		/// <inheritdoc cref="Math.Tanh(double)"/>
		public static float Tanh(float a) => (float)Math.Tanh(a);
	}
}

#endif
