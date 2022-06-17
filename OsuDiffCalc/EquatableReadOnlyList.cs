namespace OsuDiffCalc {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public class EquatableReadOnlyList<T> : IReadOnlyList<T>, IEquatable<EquatableReadOnlyList<T>> {
		private readonly T[] _values;

		public EquatableReadOnlyList(IEnumerable<T> values) {
			_values = values.ToArray();
		}

		public int Count => _values.Length;
		public T this[int index] => _values[index];
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

		public override string ToString() => $"[Count={Count}]";

		public bool Equals(EquatableReadOnlyList<T> other) {
			if (other is null)
				return false;

			int n = Count;
			if (other.Count != n) 
				return false;
			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < n; ++i) {
				if (!comparer.Equals(_values[i], other._values[i]))
					return false;
			}
			return true;
		}

		public override bool Equals(object obj) {
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not EquatableReadOnlyList<T> other) return false;
			return Equals(other);
		}

		public override int GetHashCode() {
			var comparer = EqualityComparer<T>.Default;
			int hashCode = -844513176;

			int n = _values.Length;
			hashCode = hashCode * -1521134295 + n.GetHashCode();
			for (int i = 0; i < n; ++i) {
				hashCode = hashCode * -1521134295 + comparer.GetHashCode(_values[i]);
			}
			return hashCode;
		}
	}
}