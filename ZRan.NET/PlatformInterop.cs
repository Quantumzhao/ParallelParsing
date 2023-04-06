using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ParallelParsing.ZRan.NET;

public static class ExternalCalls
{
	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int deflateEnd(nint strm);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int deflate(IntPtr strm, int flush);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int deflateInit_(
	// 	IntPtr strm, int level, IntPtr version, int stream_size);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int deflateInit2_(
	// 	z_stream* strm, 
	// 	int level, int method, 
	// 	int windowBits, 
	// 	int memLevel, 
	// 	int strategy, 
	// 	char* version, 
	// 	int stream_size
	// );

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflate(z_stream* strm, ZFlush flush);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflateEnd(z_stream* strm);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflatePrime(z_stream* strm, int bits, int value);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflateSetDictionary(
		z_stream* strm, byte* dictionary, uint dictLength);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflateCopy(z_stream* dest, z_stream* source);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflateInit_(
		z_stream* strm, char* version, int stream_size);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern ZResult inflateInit2_(
		z_stream* strm, int windowBits, char* version, int stream_size);

	[DllImport("libz.so")]
	public static unsafe extern ZResult inflateReset2(z_stream* strm, int windowBits);
	
	[DllImport("libz.so")]
	public static unsafe extern ZResult inflateReset(z_stream* strm);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int fprintf(void* stream, char* format, int param);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int fprintf(void* stream, char* format, int param1, int param2);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int fclose(void* stream);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int ferror(void* stream);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int fseeko(void* stream, long off, int whence);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern uint fread(void* ptr, ulong size, ulong n, void* stream);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int getc(void* stream);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern int ungetc(int c, void* stream);

	[DllImport("/usr/lib/x86_64-linux-gnu/libc.so.6", CharSet = CharSet.Ansi)]
	public static unsafe extern void* fopen(char* file_name, char* modes);
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct z_stream
{
	[MarshalAs(UnmanagedType.LPArray)]
	public byte* next_in;
	public uint avail_in;
	public ulong total_in;
	[MarshalAs(UnmanagedType.LPArray)]
	public byte* next_out;
	public uint avail_out;
	public ulong total_out;
	[MarshalAs(UnmanagedType.LPStr)]
	public char* msg;
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
	// unused
	public ulong reserved;
}

// public class ZStream
// {
// 	// public unsafe ZStream(z_stream* strm)
// 	// {
// 	// 	_Hndl = strm;
// 	// 	NextIn = new FixedArray<char>(_Hndl->next_in, _Hndl->avail_in);
// 	// 	NextOut = new FixedArray<char>(_Hndl->next_out, _Hndl->avail_out);
// 	// }

// 	[FixedAddressValueType]
// 	internal unsafe z_stream* _Hndl;
// 	public readonly FixedArray<char> NextIn;
// 	public unsafe ulong TotalIn
// 	{
// 		get => _Hndl->total_in;
// 		set => _Hndl->total_in = value;
// 	}
// 	public readonly FixedArray<char> NextOut;
// 	public unsafe ulong TotalOut
// 	{
// 		get => _Hndl->total_out;
// 		set => _Hndl->total_out = value;
// 	}
// 	public unsafe string? Message => Marshal.PtrToStringAnsi((IntPtr)_Hndl->msg);
// 	public unsafe int DataType
// 	{
// 		get => _Hndl->data_type;
// 		set => _Hndl->data_type = value;
// 	}

// 	public static unsafe implicit operator z_stream*(ZStream strm)
// 	{
// 		return strm._Hndl;
// 	}

// 	public static unsafe implicit operator IntPtr(ZStream strm)
// 	{
// 		return (IntPtr)strm._Hndl;
// 	}

// }

public unsafe class FixedArray<T> where T : unmanaged
{
    private T* arrayPtr;
	public readonly uint Length;

    public T this[uint i]
    {
        get 
		{
			if (i < Length && i >= 0) throw new IndexOutOfRangeException();
			else return *(arrayPtr + i); 
		}
        set 
		{ 
			if (i < Length && i >= 0) throw new IndexOutOfRangeException();
			else *(arrayPtr + i) = value; 
		}
    }

    public FixedArray(uint length)
    {
        arrayPtr = (T*)Marshal.AllocHGlobal((int)(sizeof(T) * length));
    }

	public FixedArray(T* ptr, uint size)
	{
		arrayPtr = (T*)ptr;
		Length = size;
	}

    ~FixedArray()
    {
        Marshal.FreeHGlobal((IntPtr)arrayPtr);
    }
}
