
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing.Benchmark.Naive;

public static class Parsing
{
	public static IEnumerable<FastqRecord> Parse(BigQueue<byte> raw)
	{
		string id;
		string seq;
		string other;
		string quality;
		List<FastqRecord> ret = new List<FastqRecord>(0);

		for (int i = 0; !raw.IsAtEnd; i++)
		{
			// emtry space
			if (raw.Peek() == 0)
			{
				yield break;
			}

			// skip @
			if (raw.Dequeue() != '@') throw new Exception();
			id = ParseLine(raw);
			seq = ParseLine(raw);
			// skip +
			var b = raw.Dequeue();
			if (b != '+') throw new Exception();
			other = ParseLine(raw);
			quality = ParseLine(raw);

			// yield return new FastqRecord(id, seq, other, quality);
		}
	}
	private static string ParseLine(BigQueue<byte> raw)
	{
		var sb = new StringBuilder();

		while (!raw.IsAtEnd && !IsNewLine(raw.Peek()))
		{
			sb.Append((char)raw.Dequeue());
		}

		// consume \n
		if (!raw.IsAtEnd) raw.Dequeue();
		return sb.ToString();
	}

	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}