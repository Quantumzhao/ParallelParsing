using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.IO.Compression;


//Bug: when there are more than 93 points in index, it will stop running


var testFile = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
// var testFile = "../Gzipped_FASTQ_Files/SRR11192680_original.fastq.gz";
var fs = File.OpenRead(testFile);
var i = Core.BuildDeflateIndex(fs, 200);
fs.Dispose();
// i.Serialize("../Gzipped_FASTQ_Files/test1.fastq.gzi");

fs = File.OpenRead(testFile);
var len_in = i.List[0].Input + 1;
var fileBuffer = new byte[len_in];
var outBuf = new byte[Constants.WINSIZE];
fs.ReadExactly(fileBuffer, 0, (int)len_in);
Core.ExtractDeflateIndex(fs, i, 0, outBuf, (int)len_in);
// Core.ExtractDeflateIndex(fs, i, (int)i.List[1].Output, outBuf, 500);
outBuf.PrintASCII((int)Constants.WINSIZE);

// Core.BuildDeflateIndex(fs, Constants.SPAN, 200);

// var fileName = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
// // var fileName = "../Gzipped_FASTQ_Files/tests/gplv3.txt.gz";
// using var file = File.OpenRead(fileName);
// var index = Core.BuildDeflateIndex(file, IndexIO.Deserialize(fileName).ChunkSize);
// // Console.WriteLine(len);

// const int LEN = 16384;
// byte[] buf = new byte[LEN];
// int ret;
// if (index != null)
// 	ret = Core.ExtractDeflateIndex(file, index, 200, buf, 400);
// Console.WriteLine(Encoding.ASCII.GetString(buf));
