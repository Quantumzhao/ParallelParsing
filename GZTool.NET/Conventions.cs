
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ParallelParsing.GZTool.NET;

// [StructLayout(LayoutKind.Sequential)]
// public unsafe struct Point
// {
// 	// corresponding offset in uncompressed data
// 	internal UInt64 @out;
// 	// offset in input file of first full byte
// 	internal UInt64 @in;
// 	// number of bits (1-7) from byte at in - 1, or 0
// 	internal UInt32 bits;
// 	// offset at index file where this compressed window is stored
// 	internal UInt64 window_beginning;
// 	// size of (compressed) window
// 	internal UInt32 window_size;
// 	// preceding 32K of uncompressed data, compressed
// 	internal Byte* window;
// 	internal UInt64 line_number;


// 	public Point()
// 	{
// 		Window = new FixedArray<byte>(window, window_size);
// 	}
// }

// public unsafe struct Index
// {
// 	// number of list entries filled in
// 	internal UInt64 have;
// 	// number of list entries allocated
// 	internal UInt64 size;
// 	// size of uncompressed file (useful for bgzip files)
// 	internal UInt64 file_size;
// 	// allocated list
// 	internal Point* list;
// 	// path to index file
// 	[MarshalAs(UnmanagedType.LPStr)]
// 	internal char* file_name;
// 	// 1: index is complete; 0: index is (still) incomplete
// 	internal int index_complete;
// 	// 0: default; 1: index with line numbers
// 	internal int index_version;
// 	// 0: linux \r | windows \n\r; 1: mac \n
// 	internal UInt32 line_number_format;
// 	// number of lines (only used with v1 index format)
// 	internal UInt64 number_of_lines;

// 	public ulong FileSize;
// 	public FixedArray<Point> List;
// 	public string FileName;
// 	public bool IsIndexComplete;
// 	public int IndexVersion;
// 	public uint LineNumberFormat;
// 	public ulong NumberOfLines;
// }

// public unsafe struct access
// {
// }


// 	/* index_version should be 0 (default), thus omitted */
// 	/* line_number_format should be irrelevant if index contains no info about line number */
// 	/* number_of_lines omitted, for the same reason */

// 	public Index AddPoint(uint bits, ulong input, ulong output, uint left, 
// 		char[] window, uint windowSize, ulong lineNumber, bool isCompressedWindow)
// 	{
// 		// Point next;
// 		// ulong size = windowSize;

// 		// if (isCompressedWindow)
// 		// {
// 		// 	next = new Point {
// 		// 		Output = output,
// 		// 		Input = input,
// 		// 		Bits = bits,

// 		// 	}

// 		// }
// 		// else
// 		// {

// 		// }

// 		// List.Add(next);

// 		return this;
// 	}

// 	public Index()
// 	{
// 		FileSize = 0;
// 		List = new List<Point>(8);
// 		IsIndexComplete = false;
// 		FileName = null;
// 	}

// 	// private unsafe char[]? CompressChunk(char[] source, ulong inputSize, int level)
// 	// {
// 	// 	ZSignal flush;
// 	// 	ZResult ret;
// 	// 	uint have;
// 	// 	ulong i = 0;
// 	// 	ulong outputSize = 0;
// 	// 	FixedArray<char> input, output, outComplete;

// 	// 	if (Defined.DeflateInit(out var strm, level) is not ZResult.OK) return null;

// 	// 	input = new FixedArray<char>(Constants.CHUNK);
// 	// 	output = new FixedArray<char>(Constants.CHUNK);
// 	// 	outComplete = new FixedArray<char>(Constants.CHUNK);

// 	// 	do
// 	// 	{
// 	// 		strm = (uint)(i + Constants.CHUNK < inputSize ? 
// 	// 									Constants.CHUNK : 
// 	// 									inputSize - i);

// 	// 		do
// 	// 		{
// 	// 			strm.NextOut = output;
// 	// 		} while (true);
			
