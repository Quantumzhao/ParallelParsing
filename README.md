# CMSC701_Final_Project

### Implement a “compressed checkpoint index”, and a parallel parser for FASTQ files.

Motivation: The FASTQ format is the *lingua franca* for raw input to genomics processing pipelines.  For highly-parallel  processing tasks, simply ingesting and parsing the raw read records fast enough is a substantial bottleneck to efficient data processing.  The  FASTQ format suffers from several issues, but chiefly among them are  that (a) the records do not all consume a fixed and equal number of  bytes and (b) the data is often stored, transferred and processed in a  gzipped format, making parallel processing of independent parts of the  file very difficult.  See, for example, [this paper](https://academic.oup.com/bioinformatics/article/35/3/421/5055585) describing the problem when scaling read aligners to many cores.

Project: In this project, you will devise an algorithm to build a lightweight  index over gzip compressed FASTQ files.  The index will act as a lookup  table into the gzipped file, providing the (byte) offset at the start of each chunk of reads (where the chunk size e.g. 10,000 is an input  parameter), as well as a record of the state of the decoder at this  point in the file.  With this information, truly parallel parsing is  trivial.  Each thread can jump to a separate chunk in the file, set the  decompressor state appropriately, and parse the next chunk of reads.   Ultimately, the goal will be to produce a tool that, given a gzip  compressed FASTQ file, can produce such a lightweight index, as well as a library that, given a gzip compressed FASTQ file and this index, can  efficiently parse the file in parallel.  You will benchmark this parsing against existing (necessarily serialized) parsing to see how much  faster this processing approach is. I expect that, if implemented well,  this could have a substantial *practical* impact for many genomics tools.

> With all of these, the main work will be to make them work well with FASTQ (and FASTA) format files. That is, the index checkpoints should align precisely with *record* boundaries, and when a pair of paired FASTQ files are indexed, the chunks in each should contain identical numbers of records. However, you should be able to use the code above as a jumping off point for all of these modifications.

https://www.youtube.com/watch?v=oi2lMBBjQ8s&ab_channel=BillBird

https://www.youtube.com/watch?v=KJBZyPPTwo0&ab_channel=DevPal

https://en.wikipedia.org/wiki/Database_index

### Gzip Indexing

[How to turn an ordinary gzip archive into a database](https://rushter.com/blog/gzip-indexing/)

[zindex - index and search large gzipped files quickly](https://xania.org/201505/zindex-index-your-gzip-files)

[gztool](https://github.com/circulosmeos/gztool)
