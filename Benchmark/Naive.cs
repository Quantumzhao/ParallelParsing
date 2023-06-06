
#pragma warning disable CS8618
#pragma warning disable CS8604

using System.IO.Compression;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ParallelParsing;
using ParallelParsing.Benchmark.Naive;
using ParallelParsing.ZRan.NET;

[MemoryDiagnoser]
[MinIterationCount(3)]
[MaxIterationCount(4)]
[MinWarmupCount(3)]
[MaxWarmupCount(5)]
[SimpleJob(RuntimeMoniker.NativeAot80)] 
public class Naive
{
	// [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
	// public int Number;

	[Params(
		// smaller files
		// "48000.gz"
		// "96000.gz"
		// "192000.gz"
		// "384000.gz",
		// "1536000.gz",
		// "6144000.gz"




		"48000",
		"96000",
		"192000",
		"384000",
		"768000",
		"1536000",
		"3072000",
		"6144000",
		"12288000",
		"24576000",
		"49152000",
		"98304000",
		"196608000"
	)]
	public string GzipPath;

	private FileStream? CompressedFileStream;
	// private FileStream? file;
	// private ParallelParsing.ZRan.NET.Index index;
	// private BatchedFASTQ records;

	[IterationSetup]
	public void Setup()
	{		
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath + ".gz");
		// index = IndexIO.Deserialize("./Samples/index/" + GzipPath + "_10k" + ".gzi");
		// records = new BatchedFASTQ(index, "./Samples/" + GzipPath + ".gz", enableSsdOptimization: false);

		// file = File.Create("./tmp/" + GzipPath + "file");
		// Console.WriteLine("index built---------------------------------------");
	}

	[Benchmark]
	public void Run()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		Console.WriteLine(Core.BuildDeflateIndex(CompressedFileStream, 1000000).Count);  // 10k 20k 50k 100k 200k 1M
		// Console.WriteLine(SimpleDecompressor.GetAllRecords(CompressedFileStream).Count());


		// for (int x = 0; x < index.List.Count - 1; x++)
		// {
		// 	var from = index.List[x];
		// 	var to = index.List[x + 1];
		// 	var len_in = to.Input - from.Input + 1;
		// 	var len_out = (int)(to.Output - from.Output);
		// 	var fileBuffer = new byte[len_in];
		// 	CompressedFileStream.Position = from.Input - 1;
		// 	CompressedFileStream.ReadExactly(fileBuffer, 0, (int)len_in);
		// 	var outBuf = new byte[len_out];
		// 	Core.ExtractDeflateIndex(fileBuffer, from, to, outBuf);
		// 	// Console.WriteLine(x);
		// 	// file.Write(outBuf);
		// }
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
		// File.Delete("./tmp/" + GzipPath + "file");
		// file?.Dispose();
	}

}




[MemoryDiagnoser]
[MinIterationCount(3)]
[MaxIterationCount(4)]
[MinWarmupCount(3)]
[MaxWarmupCount(4)]
[SimpleJob(RuntimeMoniker.NativeAot80)] 
public class ParallelBenchmark
{
	// [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
	// public int Number;

	[Params(
		// smaller files
		// "48000.gz"
		// "96000.gz"
		// "192000.gz"
		// "384000.gz",
		// "1536000.gz",
		// "6144000.gz"


		// "48000",
		// "96000",
		// "192000",
		// "384000",
		// "768000",
		// "1536000",
		// "3072000"



		"6144000",
		"12288000",
		"24576000",
		"49152000",
		"98304000",
		"196608000"
	)]

	public string GzipPath;
	private FileStream? CompressedFileStream;
	private ParallelParsing.ZRan.NET.Index index;
	private BatchedFASTQ records;

	[IterationSetup]
	public void Setup()
	{		
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath + ".gz");
		index = IndexIO.Deserialize("./Samples/index/" + GzipPath + "_10k" + ".gzi"); // change this!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		records = new BatchedFASTQ(index, "./Samples/" + GzipPath + ".gz", enableSsdOptimization: false);
	}

	[Benchmark]
	public void RunCount()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		Console.WriteLine(records.Count());

		// long recordCount = records.Count();
		// var count = records.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A'));
	}

	// [Benchmark]
	// public void RunPattern()
	// {
	// 	if (CompressedFileStream == null) throw new NullReferenceException();

	// 	string pattern = "GTTATACACTGC";
	// 	var count = 0;
	// 	foreach (var record in records)
	// 	{
	// 		if (record.Sequence.Contains(pattern)) count++;
	// 	}
	// 	Console.WriteLine(count);
	// }

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
		records.Dispose();
		// File.Delete("./tmp/" + GzipPath + "file");
		// file?.Dispose();
	}

}