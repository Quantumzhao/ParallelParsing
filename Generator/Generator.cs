
using System.Text;

namespace ParallelParsing.Benchmark.Generator;

public static class Generator
{
	private static Random Rng = new Random(0);
	public static IEnumerable<byte[]> GenerateAll(ulong length)
	{
		uint no = 0;
		while (no < length)
		{
			var sequenceLength = Rng.Next(128, 512);
			yield return GenerateSrrId(sequenceLength, no, '@');
			yield return GenerateSequence(sequenceLength);
			yield return GenerateSrrId(sequenceLength, no, '+');
			yield return GenerateQuality(sequenceLength);
			no++;
		}
	}

	private static byte[] GenerateSequence(int sequenceLength)
	{
		var ret = new byte[sequenceLength + 1];
		for (int i = 0; i < sequenceLength; i++)
		{
			var r = Rng.NextDouble();
			if (r < 0.25) ret[i] = (byte)'A';
			else if (r < 0.5) ret[i] = (byte)'T';
			else if (r < 0.75) ret[i] = (byte)'C';
			else ret[i] = (byte)'G';
		}
		ret[^1] = (byte)'\n';

		return ret;
	}

	private static byte[] GenerateSrrId(int sequenceLength, uint no, char prefix)
	{
		var id = Rng.Next(10_000_000, 20_000_000);
		var major = no / 2 + 1;
		var minor = no % 2 + 1;
		return Encoding.ASCII.GetBytes(
			$"{prefix}SRR{id}.{major}.{minor} {major} length={sequenceLength}\n");
	}

	private static byte[] GenerateQuality(int sequenceLength)
	{
		var ret = new byte[sequenceLength + 1];
		for (int i = 0; i < sequenceLength; i++)
		{
			var r = Rng.NextDouble();
			if (r < 0.9) ret[i] = (byte)'?';
			else if (r < 0.95) ret[i] = (byte)'*';
			else ret[i] = (byte)'!';
		}
		ret[^1] = (byte)'\n';

		return ret;
	}
}