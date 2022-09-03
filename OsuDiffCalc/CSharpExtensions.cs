using System.Collections.Generic;
using System.Linq;

namespace OsuDiffCalc;

public static class CSharpExtensions {
	// runtime support for decontstructing KeyValuePair, especially useful eg `foreach (var (key, value) in dict) { ... }`
	public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value) {
		(key, value) = (pair.Key, pair.Value);
	}

#if !NET472_OR_GREATER

	/// <summary>
	/// Creates a System.Collections.Generic.HashSet`1 from an System.Collections.Generic.IEnumerable`1.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <param name="source">An System.Collections.Generic.IEnumerable`1 to create a System.Collections.Generic.HashSet`1 from</param>
	/// <returns>A System.Collections.Generic.HashSet`1 that contains values of type TSource selected from the input sequence.</returns>
	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) {
		return source.ToHashSet();
	}

#endif
}
