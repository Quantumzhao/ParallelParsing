
using ParallelParsing.Benchmark.Generator;

const int length = 48000 * 32 * 16;
using var file = File.Create($"../Benchmark/Samples/{length}");

foreach (var buf in Generator.GenerateAll(length))
{
	file.Write(buf);
}
