
namespace ParallelParsing.Common;

public struct FastqRecord
{
	public FastqRecord(string id, string seq, string other, string q)
	{
		Identifier = id;
		Sequence = seq;
		Quality = q;
		Other = other;
	}

	public readonly string Identifier;
	public readonly string Sequence;
	public readonly string Other;
	public readonly string Quality;
}