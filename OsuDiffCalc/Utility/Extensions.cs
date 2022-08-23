namespace OsuDiffCalc.Utility {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	internal static class Extensions {
		/// <summary>
		/// Return <paramref name="value"/> clamped in the range [<paramref name="min"/>, <paramref name="max"/>]
		/// </summary>
		public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> {
			return value.CompareTo(min) < 0 ? min : (value.CompareTo(max) == 1 ? max : value);
		}

		/// <summary>
		/// Returns the first or a nullable default value when empty for struct types
		/// </summary>
		public static T? FirstOrDefaultS<T>(this IList<T> values, T? defaultValue = null) where T : struct {
			if (values is null || values.Count == 0)
				return defaultValue;
			else
				return values[0];
		}

		/// <summary>
		/// Returns the first or a nullable default value when empty for struct types
		/// </summary>
		public static T? FirstOrDefaultS<T>(this IEnumerable<T> values, T? defaultValue = null) where T : struct {
			if (values is null || !values.Any())
				return defaultValue;
			else
				return values.First();
		}

		/// <summary>
		/// Returns the last or a nullable default value when empty for struct types
		/// </summary>
		public static T? LastOrDefaultS<T>(this IList<T> values, T? defaultValue = null) where T : struct {
			if (values is null || values.Count == 0)
				return defaultValue;
			else
				return values[^1];
		}

		/// <summary>
		/// Returns the last or a nullable default value when empty for struct types
		/// </summary>
		public static T? LastOrDefaultS<T>(this IEnumerable<T> values, T? defaultValue = null) where T : struct {
			if (values is null || !values.Any())
				return defaultValue;
			else
				return values.Last();
		}

		/// <summary>
		/// HasExited with a try-catch. On exceptions, returns <see langword="true"/>.
		/// </summary>
		public static bool HasExitedSafe(this Process process) {
			try {
				return process.HasExited;
			}
			catch (System.ComponentModel.Win32Exception) {
				return true;
			}
			catch {
				return true;
			}
		}

		/// <summary>
		/// Id with a try-catch. On exceptions, returns <see langword="null"/>.
		/// </summary>
		public static int? IdSafe(this Process process) {
			try {
				return process?.Id;
			}
			catch {
				return null;
			}
		}
	}
}
