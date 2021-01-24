namespace OsuDiffCalc.FileProcessor.BeatmapObjects {
	using System;

	abstract class BeatmapElement {
		public virtual void PrintDebug(string prepend = "", string append = "") {
			Console.Write(prepend);
			Console.Write($"{GetType().Name}");
			Console.WriteLine(append);
		}
	}
}
