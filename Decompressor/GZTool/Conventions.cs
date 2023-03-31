
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
	char[] Window
);

public record class Index(
	ulong Have, 
	ulong Size,
	ulong FileSize,
	Point[] List,
	string FileName,
	bool IsIndexComplete
	/* index_version should be 0 (default), thus omitted */
	/* line_number_format should be irrelevant if index contains no info about line number */
	/* number_of_lines omitted, for the same reason */
);

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

public static class Constants
{
	public const int MAX_GIVE_ME_SI_UNIT_RETURNED_LENGTH = 14;
	[Obsolete]
	public static char[] number_output = new char[MAX_GIVE_ME_SI_UNIT_RETURNED_LENGTH]; 
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