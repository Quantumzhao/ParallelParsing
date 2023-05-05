
using System.Text;

namespace ParallelParsing;

public struct FASTQRecord
{
	public FASTQRecord(string id, char[] seq, string other, char[] q)
	{
		Identifier = id;
		Sequence = seq;
		Quality = q;
		Other = other;
	}

	public readonly string Identifier;
	public readonly char[] Sequence;
	public readonly string Other;
	public readonly char[] Quality;

	public static IReadOnlyList<FASTQRecord> Parse(Queue<char> raw)
	{
		string id;
		char[] seq;
		string other;
		char[] quality;
		List<FASTQRecord> ret = new List<FASTQRecord>();
		while (raw.Count != 0)
		{
			// the trailing new lines
			if (raw.Count == 0 || raw.Peek() == '\0') return ret;

			id = ParseID(raw);
			seq = ParseSequence(raw);
			other = ParsePlus(raw);
			quality = ParseQuality(raw);

			ret.Add(new FASTQRecord(id, seq, other, quality));
		}

		return ret;
	}

	private static string ParseID(Queue<char> raw)
	{
		if (!raw.TryDequeue(out var prefix) || prefix !='@')
			throw new InvalidOperationException("This is not a ID");

		var sb = new StringBuilder();

		while (raw.Count != 0)
		{
			var c = raw.Dequeue();
			if (IsNewLine(c)) return sb.ToString();
			else sb.Append(c);
		}

		return sb.ToString();
	}

	private static string ParsePlus(Queue<char> raw)
	{
		if (!raw.TryDequeue(out var prefix) || prefix !='+')
			throw new InvalidOperationException("Field3 error: not starting with +");

		var sb = new StringBuilder();

		while (raw.Count != 0)
		{
			var c = raw.Dequeue();
			if (IsNewLine(c)) return sb.ToString();
			else sb.Append(c);
		}

		return sb.ToString();
	}

	private static char[] ParseSequence(Queue<char> raw)
	{
		var seq = new List<char>();

		while (raw.Count != 0)
		{
			var c = raw.Dequeue();
			if (IsNewLine(c)) return seq.ToArray();
			else if (!char.IsLetter(c)) 
				throw new InvalidOperationException("Sequence contains non-ASCII characters");
			else seq.Add(c);
		}

		return seq.ToArray();
	}

	private static char[] ParseQuality(Queue<char> raw)
	{
		var seq = new List<char>();

		while (raw.Count != 0)
		{
			var c = raw.Dequeue();
			if (IsNewLine(c)) return seq.ToArray();
			else seq.Add(c);
		}

		return seq.ToArray();
	}

	private static void SkipWhiteSpaceNewLine(Queue<char> raw)
	{
		while (raw.Count != 0)
		{
			var c = raw.Dequeue();
			if (char.IsWhiteSpace(c)) continue;
			if (c == '\r' || c == '\n') continue;
			else return;
		}
	}

	private static bool IsNewLine(Queue<char> raw)
	{
		if (raw.TryPeek(out var c1) && c1 == '\r') raw.Dequeue();

		if (raw.TryPeek(out var c2) && c2 == '\n')
		{
			raw.Dequeue();
			return true;
		}

		return false;
	}
	private static bool IsNewLine(char c) => c == '\n' || c == '\r';
}