
using System.IO.Compression;
using ParallelParsing.Common;

namespace ParallelParsing.Benchmark.Naive;

public static class SimpleDecompressor
{
	public static IEnumerable<FastqRecord> GetAllRecords(FileStream file)
	{
		var bytes = DecompressFile(file);
		return Parsing.Parse(new BigQueue<byte>(bytes));
	}

	private static IEnumerable<byte[]> DecompressFile(FileStream compressedFileStream)
    {
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
		var res = 0;
		const int len = 65536;
		do
		{
			var buffer = new byte[len];
			res = decompressor.Read(buffer, 0, len);
			yield return buffer;
		}
		while (res > 0);

		Console.WriteLine("end");
    }
}