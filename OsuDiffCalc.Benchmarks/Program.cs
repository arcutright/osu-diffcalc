using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Environments;

namespace OsuDiffCalc.Benchmarks;

public class Program {
	public static void Main(string[] args) {
		try {
			// see https://benchmarkdotnet.org/articles/configs/toolchains.html
			// override from Release so that InternalsVisibleTo is injected in main project
			ManualConfig config = DefaultConfig.Instance
				.WithOptions(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.JoinSummary | ConfigOptions.Default)
				.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing"))
				// use this approach if you want to benchmark across net versions
				//.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithRuntime(ClrRuntime.Net48).WithId("net48"))
				//.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithRuntime(CoreRuntime.Core60).WithId("net6"))
			;
			BenchmarkRunner.Run<LoadMapBenchmarks>(config, args);
			Console.WriteLine("-----------benchmarks success-----------");
			Console.ReadKey();
		}
		catch {
			Console.WriteLine("-----------benchmarks err-----------");
			Console.ReadKey();
		}
		finally {
			Console.WriteLine("-----------benchmarks finished-----------");
			Console.ReadKey();
		}
	}
}
