
// using System;
// using System.IO;

// namespace ParallelParsing.GZTool;

// public static class Core
// {
// 	public record struct ActionOptions(
// 		IndexAndExtractionOptions IndexAndExtractionOptions,
// 		ulong Offset,
// 		ulong LineNumberOffset,
// 		ulong SpanBetween_Points,
// 		bool DoesEndOnFirstProperGzipEof,
// 		bool IsAlwaysCreateACompleteIndex,
// 		int WaitingTime,
// 		bool DoesWaitForFileCreation,
// 		bool DoesExtendIndexWithLines,
// 		ulong ExpectedFirstByte,
// 		bool GzipStreamMayBeDamaged,
// 		bool IsLazyGzipStreamPatchingAtEof,
// 		ulong RangeNumberOfBytes,
// 		ulong RangeNumberOfLines
// 	);

// 	public static int CreateIndex(
// 		string fileName, 
// 		Index index, 
// 		string indexFileName, 
// 		ActionOptions opts)
// 	{
// 		FileStream fileIn;
// 		FileStream fileOut;

// 		ulong numberOfIndexPoints = 0;
// 		bool waiting = false;

// 		// First of all, check that data output and index output do not collide:
// 		if (string.IsNullOrEmpty(fileName) && 
// 			string.IsNullOrEmpty(indexFileName) &&
// 			(opts.IndexAndExtractionOptions == IndexAndExtractionOptions.ExtractFromByte ||
// 			opts.IndexAndExtractionOptions == IndexAndExtractionOptions.ExtractFromLine))
// 		{
// 			throw new GenericException(
// 				"ERROR: Please, note that extracted data will be output to STDOUT\n" +
// 				"       so an index file name is needed (`-I`).\nAborted.\n");
// 		}

// 		throw new NotImplementedException();
// 	}
// }

// public static class PlatformDependentMarshal
// {
	
// }