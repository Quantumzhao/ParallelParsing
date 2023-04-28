
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;

namespace ParallelParsing;

class BatchedFASTQ : IEnumerable<FASTQRecord>, IDisposable
{

	public BatchedFASTQ(string indexPath, string gzipPath, bool enableSsdOptimization)
	{
		var index = IndexIO.Deserialize(indexPath);
		_Enumerator = new Enumerator(index, gzipPath, enableSsdOptimization);
	}

	private Enumerator _Enumerator;

	public IEnumerator<FASTQRecord> GetEnumerator() => _Enumerator;
	IEnumerator IEnumerable.GetEnumerator() => _Enumerator;

	public void Dispose()
	{
		_Enumerator.Dispose();
	}

	private class Enumerator : IEnumerator<FASTQRecord>
	{
		public Enumerator(Index index, string gzipPath, bool enableSsdOptimization)
		{
			// _Index = index;
			// _Reader = enableSsdOptimization ?
			// 		new LazyFileReadParallel() :
			// 		new LazyFileReadSequential(_Index, gzipPath);
			BufferPool = ArrayPool<byte>.Create(int.MaxValue, 1024);
		}
		// ~ 500 MB to 1 GB
		public const int RECORD_CACHE_MAX_LENGTH = 1000000;
		// = 1 GB
		public const int READ_BUFFER_SIZE_MAX_BYTES = 1 << 30;
		public ArrayPool<byte> BufferPool;
		public ConcurrentQueue<(Point, int, byte[])> ReadBuffers => _Reader.OutputQueue;
		private int _CumulativeBufferSize;
		public ConcurrentQueue<FASTQRecord> Cache;
		private LazyFileReader _Reader;
		private Index _Index;
		private int _CacheIndex = -1;
		private FASTQRecord _Current;
		public FASTQRecord Current => _Current;
		object IEnumerator.Current => this.Current;

		public void Dispose()
		{
			_Reader.Dispose();
		}

		public bool MoveNext()
		{
			if (_CumulativeBufferSize <= READ_BUFFER_SIZE_MAX_BYTES) 
				_Reader.TryReadMore(READ_BUFFER_SIZE_MAX_BYTES);
			
			if (Cache.Count < RECORD_CACHE_LENGTH) Decompressor.Try

			if (Cache.TryDequeue(out var res))
			{
				_Current = res;
				return true;
			}



			return true;
		}

		void IEnumerator.Reset() => throw new NotSupportedException();
	}
}