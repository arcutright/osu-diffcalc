using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OsuDiffCalc;

public static class CSharpExtensions {
	// runtime support for decontstructing KeyValuePair, especially useful eg `foreach (var (key, value) in dict) { ... }`
	public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value) {
		(key, value) = (pair.Key, pair.Value);
	}
}

