using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime.CompilerServices {
	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata for property <c>{ init; }</c> (C#9 feature). 
	/// This class should not be used by developers in source code.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never), DebuggerNonUserCode]
	internal static class IsExternalInit {
	}
}

namespace OsuDiffCalc {
	public static class CSharpExtensions {
		public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value) {
			(key, value) = (pair.Key, pair.Value);
		}
	}
}
