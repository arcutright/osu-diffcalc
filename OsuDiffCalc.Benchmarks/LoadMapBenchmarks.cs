using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using OsuDiffCalc.FileProcessor;

namespace OsuDiffCalc.Benchmarks;

[MemoryDiagnoser]
public class LoadMapBenchmarks {
	string _testsResourcesDir;

	[GlobalSetup]
	public void GlobalSetup() {
		try {
			string[] envArgs = Environment.GetCommandLineArgs();
			Console.WriteLine($"env args: {envArgs}");
			string programPath = envArgs?.Length >= 1 ? envArgs[0] :
				Process.GetCurrentProcess()?.MainModule?.FileName
				?? typeof(CurrentMapFinderBenchmarks)?.Assembly?.Location
				?? string.Empty;
			Console.WriteLine($"----------------------------------------");
			Console.WriteLine($"program path: {programPath}");
			var slnDir = Path.GetDirectoryName(programPath);
			Console.WriteLine($"sln dir 0: {slnDir}");
			while (!string.IsNullOrEmpty(slnDir) && !Directory.EnumerateFiles(slnDir, "*.sln").Any()) {
				slnDir = Path.GetDirectoryName(slnDir);
			}
			_testsResourcesDir = Path.Combine(slnDir, "OsuDiffCalc.Tests", "Resources");
		}
		catch (Exception ex) {
			Console.WriteLine($"----------------------------------------");
			Console.WriteLine("Failed to find resources dir");
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine($"----------------------------------------");
		}
		Console.WriteLine($"resourcesDir: {_testsResourcesDir}");
		Console.WriteLine($"----------------------------------------");
	}

	[GlobalCleanup]
	public void GlobalCleanup() {
	}

	[Params(10, 100)]
	public int N;

