using System.IO.Compression;
using Decompressor;

public class SimpleDecompressor {

    public static void Decompress(String inname, String outname) {
        FileStream input = File.Open(inname, FileMode.Open);
        FileStream output = File.Create(outname);
        var decompressor = new GZipStream(input, CompressionMode.Decompress);
        FastQRecord.Parse(decompressor);
    }

}