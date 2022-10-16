using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("615bcb15-b9d0-49fc-8bd0-a91ea763f0e9")]

#if DEBUG || RELEASE_TESTING

[assembly: InternalsVisibleTo("OsuDiffCalc.Tests")]
[assembly: InternalsVisibleTo("OsuDiffCalc.Benchmarks")]

#endif

