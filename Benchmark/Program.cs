using System;
using System.IO;
using System.IO.Compression;

public class FileCompressionModeExample
{
    private const string CompressedFileName = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
    private const string DecompressedFileName = "decompressed.txt";

    public static void Main()
    {
        DecompressFile();
        PrintResults();
    }


    private static void DecompressFile()
    {
        using FileStream compressedFileStream = File.Open(CompressedFileName, FileMode.Open);
        using FileStream outputFileStream = File.Create(DecompressedFileName);
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
        decompressor.CopyTo(outputFileStream);
    }

    private static void PrintResults()
    {
        long compressedSize = new FileInfo(CompressedFileName).Length;
        long decompressedSize = new FileInfo(DecompressedFileName).Length;

        Console.WriteLine($"The compressed file '{CompressedFileName}' weighs {compressedSize} bytes.");
        Console.WriteLine($"The decompressed file '{DecompressedFileName}' weighs {decompressedSize} bytes.");
        Console.WriteLine($"Decompressed file size is {(float)decompressedSize/(float)compressedSize} times of the compressed file size.");
    }

}