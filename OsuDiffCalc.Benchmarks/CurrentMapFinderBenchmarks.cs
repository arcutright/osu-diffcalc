using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using OsuDiffCalc.FileFinder;
using OsuDiffCalc.FileProcessor;
using OsuDiffCalc.OsuMemoryReader;
using OsuDiffCalc.OsuMemoryReader.OsuMemoryModels;
using OsuDiffCalc.Utility;

namespace OsuDiffCalc.Benchmarks;

[MemoryDiagnoser]
public class CurrentMapFinderBenchmarks {
	const string _osuBaseAddressNeedleHexString = "F80174048365"; // see OsuStateReader.cs
	ProcessPropertyReader _memoryReader;

	[GlobalSetup]
	public void GlobalSetup() {
		try {
			_memoryReader = new(_osuBaseAddressNeedleHexString) {
				TargetProcess = Finder.GetOsuProcess(NativeMethods.GetCurrentThreadId())
			};
		}
		catch (Exception ex) {
			Console.WriteLine($"----------------------------------------");
			Console.WriteLine("Failed to setup memory reader");
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine($"----------------------------------------");
		}
	}

	[GlobalCleanup]
	public void GlobalCleanup() {
		_memoryReader?.Dispose();
	}

	[Params(10, 100, 500)]
	public int N;

	/* 
Results from benchmarking safe vs unsafe property reads

Unsafe was marshalling void* and using fixed(void* ptr = buffer) all over
the gains weren't really worth how unreadable it was

|            Method |   N |       Mean |    Error |   StdDev |   Gen 0 | Allocated |
|------------------ |---- |-----------:|---------:|---------:|--------:|----------:|
|       ReadStrings |  10 |   141.9 us |  0.44 us |  0.41 us |  0.9766 |     10 KB |
| ReadStringsUnsafe |  10 |   136.4 us |  0.46 us |  0.43 us |       - |      3 KB |
|       ReadStrings | 100 | 1,411.3 us |  5.16 us |  4.82 us | 11.7188 |    100 KB |
| ReadStringsUnsafe | 100 | 1,368.1 us |  5.60 us |  5.24 us |       - |     31 KB |
|       ReadStrings | 500 | 7,106.3 us | 29.24 us | 27.35 us | 62.5000 |    498 KB |
| ReadStringsUnsafe | 500 | 6,825.3 us | 23.12 us | 21.63 us |  7.8125 |    157 KB |
	 */

	//[Benchmark]
	public void ReadStrings() {
		// note that these are only valid if osu! is running
		bool readOk = true;
		for (int i = 0; i < N; i++) {
			readOk |= _memoryReader.TryReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.MapString), out var mapString);
			readOk |= _memoryReader.TryReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.FolderName), out var folderName);
			readOk |= _memoryReader.TryReadProperty<CurrentBeatmap, string>(nameof(CurrentBeatmap.OsuFileName), out var osuFileName);
			if (!readOk || string.IsNullOrEmpty(mapString)) throw new InvalidOperationException("failed to read map string");
		}
	}

	//[Benchmark]
	//public void ReadStringsUnsafe() {
	//	for (int i = 0; i < N; i++) {
	//		_memoryReader.TryReadPropertyUnsafe<CurrentBeatmap, string>(nameof(CurrentBeatmap.MapString), out var mapString);
	//		_memoryReader.TryReadPropertyUnsafe<CurrentBeatmap, string>(nameof(CurrentBeatmap.FolderName), out var folderName);
	//		_memoryReader.TryReadPropertyUnsafe<CurrentBeatmap, string>(nameof(CurrentBeatmap.OsuFileName), out var osuFileName);
	//		if (string.IsNullOrEmpty(mapString)) throw new InvalidOperationException("failed to read map string");
	//	}
	//}
}
