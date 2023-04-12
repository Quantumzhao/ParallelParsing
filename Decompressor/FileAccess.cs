using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;

namespace ParallelParsing;

public static class FileAccess
{

}

public sealed class LazyFileReadParallel : IEnumerable<byte[]>
{
	public IEnumerator<byte[]> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}
}

public sealed class LazyFileReadSequential : IEnumerable<byte[]>
{
	private readonly FileReader _Enumerator;

	public LazyFileReadSequential(Index index, string path)
	{
		var file = File.OpenRead(path);
		_Enumerator = new FileReader(index, file);
	}

	public IEnumerator<byte[]> GetEnumerator() => _Enumerator;

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	private sealed class FileReader : IEnumerator<byte[]>
	{
		public byte[] Current => throw new NotImplementedException();

		object IEnumerator.Current => throw new NotImplementedException();

		public FileReader(Index index, FileStream file)
		{
			_Index = index;
			_File = file;
			_BinReader = new BinaryReader(file);
			_ListEnumerator = index.List.GetEnumerator();
		}

		private readonly Index _Index;
		private readonly FileStream _File;
		private readonly BinaryReader _BinReader;
		private readonly IEnumerator<Point> _ListEnumerator;


		public void Dispose()
		{
			_BinReader.Dispose();
			_File.Dispose();
			_ListEnumerator.Dispose();
		}

		public bool MoveNext()
		{
			return _ListEnumerator.MoveNext();
		}

		void IEnumerator.Reset() => new NotSupportedException();
	}
}
