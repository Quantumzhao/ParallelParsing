
using System;
using System.IO;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.NativeMemory;
using static ParallelParsing.ZRan.NET.ExternalCalls;
using static ParallelParsing.ZRan.NET.Constants;
using static ParallelParsing.ZRan.NET.Compat;

namespace ParallelParsing.ZRan.NET;

public unsafe class Index
{
	// total length of uncompressed data
	public long length;
	// allocated list of entries
	public List<Point> list;

	public Index()
	{
		list = new List<Point>(8);
	}

	public void AddPoint(int bits, long @in, long @out, uint left, byte[] window)
	{
		Point next = new Point(@out, @in, bits);

		if (left != 0)
			Array.Copy(window, WINSIZE - left, next.window, 0, left);
			
		if (left < WINSIZE)
			Array.Copy(window, 0, next.window, left, WINSIZE - left);
		this.list.Add(next);
	}
}

public struct Point
{
	// corresponding offset in uncompressed data
	public readonly long @out;
	// offset in input file of first full byte
	public readonly long @in;
	// number of bits (1-7) from byte at in-1, or 0
	public readonly int bits;
	// preceding 32K of uncompressed data
	public readonly byte[] window = new byte[WINSIZE];

	public Point(long @out, long @in, int bits)
	{
		this.@out = @out;
		this.@in = @in;
		this.bits = bits;
	}
}

public static unsafe class Core
{
	// Make one entire pass through a zlib or gzip compressed stream and build an
	// index, with access points about every span bytes of uncompressed output.
	// gzip files with multiple members are indexed in their entirety. span should
	// be chosen to balance the speed of random access against the memory
	// requirements of the list, about 32K bytes per access point. The return value
	// is the number of access points on success (>= 1), Z_MEM_ERROR for out of
	// memory, Z_DATA_ERROR for an error in the input file, or Z_ERRNO for a file
	// read error. On success, *built points to the resulting index.
	public static int deflate_index_build(void* @in, long span, out Index built)
	{
		z_stream strm;
		Index index = new Index();
		byte[] input = new byte[CHUNK];
		byte[] window = new byte[WINSIZE];

		var hWindow = GCHandle.Alloc(window, GCHandleType.Pinned);
		var hInput = GCHandle.Alloc(input, GCHandleType.Pinned);
		var pInput = (byte*)hInput.AddrOfPinnedObject();

		try
		{
			ZResult ret;
			// our own total counters to avoid 4GB limit
			long totin, totout;
			// totout value of last access point
			long last;

			// initialize inflate
			strm.zalloc = null;
			strm.zfree = null;
			strm.opaque = null;
			strm.avail_in = 0;
			strm.next_in = null;
			// automatic gzip decoding
			ret = InflateInit2(&strm, 47);
			if (ret != ZResult.OK)
			{
				throw new ZException(ret);
			}

			// inflate the input, maintain a sliding window, and build an index -- this
			// also validates the integrity of the compressed data using the check
			// information in the gzip or zlib stream
			totin = totout = last = 0;
			strm.avail_out = 0;
			do
			{
				// get some compressed data from input file
				strm.avail_in = fread(pInput, 1, CHUNK, @in);
				if (ferror(@in) != 0)
				{
					throw new ZException(ZResult.ERRNO);
				}
				if (strm.avail_in == 0)
				{
					throw new ZException(ZResult.DATA_ERROR);
				}
				strm.next_in = pInput;

				// process all of that, or until end of stream
				do
				{
					// reset sliding window if necessary
					if (strm.avail_out == 0)
					{
						strm.avail_out = WINSIZE;
						strm.next_out = (byte*)hWindow.AddrOfPinnedObject();
					}

					// inflate until out of input, output, or at end of block --
					// update the total input and output counters
					totin += strm.avail_in;
					totout += strm.avail_out;
					// return at end of block
					ret = inflate(&strm, ZFlush.BLOCK);
					totin -= strm.avail_in;
					totout -= strm.avail_out;
					if (ret == ZResult.NEED_DICT ||
						ret == ZResult.MEM_ERROR ||
						ret == ZResult.DATA_ERROR)
						throw new ZException(ret);
					if (ret == ZResult.STREAM_END)
					{
						if (strm.avail_in != 0 || ungetc(getc(@in), @in) != EOF)
						{
							ret = inflateReset(&strm);
							if (ret != ZResult.OK)
								throw new ZException(ret);
							continue;
						}
						break;
					}

					// if at end of block, consider adding an index entry (note that if
					// data_type indicates an end-of-block, then all of the
					// uncompressed data from that block has been delivered, and none
					// of the compressed data after that block has been consumed,
					// except for up to seven bits) -- the totout == 0 provides an
					// entry point after the zlib or gzip header, and assures that the
					// index always has at least one access point; we avoid creating an
					// access point after the last block by checking bit 6 of data_type
					if ((strm.data_type & 128) != 0 && (strm.data_type & 64) == 0 &&
						(totout == 0 || totout - last > span))
					{
						index.AddPoint(strm.data_type & 7, totin, totout, strm.avail_out, window);
						last = totout;
					}
				} while (strm.avail_in != 0);
			} while (ret != ZResult.STREAM_END);

			index.length = totout;
			built = index;
			return index.list.Count;
		}
		finally
		{
			// clean up and return index (release unused entries in list)
			inflateEnd(&strm);
			hWindow.Free();
			hInput.Free();
		}
	}

