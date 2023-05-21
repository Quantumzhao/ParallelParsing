using System;
using System.IO;
using System.IO.Compression;
using BenchmarkDotNet.Running;
using ParallelParsing;

public class FileCompressionModeExample
{
    private const string CompressedFileName = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
    private const string DecompressedFileName = "decompressed.txt";

    public static void Main()
    {
        // var summary = BenchmarkRunner.Run<SimpleDecompressor>();

        var instance = new SimpleDecompressor();
        instance.Setup();
        instance.Run();
        instance.Cleanup();

        // var ms = DecompressFile();
        // var entries = FASTQRecord.Parse(ms.GetBuffer());
        // PrintResults(ms);
        // ms.Dispose();
    }

    // private static MemoryStream DecompressFile()
    // {
    //     using FileStream compressedFileStream = File.Open(CompressedFileName, FileMode.Open);
    //     var memoryStream = new MemoryStream();
    //     using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
    //     decompressor.CopyTo(memoryStream);

    //     return memoryStream;
    // }

    // private static void PrintResults(MemoryStream ms)
    // {
    //     long compressedSize = new FileInfo(CompressedFileName).Length;
    //     long decompressedSize = ms.Length;

    //     Console.WriteLine($"The compressed file '{CompressedFileName}' weighs {compressedSize} bytes.");
    //     Console.WriteLine($"The decompressed object weighs {decompressedSize} bytes.");
    //     Console.WriteLine($"Decompressed file size is {(float)decompressedSize/(float)compressedSize} times of the compressed file size.");
    // }

}