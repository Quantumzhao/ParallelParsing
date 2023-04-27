using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Diagnostics;

// test file downloaded from
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR11192680
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR21524988 

namespace ParallelParsing;

public class Program
{
    public static int readByteSize = 64;
    static void Main(string[] args)
    {
        // var gzipPath = "something";
        // var indexPath = "somethingelse";
        // using var records = new BatchedFASTQ(indexPath, gzipPath, enableSsdOptimization: false);
        // records.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A'));

        ParallelTest();
        SequentialTest();
    }

    static FileStream CreateStream()
    {
        return File.Open("../Gzipped_FASTQ_Files/SRR11192680_original.fastq", FileMode.Open, 
            FileAccess.Read, FileShare.Read);
    }

    static void ParallelTest()
    {
        var streams = new FileStream[2];
        streams[0] = CreateStream();
        streams[1] = CreateStream();

        var halfLength = (int)streams[0].Length / 2;
        Console.WriteLine($"halfLength : {halfLength}");
        streams[1].Position = halfLength;

        var buffer = ArrayPool<byte>.Create(halfLength, 2);
        var ss = new string[2];

        var sw = new Stopwatch();
        sw.Start();
        Parallel.For(0, 2, i => {
            var b = buffer.Rent(halfLength);
            streams[i].ReadExactly(b, 0, halfLength);
            ss[i] = Encoding.ASCII.GetString(b);
            // Console.WriteLine(ss[i][(halfLength - 100)..halfLength]);
            // Console.WriteLine(ss[i].Length);
            buffer.Return(b);
        });
        sw.Stop();
        Console.WriteLine($"parallel: {sw.ElapsedMilliseconds}");
        
        streams[0].Dispose();
        streams[1].Dispose();
    }

    static void SequentialTest()
    {
        var stream = CreateStream();
        var buf = new byte[stream.Length];

        var sw = new Stopwatch();
        sw.Start();
        stream.ReadAtLeast(buf, (int)stream.Length, false);
        var s = Encoding.ASCII.GetString(buf);
        sw.Stop();
        Console.WriteLine($"sequential: {sw.ElapsedMilliseconds}");
        stream.Dispose();
    }
}







