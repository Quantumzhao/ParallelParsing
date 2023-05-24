using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.Collections.Concurrent;
using System.Buffers;
using System.Diagnostics;

namespace ParallelParsing;

public class LazyFileReader : IDisposable
{
	public const int FILE_THREADS_COUNT_SSD = 2;
	public const int FILE_THREADS_COUNT_HDD = 1;
	
	public readonly ConcurrentQueue<(Point from, Point to, byte[] segment)> PartitionQueue;
	private Index _Index;
	// private IEnumerator<Point> _IndexEnumerator;
	private FileStream[] _FileReads;
	private ArrayPool<byte> _BufferPool;
	private bool _IsEOF = false;
	private int _CurrPoint = -1;
	// private bool _CanGetNewPartition = true;

	public LazyFileReader(Index index, string path, ArrayPool<byte> pool, bool enableSsdOptimization)
	{
		_Index = index;
		PartitionQueue = new();
		_BufferPool = pool;
		// _IndexEnumerator = _Index.List.GetEnumerator();

		_FileReads = enableSsdOptimization ?
					   new FileStream[FILE_THREADS_COUNT_SSD] :
					   new FileStream[FILE_THREADS_COUNT_HDD];
		for (int i = 0; i < _FileReads.Length; i++)
		{
			_FileReads[i] = File.OpenRead(path);
		}
	}

	public void Dispose()
	{
		Parallel.ForEach(_FileReads, f => f.Dispose());
	}

	private void TryReadMore()
	{
		lock (this)
		{
			if (_IsEOF) return;

			var fs = _FileReads[0];
			Point from;
			Point to;
			// bool res;
			byte[] buf;
			int len;
			lock (this)
			{
				if (_CurrPoint == -1) from = new Point(0, 0, 0);
				else from = _Index.List[_CurrPoint];

				_CurrPoint++;
				if (_CurrPoint < _Index.List.Count) to = _Index.List[_CurrPoint];
				else
				{
					_IsEOF = true;
					to = new Point(0, fs.Length, 0);
				}

				len = (int)(to.Input - from.Input);
			}
			buf = _BufferPool.Rent(_Index.ChunkMaxBytes);
			
			fs.Position = from.Input;
			fs.ReadExactly(buf, 0, len);
			lock (PartitionQueue)
			{
				PartitionQueue.Enqueue((from, to, buf));
			}
		}
	}

	public bool TryGetNewPartition(out (Point from, Point to, byte[] segment) entry)
	{
		if (_IsEOF && PartitionQueue.Count == 0)
		{
			entry = default;
			return false;
		}

		Task? readBytes = null;
		if (!_IsEOF && PartitionQueue.Count <= 2) readBytes = Task.Run(TryReadMore);

		if (PartitionQueue.TryDequeue(out entry))
		{
			return true;
		}
		else
		{
			readBytes?.Wait();
			// if (_IsEOF && PartitionQueue.Count == 0) Console.WriteLine("here");
			return PartitionQueue.TryDequeue(out entry);
			// int prevCount = PartitionQueue.Count;

			// if (readBytes.Status == TaskStatus.RanToCompletion && PartitionQueue.Count == prevCount)
			// {
			// 	return false;
			// }
			// else
			// {
			// 	// Console.WriteLine("wait read");
			// 	readBytes.Wait();
			// 	// if (PartitionQueue.Count == 0) Console.WriteLine("here");
			// 	_CanGetNewPartition = PartitionQueue.TryDequeue(out entry);
			// 	return _CanGetNewPartition;
			// }
		}
	}
}
