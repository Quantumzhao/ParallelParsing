
using System;
using System.IO;

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
	internal point* PtrPoint;
	public ulong Output => PtrPoint->@out;
	public ulong Input => PtrPoint->@in;
	public uint Bits => PtrPoint->bits;
	public ulong WindowBeginning => PtrPoint->window_beginning;
	public uint WindowSize => PtrPoint->window_size;
	public FixedArray<byte> Window { get; init; }
	public ulong LineNumber => PtrPoint->line_number;

	internal Point(point* ptr)
	{
		PtrPoint = ptr;
		Window = new FixedArray<byte>(PtrPoint->window, (uint)PtrPoint->window_beginning);
	}

	public static unsafe implicit operator Point(point* p) => new Point(p);
}

public unsafe class Index
{
	internal access* Access;
	public ulong FileSize => Access->file_size;
	public bool IsIndexComplete => Access->index_complete == 1;
	public int IndexVersion => Access->index_version;
	public int LineNumberFormat => Access->index_version;
	public ulong NumberOfLines => Access->number_of_lines;
	public string FileName => Marshal.PtrToStringAnsi(Access->file_name) ?? string.Empty;
	public PointList List;

	public Index(access* ptr)
	{
		Access = ptr;
		List = new PointList(Access->list, Access->size);
	}

	public unsafe class PointList
	{
		internal point* PtrPoint;
		public ulong Length;

		public PointList(point* ptr, ulong length)
		{
			PtrPoint = ptr;
			Length = length;
		}

		public Point this[ulong index]
		{
			get => PtrPoint + index;
			set => *(PtrPoint + index) = *value.PtrPoint;
		}
	}
}

