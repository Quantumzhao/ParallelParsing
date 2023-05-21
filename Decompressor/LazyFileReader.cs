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
	
	public const int READ_BUFFER_SIZE_MAX_BYTES = 1 << 30; // 1 GB

	public readonly ConcurrentQueue<(Point from, Point to, byte[] segment)> PartitionQueue;
	private Index _Index;
	private IEnumerator<Point> _IndexEnumerator;
	private FileStream[] _FileReads;
	private ArrayPool<byte> _BufferPool;
	private int _CumulativeBufferSize;
	private bool _IsEOF = false;

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

	private int TryReadMore(int size)
	{
		Point? from;
		Point? to;
		bool res;
		lock (_IndexEnumerator)
		{
			from = _IndexEnumerator.Current ?? new Point(0, 0, 0);
			res = _IndexEnumerator.MoveNext();		
			if (res) to = _IndexEnumerator.Current;
			else if (!_IsEOF)
			{
				_IsEOF = true;
				to = new Point(0, _FileReads[0].Length, 0);
			}
			else return 0;

			if (to == null) return 0;
		}
		var bytesRead = 0;

		Parallel.ForEach(_FileReads, fs => {
			var buf = _BufferPool.Rent(_Index.ChunkMaxBytes);
			fs.Position = from.Input;
			fs.ReadExactly(buf, 0, (int)(to.Input - from.Input));
			PartitionQueue.Enqueue((from, to, buf));
			bytesRead += buf.Length;
		});

		_CumulativeBufferSize += bytesRead;
		return bytesRead;
	}

	public bool TryGetNewPartition(out (Point from, Point to, byte[] segment) entry)
	{
		Task<int> readBytes;
		int prevSize = _CumulativeBufferSize;
		if (_CumulativeBufferSize <= READ_BUFFER_SIZE_MAX_BYTES)
			readBytes = Task.Run(() => TryReadMore(READ_BUFFER_SIZE_MAX_BYTES));
		else readBytes = Task.Run(() => 0);

		if (PartitionQueue.TryDequeue(out var res))
		{
			_CumulativeBufferSize -= res.segment.Length;
			entry = res;
			return true;
		}
		else
		{
			if (readBytes.Status == TaskStatus.RanToCompletion && _CumulativeBufferSize == prevSize)
			{
				entry = default;
				return false;
			}
			else
			{
				readBytes.Wait();
				if (PartitionQueue.TryDequeue(out res))
				{
					_CumulativeBufferSize -= res.segment.Length;
					entry = res;
					return true;
				}
				else
				{
					entry = default;
					return false;
				}
			}
		}
	}
}

public class LazyFileReaderSequential : IDisposable
{	
	public const int READ_BUFFER_SIZE_MAX_BYTES = 1 << 30; // 1 GB

	public readonly ConcurrentQueue<(Point from, Point to, byte[] segment)> PartitionQueue;
	private Index _Index;
	private IEnumerator<Point> _IndexEnumerator;
	private FileStream _FileRead;
	private ArrayPool<byte> _BufferPool;
	private int _CumulativeBufferSize;
	private bool IsEOF;

	public LazyFileReaderSequential(Index index, string path, ArrayPool<byte> pool, bool enableSsdOptimization)
	{
		_Index = index;
		PartitionQueue = new();
		_BufferPool = pool;
		_IndexEnumerator = _Index.List.GetEnumerator();

		_FileRead = File.OpenRead(path);
	}

	public void Dispose()
	{
		_FileRead.Dispose();
	}

	private int TryReadMore(int size)
	{
		Point? from;
		Point? to;
		bool res;
		lock (_IndexEnumerator)
		{
			from = _IndexEnumerator.Current ?? new Point(0, 0, 0);
			res = _IndexEnumerator.MoveNext();		
			if (res) to = _IndexEnumerator.Current;
			else if (!IsEOF)
			{
				IsEOF = true;
				to = new Point(0, _FileRead.Length, 0);
			}
			else return 0;

			if (to == null) return 0;
		}
		var bytesRead = 0;

		var buf = _BufferPool.Rent(_Index.ChunkMaxBytes);
		_FileRead.Position = from.Input;
		_FileRead.ReadExactly(buf, 0, (int)(to.Input - from.Input));
		PartitionQueue.Enqueue((from, to, buf));
		bytesRead += buf.Length;

		_CumulativeBufferSize += bytesRead;
		return bytesRead;
	}

	public bool TryGetNewPartition(out (Point from, Point to, byte[] segment) entry)
	{
		int prevSize = _CumulativeBufferSize;
		if (_CumulativeBufferSize <= READ_BUFFER_SIZE_MAX_BYTES)
			TryReadMore(READ_BUFFER_SIZE_MAX_BYTES);

		if (PartitionQueue.TryDequeue(out var res))
		{
			_CumulativeBufferSize -= res.segment.Length;
			entry = res;
			return true;
		}
		else
		{
			entry = default;
			return false;
		}
	}
}

