using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Diagnostics;
using Debug = ParallelParsing.ZRan.NET.Debug;
using ParallelParsing.ZRan.NET;

// test file downloaded from
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR11192680
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR21524988 

namespace ParallelParsing;

public class Program
{
    public static int readByteSize = 64;
    static void Main(string[] args)
    {
        var gzipPath = "./../Gzipped_FASTQ_Files/SRR24496856_32.fastq";
        var indexPath = "./../Gzipped_FASTQ_Files/SRR24496856_32.fastq.i";
        // count = 588530
        // As = 37990794

        // var fs = File.OpenRead(gzipPath);
        // var index = Debug.BuildDummyIndex(fs, 5000);
        // index.Serialize("./../Gzipped_FASTQ_Files/SRR24496856_32.fastq.i");
        // fs.Dispose();

        // fs = File.OpenRead(gzipPath);
        var sw = new Stopwatch();
        // var fastq = new BatchedFASTQ(index, gzipPath, false);
        // foreach (var r in fastq)
        // {
        //     r.ToString();
        // }
        // fs.Dispose();

        // var index = IndexIO.Deserialize(indexPath);
        // var count = 0;
        // for (int i = 0; i < index.List.Count - 1; i++)
        // {
        //     count += (int)index.ChunkSize;
        // }
        // Console.WriteLine(count);

        using var records = new BatchedFASTQ(indexPath, gzipPath, enableSsdOptimization: false);
        sw.Start();
        var count = records.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A'));
        // var count = records.Count();
        sw.Stop();
        Console.WriteLine(count);
        Console.WriteLine("Ellapsed: " + sw.ElapsedMilliseconds);

        // sw.Start();
        // var bytes = File.ReadAllBytes(gzipPath);
        // var res = FASTQRecord.Parse(bytes);
        // Console.WriteLine(res.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A')));
        // sw.Stop();
        // Console.WriteLine(sw.ElapsedMilliseconds);
    }
}







