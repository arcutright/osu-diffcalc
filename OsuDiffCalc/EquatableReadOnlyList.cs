namespace OsuDiffCalc {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;

	public class EquatableReadOnlyList<T> : IReadOnlyList<T>,
																					IEquatable<EquatableReadOnlyList<T>>,
		                                      IEquatable<IReadOnlyList<T>>,
		                                      IEquatable<IList<T>>,
																					IEquatable<IEnumerable<T>>,
																					IEquatable<IEnumerable>,
																					IEquatable<T[]>
		                                      where T : IEquatable<T> {
		private readonly T[] _values;

		public EquatableReadOnlyList(IEnumerable<T> values) {
			_values = values?.ToArray() ?? Array.Empty<T>();
			Count = _values.Length;
		}
		public EquatableReadOnlyList(ReadOnlySpan<T> values) {
			_values = values.ToArray();
			Count = _values.Length;
		}

		public int Count { get; }
		public T this[int index] => _values[index];
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
		public ReadOnlySpan<T> AsSpan() => _values.AsSpan();

		public override string ToString() => $"[Count={Count}]";

		public override int GetHashCode() {
			var comparer = EqualityComparer<T>.Default;
			int n = _values.Length;
			int hashCode = n.GetHashCode();
			for (int i = 0; i < n; ++i) {
				hashCode = hashCode * -1521134295 + comparer.GetHashCode(_values[i]);
			}
			return hashCode;
		}

		public override bool Equals(object obj) {
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is EquatableReadOnlyList<T> oel) return obj.Equals(oel);
			if (obj is T[] oarr) return obj.Equals(oarr);
			if (obj is IReadOnlyList<T> orl) return obj.Equals(orl);
			if (obj is IList<T> ol) return obj.Equals(ol);
			if (obj is IEnumerable<T> oe) return obj.Equals(oe);
			if (obj is IEnumerable oe2) return obj.Equals(oe2);
			return false;
		}

		public bool SequenceEqual(ReadOnlySpan<T> other) => MemoryExtensions.SequenceEqual( _values.AsSpan(), other);

		public bool Equals(Span<T> other) => other.SequenceEqual(_values);
		public bool Equals(ReadOnlySpan<T> other) => other.SequenceEqual(_values);
		public bool Equals(EquatableReadOnlyList<T> other) => SequenceEqual(other._values);
		public bool Equals(T[] other) => SequenceEqual(other);

		public bool Equals(IReadOnlyList<T> other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			int n = Count;
			if (other.Count != n) 
				return false;
			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < n; ++i) {
				if (!comparer.Equals(_values[i], other[i]))
					return false;
			}
			return true;
		}

		public bool Equals(IList<T> other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			int n = Count;
			if (other.Count != n)
				return false;
			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < n; ++i) {
				if (!comparer.Equals(_values[i], other[i]))
					return false;
			}
			return true;
		}

		public bool Equals(IEnumerable<T> other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			int n = Count;
			var comparer = EqualityComparer<T>.Default;
			int i = 0;
			foreach (var obj in other) {
				if (i == n)
					return false;
				if (!comparer.Equals(_values[i], obj))
					return false;
				++i;
			}
			return i == n;
		}

		public bool Equals(IEnumerable other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			int n = Count;
			var comparer = EqualityComparer<T>.Default;
			int i = 0;
			foreach (var obj in other) {
				if (i == n)
					return false;
				if (!comparer.Equals(_values[i], (T)obj))
					return false;
				++i;
			}
			return i == n;
		}

		public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, T[] right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, T[] right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, ReadOnlySpan<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, ReadOnlySpan<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, Span<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, Span<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, IReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, IReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, IList<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, IList<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, IEnumerable<T> right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, IEnumerable<T> right) => !(left == right);
		public static bool operator ==(EquatableReadOnlyList<T> left, IEnumerable right) => left.Equals(right);
		public static bool operator !=(EquatableReadOnlyList<T> left, IEnumerable right) => !(left == right);

		public static bool operator ==(T[] left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(T[] left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(ReadOnlySpan<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(ReadOnlySpan<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(Span<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(Span<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(IReadOnlyList<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(IReadOnlyList<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(IList<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(IList<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(IEnumerable<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(IEnumerable<T> left, EquatableReadOnlyList<T> right) => !(left == right);
		public static bool operator ==(IEnumerable left, EquatableReadOnlyList<T> right) => left.Equals(right);
		public static bool operator !=(IEnumerable left, EquatableReadOnlyList<T> right) => !(left == right);


		public static implicit operator ReadOnlySpan<T>(EquatableReadOnlyList<T> self)         => self.AsSpan();
		public static implicit operator EquatableReadOnlyList<T>(Span<T> values)               => new(values);
		public static implicit operator EquatableReadOnlyList<T>(ReadOnlySpan<T> values)       => new(values);
		public static implicit operator EquatableReadOnlyList<T>(T[] values)                   => new(values.AsSpan());
		public static implicit operator EquatableReadOnlyList<T>(List<T> values)               => new(values);
		public static implicit operator EquatableReadOnlyList<T>(ReadOnlyCollection<T> values) => new(values);
	}
}
