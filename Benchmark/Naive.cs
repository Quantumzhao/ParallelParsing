
#pragma warning disable CS8618

using System.IO.Compression;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ParallelParsing;
using ParallelParsing.Benchmark.Naive;
using ParallelParsing.ZRan.NET;

[SimpleJob(RuntimeMoniker.NativeAot80)]
public class Naive
{
	// [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
	// public int Number;

	[Params(
		"48000.gz",
		"96000.gz",
		"192000.gz",
		"384000.gz",
		"768000.gz",
		"1536000.gz",
		"3072000.gz",
		"6144000.gz",
		"12288000.gz",
		"24576000.gz",
		"49152000.gz",
		"98304000.gz",
		"196608000.gz"
	)]
	public string GzipPath;

	private FileStream? CompressedFileStream;

	[IterationSetup]
	public void Setup()
	{
		
		// CompressedFileStream = File.OpenRead("/Volumes/Main_2/Samples/" + GzipPath);
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath);
	}

	[Benchmark]
	public void Run()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		Console.WriteLine(Core.BuildDeflateIndex_NEW(CompressedFileStream, 10000).List.Count()); 
		// Console.WriteLine(SimpleDecompressor.GetAllRecords(CompressedFileStream).Count());
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream.Position = 0;
		CompressedFileStream?.Dispose();
	}

}