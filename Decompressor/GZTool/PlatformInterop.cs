
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace ParallelParsing.GZTool;

public static class ExternalCalls
{
	[DllImport("libz.so")]
	public static unsafe extern int deflateEnd(z_stream* strm);

	[DllImport("libz.so")]
	public static unsafe extern int deflate(z_stream* strm, int flush);

	[DllImport("libz.so")]
	public static unsafe extern int deflateInit_(
		z_stream* strm, int level, char* version, int stream_size);

	[DllImport("libz.so")]
	public static unsafe extern int deflateInit2_(
		z_stream* strm, 
		int level, int method, 
		int windowBits, 
		int memLevel, 
		int strategy, 
		char* version, 
		int stream_size
	);

	[DllImport("libz.so")]
	public static unsafe extern int inflate(z_stream* strm, int flush);

	[DllImport("libz.so")]
	public static unsafe extern int inflateEnd(z_stream* strm, int flush);

	[DllImport("libz.so")]
	public static unsafe extern int inflatePrime(z_stream* strm, int bits, int value);

	[DllImport("libz.so")]
	public static unsafe extern int inflateSetDictionary(
		z_stream* strm, byte* dictionary, uint dictLength);

	[DllImport("libz.so")]
	public static unsafe extern int inflateCopy(z_stream* dest, z_stream* source);

	[DllImport("libz.so")]
	public static unsafe extern int inflateInit_(
		z_stream* strm, char* version, int stream_size);

	[DllImport("libz.so")]
	public static unsafe extern int inflateInit2_(
		z_stream* strm, int windowBits, char* version, int stream_size);
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct z_stream
{
	public byte* next_int;
	public uint avail_in;
	public ulong total_in;
	public byte* next_out;
	public uint avail_out;
	public ulong total_out;
	public SChar* msg;
	public void* state;
	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
	public delegate* unmanaged [Cdecl]<void*, uint, uint, void*> zalloc;
	public delegate* unmanaged [Cdecl]<void*, void*, void> zfree;
	public void* opaque;
	public int data_type;
	public ulong adler;
	public ulong reserved;
}

public static class Defined
{
	private const string ZLIB_VERSION = "1.2.11";
	
	public static unsafe int deflateInit(z_stream* strm, int level)
	{
		fixed (char* s = ZLIB_VERSION)
		{
			return ExternalCalls.deflateInit_(strm, level, s, (int)sizeof(z_stream));
		}
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct SChar
{
	public SChar(char c) => _Value = (byte)((short)c >> 8);

	private readonly byte _Value;

	public static explicit operator SChar(char c) => new SChar(c);
}

public class UnmanagedString
{

}
