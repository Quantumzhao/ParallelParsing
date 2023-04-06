using System.IO;
using System.Globalization;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ParallelParsing.ZRan.NET;

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

public enum ZFlush
{
	NO_FLUSH,
	PARTIAL_FLUSH,
	SYNC_FLUSH,
	FULL_FLUSH,
	FINISH,
	BLOCK,
	TREES
}

public enum SeekOpt
{
	SET,
	CUR,
	END
}

public struct ZReturn
{
	ZResult Res;
	int Val;

	public static implicit operator ZReturn(int val) => new ZReturn { Val = val };
	public static implicit operator ZReturn(ZResult res) => new ZReturn { Res = res };
	public static implicit operator ZResult(ZReturn ret) => ret.Res;
	public int ToInt() => Val;
}

public static class Constants
{
	public const string ZLIB_VERSION = "1.2.11";

	// desired distance between access points
	public const long SPAN = 10485760L;

	// sliding window size
	public const uint WINSIZE = 32768U;

	// file input buffer size
	public const uint CHUNK = 16384;

	public const int EOF = -1;
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