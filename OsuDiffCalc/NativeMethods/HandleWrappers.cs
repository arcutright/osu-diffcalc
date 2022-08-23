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

	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid {
		public SafeObjectHandle() : base(true) { }

		public SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
			base.SetHandle(preexistingHandle);
		}

		protected override bool ReleaseHandle() {
			return NativeMethods.CloseHandle(base.handle);
		}
	}

	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid {
		public SafeProcessHandle() : base(true) {
		}

		public SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
			base.SetHandle(preexistingHandle);
		}

		protected override bool ReleaseHandle() {
			return NativeMethods.CloseHandle(base.handle);
		}
	}

	/// <summary>
	/// Safe wrapper for HWINEVENTHOOK, used by SetWinEventHook and UnhookWinEvent. <br/>
	/// See https://docs.microsoft.com/en-us/windows/win32/winauto/hwineventhook)
	/// </summary>
	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public sealed class SafeWinEventHookHandle : SafeHandleZeroOrMinusOneIsInvalid {
		public SafeWinEventHookHandle() : base(true) {
		}

		public SafeWinEventHookHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
			base.SetHandle(preexistingHandle);
		}

		protected override bool ReleaseHandle() {
			return NativeMethods.UnhookWinEvent(base.handle);
		}
	}
}
