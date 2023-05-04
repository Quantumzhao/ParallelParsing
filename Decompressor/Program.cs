using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Diagnostics;
using Debug = ParallelParsing.ZRan.NET.Debug;

// test file downloaded from
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR11192680
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR21524988 

namespace ParallelParsing;

public class Program
{
    public static int readByteSize = 64;
    static void Main(string[] args)
    {
        var gzipPath = "./../Gzipped_FASTQ_Files/SRR11192680.fastq";
        var fs = File.OpenRead(gzipPath);
        var index = Debug.BuildDummyIndex(fs, 200);
        fs.Dispose();

        fs = File.OpenRead(gzipPath);
        var fastq = new BatchedFASTQ(index, gzipPath, false);
        foreach (var r in fastq)
        {
            r.ToString();
        }
        fs.Dispose();

        // var gzipPath = "something";
        // var indexPath = "somethingelse";
        // using var records = new BatchedFASTQ(indexPath, gzipPath, enableSsdOptimization: false);
        // records.Aggregate(0, (a, x) => a + x.Sequence.Count(c => c == 'A'));
    }
}







