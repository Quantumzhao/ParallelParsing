
using System.IO.Compression;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ParallelParsing;

[SimpleJob(RuntimeMoniker.NativeAot70)]
public class SimpleDecompressor
{
	// [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
	// public int Number;

	private FileStream? CompressedFileStream;

	[IterationSetup]
	public void Setup()
	{
		CompressedFileStream = File.OpenRead("./Samples/SRR24554569_575.fastq.gz");
	}

	[Benchmark]
	public void Run()
	{
		if (CompressedFileStream == null) throw new NullReferenceException();
		foreach (var rs in DecompressFile(CompressedFileStream))
		{
			FASTQRecord.Parse(rs);
		}
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
	}

	private static IEnumerable<byte[]> DecompressFile(FileStream compressedFileStream)
    {
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
		var ret = new List<byte[]>();
		var res = 0;
		while (res != 0 && res < int.MaxValue)
		{
			var buffer = new byte[int.MaxValue];
			res = decompressor.Read(buffer, 0, int.MaxValue);
			yield return buffer;
		}
    }

}