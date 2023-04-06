
using System;
using System.IO;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.NativeMemory;
using static ParallelParsing.GZTool.NET.ExternalCalls;
using static ParallelParsing.GZTool.NET.Constants;

namespace ParallelParsing.GZTool.NET;

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

// public unsafe class Point
// {
// 	// internal point* PtrPoint;
// 	// public ulong Output => PtrPoint->@out;
// 	// public ulong Input => PtrPoint->@in;
// 	// public uint Bits => PtrPoint->bits;
// 	//// public ulong WindowBeginning => PtrPoint->window_beginning;
// 	//// public uint WindowSize => PtrPoint->window_size;
// 	// public FixedArray<byte> Window { get; init; }
// 	//// public ulong LineNumber => PtrPoint->line_number;

// 	// internal Point(point* ptr)
// 	// {
// 	// 	PtrPoint = ptr;
// 	// 	Window = new FixedArray<byte>(PtrPoint->window, .WINSIZE);
// 	// }

// 	// public static unsafe implicit operator Point(point* p) => new Point(p);
// 	public long Output;
// 	public long Input;
// 	public int Bits;
// 	public byte[] Window = new byte[.WINSIZE];
// }

// public unsafe class Index
// {
// 	// internal index* Access;
// 	//// public ulong FileSize => Access->file_size;
// 	//// public bool IsIndexComplete => Access->index_complete == 1;
// 	//// public int IndexVersion => Access->index_version;
// 	//// public int LineNumberFormat => Access->index_version;
// 	//// public ulong NumberOfLines => Access->number_of_lines;
// 	//// public string FileName => Marshal.PtrToStringAnsi(Access->file_name) ?? string.Empty;
// 	// public PointList List;

// 	// public Index(index* ptr)
// 	// {
// 	// 	Access = ptr;
// 	// 	List = new PointList(Access->list, Access->have);
// 	// }

// 	// public unsafe class PointList
// 	// {
// 	// 	internal point* PtrPoint;
// 	// 	public int Length;

// 	// 	public PointList(point* ptr, int length)
// 	// 	{
// 	// 		PtrPoint = ptr;
// 	// 		Length = length;
// 	// 	}

// 	// 	public Point this[int index]
// 	// 	{
// 	// 		get => PtrPoint + index;
// 	// 		set => *(PtrPoint + index) = *value.PtrPoint;
// 	// 	}
// 	// }
// 	public List<Point> List = new List<Point>();
// }

public unsafe struct Index
{
	public int have;
	public int gzip;
	public long length;
	public Point* list;
}

public unsafe struct Point
{
	public long @out;
	public long @in;
	public int bits;
	public byte* window;
}

public static unsafe class Defined
{
	public static unsafe ZResult InflateInit2(z_stream* strm, int windowBits)
	{
		var version = Marshal.StringToHGlobalAnsi(ZLIB_VERSION);
		return (ZResult)inflateInit2_(strm, windowBits, version, sizeof(z_stream));
	}

	public static void FreeDeflateIndex(Index* index)
	{
		if (index != null)
		{
			Free(index->list);
			Free(index);
		}
	}

	public static Index* AddPoint(Index* index, int bits, long @in, long @out, uint left, byte* window)
	{
		Point* next;

		if (index == null)
		{
			index = (Index*)Alloc((nuint)sizeof(Index));
			if (index == null) return null;
			index->list = (Point*)Alloc((nuint)sizeof(Point) << 3);
			if (index->list == null)
			{
				Free(index);
				return null;
			}
			index->gzip = 8;
			index->have = 0;
		}

		// if list is full, make it bigger
		else if (index->have == index->gzip)
		{
			index->gzip <<= 1;
			next = (Point*)Realloc(index->list, (nuint)(sizeof(Point) * index->gzip));
			if (next == null)
			{
				FreeDeflateIndex(index);
				return null;
			}
			index->list = next;
		}

		// fill in entry and increment how many we have
		next = (Point*)(index->list) + index->have;
		next->bits = bits;
		next->@in = @in;
		next->@out = @out;
		next->window = (byte*)Alloc(WINSIZE);
		if (left != 0)
			Copy(window + WINSIZE - left, next->window, left);
		if (left < WINSIZE)
			Copy(window, next->window + left, WINSIZE - left);
		index->have++;

		// return list, possibly reallocated
		return index;
	}

