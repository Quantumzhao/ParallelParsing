using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ParallelParsing.GZTool.NET;

public static class ExternalCalls
{
	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern int deflateEnd(nint strm);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern int deflate(IntPtr strm, int flush);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern int deflateInit_(
		IntPtr strm, int level, IntPtr version, int stream_size);

	[DllImport("libz.so", CharSet = CharSet.Ansi)]
	public static unsafe extern int deflateInit2_(
		z_stream* strm, 
		int level, int method, 
		int windowBits, 
		int memLevel, 
		int strategy, 
		char* version, 
		int stream_size
	);

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
		z_stream* strm, int windowBits, IntPtr version, int stream_size);

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

	// [DllImport("gztool", CharSet = CharSet.Ansi)]
	// public static unsafe extern EXIT_RETURNED_VALUES action_create_index(
	// 	[MarshalAs(UnmanagedType.LPStr)] char* file_name,
	// 	index** index,
	// 	[MarshalAs(UnmanagedType.LPStr)] char* index_filename,
	// 	IndexAndExtractionOptions indx_n_extraction_opts,
	// 	UInt64 offset,
	// 	UInt64 line_number_offset,
	// 	UInt64 span_between_points,
	// 	Int32 end_on_first_proper_gzip_eof,
	// 	Int32 always_create_a_complete_index,
	// 	Int32 waiting_time,
	// 	Int32 wait_for_file_creation,
	// 	Int32 extend_index_with_lines,
	// 	UInt64 expected_first_byte,
	// 	Int32 gzip_stream_may_be_damaged,
	// 	[MarshalAs(UnmanagedType.I1)] bool lazy_gzip_stream_patching_at_eof,
	// 	UInt64 range_number_of_bytes,
	// 	UInt64 range_number_of_lines
	// );

	// [DllImport("gztool")]
	// public static unsafe extern int serialize_index_to_file(
	// 	IntPtr output_file,
	// 	index* index,
	// 	UInt64 index_last_written_point
	// );

	// [DllImport("gztool", CharSet = CharSet.Ansi)]
	// public static unsafe extern int deserialize_index_from_file(
	// 	IntPtr input_file, 
	// 	int load_windows, 
	// 	[MarshalAs(UnmanagedType.LPStr)] char* file_name,
	// 	int extern_index_with_lines
	// );
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

// public static class Defined
// {
	// private const string ZLIB_VERSION = "1.2.11";
	
	// public unsafe static ZResult DeflateInit(out ZStream strm, int level)
	// {
	// 	var s = Marshal.StringToHGlobalAnsi(ZLIB_VERSION);
	// 	z_stream* ret = default;
	// 	var res = (ZResult)ExternalCalls.deflateInit_((IntPtr)ret, level, s, (int)sizeof(z_stream));
	// 	strm = new ZStream(ret);
	// 	return res;
	// }

	// public unsafe static ZResult DeflateEnd(in ZStream strm)
	// {
	// 	z_stream* res = default;
	// 	var ret = (ZResult)ExternalCalls.deflateEnd((IntPtr)strm);
	// 	return ret;
	// }

	// public unsafe static EXIT_RETURNED_VALUES Decompress(
	// 	char* file_name,
	// 	UInt64 extract_from_byte,
	// 	char* index_filename)
	// {
	// 	var index_filename_indicated = 1;
		

		// return ExternalCalls.action_create_index(
		// 	file_name,
		// 	index_filename,
		// 	IndexAndExtractionOptions.ExtractFromByte,
		// 	offset: extract_from_byte, 
		// 	line_number_offset,
		// 	span_between_points,
		// 	end_on_first_proper_gzip_eof,
		// 	always_create_a_complete_index,
		// 	waiting_time: 4,
		// 	wait_for_file_creation,
		// 	extend_index_with_lines,
		// 	expected_first_byte,
		// 	gzip_stream_may_be_damaged,
		// 	lazy_gzip_stream_patching_at_eof,
		// 	range_number_of_bytes,
		// 	range_number_of_lines
		// );

	// 	throw new NotImplementedException();
	// }
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
