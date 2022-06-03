/*****************************************************************************
 * 
 * This file contains wrappers used in native method calls
 * 
 *****************************************************************************/
#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Runtime.ConstrainedExecution;
	using System.Security;
	using System.Security.Permissions;
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.Win32.SafeHandles;
	
	// see https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke
	using HANDLE  = System.IntPtr;
	using PVOID   = System.IntPtr;

	internal static partial class NativeMethods {
		[SecurityCritical]
		[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
		public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private SafeObjectHandle() : base(true) { }

			internal SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
				base.SetHandle(preexistingHandle);
			}

			protected override bool ReleaseHandle() {
				return CloseHandle(base.handle);
			}
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
		public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private SafeProcessHandle() : base(true) {
			}

			internal SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
				base.SetHandle(preexistingHandle);
			}

			protected override bool ReleaseHandle() {
				return CloseHandle(base.handle);
			}
		}
	}
}
