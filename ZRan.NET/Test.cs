using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;
using System.IO.Compression;
using static ParallelParsing.ZRan.NET.Constants;
using static ParallelParsing.ZRan.NET.Compat;
using SDebug = System.Diagnostics.Debug;


//Bug: when there are more than 93 points in index, it will stop running

// var testFile = "../Gzipped_FASTQ_Files/SRR11192680.fastq.gz";
var testFile = "../Gzipped_FASTQ_Files/SRR11192680_original.fastq.gz";
var fs = File.OpenRead(testFile);
// var index = Core.BuildDeflateIndex_OLD(fs, span: 1048576L);
var index = Core.BuildDeflateIndex(fs, chunksize: 2000);
// i.Serialize("../Gzipped_FASTQ_Files/test1.fastq.gzi");


// for (int x = 0; x < index.List.Count - 1; x++)
// {
// 	fs.Position = 0;
// 	var len_in = index.List[x+1].Input - index.List[x].Input;
// 	var from = index.List[x];
// 	var to = index.List[x + 1];
// 	var len_out = (int)(to.Output - from.Output);
// 	var fileBuffer = new byte[len_in];
// 	var outBuf = new byte[2_000_000]; // change size *****************************************
// 	Core.ExtractDeflateIndex_OLD(fs, index, from.Output, outBuf, len_out);
//     outBuf.PrintASCII(2000000);
// }

// index.List[2].Input--;
// index.List[1].Bits = 6;

int x = 1;
fs.Position = 0;
var from = index.List[x];
var to = index.List[x + 1];
var outBuf = new byte[2_000_000]; // change size *****************************************
Core.ExtractDeflateIndex(fs, index, from.Output, outBuf, (int)(to.Output - from.Output));
outBuf.PrintASCII(2000000);

int xx = 0;











































// Core.ExtractDeflateIndex(fs, i, (int)i.List[1].Output, outBuf, (int)i.List[2].Output-(int)i.List[1].Output);
// Core.ExtractDeflateIndex(fs, i, (int)i.List[1].Output, outBuf, 500);


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

// for (int j = 1000; j < 10000; j += 200)
// {
//     var testFile1 = "../Gzipped_FASTQ_Files/SRR11192680_original.fastq.gz";
//     var fs1 = File.OpenRead(testFile1);
//     var i1 = Core.BuildDeflateIndex(fs1, (uint)j);
//     fs1.Dispose();
//     Console.WriteLine($"-------------finished building index with chunk size {j}.");
// }

// using var fs = File.OpenRead(testFile);
// using var ms = new MemoryStream();

// ZResult ret;
// uint have;
// ZStream strm = new ZStream();
// byte[] input = new byte[CHUNK];
// byte[] output = new byte[CHUNK];

// ret = InflateInit(strm, 47);
// SDebug.Assert(ret == ZResult.OK);

// do
// {
// 	strm.AvailIn = (uint)fs.Read(input, 0, (int)CHUNK);

// 	if (strm.AvailIn == 0) break;
// 	strm.NextIn = input;

// 	do
// 	{
// 		strm.AvailOut = CHUNK;
// 		strm.NextOut = output;

// 		ret = Inflate(strm, ZFlush.NO_FLUSH);
// 		SDebug.Assert(ret != ZResult.DATA_ERROR);
// 		SDebug.Assert(ret != ZResult.STREAM_ERROR);

// 		have = CHUNK - strm.AvailOut;
// 		ms.Write(output, 0, (int)CHUNK);

// 	} while (strm.AvailOut == 0);

// } while (ret != ZResult.STREAM_END);

// ret = InflateEnd(strm);

// var str = Encoding.ASCII.GetString(ms.GetBuffer());
// Console.WriteLine(str);