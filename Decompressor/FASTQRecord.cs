
using System.Runtime.CompilerServices;
using System.Text;

namespace ParallelParsing;

public unsafe struct FASTQRecord
{
	public FASTQRecord(string id, string seq, string other, string q)
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

	// public static IReadOnlyList<FASTQRecord> Parse(Queue<char> raw)
	// {
	// 	string id;
	// 	char[] seq;
	// 	string other;
	// 	char[] quality;
	// 	List<FASTQRecord> ret = new List<FASTQRecord>();
	// 	while (raw.Count != 0)
	// 	{
	// 		// the trailing new lines
	// 		if (raw.Count == 0 || raw.Peek() == '\0') return ret;

	// 		id = ParseID(raw);
	// 		seq = ParseSequence(raw);
	// 		other = ParsePlus(raw);
	// 		quality = ParseQuality(raw);

	// 		ret.Add(new FASTQRecord(id, seq, other, quality));
	// 	}

	// 	return ret;
	// }

	public static IReadOnlyList<FASTQRecord> Parse(byte[] raw)
	{
		string id;
		string seq;
		string other;
		string quality;
		List<FASTQRecord> ret = new List<FASTQRecord>(256);

		fixed (byte* start = raw)
		{
			var curr = start;
			for (int i = 0; (curr - start) < raw.Length; i++)
			{
				// emtry space
				if (*curr == '\0') return ret;

				// skip @
				curr++;
				id = ParseLine(&curr);
				seq = ParseLine(&curr);
				// skip +
				curr++;
				other = ParseLine(&curr);
				quality = ParseLine(&curr);

				ret.Add(new FASTQRecord(id, seq, other, quality));
			}

		}

		return ret;
	}
	private static string ParseLine(byte** currChar)
	{
		var sb = new StringBuilder();

		while (!IsNewLine(**currChar))
		{
			if (IsNewLine(**currChar)) break;
			else sb.Append((char)**currChar);

			(*currChar)++;
		}

		// consume \n
		(*currChar)++;
		return sb.ToString();
	}


	// private static string ParseID(byte** currChar)
	// {
	// 	if (**currChar != '@') throw new InvalidOperationException("This is not a ID");

	// 	var sb = new StringBuilder();

	// 	while (!IsNewLine(**currChar))
	// 	{
	// 		if (IsNewLine(**currChar)) break;
	// 		else sb.Append(**currChar);

	// 		(*currChar)++;
	// 	}

	// 	return sb.ToString();
	// }

	// private static string ParsePlus(byte** currChar)
	// {
	// 	var sb = new StringBuilder();

	// 	while (!IsNewLine(**currChar))
	// 	{
	// 		if (IsNewLine(**currChar)) break;
	// 		else sb.Append(**currChar);

	// 		(*currChar)++;
	// 	}

	// 	return sb.ToString();
	// }

	// private static string ParseSequence(byte[] raw, ref int* offset)
	// {
	// 	var sb = new StringBuilder();

	// 	for (; *offset != raw.Length; offset++)
	// 	{
	// 		var c = (char)raw[offset];
	// 		if (IsNewLine(c)) return sb.ToString();
	// 		else if (!char.IsLetter(c)) 
	// 			throw new InvalidOperationException("Sequence contains non-ASCII characters");
	// 		else sb.Append(c);
	// 	}

	// 	return sb.ToString();
	// }

	// private static char[] ParseQuality(byte[] raw)
	// {
	// 	var seq = new List<char>();

	// 	while (raw.Count != 0)
	// 	{
	// 		var c = raw.Dequeue();
	// 		if (IsNewLine(c)) return seq.ToArray();
	// 		else seq.Add(c);
	// 	}

	// 	return seq.ToArray();
	// }

	// private static void SkipWhiteSpaceNewLine(Queue<char> raw)
	// {
	// 	while (raw.Count != 0)
	// 	{
	// 		var c = raw.Dequeue();
	// 		if (char.IsWhiteSpace(c)) continue;
	// 		if (c == '\r' || c == '\n') continue;
	// 		else return;
	// 	}
	// }

	// private static bool IsNewLine(Queue<char> raw)
	// {
	// 	if (raw.TryPeek(out var c1) && c1 == '\r') raw.Dequeue();

	// 	if (raw.TryPeek(out var c2) && c2 == '\n')
	// 	{
	// 		raw.Dequeue();
	// 		return true;
	// 	}

	// 	return false;
	// }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}