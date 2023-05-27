
#pragma warning disable CS8618

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

	[Params(
		"10_SRR24677526.fastq.gz",
		"42_SRR24651315.fastq.gz",
		"65_SRR24651321.fastq.gz", 
		"100_SRR24652360.fastq.gz",
		"203_SRR24650904.fastq.gz",
		"1600_ERR908507_2.fastq.gz",
		"2400_SRR315307_2.fastq.gz",
		"3100_SRR23885769.fastq.gz",
		"3900_SRR317047_1.fastq.gz",
		"11000_SRR1448791_1.fastq.gz",
		"21000_SRR534304_1.fastq.gz",
		"48000_SRR23885771.fastq.gz"
	)]
	public string GzipPath;

	private FileStream? CompressedFileStream;

	[IterationSetup]
	public void Setup()
	{
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath);
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