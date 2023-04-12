
using static ParallelParsing.ZRan.NET.Constants;
using static ParallelParsing.ZRan.NET.Compat;

namespace ParallelParsing.ZRan.NET;

public class Index
{
	// allocated list of entries
	public List<Point> List;

	public Index()
	{
		List = new List<Point>(8);
	}

	public void AddPoint(int bits, long input, long output, uint left, byte[] window)
	{
		Point next = new Point(output, input, bits);

		if (left != 0)
			Array.Copy(window, WINSIZE - left, next.Window, 0, left);
			
		if (left < WINSIZE)
			Array.Copy(window, 0, next.Window, left, WINSIZE - left);
		this.List.Add(next);
	}
}

public class Point
{
	// corresponding offset in uncompressed data
	public readonly long Output;
	// offset in input file of first full byte
	public readonly long Input;
	// number of bits (1-7) from byte at in-1, or 0
	public readonly int Bits;
	// preceding 32K of uncompressed data
	public readonly byte[] Window;

	public Point(long output, long input, int bits)
	{
		this.Output = output;
		this.Input = input;
		this.Bits = bits;
		this.Window = new byte[WINSIZE];
	}

	internal Point(long output, long input, int bits, byte[] window)
	: this(output, input, bits)
	{
		this.Window = window;
	}
}

