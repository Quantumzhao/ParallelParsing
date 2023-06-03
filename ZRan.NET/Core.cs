
using static ParallelParsing.ZRan.NET.Constants;
using static ParallelParsing.ZRan.NET.Compat;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;

namespace ParallelParsing.ZRan.NET;

public static class Core
{
	public static Index BuildDeflateIndex_NEW(FileStream file, uint chunksize)
	{
		ZStream strm = new();
		Index index = new Index(0);
		byte[] input = new byte[CHUNK];
		byte[] window = new byte[WINSIZE];

		int recordCounter = 0;
		int prevAvailOut = 0;
		byte[] offsetBeforePoint = new byte[WINSIZE];
		int offsetArraySize = 0;

		try
		{
			ZResult ret;
			// our own total counters to avoid 4GB limit
			long totin, totout;
			// totout value of last access point
			// long last;

			// automatic gzip decoding
			ret = InflateInit(strm, 47);
			if (ret != ZResult.OK)
			{
				throw new ZException(ret);
			}

			// inflate the input, maintain a sliding window, and build an index -- this
			// also validates the integrity of the compressed data using the check
			// information in the gzip or zlib stream
			totin = totout = 0;
			strm.AvailOut = 0;
			do
			{
				// get some compressed data from input file
				strm.AvailIn = (uint)file.Read(input, 0, (int)CHUNK);
				// if (ferror(@in) != 0)
				// {
				// 	throw new ZException(ZResult.ERRNO);
				// }
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
						ret == ZResult.DATA_ERROR ||
						ret == ZResult.STREAM_ERROR ||
						ret == ZResult.BUF_ERROR ||
						ret == ZResult.VERSION_ERROR)
						throw new ZException(ret);
					
					if (strm.NextOut != null)
					{
						// Count how many "@"s are in NextIn
						int currNextOutLength = strm.NextOut.Length;
						int iStartPos = prevAvailOut == 0 ? 0 : currNextOutLength - prevAvailOut;

						for (int i = iStartPos; i < currNextOutLength - strm.AvailOut; i++)
						{
							var c = strm.NextOut[i];

							if (c == 64)
							{
								recordCounter++;
								Array.Clear(offsetBeforePoint, 0, offsetBeforePoint.Length);
								offsetArraySize = 0;
							}

							offsetBeforePoint[offsetArraySize] = c;
							offsetArraySize++;
						}
						prevAvailOut = strm.AvailOut > 0 ? (int)strm.AvailOut : 0;
						
						if ((strm.DataType & 128) != 0 && (strm.DataType & 64) == 0)
						{
							// Add the first point after the header
							if (totout == 0) 
								index.AddPoint_NEW(strm.DataType & 7, totin, totout, strm.AvailOut, window, new byte[0]);
							else
							{
								if (recordCounter > chunksize - 8)
								{
									index.AddPoint_NEW(strm.DataType & 7, totin, totout, strm.AvailOut, window, offsetBeforePoint[0..offsetArraySize]);
									recordCounter = 0;
								}
							}
						}
					}

					if (ret == ZResult.STREAM_END)
					{
						if (strm.AvailIn != 0 || file.Position != file.Length)
						{
							ret = InflateReset(strm);
							if (ret != ZResult.OK)
								throw new ZException(ret);
							continue;
						}
						index.AddPoint_NEW(strm.DataType & 7, totin, totout, strm.AvailOut, window, new byte[0]);
						break;
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

// 240k / 1100 = 218
// 240k / 300  = 800

// 218*700 = 152600 = 152k
// 240k - 152600 = 93160
// 240k - 218*1100 = 5960
// 5960/700 = 8.5


	private static int TryCopy<T>(T[] src, int srcOffset, T[] dst,
		int length) where T : unmanaged
	{
		var minDstLen = Math.Min(dst.Length, length);
		var srcDstLen = Math.Min(src.Length - srcOffset, minDstLen);
		Array.Copy(src, srcOffset, dst, 0, srcDstLen);
		return srcDstLen;
	}


	public static int ExtractDeflateIndex(
		byte[] fileBuffer, Point from, Point to, byte[] buf)
	{
		// lock (o) {
		// no need to pin (I guess); it's an unmanaged struct on stack
		using var strm = new ZStream();
		byte[] input = new byte[CHUNK];
		var len = (int)(to.Output - from.Output);

		ZResult ret;
		int value = 0;

		// proceed only if something reasonable to do
		if (len < 0)
			return 0;

		// raw inflate
		ret = InflateInit(strm, -15);
		if (ret != ZResult.OK) throw new ZException(ret);

		var posInFile = from.Bits == 0 ? 1 : 0;
		if (from.Bits != 0)
		{
			value = fileBuffer[0];
			InflatePrime(strm, from.Bits, value >> (8 - from.Bits));
			posInFile++;
		}
		InflateSetDictionary(strm, from.Window, WINSIZE);

		strm.AvailIn = 0;
		strm.AvailOut = (uint)len;
		strm.NextOut = buf;
		do
		{
			if (strm.AvailIn == 0)
			{
				value = TryCopy(fileBuffer, posInFile, input, (int)CHUNK);
				strm.AvailIn = (uint)value;
				posInFile += value;
				if (value == 0) throw new ZException(ZResult.DATA_ERROR);
				strm.NextIn = input;
			}
			ret = Inflate(strm, ZFlush.NO_FLUSH);
			// strm.NextOut.PrintASCII(32*1024-1);
			// normal inflate
			if (ret == ZResult.MEM_ERROR || ret == ZResult.DATA_ERROR || ret == ZResult.NEED_DICT)
				throw new ZException(ret);
			if (ret == ZResult.STREAM_ERROR)
			{
				Console.WriteLine("stream error");
				break;
			}
			if (ret == ZResult.STREAM_END) break;

			// continue to process the available input before reading more
		} while (strm.AvailOut != 0);

		// compute the number of uncompressed bytes read after the offset
		return len - (int)strm.AvailOut;
	}
	// }
}
