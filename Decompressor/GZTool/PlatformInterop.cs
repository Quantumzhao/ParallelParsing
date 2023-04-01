using System.IO;
using System.Runtime.CompilerServices;
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
	public SChar* next_in;
	public uint avail_in;
	public ulong total_in;
	public SChar* next_out;
	public uint avail_out;
	public ulong total_out;
	public SChar* msg;
	public void* state;
	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
	// unused
	public delegate* unmanaged[Cdecl]<void*, uint, uint, void*> zalloc;
	// unused
	public delegate* unmanaged[Cdecl]<void*, void*, void> zfree;
	// unused
	public void* opaque;
	public int data_type;
	// unused
	public ulong adler;
	public ulong reserved;
}

public class ZStream
{
	public unsafe ZStream(z_stream* strm)
	{
		_Value = *strm;
		NextIn = new UnmanagedArray<SChar>(_Value.next_in, _Value.avail_in);
		NextOut = new UnmanagedArray<SChar>(_Value.next_out, _Value.avail_out);
		Message = Marshal.PtrToStringAnsi((IntPtr)_Value.msg);
	}

	[FixedAddressValueType]
	private z_stream _Value;

	public UnmanagedArray<SChar> NextIn;
	public ulong TotalIn => _Value.total_in;
	public UnmanagedArray<SChar> NextOut;
	public ulong TotalOut => _Value.total_out;
	public string? Message;
	public int DataType => _Value.data_type;

	public static unsafe implicit operator z_stream*(ZStream strm)
	{
		fixed (z_stream* ptr = &strm._Value)
		{
			return ptr;
		}
	}
}

public static class Defined
{
	private const string ZLIB_VERSION = "1.2.11";
	
	public unsafe static ZResult DeflateInit(out ZStream strm, int level)
	{
		fixed (char* s = ZLIB_VERSION)
		{
			z_stream* ret = default;
			var res = (ZResult)ExternalCalls.deflateInit_(ret, level, s, (int)sizeof(z_stream));
			strm = new ZStream(ret);
			return res;
		}
	}

	public unsafe static ZResult DeflateEnd(in ZStream strm)
	{
		z_stream* res = default;
		var ret = (ZResult)ExternalCalls.deflateEnd((z_stream*)strm);
		return ret;
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

public unsafe class UnmanagedArray<T> where T : unmanaged
{
	public UnmanagedArray(T* ptr, uint size)
	{
		_Ptr = ptr;
		Length = size;
	}

	public readonly uint Length;
	private readonly T* _Ptr;

	public T this[int index]
	{
		get
		{
			if (index < Length) return *(_Ptr + index);
			else throw new IndexOutOfRangeException();
		}

		set
		{
			if (index < Length) *(_Ptr + index) = value;
			else throw new IndexOutOfRangeException();
		}
	}
}
