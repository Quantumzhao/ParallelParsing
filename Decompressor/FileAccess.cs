
using System.Collections;

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
	private readonly Index _Index;

	public LazyFileReadSequential(Index index)
	{
		_Index = index;
	}

	public IEnumerator<byte[]> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}
}
