
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.Collections.Concurrent;
using System.Buffers;
using System.Diagnostics;
using PrioritySchedulingTools;

namespace ParallelParsing;

public sealed class LazyFileReader : IDisposable
{
	public const int FILE_THREADS_COUNT_SSD = 8;
	public const int FILE_THREADS_COUNT_HDD = 1;
	
	public readonly ConcurrentQueue<(Point from, Point to, Memory<byte> segment, IMemoryOwner<byte>)> PartitionQueue;
	private Index _Index;
	// private IEnumerator<Point> _IndexEnumerator;
	private Stream[] _FileReads;
	private ArrayPool<byte> _BufferPool;
	private bool _IsEOF = false;
	private int _CurrPoint = 0;
	// private bool _CanGetNewPartition = true;
	// byte[] buf;

	public LazyFileReader(Index index, string path, ArrayPool<byte> pool, bool enableSsdOptimization)
	{
		_Index = index;
		PartitionQueue = new();
		_BufferPool = pool;
		// _IndexEnumerator = _Index.List.GetEnumerator();
		// buf = File.ReadAllBytes(path);
		

		_FileReads = enableSsdOptimization ?
					   new Stream[FILE_THREADS_COUNT_SSD] :
					   new Stream[FILE_THREADS_COUNT_HDD];
		for (int i = 0; i < _FileReads.Length; i++)
		{
			_FileReads[i] = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			// _FileReads[i] = new MemoryStream(buf);
		}
	}

	public void Dispose()
	{
		Parallel.ForEach(_FileReads, f => f.Dispose());
	}

	private int TryReadMore()
	{
		Parallel.ForEach(_FileReads, fs => {
			if (_IsEOF) return;

			Point from;
			Point to;
			Memory<byte> buf;
			IMemoryOwner<byte> bufOwner;
			int len;
			lock (this)
			{
				from = _Index[_CurrPoint];

				_CurrPoint++;
				if (_CurrPoint < _Index.Count) to = _Index[_CurrPoint];
				else
				{
					_IsEOF = true;
					return;
				}

				len = (int)(to.Input - from.Input + 1);
			}
			bufOwner = MemoryPool<byte>.Shared.Rent(len);
			buf = bufOwner.Memory.Slice(0, len);
			
			fs.Position = from.Input - 1;
			fs.Read(buf.Span);
			PartitionQueue.Enqueue((from, to, buf, bufOwner));
		});

		
		return 0;
	}

	public bool TryGetNewPartition(out (Point from, Point to, Memory<byte> segment, IMemoryOwner<byte>) entry)
	{
		if (_IsEOF && PartitionQueue.Count == 0)
		{
			entry = default;
			return false;
		}

		Task? readBytes = null;
		if (!_IsEOF && PartitionQueue.Count <= 32) readBytes = BatchedFASTQ.Scheduler.Run<int>(0, 0, TryReadMore);

		if (PartitionQueue.TryDequeue(out entry))
		{
			return true;
		}
		else
		{
			// var sw = new Stopwatch();
			// sw.Start();
			// Console.WriteLine("here");
			readBytes?.Wait();
			// sw.Stop();
			// Console.WriteLine(sw.ElapsedMilliseconds);
			// if (_IsEOF && PartitionQueue.Count == 0) Console.WriteLine("here");
			return PartitionQueue.TryDequeue(out entry);
			// int prevCount = PartitionQueue.Count;
		}
	}
}
