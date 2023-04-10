
using System.Text;
using ParallelParsing.ZRan.NET;

namespace ParallelParsing;

public sealed class Decompressor
{
	public static IEnumerable<FASTQRecord> DecompressAll(string indexPath, string gzipPath, 
		bool enableSsdOptimization = false)
	{
		var index = IndexIO.DeSerialize(indexPath);
		var reader = new LazyFileReadSequential(index, gzipPath);
		var allRecords = new List<FASTQRecord>();
		var res = Parallel.ForEach(reader, outputBytes => {
			var parsed = FASTQRecord.Parse(Encoding.ASCII.GetString(outputBytes));
			allRecords.AddRange(parsed);
		});

		return allRecords;
	}
}