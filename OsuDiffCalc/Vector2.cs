namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading.Tasks;
	using CSharpPolyfills;

	/// <summary>
	/// A readonly vector with 2 floats (X, Y) and a Length
	/// </summary>
	public readonly struct Vector2 : IEquatable<Vector2>, IEquatable<System.Numerics.Vector2> {
		private static readonly Vector2 _zero  = new(0, 0);
		private static readonly Vector2 _one   = new(1, 1);
		private static readonly Vector2 _unitX = new(1, 0);
		private static readonly Vector2 _unitY = new(0, 1);
		public static ref readonly Vector2 Zero => ref _zero;
		public static ref readonly Vector2 One => ref _one;
		public static ref readonly Vector2 UnitX => ref _unitX;
		public static ref readonly Vector2 UnitY => ref _unitY;

		public Vector2() : this(0, 0) { }
		public Vector2(float xy) : this(xy, xy) { }
		public Vector2(float x, float y) {
			X = x;
			Y = y;
			Length = MathF.Sqrt((x * x) + (y * y));
		}

		public float X { get; }
		public float Y { get; }

		/// <summary> The length of the vector </summary>
		public float Length { get; }

		/// <summary> The length of the vector, squared </summary>
		public float LengthSquared() => Length * Length; // (X * X) + (Y * Y);

		/// <inheritdoc cref="System.Numerics.Vector2.Abs(System.Numerics.Vector2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 Abs() => new(Math.Abs(X), Math.Abs(Y));

		/// <summary> Returns the vector scaled to have a length of 1 </summary>
		public Vector2 Normalized() => this / Length;

		public override bool Equals(object obj) {
			if (obj is Vector2 vector) return Equals(vector);
			if (obj is System.Numerics.Vector2 sv) return Equals(sv);
			return false;
		}
		public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
		public bool Equals(System.Numerics.Vector2 other) => X == other.X && Y == other.Y;

		public override int GetHashCode() {
#if NET5_0_OR_GREATER
			return HashCode.Combine(X, Y);
#else
			int hashCode = 1861411795;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			return hashCode;
#endif
		}

		public override string ToString() => $"X: {X:g5}, Y: {Y:g5}";

#region Static methods

		public static Vector2 From(float x, float y) => new(x, y);
		public static Vector2 From((float X, float Y) tup) => new(tup.X, tup.Y);
		public static Vector2 From(System.Numerics.Vector2 vector) => new(vector.X, vector.Y);

		/// <inheritdoc cref="System.Numerics.Vector2.Dot(System.Numerics.Vector2, System.Numerics.Vector2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(Vector2 value1, Vector2 value2) => (value1.X * value2.X) + (value1.Y * value2.Y);

		/// <summary>
		/// Fast approximation of sqrt(x^2 + y^2) using alpha max estimate (within ~2%)
		/// </summary>
		/// <remarks> see https://en.wikipedia.org/wiki/Alpha_max_plus_beta_min_algorithm </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double EstimateLength(double x, double y) {
			const double alpha1 = 0.898204193266868;
			const double beta1 = 0.485968200201465;
			(x, y) = (Math.Abs(x), Math.Abs(y));
			var (min, max) = x < y ? (x, y) : (y, x);
			var z1 = alpha1 * max + beta1 * min;
			return max > z1 ? max : z1;
		}

		/// <inheritdoc cref="EstimateLength(double, double)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EstimateLength(float x, float y) {
			const float alpha1 = 0.898204193266868f;
			const float beta1 = 0.485968200201465f;
			(x, y) = (Math.Abs(x), Math.Abs(y));
			var (min, max) = x < y ? (x, y) : (y, x);
			var z1 = alpha1 * max + beta1 * min;
			return max > z1 ? max : z1;
		}

#endregion

#region Operators

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator *(Vector2 left, Vector2 right) => new(left.X * right.X, left.Y * right.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator *(Vector2 vector, float scalar) => new(vector.X * scalar, vector.Y * scalar);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator *(float scalar, Vector2 vector) => new(vector.X * scalar, vector.Y * scalar);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator /(Vector2 left, Vector2 right) => new(left.X / right.X, left.Y / right.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator /(Vector2 vector, float scalar) => new(vector.X / scalar, vector.Y / scalar);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);

		public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
		public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);
		public static bool operator ==(Vector2 left, System.Numerics.Vector2 right) => left.Equals(right);
		public static bool operator !=(Vector2 left, System.Numerics.Vector2 right) => !left.Equals(right);
		public static bool operator ==(System.Numerics.Vector2 left, Vector2 right) => left.Equals(right);
		public static bool operator !=(System.Numerics.Vector2 left, Vector2 right) => !left.Equals(right);

		// implicit constructors
		public static implicit operator Vector2(System.Numerics.Vector2 value) => new(value.X, value.Y);
		public static implicit operator Vector2((float x, float y) tup) => new(tup.x, tup.y);
		// implicit destructors
		public static implicit operator System.Numerics.Vector2(Vector2 value) => new(value.X, value.Y);
		public static implicit operator ValueTuple<float, float>(Vector2 value) => (value.X, value.Y);

#endregion
	}
}
