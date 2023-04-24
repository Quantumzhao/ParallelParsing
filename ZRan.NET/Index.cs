
using static ParallelParsing.ZRan.NET.Constants;

namespace ParallelParsing.ZRan.NET;

public class Index
{
	// allocated list of entries
	public List<Point> List;
	// chunk size
	public uint ChunkSize;

	public Index(uint chunksize)
	{
		List = new List<Point>(8);
		ChunkSize = chunksize;
	}

	public void AddPoint(int bits, long input, long output, uint left, byte[] window)
	{
		Point next = new Point(output, input, bits);

		// if (left != 0)
		// 	Array.Copy(window, WINSIZE - left, next.Window, 0, left);
			
		// if (left < WINSIZE)
		// 	Array.Copy(window, 0, next.Window, left, WINSIZE - left);

		Array.Copy(window, next.Window, WINSIZE);
		
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
