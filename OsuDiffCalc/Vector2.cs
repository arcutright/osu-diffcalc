namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// A readonly vector with 2 floats (X, Y) and a Length
	/// </summary>
	public readonly record struct Vector2(float X, float Y) {
		private static readonly Vector2 _zero  = new(0, 0);
		private static readonly Vector2 _one   = new(1, 1);
		private static readonly Vector2 _unitX = new(1, 0);
		private static readonly Vector2 _unitY = new(0, 1);

		/// <summary> The length of the vector </summary>
		public float Length { get; } = (float)Math.Sqrt((X * X) + (Y * Y));

		/// <summary> The length of the vector, squared </summary>
		public float LengthSquared() => Length * Length;

		/// <inheritdoc cref="System.Numerics.Vector2.Abs(System.Numerics.Vector2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 Abs() => new(Math.Abs(X), Math.Abs(Y));

		/// <summary> Returns the vector scaled to have a length of 1 </summary>
		public Vector2 Normalized() => this / Length;

		public static ref readonly Vector2 Zero => ref _zero;
		public static ref readonly Vector2 One => ref _one;
		public static ref readonly Vector2 UnitX => ref _unitX;
		public static ref readonly Vector2 UnitY => ref _unitY;

		public static Vector2 From(float x, float y) => new(x, y);
		public static Vector2 From(System.Numerics.Vector2 vector) => new(vector.X, vector.Y);

		#region Static methods

		/// <inheritdoc cref="System.Numerics.Vector2.Dot(System.Numerics.Vector2, System.Numerics.Vector2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(Vector2 value1, Vector2 value2) => (value1.X * value2.X) + (value1.Y * value2.Y);

		/// <inheritdoc cref="System.Numerics.Vector2.DistanceSquared(System.Numerics.Vector2, System.Numerics.Vector2)"/>
		public static float DistanceSquared(Vector2 value1, Vector2 value2) {
#if NET47_OR_GREATER
			if (System.Numerics.Vector.IsHardwareAccelerated) {
				Vector2 vector = value1 - value2;
				return Dot(vector, vector);
			}
			else {
				float dx = value1.X - value2.X;
				float dy = value1.Y - value2.Y;
				return dx * dx + dy * dy;
			}
#else
			float dx = value1.X - value2.X;
			float dy = value1.Y - value2.Y;
			return dx * dx + dy * dy;
#endif
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
		public static Vector2 operator /(Vector2 vector, float scalar) {
			float x = 1f / scalar;
			return new Vector2(vector.X * x, vector.Y * x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);


		public static implicit operator System.Numerics.Vector2(Vector2 value) => new(value.X, value.Y);
		public static implicit operator Vector2(System.Numerics.Vector2 value) => new(value.X, value.Y);

#endregion
	}
}