// 	// 	} while (flush != ZSignal.FINISH);

// 	// 	if (ret != ZResult.STREAM_END) throw new UnreachableException();

// 	// 	Defined.DeflateEnd(strm);
// 	// 	size = outputSize;
// 	// 	return outComplete;
// 	// }
// }

public enum IndexAndExtractionOptions
{
	JustCreateIndex = 1,
	ExtractFromByte,
	CompressAndCreateIndex,
	[Obsolete]
	// it shouldn't be necessary to use any info about line
	ExtractFromLine
}

public enum Action
{
	NotSet,
	ExtractFromByte,
	CreateIndex,
	[Obsolete]
	ExtractFromLine
}

public enum GzipMarkFoundType
{
	Error = 8,
	FatalError,
	None,
	Beginning,
	FullFlush
}

public enum DecompressInAdvanceInitializers
{
	// initial total reset
	Reset,
	// no reset, just continue processing
	Continue,
	// reset all but last_correct_reentry_point_returned
	// in order to continue processing the same gzip stream.
	SoftReset
}

public enum ZResult
{
	OK = 0,
	STREAM_END = 1,
	NEED_DICT = 2,
	ERRNO = -1,
	STREAM_ERROR = -2,
	DATA_ERROR = -3,
	MEM_ERROR = -4,
	BUF_ERROR = -5,
	VERSION_ERROR = -6
}

public enum ZSignal
{
	NO_FLUSH,
	PARTIAL_FLUSH,
	SYNC_FLUSH,
	FULL_FLUSH,
	FINISH,
	BLOCK,
	TREES
}

public enum EXIT_RETURNED_VALUES
{
	OK = 0,
	GENERIC_ERROR = 1,
	INVALID_OPTION = 2,
	FILE_OVERWRITTEN = 100
}

public static class Constants
{
	public const int MAX_GIVE_ME_SI_UNIT_RETURNED_LENGTH = 14;
	[Obsolete]
	public static char[] number_output = new char[MAX_GIVE_ME_SI_UNIT_RETURNED_LENGTH]; 
	
	// desired distance between access points
	public const long SPAN = 10485760L;

	// sliding window size
	public const uint WINSIZE = 32768U;

	// file input buffer size
	public const uint CHUNK = 16384;

	// window is an uncompressed WINSIZE size window
	public const uint UNCOMPRESSED_WINDOW = uint.MaxValue;

	// default index version (v0)
	public const string GZIP_INDEX_IDENTIFIER_STRING = "gzipindx";

	// index version with line number info
	public const string GZIP_INDEX_IDENTIFIER_STRING_V1 = "gzipindeX";

	// header size in bytes of gztool's .gzi files
	public const int GZIP_INDEX_HEADER_SIZE = 16;

	// header size in bytes of gzip files created by zlib
	// github.com/madler/zlib/blob/master/zlib.h
	public const int GZIP_INDEX_SIZE_BY_ZLIB = 10;

	// If deflateSetHeader is not used, the default gzip header has text false,
	// the time set to zero, and os set to 255, with no extra, name, or comment fields.
	// default waiting time in seconds when supervising a growing gzip file:
	public const int WAITING_TIME = 4;

	// how many CHUNKs will be decompressed in advance if it is needed (parameter gzip_stream_may_be_damaged, `-p`)
	public const int CHUNKS_TO_DECOMPRESS_IN_ADVANCE = 3;

	// how many CHUNKs will be look to backwards for a new good gzip reentry point after an error is found (with `-p`)
	public const int CHUNKS_TO_LOOK_BACKWARDS = 3;
}

[Obsolete]
public class GenericException : Exception 
{ 
	public GenericException(string message) : base(message) { }
}

[Obsolete]
public class InvalidOptionException : Exception { }

[Obsolete]
public class ExitFileOverwrittenException : Exception { }