public static class Core
{
	/// <summary>
	/// Make one entire pass through a zlib or gzip compressed stream and build an
	/// index, with access points about every span bytes of uncompressed output.
	/// gzip files with multiple members are indexed in their entirety. 
	/// </summary>
	/// <param name="span">
	/// span should be chosen to balance the speed of random access against the memory 
	/// requirements of the list, about 32K bytes per access point. 
	/// </param>
	public static Index BuildDeflateIndex(FileStream file, long span)
	{
		ZStream strm = new();
		Index index = new Index();
		byte[] input = new byte[CHUNK];
		byte[] window = new byte[WINSIZE];

		try
		{
			ZResult ret;
			// our own total counters to avoid 4GB limit
			long totin = 0;
			long totout = 0;
			// totout value of last access point
			long last = 0;

			// 47: enable zlib and gzip decoding with automatic header detection
			ret = InflateInit(strm, 47);
			if (ret != ZResult.OK)
			{
				throw new ZException(ret);
			}
			strm.AvailOut = 0;

			// inflate the input, maintain a sliding window, and build an index -- this
			// also validates the integrity of the compressed data using the check
			// information in the gzip or zlib stream
			do
			{
				// get some compressed data from input file
				strm.AvailIn = (uint)file.Read(input, 0, (int)CHUNK);

				if (strm.AvailIn == 0)
				{
					throw new ZException(ZResult.DATA_ERROR);
				}
				strm.NextIn = input;

				// process all of that, or until end of stream
				do
				{
					// reset sliding window if necessary
					if (strm.AvailOut == 0)
					{
						strm.AvailOut = WINSIZE;
						strm.NextOut = window;
					}

					// inflate until out of input, output, or at end of block --
					// update the total input and output counters
					totin += strm.AvailIn;
					totout += strm.AvailOut;
					// return at end of block
					ret = Inflate(strm, ZFlush.BLOCK);
					totin -= strm.AvailIn;
					totout -= strm.AvailOut;
					if (ret == ZResult.NEED_DICT ||
						ret == ZResult.MEM_ERROR ||
						ret == ZResult.DATA_ERROR)
						throw new ZException(ret);
					if (ret == ZResult.STREAM_END)
					{
						if (strm.AvailIn != 0 || file.Position != file.Length)
						{
							ret = InflateReset(strm);
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
					// * strm.DataType: the number of unused bits 
					// * in the last byte taken from strm->next_in
					// * +64 if inflate() is currently decoding the last block in the deflate stream
					// * +128 if inflate() returned immediately after decoding an end-of-block code
					// *      or decoding the complete header up to just before the first byte 
					// *      of the deflate stream
					if ((strm.DataType & 128) != 0 && (strm.DataType & 64) == 0 &&
						(totout == 0 || totout - last > span))
					{
						index.AddPoint(strm.DataType & 7, totin, totout, strm.AvailOut, window);
						last = totout;
					}
				} while (strm.AvailIn != 0);
			} while (ret != ZResult.STREAM_END);

			// index.length = totout;
			return index;
		}
		finally
		{
			// clean up and return index (release unused entries in list)
			InflateEnd(strm);
		}
	}

	/// <summary>Use the index to read len bytes from offset into buf. </summary>
	/// <param name="offset">starting from `offset` of uncompressed bytes</param>
	/// <param name="len">count of uncompressed bytes</param>
	/// <returns>
	/// bytes read. If data is requested past the end of the uncompressed data, 
	/// then deflate_index_extract() will return a value less than len, 
	/// indicating how much was actually read into buf. 
	/// This function should not throw a data error unless the file was modified since
	/// the index was generated, since deflate_index_build() validated all of the
	/// input. deflate_index_extract() will return Z_ERRNO if there is an error on
	/// reading or seeking the input file.
	/// </returns>
	public static int ExtractDeflateIndex(
		FileStream file, Index index, long offset, byte[] buf, int len)
	{
		ZStream strm = new();
		byte[] input = new byte[CHUNK];
		byte[] discard = new byte[WINSIZE];

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
			value = index.List.Count;
			while (--value != 0 && index.List[streamOffset + 1].Output <= offset)
				streamOffset++;
			here = index.List[streamOffset];

			// raw inflate
			// - -windowBits determines the window size
			// - not looking for a zlib or gzip header
			// - not generating a check value
			// - not looking for any check values for comparison at the end of the stream
			ret = InflateInit(strm, -15);
			if (ret != ZResult.OK)
				throw new ZException(ret);
			file.Seek(here.Input - (here.Bits != 0 ? 1 : 0), SeekOrigin.Begin);
			if (here.Bits != 0)
			{
				ret = (ZResult)file.ReadByte();
				if (ret == ZResult.ERRNO)
				{
					throw new ZException(ZResult.DATA_ERROR);
				}
				InflatePrime(strm, here.Bits, value >> (8 - here.Bits));
			}
			InflateSetDictionary(strm, here.Window, WINSIZE);

			// skip uncompressed bytes until offset reached, then satisfy request
			offset -= here.Output;
			strm.AvailIn = 0;
			// while skipping to offset
			skip = true;
			do
			{
				// define where to put uncompressed data, and how much
				if (offset > WINSIZE)
				{   
					// skip WINSIZE bytes
					strm.AvailOut = WINSIZE;
					strm.NextOut = discard;
					offset -= WINSIZE;
				}
				else if (offset > 0)
				{   // last skip
					strm.AvailOut = (uint)offset;
					strm.NextOut = discard;
					offset = 0;
				}
				else if (skip)
				{   
					// at offset now
					strm.AvailOut = (uint)len;
					strm.NextOut = buf;
					// only do this once
					skip = false;
				}

				// uncompress until avail_out filled, or end of stream
				do
				{
					if (strm.AvailIn == 0)
					{
						strm.AvailIn = (uint)file.Read(input, 0, (int)CHUNK);
						if (strm.AvailIn == 0)
						{
							throw new ZException(ZResult.DATA_ERROR);
						}
						strm.NextIn = input;
					}
					ret = Inflate(strm, ZFlush.NO_FLUSH);
					// normal inflate
					if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR || ret == ZResult.NEED_DICT)
						throw new ZException(ret);
					if (ret == ZResult.STREAM_END)
					{
						// near the end of a gzip member, which might be followed by
						// another gzip member -- skip the gzip trailer and see if
						// there is more input after it
						if (strm.AvailIn < 8)
						{
							file.Seek(8 - strm.AvailIn, SeekOrigin.Current);
							strm.AvailIn = 0;
						}
						else
						
						if (strm.AvailIn == 0 && file.Position == file.Length)
							// the input ended after the gzip trailer -- done
							break;

						// there is more input, so another gzip member should follow --
						// validate and skip the gzip header
						ret = InflateReset(strm, 31);
						if (ret != ZResult.OK)
							throw new ZException(ret);
						do
						{
							if (strm.AvailIn == 0)
							{
								strm.AvailIn = (uint)file.Read(input, 0, (int)CHUNK);

								if (strm.AvailIn == 0)
								{
									ret = ZResult.DATA_ERROR;
									throw new ZException(ZResult.DATA_ERROR);
								}
								strm.NextIn = input;
							}
							ret = Inflate(strm, ZFlush.BLOCK);
							if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR)
								throw new ZException(ret);
						} while ((strm.DataType & 128) == 0);

						// set up to continue decompression of the raw deflate stream
						// that follows the gzip header
						ret = InflateReset(strm, -15);
						if (ret != ZResult.OK)
							throw new ZException(ret);
					}

					// continue to process the available input before reading more
				} while (strm.AvailOut != 0);

				if (ret == ZResult.STREAM_END)
					// reached the end of the compressed data -- return the data that
					// was available, possibly less than requested
					break;

				// do until offset reached and requested data read
			} while (skip);

			// compute the number of uncompressed bytes read after the offset
			value = skip ? 0 : len - (int)strm.AvailOut;

			return value;
		}
		finally
		{
			// clean up and return the bytes read, or the negative error
			InflateEnd(strm);
		}
	}

	public static byte[] ExtractDeflateRange(in byte[] inputBuffer, Point p, byte[] outputBuffer)
	{
		throw new NotImplementedException();
	}
}
