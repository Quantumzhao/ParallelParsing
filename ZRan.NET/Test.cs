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
// var fs = File.OpenRead(testFile);
// var i = Core.BuildDeflateIndex(fs, span: 2000);
// i.Serialize("../Gzipped_FASTQ_Files/test1.fastq.gzi");


// fs = File.OpenRead(testFile);
// var len_in = i.List[1].Input + 1;
// var fileBuffer = new byte[len_in];
// var outBuf = new byte[Constants.WINSIZE];
// fs.Position = i.List[0].Input;
// fs.ReadExactly(fileBuffer, 0, (int)len_in-(int)i.List[0].Input);
// Core.ExtractDeflateRange2(fileBuffer, i.List[0], i.List[1], outBuf);
// outBuf.PrintASCII((int)Constants.WINSIZE);

// fs = File.OpenRead(testFile);
// var len_in = i.List[1].Input - i.List[0].Input;
// var fileBuffer = new byte[len_in];
// var outBuf = new byte[Constants.WINSIZE];
// fs.Position = i.List[0].Input;
// fs.ReadExactly(fileBuffer, 0, (int)i.List[1].Input - (int)i.List[0].Input);
// Core.ExtractDeflateRange2(fileBuffer, i.List[0], i.List[1], outBuf);


// int x = 8;
// fs.Position = 0;
// var len_in = i.List[x+1].Input - i.List[x].Input;
// var from = i.List[x];
// var to = i.List[x + 1];
// var len_out = (int)(to.Output - from.Output);
// var fileBuffer = new byte[len_in];
// var outBuf = new byte[4_000_000]; // change size *****************************************
// fs.Position = i.List[x].Input;
// fs.ReadExactly(fileBuffer, 0, (int)i.List[x+1].Input - (int)i.List[x].Input);
// Core.ExtractDeflateRange2(fileBuffer, i.List[x], i.List[x+1], outBuf);
// Core.ExtractDeflateIndex(fs, i, from.Output, outBuf, len_out);
// Console.WriteLine(Encoding.ASCII.GetString(outBuf));
// outBuf.PrintASCII(500);












































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

using var fs = File.OpenRead(testFile);
using var ms = new MemoryStream();

ZResult ret;
uint have;
ZStream strm = new ZStream();
byte[] input = new byte[CHUNK];
byte[] output = new byte[CHUNK];

ret = InflateInit(strm, 47);
SDebug.Assert(ret == ZResult.OK);

do
{
	strm.AvailIn = (uint)fs.Read(input, 0, (int)CHUNK);

	if (strm.AvailIn == 0) break;
	strm.NextIn = input;

	do
	{
		strm.AvailOut = CHUNK;
		strm.NextOut = output;

		ret = Inflate(strm, ZFlush.NO_FLUSH);
		SDebug.Assert(ret != ZResult.DATA_ERROR);
		SDebug.Assert(ret != ZResult.STREAM_ERROR);

		have = CHUNK - strm.AvailOut;
		ms.Write(output, 0, (int)CHUNK);

	} while (strm.AvailOut == 0);

} while (ret != ZResult.STREAM_END);

InflateEnd(strm);

var str = Encoding.ASCII.GetString(ms.GetBuffer());
Console.WriteLine(str);