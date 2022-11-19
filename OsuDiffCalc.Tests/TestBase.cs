using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OsuDiffCalc.FileProcessor;

namespace OsuDiffCalc.Tests;

class TestBase {
	public static string ResourcesDir { get; }

	static TestBase() {
		string[] envArgs = Environment.GetCommandLineArgs();
		Console.WriteLine($"env args: {envArgs}");
		string programPath = envArgs?.Length >= 1 ? envArgs[0] :
			Process.GetCurrentProcess()?.MainModule?.FileName
			?? typeof(TestBase)?.Assembly?.Location
			?? string.Empty;

		var slnDir = Path.GetDirectoryName(programPath);
		Console.WriteLine($"sln dir 0: {slnDir}");
		while (!string.IsNullOrEmpty(slnDir) && !Directory.EnumerateFiles(slnDir, "*.sln").Any()) {
			slnDir = Path.GetDirectoryName(slnDir);
		}
		ResourcesDir = Path.Combine(slnDir, $"{nameof(OsuDiffCalc)}.{nameof(Tests)}", "Resources");
	}

	public static Beatmap LoadMap(string filePath) {
		filePath = !Path.IsPathRooted(filePath) ? Path.Combine(ResourcesDir, filePath) : filePath;
		Assert.IsTrue(Parser.TryParse(filePath, out var beatmap, out string failureMsg), $"Could not parse beatmap at '{filePath}': \nmsg: '{failureMsg}'");
		return beatmap;
	}
}
