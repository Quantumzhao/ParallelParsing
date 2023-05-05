
namespace ParallelParsing.ZRan.NET;

public static class Debug
{
	public static void Print(this byte[] arr, int first = int.MaxValue)
	{
		for (int i = 0; i < Math.Min(arr.Length, Math.Min(first, 10240)); i++)
		{
			Console.Write($"{arr[i].ToString("X2")} ");
		}
		Console.WriteLine();
	}

	public static void PrintASCII(this byte[] arr, int first)
	{
		Console.Write(System.Text.Encoding.UTF8.GetString(arr).Substring(0, first));
		Console.WriteLine();
	}

	public static void PrintASCIIFromTo(this byte[] arr, int from, int len)
	{
		Console.Write(System.Text.Encoding.UTF8.GetString(arr).Substring(from, len));
		Console.WriteLine();
	}

	public static void PrintASCIIFirstAndLast(this byte[] arr, int first)
	{
		Console.WriteLine("------------------------- first " + first + " characters--------------------------");
		Console.Write(System.Text.Encoding.UTF8.GetString(arr).Substring(0, first));
		Console.WriteLine();
		Console.WriteLine("-------------------------- last " + first + " characters--------------------------");
		Console.Write(System.Text.Encoding.UTF8.GetString(arr).Substring(Math.Max(0, arr.Length - first)));
		Console.WriteLine();
	}

	public static Index BuildDummyIndex(FileStream file, uint chunksize)
	{
		using var sr = new StreamReader(file);
		var index = new Index(chunksize);
		
		var newLineCounter = 0;
		while (file.Position != file.Length)
		{
			var b = (byte)file.ReadByte();
			if (b == '\n') newLineCounter++;

			if (newLineCounter == 4 * chunksize)
			{
				index.AddPoint(0, file.Position, file.Position, new byte[Constants.WINSIZE]);
				newLineCounter = 0;
			}
		}

		return index;
	}

	public static void ExtractDummyRange(byte[] fileBuffer, Point from, Point to, byte[] buf)
	{
		Array.Copy(fileBuffer, buf, to.Input - from.Input);
	}
}