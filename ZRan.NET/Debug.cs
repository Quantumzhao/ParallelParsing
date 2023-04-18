
namespace ParallelParsing.ZRan.NET;

internal static class Debug
{
	public static void Print(this byte[] arr, int first = int.MaxValue)
	{
		for (int i = 0; i < Math.Min(arr.Length, Math.Min(first, 64)); i++)
		{
			Console.Write($"{arr[i].ToString("X2")} ");
		}
		Console.WriteLine();
	}
}