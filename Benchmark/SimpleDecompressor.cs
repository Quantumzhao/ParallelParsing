
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
		FASTQRecord.Parse(DecompressFile(CompressedFileStream));
	}

	[IterationCleanup]
	public void Cleanup()
	{
		CompressedFileStream?.Dispose();
	}

	private static byte[] DecompressFile(FileStream compressedFileStream)
    {
        using var memoryStream = new MemoryStream();
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
        decompressor.CopyTo(memoryStream);

        return memoryStream.GetBuffer();
    }

}