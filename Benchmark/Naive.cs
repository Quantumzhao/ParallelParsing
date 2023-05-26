
using System.IO.Compression;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ParallelParsing;
using ParallelParsing.Benchmark.Naive;

[SimpleJob(RuntimeMoniker.NativeAot70)]
public class Naive
{
	// [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
	// public int Number;

	private FileStream? CompressedFileStream;

	[IterationSetup]
	public void Setup()
	{
		CompressedFileStream = File.OpenRead("./Samples/100_SRR24652360.fastq.gz");
	}

	[Benchmark]
	public void Run()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		Console.WriteLine(SimpleDecompressor.GetAllRecords(CompressedFileStream).Count());
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
	}

}