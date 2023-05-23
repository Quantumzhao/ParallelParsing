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
	
	public readonly Queue<(Point from, Point to, byte[] segment)> PartitionQueue;
	private Index _Index;
	private IEnumerator<Point> _IndexEnumerator;
	private FileStream[] _FileReads;
	private ArrayPool<byte> _BufferPool;
	private bool _IsEOF = false;
	private bool _CanGetNewPartition = true;

	public LazyFileReader(Index index, string path, ArrayPool<byte> pool, bool enableSsdOptimization)
	{
		_Index = index;
		PartitionQueue = new();
		_BufferPool = pool;
		_IndexEnumerator = _Index.List.GetEnumerator();

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
			var fs = _FileReads[0];
			Point? from;
			Point? to;
			bool res;
			byte[] buf;
			int len;
			lock (_IndexEnumerator)
			{
				from = _IndexEnumerator.Current ?? new Point(0, 0, 0);
				res = _IndexEnumerator.MoveNext();		
				if (res) to = _IndexEnumerator.Current;
				else if (!_IsEOF)
				{
					_IsEOF = true;
					to = new Point(0, fs.Length, 0);
				}
				else return;

				if (to == null) return;

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
		// if (!_CanGetNewPartition)
		// {
		// 	entry = default;
		// 	return false;
		// }
		// if (PartitionQueue.Count <= 1) Task.Run(TryReadMore);

		if (PartitionQueue.TryDequeue(out entry))
		{
			return true;
		}
		else
		{
			int prevCount = PartitionQueue.Count;
			var readBytes = Task.Run(TryReadMore);

			if (readBytes.Status == TaskStatus.RanToCompletion && PartitionQueue.Count == prevCount)
			{
				return false;
			}
			else
			{
				// Console.WriteLine("wait read");
				readBytes.Wait();
				// if (PartitionQueue.Count == 0) Console.WriteLine("here");
				_CanGetNewPartition = PartitionQueue.TryDequeue(out entry);
				return _CanGetNewPartition;
			}
		}
	}
}