	// Use the index to read len bytes from offset into buf. Return bytes read or
	// negative for error (Z_DATA_ERROR or Z_MEM_ERROR). If data is requested past
	// the end of the uncompressed data, then deflate_index_extract() will return a
	// value less than len, indicating how much was actually read into buf. This
	// function should not return a data error unless the file was modified since
	// the index was generated, since deflate_index_build() validated all of the
	// input. deflate_index_extract() will return Z_ERRNO if there is an error on
	// reading or seeking the input file.
	public static int deflate_index_extract(void* @in, Index index, long offset, byte[] buf, int len)
	{
		// no need to pin (I guess); it's an unmanaged struct on stack
		z_stream strm;
		byte[] input = new byte[CHUNK];
		byte[] discard = new byte[WINSIZE];
		var hInput = GCHandle.Alloc(input, GCHandleType.Pinned);
		var hDiscard = GCHandle.Alloc(discard, GCHandleType.Pinned);
		var hBuf = GCHandle.Alloc(buf, GCHandleType.Pinned);
		var pInput = (byte*)hInput.AddrOfPinnedObject();
		var pDiscard = (byte*)hInput.AddrOfPinnedObject();
		var pBuf = (byte*)hBuf.AddrOfPinnedObject();

		try
		{
			ZResult ret;
			int value = 0;
			bool skip;
			Point here;
			var streamOffset = 0;

			// proceed only if something reasonable to do
			if (len < 0)
				return 0;

			// find where in stream to start
			value = index.list.Count;
			while (--value != 0 && index.list[streamOffset + 1].@out <= offset)
				streamOffset++;
			here = index.list[streamOffset];

			// initialize file and inflate state to start there
			strm.zalloc = null;
			strm.zfree = null;
			strm.opaque = null;
			strm.avail_in = 0;
			strm.next_in = null;
			// raw inflate
			ret = InflateInit2(&strm, -15);
			if (ret != ZResult.OK)
				throw new ZException(ret);
			ret = (ZResult)fseeko(@in, here.@in - (here.bits != 0 ? 1 : 0), (int)SeekOpt.SET);
			if (ret == ZResult.ERRNO)
				throw new ZException(ret);
			if (here.bits != 0)
			{
				ret = (ZResult)getc(@in);
				if (ret == ZResult.ERRNO)
				{
					ret = ferror(@in) != 0 ? ZResult.ERRNO : ZResult.DATA_ERROR;
					throw new ZException(ret);
				}
				inflatePrime(&strm, here.bits, value >> (8 - here.bits));
			}
			InflateSetDictionary(&strm, here.window, WINSIZE);

			// skip uncompressed bytes until offset reached, then satisfy request
			offset -= here.@out;
			strm.avail_in = 0;
			// while skipping to offset
			skip = true;
			do
			{
				// define where to put uncompressed data, and how much
				if (offset > WINSIZE)
				{   
					// skip WINSIZE bytes
					strm.avail_out = WINSIZE;
					strm.next_out = pDiscard;
					offset -= WINSIZE;
				}
				else if (offset > 0)
				{   // last skip
					strm.avail_out = (uint)offset;
					strm.next_out = pDiscard;
					offset = 0;
				}
				else if (skip)
				{   
					// at offset now
					strm.avail_out = (uint)len;
					strm.next_out = pBuf;
					// only do this once
					skip = false;
				}

				// uncompress until avail_out filled, or end of stream
				do
				{
					if (strm.avail_in == 0)
					{
						strm.avail_in = fread(pInput, 1, CHUNK, @in);
						if (ferror(@in) != 0)
						{
							throw new ZException(ZResult.ERRNO);
						}
						if (strm.avail_in == 0)
						{
							throw new ZException(ZResult.DATA_ERROR);
						}
						strm.next_in = pInput;
					}
					ret = inflate(&strm, ZFlush.NO_FLUSH);
					// normal inflate
					if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR || ret == ZResult.NEED_DICT)
						throw new ZException(ret);
					if (ret == ZResult.STREAM_END)
					{
						// near the end of a gzip member, which might be followed by
						// another gzip member -- skip the gzip trailer and see if
						// there is more input after it
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
							// the input ended after the gzip trailer -- done
							break;

						// there is more input, so another gzip member should follow --
						// validate and skip the gzip header
						ret = inflateReset2(&strm, 31);
						if (ret != ZResult.OK)
							throw new ZException(ret);
						do
						{
							if (strm.avail_in == 0)
							{
								strm.avail_in = fread(pInput, 1, CHUNK, @in);
								if (ferror(@in) != 0)
								{
									ret = ZResult.ERRNO;
									throw new ZException(ZResult.ERRNO);
								}
								if (strm.avail_in == 0)
								{
									ret = ZResult.DATA_ERROR;
									throw new ZException(ZResult.DATA_ERROR);
								}
								strm.next_in = pInput;
							}
							ret = inflate(&strm, ZFlush.BLOCK);
							if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR)
								throw new ZException(ret);
						} while ((strm.data_type & 128) == 0);

						// set up to continue decompression of the raw deflate stream
						// that follows the gzip header
						ret = inflateReset2(&strm, -15);
						if (ret != ZResult.OK)
							throw new ZException(ret);
					}

					// continue to process the available input before reading more
				} while (strm.avail_out != 0);

				if (ret == ZResult.STREAM_END)
					// reached the end of the compressed data -- return the data that
					// was available, possibly less than requested
					break;

				// do until offset reached and requested data read
			} while (skip);

			// compute the number of uncompressed bytes read after the offset
			value = skip ? 0 : len - (int)strm.avail_out;

			return value;
		}
		finally
		{
			// clean up and return the bytes read, or the negative error
			inflateEnd(&strm);
			hInput.Free();
			hDiscard.Free();
		}
	}
}
