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
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace OsuDiffCalc.Benchmarks;

public class Program {
	public static void Main(string[] args) {
		try {
			// Note: we must manually specify '*-windows' toolchain to avoid issues with Windows Defender (benchmark.net doesn't ship defaults for '*-windows')
			// see https://benchmarkdotnet.org/articles/configs/toolchains.html
			// see https://github.com/dotnet/BenchmarkDotNet/discussions/2408
			var net6_windows = CsProjCoreToolchain.From(new NetCoreAppSettings("net6.0-windows", null, ".NET 6.0 Windows"));
			var net7_windows = CsProjCoreToolchain.From(new NetCoreAppSettings("net7.0-windows", null, ".NET 7.0 Windows"));

			// override from Release -> Release_testing so that InternalsVisibleTo is injected in main project
			ManualConfig config = DefaultConfig.Instance
				.WithOptions(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.JoinSummary | ConfigOptions.StopOnFirstError)
				//.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithToolchain(InProcessNoEmitToolchain.Instance))
				// use this approach if you want to benchmark across net versions
				.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithRuntime(ClrRuntime.Net48).WithId("net48"))
				.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithRuntime(CoreRuntime.Core60).WithToolchain(net6_windows).WithId("net6"))
				//.AddJob(Job.Default.WithCustomBuildConfiguration("Release_testing").WithRuntime(CoreRuntime.Core70).WithToolchain(net7_windows).WithId("net7"))
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
