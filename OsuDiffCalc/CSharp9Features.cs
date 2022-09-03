using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices {
	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata for property <c>{ init; }</c> (C#9 feature). 
	/// This class should not be used by developers in source code.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never), DebuggerNonUserCode]
	public static class IsExternalInit {
	}

	// https://referencesource.microsoft.com/#mscorlib/system/runtime/compilerservices/runtimehelpers.cs,b8d6172d5976a77a
	public static class RuntimeHelpers {
		// runtime support for range operator eg `myarray[0..5]`
		public static T[] GetSubArray<T>(T[] array, Range range) {
			var (offset, length) = range.GetOffsetAndLength(array.Length);
			if (length == 0)
				return Array.Empty<T>();

			T[] dest;
			if (typeof(T).IsValueType || typeof(T[]) == array.GetType()) {
				// We know the type of the array to be exactly T[] or an array variance
				// compatible value type substitution like int[] <-> uint[].
				if (length == 0)
					return Array.Empty<T>();

				dest = new T[length];
			}
			else {
				// The array is actually a U[] where U:T. We'll make sure to create
				// an array of the exact same backing type. The cast to T[] will
				// never fail.
				dest = (T[])(Array.CreateInstance(array.GetType().GetElementType()!, length));
			}

			Array.Copy(array, offset, dest, 0, length);
			return dest;
		}
	}
}

