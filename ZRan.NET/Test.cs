using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.IO.Compression;
using static ParallelParsing.ZRan.NET.Constants;
using static ParallelParsing.ZRan.NET.Compat;
using SDebug = System.Diagnostics.Debug;



// var testFile = "../Gzipped_FASTQ_Files/SRR11192680_original.fastq.gz";
var testFile = "../Benchmark/Samples/1536000.gz";
var fs = File.OpenRead(testFile);
var index = Core.BuildDeflateIndex(fs, 6000); //1048576L
// var index = Core.BuildDeflateIndex_OLD(fs, span: 32768); //1048576L
// var index = Core.BuildDeflateIndex(fs, chunksize: 1200);
// i.Serialize("../Gzipped_FASTQ_Files/test1.fastq.gzi");

// Console.WriteLine(Core.BuildDeflateIndex_NEW(fs, span: 32768, 20000).List.Count());
// chunsize 30,000 works for 12288000.gz
