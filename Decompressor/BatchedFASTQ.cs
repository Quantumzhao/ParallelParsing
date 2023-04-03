
using System.Collections;

namespace ParallelParsing;

class BatchedFASTQ : IReadOnlyList<FASTQRecord>
{
	public FASTQRecord this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public int Count => throw new NotImplementedException();

	public IEnumerator<FASTQRecord> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}
}