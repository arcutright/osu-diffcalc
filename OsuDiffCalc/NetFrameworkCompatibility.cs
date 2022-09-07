using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

/*
 * This file contains shims so we can write code against modern C# features and .NET APIs
 * but still support targeting .NET Framework versions
 */

#region C#9 Index + Range support
#if NETFRAMEWORK

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

#endif
#endregion

#region Modern extension methods

namespace OsuDiffCalc {
	public static class MissingExtensionMethods {
#if NETFRAMEWORK && !NET472_OR_GREATER

		/// <summary>
		/// Creates a System.Collections.Generic.HashSet`1 from an System.Collections.Generic.IEnumerable`1.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="source">An System.Collections.Generic.IEnumerable`1 to create a System.Collections.Generic.HashSet`1 from</param>
		/// <returns>A System.Collections.Generic.HashSet`1 that contains values of type TSource selected from the input sequence.</returns>
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) {
			return new HashSet<TSource>(source);
		}

#endif
	}
}

#endregion

#region Modern types

namespace System.Diagnostics.CodeAnalysis {
#if !NET5_0_OR_GREATER
	/// <summary>
	/// Indicates that certain members on a specified System.Type are accessed dynamically,
	/// for example, through System.Reflection.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
	public sealed class DynamicallyAccessedMembersAttribute : Attribute {
		/// <summary>
		/// Initializes a new instance of the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute
		/// class with the specified member types.
		/// </summary>
		/// <param name="memberTypes">The types of the dynamically accessed members.</param>
		public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) {
			MemberTypes = memberTypes;
		}

		/// <summary>
		/// Gets the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes that
		/// specifies the type of dynamically accessed members.
		/// </summary>
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
	}

	/// <summary>
	/// Specifies the types of members that are dynamically accessed. This enumeration
	/// has a System.FlagsAttribute attribute that allows a bitwise combination of its
	/// member values.
	/// </summary>
	[Flags]
	public enum DynamicallyAccessedMemberTypes {
		/// <summary>
		/// Specifies all members.
		/// </summary>
		All = -1,
		/// <summary>
		///  Specifies no members.
		/// </summary>
		None = 0,
		/// <summary>
		/// Specifies the default, parameterless public constructor.
		/// </summary>
		PublicParameterlessConstructor = 1,
		/// <summary>
		/// Specifies all public constructors.
		/// </summary>
		PublicConstructors = 3,
		/// <summary>
		/// Specifies all non-public constructors.
		/// </summary>
		NonPublicConstructors = 4,
		/// <summary>
		/// Specifies all public methods.
		/// </summary>
		PublicMethods = 8,
		/// <summary>
		/// Specifies all non-public methods.
		/// </summary>
		NonPublicMethods = 16,
		/// <summary>
		/// Specifies all public fields. 
		/// </summary>
		PublicFields = 32,
		/// <summary>
		/// Specifies all non-public fields.
		/// </summary>
		NonPublicFields = 64,
		/// <summary>
		/// Specifies all public nested types.
		/// </summary>
		PublicNestedTypes = 128,
		/// <summary>
		/// Specifies all non-public nested types.
		/// </summary>
		NonPublicNestedTypes = 256,
		/// <summary>
		/// Specifies all public properties.
		/// </summary>
		PublicProperties = 512,
		/// <summary>
		/// Specifies all non-public properties.
		/// </summary>
		NonPublicProperties = 1024,
		/// <summary>
		/// Specifies all public events.
		/// </summary>
		PublicEvents = 2048,
		/// <summary>
		/// Specifies all non-public events.
		/// </summary>
		NonPublicEvents = 4096,
		/// <summary>
		/// Specifies all interfaces implemented by the type.
		/// </summary>
		Interfaces = 8192
	}
#endif
}

#endregion


// end file
