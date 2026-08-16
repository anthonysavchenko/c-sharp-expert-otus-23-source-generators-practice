using BenchmarkDotNet.Running;

namespace Demo.Benchmarks
{
	internal class Program
	{
		static void Main(string[] args)
		{
			BenchmarkRunner.Run<SerializationBenchmark>();
		}
	}
}