	/* 
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.2006 (21H2)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
DefaultJob : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT

Results for Vector2 including Length property ({get;} =)

|                Method |   N |       Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------- |---- |-----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|           LoadBeatmap |  10 |   9.358 ms | 0.0315 ms | 0.0295 ms |  2140.6250 |  703.1250 |         - |     13 MB |
| LoadAndAnalyzeBeatmap |  10 |  20.129 ms | 0.0492 ms | 0.0460 ms |  4250.0000 |  125.0000 |   31.2500 |     26 MB |
|           LoadBeatmap | 100 |  92.323 ms | 0.0499 ms | 0.0417 ms | 21333.3333 | 7000.0000 |         - |    129 MB |
| LoadAndAnalyzeBeatmap | 100 | 175.170 ms | 0.3823 ms | 0.3576 ms | 37333.3333 | 2000.0000 | 1000.0000 |    225 MB |
|           LoadBeatmap | 500 | 466.2   ms | 1.22   ms | 1.08   ms | 107000.000 |35000.0000 |         - |    643 MB |
| LoadAndAnalyzeBeatmap | 500 | 863.4   ms | 2.23   ms | 1.97   ms | 184000.000 |12000.0000 | 4000.0000 |  1,119 MB |

Results for Vector2 recalculating Length property (=>)

|                Method |   N |       Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------- |---- |-----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|           LoadBeatmap |  10 |   9.191 ms | 0.0308 ms | 0.0288 ms |  2078.1250 |  578.1250 |         - |     13 MB |
| LoadAndAnalyzeBeatmap |  10 |  27.353 ms | 0.0369 ms | 0.0345 ms |  3468.7500 |   93.7500 |   31.2500 |     21 MB |
|           LoadBeatmap | 100 |  91.205 ms | 0.2164 ms | 0.2024 ms | 20833.3333 | 5666.6667 |         - |    126 MB |
| LoadAndAnalyzeBeatmap | 100 | 241.473 ms | 0.2681 ms | 0.2238 ms | 30333.3333 | 2666.6667 | 1000.0000 |    183 MB |
	 */
	/*
.NET SDK=7.0.100-preview.7.22377.5
DefaultJob : .NET 6.0.9 (6.0.922.41905), X64 RyuJIT

|                Method |   N |       Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------- |---- |-----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|           LoadBeatmap |  10 |   5.885 ms | 0.0441 ms | 0.0368 ms |   507.8125 |  250.0000 |   23.4375 |      8 MB |
| LoadAndAnalyzeBeatmap |  10 |  13.719 ms | 0.0845 ms | 0.0790 ms |  1484.3750 |  484.3750 |   31.2500 |     24 MB |
|           LoadBeatmap | 100 |  58.564 ms | 0.3284 ms | 0.3072 ms |  5111.1111 | 2555.5556 |  222.2222 |     82 MB |
| LoadAndAnalyzeBeatmap | 100 | 119.256 ms | 0.7282 ms | 0.6080 ms | 13800.0000 | 2000.0000 | 1000.0000 |    213 MB |
|           LoadBeatmap | 500 | 295.9   ms | 1.39   ms | 1.23   ms | 29000.0000 |14000.0000 | 1000.0000 |    465 MB |
| LoadAndAnalyzeBeatmap | 500 | 584.6   ms | 4.02   ms | 3.35   ms | 67000.0000 | 7000.0000 | 4000.0000 |  1,060 MB |

DefaultJob : .NET 7.0.0 (7.0.22.37506), X64 RyuJIT

|                Method |   N |       Mean |     Error |    StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------- |---- |-----------:|----------:|----------:|-----------:|----------:|----------:|----------:|
|           LoadBeatmap |  10 |   5.852 ms | 0.0885 ms | 0.0785 ms |   578.1250 |  218.7500 |         - |      9 MB |
| LoadAndAnalyzeBeatmap |  10 |  14.151 ms | 0.0978 ms | 0.0915 ms |  1484.3750 |  500.0000 |   31.2500 |     24 MB |
|           LoadBeatmap | 100 |  57.093 ms | 0.2834 ms | 0.2513 ms |  5777.7778 | 2111.1111 |         - |     93 MB |
| LoadAndAnalyzeBeatmap | 100 | 123.180 ms | 0.8179 ms | 0.7650 ms | 13000.0000 | 2000.0000 | 1000.0000 |    213 MB |
|           LoadBeatmap | 500 | 282.8   ms | 5.04   ms | 4.47   ms | 29000.0000 |10000.0000 |         - |    465 MB |
| LoadAndAnalyzeBeatmap | 500 | 616.1   ms | 3.84   ms | 3.41   ms | 67000.0000 | 7000.0000 | 4000.0000 |  1,060 MB |
	 */

	[Benchmark]
	public void LoadBeatmap() {
		for (int i = 0; i < N; i++) {
			var map = LoadMap("Excision, Downlink & Space Laces - Raise Your Fist (Critality) [Fisted].osu");
		}
	}

	[Benchmark]
	public void LoadAndAnalyzeBeatmap() {
		var map = LoadMap("Excision, Downlink & Space Laces - Raise Your Fist (Critality) [Fisted].osu");

		// warm up analysis path
		Analyzer.Analyze(map, false);
		if (!map.IsAnalyzed) throw new InvalidOperationException($"failed to analyze map on iteration -1");
		map.IsAnalyzed = false;

		// benchmark
		for (int i = 0; i < N; i++) {
			Analyzer.Analyze(map, false);
			if (!map.IsAnalyzed) throw new InvalidOperationException($"failed to analyze map on iteration {i + 1}");
			map.IsAnalyzed = false;
		}
	}

	private Beatmap LoadMap(string filePath) {
		filePath = !Path.IsPathRooted(filePath) ? Path.Combine(_testsResourcesDir, filePath) : filePath;
		bool couldParse = Parser.TryParse(filePath, out var beatmap, out string failureMsg);
		if (!couldParse || beatmap is null)
			throw new FileLoadException($"Could not parse beatmap at '{filePath}': \nmsg: '{failureMsg}'");
		return beatmap;
	}
}
