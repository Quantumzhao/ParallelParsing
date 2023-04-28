
using System.Text;
using ParallelParsing.ZRan.NET;
using System.Threading;

namespace ParallelParsing;

public sealed class Decompressor
{
	public static IEnumerable<FASTQRecord> DecompressAll(string indexPath, string gzipPath, 
		bool enableSsdOptimization = false)
	{
		// var index = IndexIO.Deserialize(indexPath);
		// LazyFileRead reader = enableSsdOptimization ?
		// 					  new LazyFileReadParallel() :
		// 					  new LazyFileReadSequential(index, gzipPath);
		var allRecords = new List<FASTQRecord>();

		// assume the output bytes are exactly aligned, no more, no less
		// Parallel.ForEach(reader, outputBytes => {
		// 	var parsed = FASTQRecord.Parse(new Queue<char>(Encoding.ASCII.GetChars(outputBytes)));
		// 	allRecords.AddRange(parsed);
		// });
		return allRecords;
	}

	public static IEnumerable<FASTQRecord> DecompressRange(string indexPath, string gzipPath, 
		Point fromPoint, Point toPoint, bool enableSsdOptimization)
	{
		// var index = IndexIO.Deserialize(indexPath);
		// var reader = new LazyFileReadSequential(index, gzipPath);
		
		throw new NotImplementedException();
	}
}