
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Mem = System.Memory<byte>;

namespace ParallelParsing.Common;

public struct FastqRecord : IDisposable
{
	// public FastqRecord(string id, string seq, string other, string q)
	// {
	// 	Identifier = id;
	// 	Sequence = seq;
	// 	Quality = q;
	// 	Other = other;
	// }

	public FastqRecord(IMemoryOwner<byte> owner, Mem idn, Mem seq, Mem pls, Mem qlt)
	{
		Owner = owner;
		_Identifier = idn;
		_Sequence = seq;
		_Other = pls;
		_Quality = qlt;
	}

	private Memory<byte> _Memory;
	private Mem _Identifier;
	private Mem _Sequence;
	private Mem _Other;
	private Mem _Quality;

	internal IMemoryOwner<byte> Owner;

	public string Identifier => Encoding.ASCII.GetString(_Identifier.Span);
	public string Sequence => Encoding.ASCII.GetString(_Sequence.Span);
	public string Other => Encoding.ASCII.GetString(_Other.Span);
	public string Quality => Encoding.ASCII.GetString(_Quality.Span);

	public void Dispose()
	{
		_Memory.Span.Clear();
		Owner.Dispose();
	}
}