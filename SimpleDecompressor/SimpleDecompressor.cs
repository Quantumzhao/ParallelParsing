
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