
namespace ParallelParsing.ZRan.NET;

public static class IndexIO
{
	// chunksize : uint | chunkMaxBytes : int | count : int | points : [(output : long) 
	// | (input : long) | (bits : int) | (winLen : int) | (win : [byte])]
	public static void Serialize(this Index index, string path)
	{
		using var stream = File.Create(path);
		using var bw = new BinaryWriter(stream);

		bw.Write(index.ChunkSize);
		bw.Write(index.ChunkMaxBytes);

		bw.Write(index.List.Count);

		for (int i = 0; i < index.List.Count; i++)
		{
			bw.Write(index.List[i].Output);
			bw.Write(index.List[i].Input);
			bw.Write(index.List[i].Bits);
			bw.Write(index.List[i].Window.Length);
			bw.Write(index.List[i].Window);
			bw.Write(index.List[i].offset.Length);
			bw.Write(index.List[i].offset);
		}
	}

	public static Index Deserialize(string path)
	{
		using var stream = File.OpenRead(path);
		using var br = new BinaryReader(stream);

		var ChunkSize = br.ReadUInt32();
		var chunkMaxBytes = br.ReadInt32();

		var count = br.ReadInt32();
		var points = new Point[count];

		for (int i = 0; i < count; i++)
		{
			var output = br.ReadInt64();
			var input = br.ReadInt64();
			var bits = br.ReadInt32();
			var winLen = br.ReadInt32();
			var window = br.ReadBytes(winLen);
			var offsetLen = br.ReadInt32();
			var offset = br.ReadBytes(offsetLen);
			points[i] = new Point(output, input, bits, window, offset);
		}

		return new Index(ChunkSize) { 
			List = new List<Point>(points), 
			ChunkMaxBytes = chunkMaxBytes 
		};
	}
}
