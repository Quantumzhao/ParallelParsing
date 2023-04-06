
using System.Runtime.InteropServices;
using ParallelParsing.ZRan.NET;
using Index = ParallelParsing.ZRan.NET.Index;

unsafe
{
	var fileName = Marshal.StringToHGlobalAnsi("../Gzipped_FASTQ_Files/SRR11192680.fastq.gz");
	var modes = Marshal.StringToHGlobalAnsi("rb");
	void* file = ExternalCalls.fopen((char*)fileName, (char*)modes);
	var len = Defined.deflate_index_build(file, Constants.SPAN, out var index);
	// Console.WriteLine(len);

	const int LEN = 16384;
	byte* buf = (byte*)NativeMemory.Alloc(LEN);
	int ret;
	if (index != null)
		ret = Defined.deflate_index_extract(file, index, 300, buf, 400);
	Console.WriteLine(Marshal.PtrToStringAnsi((IntPtr)buf));
	// if (index != null)
	// 	Defined.FreeDeflateIndex(index);
	ExternalCalls.fclose(file);
}