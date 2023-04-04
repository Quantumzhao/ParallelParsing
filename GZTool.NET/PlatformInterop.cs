using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ParallelParsing.GZTool.NET;

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

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflate(z_stream* strm, int flush);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflateEnd(z_stream* strm, int flush);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflatePrime(z_stream* strm, int bits, int value);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflateSetDictionary(
	// 	z_stream* strm, byte* dictionary, uint dictLength);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflateCopy(z_stream* dest, z_stream* source);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflateInit_(
	// 	z_stream* strm, char* version, int stream_size);

	// [DllImport("libz.so", CharSet = CharSet.Ansi)]
	// public static unsafe extern int inflateInit2_(
	// 	z_stream* strm, int windowBits, char* version, int stream_size);

	[DllImport("gztool", CharSet = CharSet.Ansi)]
	public static unsafe extern EXIT_RETURNED_VALUES action_create_index(
		[MarshalAs(UnmanagedType.LPStr)] char* file_name,
		access** index,
		[MarshalAs(UnmanagedType.LPStr)] char* index_filename,
		IndexAndExtractionOptions indx_n_extraction_opts,
		UInt64 offset,
		UInt64 line_number_offset,
		UInt64 span_between_points,
		Int32 end_on_first_proper_gzip_eof,
		Int32 always_create_a_complete_index,
		Int32 waiting_time,
		Int32 wait_for_file_creation,
		Int32 extend_index_with_lines,
		UInt64 expected_first_byte,
		Int32 gzip_stream_may_be_damaged,
		[MarshalAs(UnmanagedType.I1)] bool lazy_gzip_stream_patching_at_eof,
		UInt64 range_number_of_bytes,
		UInt64 range_number_of_lines
	);

	[DllImport("gztool")]
	public static unsafe extern int serialize_index_to_file(
		IntPtr output_file,
		access* index,
		UInt64 index_last_written_point
	);

	[DllImport("gztool", CharSet = CharSet.Ansi)]
	public static unsafe extern int deserialize_index_from_file(
		IntPtr input_file, 
		int load_windows, 
		[MarshalAs(UnmanagedType.LPStr)] char* file_name,
		int extern_index_with_lines
	);
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct point
{
	// corresponding offset in uncompressed data
	internal UInt64 @out;
	// offset in input file of first full byte
	internal UInt64 @in;
	// number of bits (1-7) from byte at in - 1, or 0
	internal UInt32 bits;
	// offset at index file where this compressed window is stored
	internal UInt64 window_beginning;
	// size of (compressed) window
	internal UInt32 window_size;
	// preceding 32K of uncompressed data, compressed
	internal Byte* window;
	internal UInt64 line_number;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct access
{
	// number of list entries filled in
	internal UInt64 have;
	// number of list entries allocated
	internal UInt64 size;
	// size of uncompressed file (useful for bgzip files)
	internal UInt64 file_size;
	// allocated list
	internal point* list;
	// path to index file
	internal IntPtr file_name;
	// 1: index is complete; 0: index is (still) incomplete
	internal int index_complete;
	// 0: default; 1: index with line numbers
	internal int index_version;
	// 0: linux \r | windows \n\r; 1: mac \n
	internal UInt32 line_number_format;
	// number of lines (only used with v1 index format)
	internal UInt64 number_of_lines;
}

// [StructLayout(LayoutKind.Sequential)]
// public unsafe struct z_stream
// {
// 	[MarshalAs(UnmanagedType.LPArray)]
// 	public char* next_in;
// 	public uint avail_in;
// 	public ulong total_in;
// 	[MarshalAs(UnmanagedType.LPArray)]
// 	public char* next_out;
// 	public uint avail_out;
// 	public ulong total_out;
// 	[MarshalAs(UnmanagedType.LPStr)]
// 	public char* msg;
// 	public void* state;
// 	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
// 	// unused
// 	public delegate* unmanaged[Cdecl]<void*, uint, uint, void*> zalloc;
// 	// unused
// 	public delegate* unmanaged[Cdecl]<void*, void*, void> zfree;
// 	// unused
// 	public void* opaque;
// 	public int data_type;
// 	// unused
// 	public ulong adler;
// 	// unused
// 	public ulong reserved;
// }

// public class ZStream
// {
// 	public unsafe ZStream(z_stream* strm)
// 	{
// 		_Hndl = strm;
// 		NextIn = new FixedArray<char>(_Hndl->next_in, _Hndl->avail_in);
// 		NextOut = new FixedArray<char>(_Hndl->next_out, _Hndl->avail_out);
// 	}

// 	[FixedAddressValueType]
// 	private unsafe z_stream* _Hndl;
// 	public readonly FixedArray<char> NextIn;
// 	public unsafe ulong TotalIn => _Hndl->total_in;
// 	public readonly FixedArray<char> NextOut;
// 	public unsafe ulong TotalOut => _Hndl->total_out;
// 	public unsafe string? Message => Marshal.PtrToStringAnsi((IntPtr)_Hndl->msg);
// 	public unsafe int DataType => _Hndl->data_type;

// 	public static unsafe implicit operator z_stream*(ZStream strm)
// 	{
// 		return strm._Hndl;
// 	}

// 	public static unsafe implicit operator IntPtr(ZStream strm)
// 	{
// 		return (IntPtr)strm._Hndl;
// 	}

// }

public static class Defined
{
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
}

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
