namespace OsuDiffCalc.Utility {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	internal class AssociatedApplicationHelper {
		const bool SetLastError
#if DEBUG
		= true;
#else
		= false;
#endif

		static readonly Regex _commandRegex = new(@"\s*((?:['""].*\.exe['""])|(?:.*\.exe\s+)).*");

		/// <summary>
		/// Find the path to the executable associated with the given file extension
		/// </summary>
		/// <param name="extension">file extension. ex '.pdf'</param>
		public static string FindAssociatedApplication(string extension) {
			extension = extension?.Trim();
			if (string.IsNullOrEmpty(extension))
				return null;
			// ensure extension has leading '.'
			if (extension[0] != '.')
				extension = $".{extension}";

			// try to find associated program
			try {
				string associatedPath = expandAndUnquote(AssocQueryString(AssocStr.Executable, extension));
				if (!string.IsNullOrEmpty(associatedPath))
					return associatedPath;
			}
			catch { }

			// fall back to associated command
			try {
				string associatedPath = getPathFromCommand(AssocQueryString(AssocStr.Command, extension));
				if (!string.IsNullOrEmpty(associatedPath))
					return associatedPath;
			}
			catch { }

			// fall back to using the registry
			try {
				using var classKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension);
				if (classKey is not null) {
					string value = classKey.GetValue("")?.ToString();
					if (!string.IsNullOrEmpty(value)) {
						using var objKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($"{value}\\shell\\open\\command");
						if (objKey is not null)
							return getPathFromCommand(objKey.GetValue("")?.ToString());
					}
				}
			}
			catch { }

			return null;

			static string getPathFromCommand(string command) {
				var match = _commandRegex.Match(command);
				if (match.Success)
					return expandAndUnquote(match.Groups[1].Value);
				else
					return null;
			}

			static string expandAndUnquote(string path) {
				path = path.Trim().Trim('"');
				return System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
			}
		}

		private static string AssocQueryString(AssocStr association, string extension) {
			uint length = 0;
			var ret = AssocQueryString(AssocF.None, association, extension, null, null, ref length);
			if (ret != AssocQueryStringResult.S_FALSE)
				throw new InvalidOperationException($"Could not determine associated string, unable to get the required buffer length. Error code: 0x{ret:x}");

			var sb = new StringBuilder((int)length); // (length-1) will probably work too as null termination is added
			ret = AssocQueryString(AssocF.None, association, extension, null, sb, ref length);
			if (ret != AssocQueryStringResult.S_OK) // expected S_OK
				throw new InvalidOperationException($"Could not determine associated string. Error code: 0x{ret:x}");

			return sb.ToString();
		}

		/// <summary>
		/// Searches for and retrieves a file or protocol association-related string from the registry.
		/// </summary>
		/// <param name="flags">
		/// The flags that can be used to control the search. 
		/// It can be any combination of ASSOCF values, except that only one ASSOCF_INIT value can be included.
		/// </param>
		/// <param name="str">The ASSOCSTR value that specifies the type of string that is to be returned.</param>
		/// <param name="pszAssoc">
		/// A pointer to a null-terminated string that is used to determine the root key. The following four types of strings can be used.
		/// <code>
		/// File name extension | A file name extension, such as .txt.
		/// CLSID               | A CLSID GUID in the standard "{GUID}" format.
		/// ProgID              | An application's ProgID, such as Word.Document.8.
		/// Executable name     | The name of an application's .exe file. The ASSOCF_OPEN_BYEXENAME flag must be set in flags.
		/// </code>
		/// </param>
		/// <param name="pszExtra">
		/// An optional null-terminated string with additional information about the location of the string. It is typically set to
		/// a Shell verb such as open. Set this parameter to NULL if it is not used.
		/// </param>
		/// <param name="pszOut">
		/// Pointer to a null-terminated string that, when this function returns successfully, receives the requested string.
		/// Set this parameter to NULL to retrieve the required buffer size.
		/// </param>
		/// <param name="pcchOut">
		/// A pointer to a value that, when calling the function, is set to the number of characters in the pszOut buffer.
		/// When the function returns successfully, the value is set to the number of characters actually placed in the buffer. <br/>
		/// If the ASSOCF_NOTRUNCATE flag is set in flags and the buffer specified in pszOut is too small, the function
		/// returns E_POINTER and the value is set to the required size of the buffer. <br/>
		/// If pszOut is NULL, the function returns S_FALSE and pcchOut points to the required size, in characters, of the buffer.
		/// </param>
		/// <returns></returns>
		/// <remarks> https://docs.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringa?redirectedfrom=MSDN </remarks>
		[DllImport("Shlwapi.dll", CharSet = CharSet.Auto, SetLastError = SetLastError)]
		private static extern AssocQueryStringResult AssocQueryString(
			[In] AssocF flags,
			[In] AssocStr str,
			[In] string pszAssoc, // LPCSTR
			[In] string pszExtra, // LPCSTR
			[Out] StringBuilder pszOut, // LPSTR
			[In, Out] ref uint pcchOut // DWORD
		);

		/// <summary>
		/// Result from AssocQueryString call
		/// </summary>
		private enum AssocQueryStringResult : uint {
			/// <summary> Success </summary>
			S_OK = 0,
			/// <summary> pszOut is NULL. pcchOut contains the required buffer size. </summary>
			S_FALSE = 1,
			/// <summary>	The pszOut buffer is too small to hold the entire string. </summary>
			E_POINTER = 0x80004003,
		}

		/// <summary> Flags used for AssocQueryString </summary>
		/// <remarks> https://www.pinvoke.net/default.aspx/shlwapi/AssocQueryString.html </remarks>
		[Flags]
		private enum AssocF : uint {
			None                 = 0,
			Init_NoRemapCLSID    = 0x1,
			Init_ByExeName       = 0x2,
			Open_ByExeName       = 0x2,
			Init_DefaultToStar   = 0x4,
			Init_DefaultToFolder = 0x8,
			NoUserSettings       = 0x10,
			NoTruncate           = 0x20,
			Verify               = 0x40,
			RemapRunDll          = 0x80,
			NoFixUps             = 0x100,
			IgnoreBaseClass      = 0x200,
			Init_IgnoreUnknown   = 0x400,
			Init_FixedProgId     = 0x800,
			IsProtocol           = 0x1000,
			InitForFile          = 0x2000,
		}

		/// <summary> Options used for AssocQueryString </summary>
		/// <remarks> https://www.pinvoke.net/default.aspx/shlwapi/AssocQueryString.html </remarks>
		private enum AssocStr {
			Command = 1,
			Executable,
			FriendlyDocName,
			FriendlyAppName,
			NoOpen,
			ShellNewValue,
			DDECommand,
			DDEIfExec,
			DDEApplication,
			DDETopic,
			InfoTip,
			QuickTip,
			TileInfo,
			ContentType,
			DefaultIcon,
			ShellExtension,
			DropTarget,
			DelegateExecute,
			Supported_Uri_Protocols,
			ProgID,
			AppID,
			AppPublisher,
			AppIconReference,
			Max
		}
	}
}
