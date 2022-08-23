/*****************************************************************************
 * 
 * This file contains structs used by native methods.
 * 
 * These are defined by Windows and documentation has been added if available.
 * Several structs were reverse engineered by myself or others because
 * Microsoft does not document everything in the Winows kernel.
 * 
 *****************************************************************************/
#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
namespace OsuDiffCalc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading.Tasks;

	// see https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke
	using HANDLE  = System.IntPtr;
	using PVOID   = System.IntPtr;
	using DWORD   = System.UInt32;
	using WORD    = System.UInt16;
	using ULONG   = System.UInt32;
	using UINT    = System.UInt32;
	using USHORT  = System.UInt16;
	using LONG    = System.Int32;
	using INT     = System.Int32;
	using SHORT   = System.Int16;
	using UCHAR   = System.Byte;
	using CHAR    = System.Byte;
	using CCHAR   = System.Byte;
	using BYTE    = System.Byte;
	using BOOL    = System.Boolean;
	using BOOLEAN = System.Byte; // true = 1, false = 0
	using static OsuDiffCalc.NativeMethods;

	// notes from WinDef.h           S/U is signed/unsigned
	// Type                        | S/U | x86    | x64
	// ----------------------------+-----+--------+-------
	// BYTE, BOOLEAN               | U   | 8 bit  | 8 bit
	// ----------------------------+-----+--------+-------
	// SHORT                       | S   | 16 bit | 16 bit
	// USHORT, WORD                | U   | 16 bit | 16 bit
	// ----------------------------+-----+--------+-------
	// INT, LONG                   | S   | 32 bit | 32 bit
	// UINT, ULONG, DWORD          | U   | 32 bit | 32 bit
	// ----------------------------+-----+--------+-------
	// INT_PTR, LONG_PTR, LPARAM   | S   | 32 bit | 64 bit
	// UINT_PTR, ULONG_PTR, WPARAM | U   | 32 bit | 64 bit
	// ----------------------------+-----+--------+-------
	// LONGLONG                    | S   | 64 bit | 64 bit
	// ULONGLONG, QWORD            | U   | 64 bit | 64 bit

	internal static partial class NativeMethods {
		// https://docs.microsoft.com/en-us/dotnet/framework/interop/marshalling-data-with-platform-invoke\

		/// <summary>
		/// win32 struct
		/// </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/winprog/windows-data-types#unicde_string </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct UNICODE_STRING {
			public USHORT Length;
			public USHORT MaximumLength;
			public IntPtr Buffer; // wchar_t*
		}

		/// <summary>
		/// Undocumented win32 type optionally returned by NtQuerySystemInfo
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/handle_table_entry.htm
		/// <br/> https://www.codeproject.com/Articles/18975/Listing-Used-Files
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_HANDLE_INFORMATION {
			public IntPtr NumberOfHandles; // source says ULONG but I see 32 bit: 4 byte, 64 bit: 8 byte
			public SYSTEM_HANDLE_TABLE_ENTRY_INFO[] Handles;
		}

		/// <summary>
		/// Undocumented win32 type optionally returned by NtQuerySystemInfo
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/handle_table_entry.htm
		/// <br/> https://www.codeproject.com/Articles/18975/Listing-Used-Files
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO {
			public USHORT OwnerPid;
			public USHORT CreatorBackTraceIndex;
			public UCHAR ObjectType;
			public UCHAR HandleFlags;
			public USHORT HandleValue;
			public PVOID ObjectPointer;
			public ACCESS_MASK GrantedAccess;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/basic.htm?tx=135 </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_BASIC_INFORMATION {
			public uint Attributes;
			public uint GrantedAccess;
			public uint HandleCount;
			public uint PointerCount;
			public uint PagedPoolCharge;
			public uint NonPagedPoolCharge;
			[MarshalAs(UnmanagedType.U4, SizeConst = 3)]
			public uint[] Reserved;
			public uint TotalNumberOfHandles;
			public uint UnknownAt0x20;
			public uint NameInfoSize;
			public uint TypeInfoSize;
			public uint SecurityDescriptorSize;
			public long CreationTime;
		}

		/// <summary>
		/// The GENERIC_MAPPING structure defines the mapping of generic access rights to specific and standard
		/// access rights for an object. When a client application requests generic access to an object, that
		/// request is mapped to the access rights defined in this structure.
		/// </summary>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-generic_mapping </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct GENERIC_MAPPING {
			/// <summary> Specifies an access mask defining read access to an object. </summary>
			public ACCESS_MASK GenericRead;
			/// <summary> Specifies an access mask defining write access to an object. </summary>
			public ACCESS_MASK GenericWrite;
			/// <summary> Specifies an access mask defining execute access to an object. </summary>
			public ACCESS_MASK GenericExecute;
			/// <summary> Specifies an access mask defining all possible types of access to an object. </summary>
			public ACCESS_MASK GenericAll;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks>
		/// <br/> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/type.htm?tx=135
		/// <br/> https://processhacker.sourceforge.io/doc/struct___o_b_j_e_c_t___t_y_p_e___i_n_f_o_r_m_a_t_i_o_n.html
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_TYPE_INFORMATION {
			public UNICODE_STRING TypeName;
			public ULONG TotalNumberOfObjects;
			public ULONG TotalNumberOfHandles;
			public ULONG TotalPagedPoolUsage;
			public ULONG TotalNonPagedPoolUsage;
			public ULONG TotalNamePoolUsage;
			public ULONG TotalHandleTableUsage;
			public ULONG HighWaterNumberOfObjects;
			public ULONG HighWaterNumberOfHandles;
			public ULONG HighWaterPagedPoolUsage;
			public ULONG HighWaterNonPagedPoolUsage;
			public ULONG HighWaterNamePoolUsage;
			public ULONG HighWaterHandleTableUsage;
			public ULONG InvalidAttributes;
			public GENERIC_MAPPING GenericMapping;
			public ULONG ValidAccessMask;
			public BOOLEAN SecurityRequired;
			public BOOLEAN MaintainHandleCount;
			public UCHAR TypeIndex; // since WINBLUE
			public CHAR ReservedByte;
			public ULONG PoolType;
			public ULONG DefaultPagedPoolCharge;
			public ULONG DefaultNonPagedPoolCharge;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_NAME_INFORMATION {
			public UNICODE_STRING Name;
			public IntPtr NameBuffer; // wchar
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		public struct OBJECT_DATA_INFORMATION {
			public BOOLEAN InheritHandle;
			public BOOLEAN ProtectFromClose;
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks>
		/// https://processhacker.sourceforge.io/doc/ntbasic_8h_source.html#l00186
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_ATTRIBUTES {
			public ULONG Length;
			public HANDLE RootDirectory;
			public IntPtr ObjectName; // PUNICODE_STRING
			public ULONG Attributes;
			public PVOID SecurityDescriptor; // PSECURITY_DESCRIPTOR;
			public PVOID SecurityQualityOfService; // PSECURITY_QUALITY_OF_SERVICE
		}

		/// <summary>
		/// Undocumented win32 struct optionally returned by NtQueryObject
		/// </summary>
		/// <remarks> https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ob/obquery/handle_flag.htm?tx=135 </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_HANDLE_FLAG_INFORMATION {
			public bool Inherit;
			public bool ProtectFromClose;
		}

		/// <summary>
		/// Win32 RECT
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT {
			/// <summary> x position of upper-left corner </summary>
			public int Left;
			/// <summary> y position of upper-left corner </summary>
			public int Top;
			/// <summary> x position of bottom-right corner </summary>
			public int Right;
			/// <summary> y position of bottom-right corner </summary>
			public int Bottom;
			public override string ToString() => $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION {
			/// <summary>
			/// A pointer to the base address of the region of pages.
			/// </summary>
			public PVOID BaseAddress;
			/// <summary>
			/// A pointer to the base address of a range of pages allocated by the VirtualAlloc function.
			/// The page pointed to by the BaseAddress member is contained within this allocation range.
			/// </summary>
			public PVOID AllocationBase;
			/// <summary>
			/// The memory protection option when the region was initially allocated. <br/>
			/// This member can be one of the memory protection constants or 0 if the caller does not have access.
			/// </summary>
			public MEM_PROTECT AllocationProtect;
			/// <summary>
			/// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
			/// </summary>
			public nint RegionSize;
			/// <summary>
			/// The state of the pages in the region.
			/// </summary>
			public MBI_STATE State;
			/// <summary>
			/// The access protection of the pages in the region. <br/>
			/// This member is one of the values listed for the AllocationProtect member.
			/// </summary>
			public MEM_PROTECT Protect;
			/// <summary>
			/// The type of pages in the region.
			/// </summary>
			public MBI_TYPE Type;
		}

		/// <summary>
		/// Contains information about the current computer system. This includes the architecture and type of the processor,
		/// the number of processors in the system, the page size, and other such information.
		/// </summary>
		/// <remarks>
		/// See https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-system_info
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO {
			/// <summary>
			/// The processor architecture of the installed operating system. This member can be one of the following values.
			/// </summary>
			public SI_PROCESSOR_ARCHITECTURE wProcessorArchitecture;
			/// <summary>
			/// This member is reserved for future use.
			/// </summary>
			public WORD wReserved;
			/// <summary>
			/// The page size and the granularity of page protection and commitment.
			/// This is the page size used by the VirtualAlloc function.
			/// </summary>
			public DWORD dwPageSize;
			/// <summary>
			/// A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).
			/// </summary>
			public IntPtr lpMinimumApplicationAddress;
			/// <summary>
			/// A pointer to the highest memory address accessible to applications and DLLs.
			/// </summary>
			public IntPtr lpMaximumApplicationAddress;
			/// <summary>
			/// A mask representing the set of processors configured into the system.
			/// Bit 0 is processor 0; bit 31 is processor 31.
			/// </summary>
			public IntPtr dwActiveProcessorMask;
			/// <summary>
			/// The number of logical processors in the current group. To retrieve this value, use the
			/// GetLogicalProcessorInformation function.
			/// </summary>
			public DWORD dwNumberOfProcessors;
			/// <summary>
			/// An obsolete member that is retained for compatibility. Use the wProcessorArchitecture,
			/// wProcessorLevel, and wProcessorRevision members to determine the type of processor.
			/// </summary>
			public DWORD dwProcessorType;
			/// <summary>
			/// The granularity for the starting address at which virtual memory can be allocated.
			/// For more information, see VirtualAlloc.
			/// </summary>
			public DWORD dwAllocationGranularity;
			/// <summary>
			/// The architecture-dependent processor level. It should be used only for display purposes.
			/// To determine the feature set of a processor, use the IsProcessorFeaturePresent function. <br/>
			/// If wProcessorArchitecture is PROCESSOR_ARCHITECTURE_INTEL, wProcessorLevel is defined by the CPU vendor. <br/>
			/// If wProcessorArchitecture is PROCESSOR_ARCHITECTURE_IA64, wProcessorLevel is set to 1.
			/// </summary>
			public WORD wProcessorLevel;
			/// <summary>
			/// The architecture-dependent processor revision. The following table shows how the revision
			/// value is assembled for each type of processor architecture.
			/// </summary>
			public WORD wProcessorRevision;
		}
	}

	internal static class NativeStructExtensions {
		public static int Width(this RECT rect) => rect.Right - rect.Left + 1;
		public static int Height(this RECT rect) => rect.Bottom - rect.Top + 1;
	}
}
