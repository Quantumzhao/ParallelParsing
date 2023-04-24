
using System.Collections;
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
			_Index = index;
			_Reader = enableSsdOptimization ?
					new LazyFileReadParallel() :
					new LazyFileReadSequential(_Index, gzipPath);
		}
		private LazyFileRead _Reader;
		private Index _Index;
		private IReadOnlyList<FASTQRecord>? _Cache = null;
		private int _CacheIndex = -1;
		public FASTQRecord Current => throw new NotImplementedException();
		object IEnumerator.Current => this.Current;

		public void Dispose()
		{
			_Reader.Dispose();
		}

		public bool MoveNext()
		{
			if (_Cache?.Count == 0)
			{
				var e = _Reader.GetEnumerator();
				var ret = e.MoveNext();
				if (!ret) return false;

				var buf = new byte[Constants.WINSIZE];
				var currPoint = _Index.List.GetEnumerator().Current;
				Core.ExtractDeflateRange(e.Current, ,buf, );
				var queue = new Queue<char>(Encoding.ASCII.GetChars(buf, 0, buf.Length));
				_Cache = FASTQRecord.Parse(queue);
			}
			else
			{

			}

			return true;
		}

		void IEnumerator.Reset() => throw new NotSupportedException();
	}
}