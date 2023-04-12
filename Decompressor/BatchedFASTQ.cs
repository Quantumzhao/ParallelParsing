
using System.Collections;

namespace ParallelParsing;

class BatchedFASTQ : IEnumerable<FASTQRecord>
{
	public BatchedFASTQ()
	{
		_Enumerator = new Enumerator();
	}

	private Enumerator _Enumerator;

	public IEnumerator<FASTQRecord> GetEnumerator() => _Enumerator;
	IEnumerator IEnumerable.GetEnumerator() => _Enumerator;

	private class Enumerator : IEnumerator<FASTQRecord>
	{
		public FASTQRecord Current => throw new NotImplementedException();
		object IEnumerator.Current => this.Current;

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public bool MoveNext()
		{
			throw new NotImplementedException();
		}

		void IEnumerator.Reset() => throw new NotSupportedException();
	}
}