	/* See comments in zran.h. */
	public static ZReturn deflate_index_build(void* @in, long span, Index** built)
	{
		ZResult ret;
		int gzip = 0;               /* true if reading a gzip file */
		long totin, totout;        /* our own total counters to avoid 4GB limit */
		long last;                 /* totout value of last access point */
		Index* index;    /* access points being generated */
		z_stream strm;
		byte* input = (byte*)Alloc(CHUNK);
		byte* window = (byte*)Alloc(WINSIZE);

		/* initialize inflate */
		strm.zalloc = null;
		strm.zfree = null;
		strm.opaque = null;
		strm.avail_in = 0;
		strm.next_in = null;
		ret = InflateInit2(&strm, 47);      /* automatic zlib or gzip decoding */
		if (ret != ZResult.OK)
			return ret;

		/* inflate the input, maintain a sliding window, and build an index -- this
		   also validates the integrity of the compressed data using the check
		   information in the gzip or zlib stream */
		totin = totout = last = 0;
		index = null;               /* will be allocated by first addpoint() */
		strm.avail_out = 0;
		do
		{
			/* get some compressed data from input file */
			strm.avail_in = fread(input, 1, CHUNK, @in);
			if (ferror(@in) != 0)
			{
				ret = ZResult.ERRNO;
				goto deflate_index_build_error;
			}
			if (strm.avail_in == 0)
			{
				ret = ZResult.DATA_ERROR;
				goto deflate_index_build_error;
			}
			strm.next_in = input;

			/* check for a gzip stream */
			if (totin == 0 && strm.avail_in >= 3 &&
				input[0] == 31 && input[1] == 139 && input[2] == 8)
				gzip = 1;

			/* process all of that, or until end of stream */
			do
			{
				/* reset sliding window if necessary */
				if (strm.avail_out == 0)
				{
					strm.avail_out = WINSIZE;
					strm.next_out = window;
				}

				/* inflate until out of input, output, or at end of block --
				   update the total input and output counters */
				totin += strm.avail_in;
				totout += strm.avail_out;
				ret = inflate(&strm, ZFlush.BLOCK);      /* return at end of block */
				totin -= strm.avail_in;
				totout -= strm.avail_out;
				if (ret == ZResult.NEED_DICT)
					ret = ZResult.DATA_ERROR;
				if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR)
					goto deflate_index_build_error;
				if (ret == ZResult.STREAM_END)
				{
					if (gzip != 0 && (strm.avail_in != 0 || ungetc(getc(@in), @in) != EOF))
					{
						ret = inflateReset(&strm);
						if (ret != ZResult.OK)
							goto deflate_index_build_error;
						continue;
					}
					break;
				}

				/* if at end of block, consider adding an index entry (note that if
				   data_type indicates an end-of-block, then all of the
				   uncompressed data from that block has been delivered, and none
				   of the compressed data after that block has been consumed,
				   except for up to seven bits) -- the totout == 0 provides an
				   entry point after the zlib or gzip header, and assures that the
				   index always has at least one access point; we avoid creating an
				   access point after the last block by checking bit 6 of data_type
				 */
				if ((strm.data_type & 128) != 0 && (strm.data_type & 64) == 0 &&
					(totout == 0 || totout - last > span))
				{
					index = AddPoint(index, strm.data_type & 7, totin,
									 totout, strm.avail_out, window);
					if (index == null)
					{
						ret = ZResult.MEM_ERROR;
						goto deflate_index_build_error;
					}
					last = totout;
				}
			} while (strm.avail_in != 0);
		} while (ret != ZResult.STREAM_END);

		/* clean up and return index (release unused entries in list) */
		inflateEnd(&strm);
		index->list = (Point*)Realloc(index->list, (nuint)(sizeof(Point) * index->have));
		index->gzip = gzip;
		index->length = totout;
		*built = index;
		return index->have;

