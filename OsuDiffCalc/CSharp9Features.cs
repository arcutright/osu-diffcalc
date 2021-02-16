namespace System.Runtime.CompilerServices {
	using System.ComponentModel;
	using System.Diagnostics;

	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata for property <c>{ init; }</c> (C#9 feature). 
	/// This class should not be used by developers in source code.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never), DebuggerNonUserCode]
	internal static class IsExternalInit {
	}
}
