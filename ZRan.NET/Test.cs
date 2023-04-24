using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.IO.Compression;

var testFile = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
using var fs = File.OpenRead(testFile);
Core.BuildDeflateIndex(fs, Constants.SPAN, 200);
// Core.BuildDeflateIndex(fs, Constants.SPAN, 200);

// var fileName = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
// // var fileName = "../Gzipped_FASTQ_Files/tests/gplv3.txt.gz";
// using var file = File.OpenRead(fileName);
// var index = Core.BuildDeflateIndex(file, Constants.SPAN, IndexIO.Deserialize(fileName).ChunkSize);
// // Console.WriteLine(len);

// const int LEN = 16384;
// byte[] buf = new byte[LEN];
// int ret;
// if (index != null)
// 	ret = Core.ExtractDeflateIndex(file, index, 200, buf, 400);
// Console.WriteLine(Encoding.ASCII.GetString(buf));
