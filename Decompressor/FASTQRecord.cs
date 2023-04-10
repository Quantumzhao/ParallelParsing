
namespace ParallelParsing;

public sealed record class FASTQRecord
{
	public FASTQRecord(string id, string desc, char[] seq, char[] q)
	{
		Identifier = id;
		Description = desc;
		Sequence = seq;
		Quality = q;
	}

	public readonly string Identifier;
	public readonly string Description;
	public readonly char[] Sequence;
	public readonly char[] Quality;

	public static List<FASTQRecord> Parse(string raw)
	{
		throw new NotImplementedException();
	}
}