	/* return error */
	deflate_index_build_error:
		inflateEnd(&strm);
		FreeDeflateIndex(index);
		return ret;
	}

	// See comments in zran.h.
	public static ZReturn deflate_index_extract(void* @in, Index* index, long offset, byte* buf, int len)
	{
		ZResult ret;
		int value = 0;
		bool skip;
		z_stream strm;
		Point* here;
		byte* input = (byte*)Alloc(CHUNK);
		byte* discard = (byte*)Alloc(WINSIZE);

		/* proceed only if something reasonable to do */
		if (len < 0)
			return 0;

		/* find where in stream to start */
		here = index->list;
		value = index->have;
		while (--value != 0 && here[1].@out <= offset)
			here++;

		/* initialize file and inflate state to start there */
		strm.zalloc = null;
		strm.zfree = null;
		strm.opaque = null;
		strm.avail_in = 0;
		strm.next_in = null;
		ret = InflateInit2(&strm, -15);         /* raw inflate */
		if (ret != ZResult.OK)
			return ret;
		ret = (ZResult)fseeko(@in, here->@in - (here->bits != 0 ? 1 : 0), (int)SeekOpt.SET);
		if (ret == ZResult.ERRNO)
			goto deflate_index_extract_ret;
		if (here->bits != 0)
		{
			ret = (ZResult)getc(@in);
			if (ret == ZResult.ERRNO)
			{
				ret = ferror(@in) != 0 ? ZResult.ERRNO : ZResult.DATA_ERROR;
				goto deflate_index_extract_ret;
			}
			inflatePrime(&strm, here->bits, value >> (8 - here->bits));
		}
		inflateSetDictionary(&strm, here->window, WINSIZE);

		/* skip uncompressed bytes until offset reached, then satisfy request */
		offset -= here->@out;
		strm.avail_in = 0;
		skip = true;                               /* while skipping to offset */
		do
		{
			/* define where to put uncompressed data, and how much */
			if (offset > WINSIZE)
			{             /* skip WINSIZE bytes */
				strm.avail_out = WINSIZE;
				strm.next_out = discard;
				offset -= WINSIZE;
			}
			else if (offset > 0)
			{              /* last skip */
				strm.avail_out = (uint)offset;
				strm.next_out = discard;
				offset = 0;
			}
			else if (skip)
			{                    /* at offset now */
				strm.avail_out = (uint)len;
				strm.next_out = buf;
				skip = false;                       /* only do this once */
			}

			/* uncompress until avail_out filled, or end of stream */
			do
			{
				if (strm.avail_in == 0)
				{
					strm.avail_in = fread(input, 1, CHUNK, @in);
					if (ferror(@in) != 0)
					{
						ret = ZResult.ERRNO;
						goto deflate_index_extract_ret;
					}
					if (strm.avail_in == 0)
					{
						ret = ZResult.DATA_ERROR;
						goto deflate_index_extract_ret;
					}
					strm.next_in = input;
				}
				ret = inflate(&strm, ZFlush.NO_FLUSH);       /* normal inflate */
				if (ret == ZResult.NEED_DICT)
					ret = ZResult.DATA_ERROR;
				if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR)
					goto deflate_index_extract_ret;
				if (ret == ZResult.STREAM_END)
				{
					/* the raw deflate stream has ended */
					if (index->gzip == 0)
						/* this is a zlib stream that has ended -- done */
						break;

					/* near the end of a gzip member, which might be followed by
					   another gzip member -- skip the gzip trailer and see if
					   there is more input after it */
					if (strm.avail_in < 8)
					{
						fseeko(@in, 8 - strm.avail_in, (int)SeekOpt.CUR);
						strm.avail_in = 0;
					}
					else
					{
						strm.avail_in -= 8;
						strm.next_in += 8;
					}
					if (strm.avail_in == 0 && ungetc(getc(@in), @in) == EOF)
						/* the input ended after the gzip trailer -- done */
						break;

					/* there is more input, so another gzip member should follow --
					   validate and skip the gzip header */
					ret = inflateReset2(&strm, 31);
					if (ret != ZResult.OK)
						goto deflate_index_extract_ret;
					do
					{
						if (strm.avail_in == 0)
						{
							strm.avail_in = fread(input, 1, CHUNK, @in);
							if (ferror(@in) != 0)
							{
								ret = ZResult.ERRNO;
								goto deflate_index_extract_ret;
							}
							if (strm.avail_in == 0)
							{
								ret = ZResult.DATA_ERROR;
								goto deflate_index_extract_ret;
							}
							strm.next_in = input;
						}
						ret = inflate(&strm, ZFlush.BLOCK);
						if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR)
							goto deflate_index_extract_ret;
					} while ((strm.data_type & 128) == 0);

					/* set up to continue decompression of the raw deflate stream
					   that follows the gzip header */
					ret = inflateReset2(&strm, -15);
					if (ret != ZResult.OK)
						goto deflate_index_extract_ret;
				}

				/* continue to process the available input before reading more */
			} while (strm.avail_out != 0);

			if (ret == ZResult.STREAM_END)
				/* reached the end of the compressed data -- return the data that
				   was available, possibly less than requested */
				break;

			/* do until offset reached and requested data read */
		} while (skip);

		/* compute the number of uncompressed bytes read after the offset */
		value = skip ? 0 : len - (int)strm.avail_out;

	/* clean up and return the bytes read, or the negative error */
	deflate_index_extract_ret:
		inflateEnd(&strm);
		return value;
	}


	// public ZResult BuildDeflateIndex(FileStream @in, long span, out Index? built)
	// {
	// 	ZResult ret;
	// 	// our own total counters to avoid 4GB limit
	// 	long totin, totout;
	// 	// totout value of last access point
	// 	long last;
	// 	// access points being generated
	// 	Index index = new Index();
	// 	ZStream strm = new ZStream();
	// 	byte[] input = new byte[..CHUNK];
	// 	byte[] window = new byte[.WINSIZE];

	// 	ret = InflateInit2(strm, 47);
	// 	if (ret != ZResult.OK)
	// 	{
	// 		built = null;
	// 		return ret;
	// 	}

	// 	totin = totout = last = 0;
	// 	do
	// 	{

	// 	} while (true);
	// }
}
