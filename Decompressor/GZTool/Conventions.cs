
using System.Diagnostics;

namespace ParallelParsing.GZTool;

public record class Point(
	// corresponding offset in uncompressed data
	ulong Output, 
	// offset in input file of first full byte
	ulong Input, 
	// number of bits (1-7) from byte at in - 1, or 0
	uint Bits, 
	// offset at index file where this compressed window is stored
	ulong WindowBeginning, 
	// size of (compressed) window
	uint WindowSize,
	// preceding 32K of uncompressed data, compressed
	SChar[] Window,
	ulong lineNumber
);

// public class Index
// {
// 	public ulong FileSize;
// 	public List<Point> List;
// 	public string? FileName;
// 	public bool IsIndexComplete;
// 	/* index_version should be 0 (default), thus omitted */
// 	/* line_number_format should be irrelevant if index contains no info about line number */
// 	/* number_of_lines omitted, for the same reason */

// 	public Index AddPoint(uint bits, ulong input, ulong output, uint left, 
// 		ref SChar window, uint windowSize, ulong lineNumber, bool isCompressedWindow)
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

// 	// private unsafe SChar[]? CompressChunk(ref SChar source, ref ulong size, int level)
// 	// {
// 	// 	ZSignal flush;
// 	// 	ZResult ret;
// 	// 	uint have;
// 	// 	ulong i = 0;
// 	// 	ulong outputSize = 0;
// 	// 	ulong inputSize = size;
// 	// 	SChar[] input, output, outComplete;

// 	// 	if (Defined.DeflateInit(out var strm, level) is not ZResult.OK) return null;

// 	// 	input = new SChar[Constants.CHUNK];
// 	// 	output = new SChar[Constants.CHUNK];
// 	// 	outComplete = new SChar[Constants.CHUNK];

// 	// 	do
// 	// 	{
// 	// 		strm.Value.avail_in = (uint)(i + Constants.CHUNK < inputSize ? 
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