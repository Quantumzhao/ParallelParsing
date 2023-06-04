
#pragma warning disable CS8618

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
		"48000.gz",
		"96000.gz"
		// "192000.gz",
		// "384000.gz",
		// "1536000.gz",
		// "6144000.gz"




		// "48000.gz",
		// "96000.gz",
		// "192000.gz",
		// "384000.gz",
		// "768000.gz",
		// "1536000.gz",
		// "3072000.gz",
		// "6144000.gz",
		// "12288000.gz",
		// "24576000.gz",
		// "49152000.gz",
		// "98304000.gz",
		// "196608000.gz"
	)]
	public string GzipPath;

	private FileStream? CompressedFileStream;
	private FileStream? file;
	private ParallelParsing.ZRan.NET.Index index;

	[IterationSetup]
	public void Setup()
	{		
		CompressedFileStream = File.OpenRead("./Samples/" + GzipPath);
		index = IndexIO.Deserialize("./Samples/" + GzipPath + "i");
		file = File.Create("./tmp/" + GzipPath + "file");
		// Console.WriteLine("index built---------------------------------------");
	}

	[Benchmark]
	public void Run()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		// Console.WriteLine(Core.BuildDeflateIndex_NEW(CompressedFileStream, 10000).List.Count()); 
		// Console.WriteLine(SimpleDecompressor.GetAllRecords(CompressedFileStream).Count());


		for (int x = 0; x < index.List.Count - 1; x++)
		{
			var from = index.List[x];
			var to = index.List[x + 1];
			var len_in = to.Input - from.Input + 1;
			var len_out = (int)(to.Output - from.Output);
			var fileBuffer = new byte[len_in];
			CompressedFileStream.Position = from.Input - 1;
			CompressedFileStream.ReadExactly(fileBuffer, 0, (int)len_in);
			var outBuf = new byte[len_out];
			Core.ExtractDeflateIndex(fileBuffer, from, to, outBuf);
			// Console.WriteLine(x);
			// file.Write(outBuf);
		}
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
		File.Delete("./tmp/" + GzipPath + "file");
		file?.Dispose();
	}

}