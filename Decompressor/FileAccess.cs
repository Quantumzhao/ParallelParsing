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
		private byte[]? _Buffer;
		public byte[] Current
		{
			get
			{
				if (_Buffer == null) throw new InvalidOperationException();
				else return _Buffer;
			}
		}

		object IEnumerator.Current => this.Current;

		public FileReader(Index index, FileStream file)
		{
			_Index = index;
			_File = file;
			_BinReader = new BinaryReader(file);
			_ListEnumerator = index.List.GetEnumerator();
			_CurrPoint = new Point(0, 0, 0);
		}

		private readonly Index _Index;
		private readonly FileStream _File;
		private readonly BinaryReader _BinReader;
		private readonly IEnumerator<Point> _ListEnumerator;
		private readonly Point _CurrPoint;


		public void Dispose()
		{
			_BinReader.Dispose();
			_File.Dispose();
			_ListEnumerator.Dispose();
		}

		public bool MoveNext()
		{
			var from = (int)(_ListEnumerator.Current?.Output ?? 0);
			
			var ret = _ListEnumerator.MoveNext();
			var to = (int)(_ListEnumerator.Current?.Output ?? _File.Length);
			if (!ret) return false;

			// ? might be problematic once file size > 2GB
			var len = to - from;
			_Buffer = new byte[len];
			_File.ReadExactly(_Buffer, from, len);
			return true;
		}

		void IEnumerator.Reset() => new NotSupportedException();
	}
}
