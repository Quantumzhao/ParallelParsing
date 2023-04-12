using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

// test file downloaded from
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR11192680
// https://trace.ncbi.nlm.nih.gov/Traces/?view=run_browser&acc=SRR21524988 

namespace ParallelParsing;

public class Program
{
    public static int readByteSize = 64;
    static void Main(string[] args)
    {
        var gzipPath = "something";
        var indexPath = "somethingelse";
        Decompressor.DecompressAll(indexPath, gzipPath).Aggregate(0, 
            (a, x) => a + x.Sequence.Count(c => c == 'A'));
        // byte[] buffer = File.ReadAllBytes("Gzipped_FASTQ_Files/SRR11192680.fastq.gz");

        // byte[] buffer = FileToByteArray("Gzipped_FASTQ_Files/SRR11192680.fastq.gz");
        byte[] buffer = FileToByteArray("../Gzipped_FASTQ_Files/SRR11192680.fastq.gz");
        // byte[] buffer = FileToByteArray("Gzipped_FASTQ_Files/examplefile.gz");

        string outputString = BitConverter.ToString(buffer);
        // Console.WriteLine("byte 0x:   " + outputString);

        byte[] bytes_0_to_9 = new byte[10];
        byte[] bytes_10_to_19 = new byte[10];
        Array.Copy(buffer, 0, bytes_0_to_9, 0, 10);
        Array.Copy(buffer, 10, bytes_10_to_19, 0, 10);
        Console.WriteLine("bytes[0 :: 9] " + BitConverter.ToString(bytes_0_to_9));
        Console.WriteLine("bytes[10::19] " + BitConverter.ToString(bytes_10_to_19));

        // Assume no flags in the header for now
        var bits = new BitArray(new byte[] {buffer[10]});
        string firstByteOfFirstBlock = BitArrayToString(bits);
        Console.WriteLine("10th Byte: "+ BitConverter.ToString(new byte[] {buffer[10]}) + " " + firstByteOfFirstBlock);
        Console.WriteLine("isLastBlock: "+ (firstByteOfFirstBlock[0] == '0' ? "No" : "Yes"));
        string blockType = firstByteOfFirstBlock[1] == '0' ? 
                (firstByteOfFirstBlock[2] == '0' ? "type 0 (no compression)" : "type 2 (compressed with dynamic Huffman codes)") :
                (firstByteOfFirstBlock[2] == '0' ? "type 1 (compressed with fixed Huffman codes)" : "error");
        Console.WriteLine("blockType: " + blockType);
        // 00 -> type 0 (no compression)
        // 01 -> 10 -> type 2 (compressed with dynamic Huffman codes)
        // 10 -> 01 -> type 1 (compressed with fixed Huffman codes)
        
    }

    // Alternative way to read Byte, should be be better at reading large file than File.ReadAllBytes()
    public static byte[] FileToByteArray(string fileName)
    {
        byte[] fileData;

        using (FileStream fs = File.OpenRead(fileName)) 
        { 
            using (BinaryReader binaryReader = new BinaryReader(fs))
            {
                // fileData = binaryReader.ReadBytes((int)fs.Length); 
                fileData = binaryReader.ReadBytes(readByteSize); 
            }
        }
        return fileData;
    }

    public static string BitArrayToString(BitArray bits)
    {
        var stringBuilder = new StringBuilder();
        for (int i = 0; i < bits.Count; i++) 
        {
            char c = bits[i] ? '1' : '0';
            stringBuilder.Append(c);
        }
        return stringBuilder.ToString();
    }
}







