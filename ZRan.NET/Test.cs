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
// var index = Core.BuildDeflateIndex(fs, 6000); //1048576L
// var index = Core.BuildDeflateIndex_OLD(fs, span: 32768); //1048576L
// var index = Core.BuildDeflateIndex(fs, chunksize: 1200);
// i.Serialize("../Gzipped_FASTQ_Files/test1.fastq.gzi");

var index = IndexIO.Deserialize("../Benchmark/Samples/index/1536000_20k.gzi");
var file = File.Create("../Benchmark/tmp/1536000_20k");

for (int x = 0; x < index.Count - 1; x++)
{
	var from = index[x];
	var to = index[x + 1];
	var len_in = to.Input - from.Input + 1;
	var len_out = (int)(to.Output - from.Output);
	var fileBuffer = new byte[len_in];
	fs.Position = from.Input - 1;
	fs.ReadExactly(fileBuffer, 0, (int)len_in);
	var outBuf = new byte[len_out];
	Core.ExtractDeflateIndex(fileBuffer, from, to, outBuf);
	// Console.WriteLine(x);
	file.Write(outBuf);
}





// var fileNames = new List<string>(){
//     "48000"

//     // // "96000",
//     // // "192000",
//     // // "384000",
//     // // "768000",
//     // // "1536000",
//     // // "3072000",
//     // // "6144000"

//     // // "12288000",
    
//     // "24576000",
//     // "49152000",
//     // "98304000",
//     // "196608000"
// };

// IDictionary<int, string> chunkSizes = new Dictionary<int, string>() {
//     {20000, "20k"}, {50000, "50k"}, {100000, "100k"}, {200000, "200k"}, {1000000, "1000k"}
// };

// foreach (var filename in fileNames)
// {   
//     foreach (var chunk in chunkSizes)
//     {
//         var testFile = "../Benchmark/Samples/" + filename + ".gz";
//         var fs = File.OpenRead(testFile);
//         var index = Core.BuildDeflateIndex(fs, (uint)chunk.Key); 
//         Console.WriteLine(index.Count);
//         index.Serialize("../Benchmark/Samples/index/" + filename + "_" + chunk.Value + ".gzi");
//         fs.Dispose();
//     }
// }
