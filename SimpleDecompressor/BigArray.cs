
namespace ParallelParsing.Benchmark.Naive;

public class BigQueue<T> where T : struct
{
	public BigQueue(IEnumerable<T[]> ts)
	{
		_Enumerator = ts.GetEnumerator();
		_Enumerator.MoveNext();
		_CurrentArray = _Enumerator.Current;
	}
	private IEnumerator<T[]> _Enumerator;
	private T[] _CurrentArray;
	private int _CurrentIndex = 0;

	public bool TryDequeue(out T t)
	{
		if (_CurrentIndex < _CurrentArray.Length)
		{
			t = _CurrentArray[_CurrentIndex];
			_CurrentIndex++;
			return true;
		}

		if (_Enumerator.MoveNext())
		{
			_CurrentArray = _Enumerator.Current;
			_CurrentIndex = 0;
			t = _CurrentArray[_CurrentIndex];
			return true;
		}

		t = default;
		return false;
	}

	public T Peek() => _CurrentArray[_CurrentIndex];

	public bool IsAtEnd => _CurrentIndex == _CurrentArray.Length;

	public T Dequeue()
	{
		var res = TryDequeue(out var t);
		if (!res) throw new Exception();
		else return t;
	}
}