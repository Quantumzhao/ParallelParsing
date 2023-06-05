
#pragma warning disable CS8618
#pragma warning disable CS8604

using System.IO.Compression;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ParallelParsing;
using ParallelParsing.Benchmark.Naive;
using ParallelParsing.ZRan.NET;

[SimpleJob(RuntimeMoniker.NativeAot80)] [MemoryDiagnoser]
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
	private FileStream? file;
	private ParallelParsing.ZRan.NET.Index index;
	private BatchedFASTQ records;

	[IterationSetup]
	public void Setup()
	{		
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath + ".gz");
		index = IndexIO.Deserialize("./Samples/index/" + GzipPath + "_10k" + ".gzi");
		records = new BatchedFASTQ(index, "./Samples/" + GzipPath + ".gz", enableSsdOptimization: false);

		// file = File.Create("./tmp/" + GzipPath + "file");
		// Console.WriteLine("index built---------------------------------------");
	}

	[Benchmark]
	public void Run()
	{
		// if (CompressedFileStream == null) throw new NullReferenceException();
		// Console.WriteLine(Core.BuildDeflateIndex(CompressedFileStream, 10000).Count);  // 10k 20k 50k 100k 200k 1M
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



		Console.WriteLine(records.Count());
		// long recordCount = records.Count();
		// var count = records.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A'));
		// string pattern = "GTTATACACTGC";
		// var count = 0;
		// foreach (var record in records)
		// {
		// 	if (record.Sequence.Contains(pattern)) count++;
		// }
		// Console.WriteLine(count);
	}


	//parallel records count, count pattern occurance 

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
		// File.Delete("./tmp/" + GzipPath + "file");
		// file?.Dispose();
	}

}