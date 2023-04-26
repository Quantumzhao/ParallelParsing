using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Buffers;

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

        var path = "../../Assignment1/Examples/benchmark9";
        var createStream = () => File.Open("../../Assignment1/Examples/benchmark9", FileMode.Open, 
            FileAccess.Read, FileShare.Read);
        var disposeStream = (FileStream s) => s.Dispose();
        var streams = new FileStream[2];
        streams[0] = createStream();
        streams[1] = createStream();

        var halfLength = (int)streams[0].Length / 2;
        Console.WriteLine(halfLength);
        streams[1].Position = halfLength;
        var buffer = ArrayPool<byte>.Create(halfLength, 2);
        var ss = new string[2];
        Parallel.For(0, 2, i => {
            var b = buffer.Rent(halfLength);
            Console.WriteLine(b.Length);
            streams[i].ReadExactly(b, 0, halfLength);
            ss[i] = Encoding.ASCII.GetString(b);
            Console.WriteLine(ss[i].Substring(0, 100));
            Console.WriteLine(ss[i].Length);
            buffer.Return(b);
        });
        
        streams[0].Dispose();
        streams[1].Dispose();
    }
}







