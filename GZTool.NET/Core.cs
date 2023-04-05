
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ParallelParsing.GZTool.NET;

public static class Core
{

	// public static EXIT_RETURNED_VALUES CreateIndex(
	// 	string fileName, 
	// 	Index index, 
	// 	string indexFileName, 
	// 	ActionOptions opts)
	// {
	// 	FileStream fileIn;
	// 	FileStream fileOut;

	// 	ulong numberOfIndexPoints = 0;
	// 	bool waiting = false;

	// 	// First of all, check that data output and index output do not collide:
	// 	if (string.IsNullOrEmpty(fileName) && 
	// 		string.IsNullOrEmpty(indexFileName) &&
	// 		(opts.IndexAndExtractionOptions == IndexAndExtractionOptions.ExtractFromByte ||
	// 		opts.IndexAndExtractionOptions == IndexAndExtractionOptions.ExtractFromLine))
	// 	{
	// 		throw new GenericException(
	// 			"ERROR: Please, note that extracted data will be output to STDOUT\n" +
	// 			"       so an index file name is needed (`-I`).\nAborted.\n");
	// 	}

	// 	throw new NotImplementedException();
	// }
}

public record struct ActionOptions(
	IndexAndExtractionOptions IndexAndExtractionOptions,
	ulong Offset,
	ulong LineNumberOffset,
	ulong SpanBetween_Points,
	bool DoesEndOnFirstProperGzipEof,
	bool IsAlwaysCreateACompleteIndex,
	int WaitingTime,
	bool DoesWaitForFileCreation,
	bool DoesExtendIndexWithLines,
	ulong ExpectedFirstByte,
	bool GzipStreamMayBeDamaged,
	bool IsLazyGzipStreamPatchingAtEof,
	ulong RangeNumberOfBytes,
	ulong RangeNumberOfLines
);

public unsafe class Point
{
	// internal point* PtrPoint;
	// public ulong Output => PtrPoint->@out;
	// public ulong Input => PtrPoint->@in;
	// public uint Bits => PtrPoint->bits;
	//// public ulong WindowBeginning => PtrPoint->window_beginning;
	//// public uint WindowSize => PtrPoint->window_size;
	// public FixedArray<byte> Window { get; init; }
	//// public ulong LineNumber => PtrPoint->line_number;

	// internal Point(point* ptr)
	// {
	// 	PtrPoint = ptr;
	// 	Window = new FixedArray<byte>(PtrPoint->window, Constants.WINSIZE);
	// }

	// public static unsafe implicit operator Point(point* p) => new Point(p);
	public long Output;
	public long Input;
	public int Bits;
	public byte[] Window = new byte[Constants.WINSIZE];
}

public unsafe class Index
{
	// internal index* Access;
	//// public ulong FileSize => Access->file_size;
	//// public bool IsIndexComplete => Access->index_complete == 1;
	//// public int IndexVersion => Access->index_version;
	//// public int LineNumberFormat => Access->index_version;
	//// public ulong NumberOfLines => Access->number_of_lines;
	//// public string FileName => Marshal.PtrToStringAnsi(Access->file_name) ?? string.Empty;
	// public PointList List;

	// public Index(index* ptr)
	// {
	// 	Access = ptr;
	// 	List = new PointList(Access->list, Access->have);
	// }

	// public unsafe class PointList
	// {
	// 	internal point* PtrPoint;
	// 	public int Length;

	// 	public PointList(point* ptr, int length)
	// 	{
	// 		PtrPoint = ptr;
	// 		Length = length;
	// 	}

	// 	public Point this[int index]
	// 	{
	// 		get => PtrPoint + index;
	// 		set => *(PtrPoint + index) = *value.PtrPoint;
	// 	}
	// }
	public List<Point> List = new List<Point>();
}

public static unsafe class Defined
{
	public static unsafe ZResult InflateInit2(ZStream strm, int windowBits)
	{
		var version = Marshal.StringToHGlobalAnsi(Constants.ZLIB_VERSION);
		return (ZResult)ExternalCalls.inflateInit2_(strm, windowBits, version, sizeof(z_stream));
	}


	public static Index AddPoint(Index index, int bits, long input, long output, int left, char[] window)
	{
		Point next = new Point();

		next.Bits = bits;
		next.Input = input;
		next.Output = output;

		if (left != 0)
		{
			// ???
		}

		if (left < Constants.WINSIZE)
		{
			// ???
		}

		index.List.Add(next);

		return index;
	}

	public ZResult BuildDeflateIndex(FileStream @in, long span, out Index? built)
	{
		ZResult ret;
		// our own total counters to avoid 4GB limit
		long totin, totout;
		// totout value of last access point
		long last;
		// access points being generated
		Index index = new Index();
		ZStream strm = new ZStream();
		byte[] input = new byte[Constants.CHUNK];
		byte[] window = new byte[Constants.WINSIZE];

		ret = InflateInit2(strm, 47);
		if (ret != ZResult.OK)
		{
			built = null;
			return ret;
		}

		totin = totout = last = 0;
		do
		{
			
		} while (true);
	}
}
