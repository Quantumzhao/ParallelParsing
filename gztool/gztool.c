//
// gztool
//
// Create small indexes for gzipped files and use them
// for quick and random data extraction.
// No more waiting when the end of a 10 GiB gzip is needed!
//
// based on parts from
// zlib/examples/zran.c & zlib/examples/zpipe.c by Mark Adler
// //github.com/madler/zlib
//
//.................................................
//
// LICENSE:
//
// v0.1 to v1.5* by Roberto S. Galende, 2019, 2020, 2021, 2022
// //github.com/circulosmeos/gztool
// A work by Roberto S. Galende
// distributed under the same License terms covering
// zlib from Mark Adler (aka Zlib license):
//   This software is provided 'as-is', without any express or implied
//   warranty.  In no event will the authors be held liable for any damages
//   arising from the use of this software.
//   Permission is granted to anyone to use this software for any purpose,
//   including commercial applications, and to alter it and redistribute it
//   freely, subject to the following restrictions:
//   1. The origin of this software must not be misrepresented; you must not
//      claim that you wrote the original software. If you use this software
//      in a product, an acknowledgment in the product documentation would be
//      appreciated but is not required.
//   2. Altered source versions must be plainly marked as such, and must not be
//      misrepresented as being the original software.
//   3. This notice may not be removed or altered from any source distribution.
//
// License contained in zlib.h by Mark Adler is copied here:
//
/* zlib.h -- interface of the 'zlib' general purpose compression library
  version 1.2.11, January 15th, 2017
  Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.
  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:
  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.
  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu
  The data format used by the zlib library is described by RFCs (Request for
  Comments) 1950 to 1952 in the files http://tools.ietf.org/html/rfc1950
  (zlib format), rfc1951 (deflate format) and rfc1952 (gzip format).
*/
//.................................................
//
// Original zran.c notice is copied here:
//
/* zran.c -- example of zlib/gzip stream indexing and random access
 * Copyright (C) 2005, 2012 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
   Version 1.1  29 Sep 2012  Mark Adler */

/* Version History:
 1.0  29 May 2005  First version
 1.1  29 Sep 2012  Fix memory reallocation error
 */

/* Illustrate the use of Z_BLOCK, inflatePrime(), and inflateSetDictionary()
   for random access of a compressed file.  A file containing a zlib or gzip
   stream is provided on the command line.  The compressed stream is decoded in
   its entirety, and an index built with access points about every SPAN bytes
   in the uncompressed output.  The compressed file is left open, and can then
   be read randomly, having to decompress on the average SPAN/2 uncompressed
   bytes before getting to the desired block of data.
   An access point can be created at the start of any deflate block, by saving
   the starting file offset and bit of that block, and the 32K bytes of
   uncompressed data that precede that block.  Also the uncompressed offset of
   that block is saved to provide a reference for locating a desired starting
   point in the uncompressed stream.  build_index() works by decompressing the
   input zlib or gzip stream a block at a time, and at the end of each block
   deciding if enough uncompressed data has gone by to justify the creation of
   a new access point.  If so, that point is saved in a data structure that
   grows as needed to accommodate the points.
   To use the index, an offset in the uncompressed data is provided, for which
   the latest access point at or preceding that offset is located in the index.
   The input file is positioned to the specified location in the index, and if
   necessary the first few bits of the compressed data is read from the file.
   inflate is initialized with those bits and the 32K of uncompressed data, and
   the decompression then proceeds until the desired offset in the file is
   reached.  Then the decompression continues to read the desired uncompressed
   data from the file.
   Another approach would be to generate the index on demand.  In that case,
   requests for random access reads from the compressed data would try to use
   the index, but if a read far enough past the end of the index is required,
   then further index entries would be generated and added.
   There is some fair bit of overhead to starting inflation for the random
   access, mainly copying the 32K byte dictionary.  So if small pieces of the
   file are being accessed, it would make sense to implement a cache to hold
   some lookahead and avoid many calls to extract() for small lengths.
   Another way to build an index would be to use inflateCopy().  That would
   not be constrained to have access points at block boundaries, but requires
   more memory per access point, and also cannot be saved to file due to the
   use of pointers in the state.  The approach here allows for storage of the
   index in a file.
 */
//

// .................................................
// large file support (LFS) (files with size >2^31 (2 GiB) in linux, and >4 GiB in Windows)
#define _FILE_OFFSET_BITS 64 // defining _FILE_OFFSET_BITS with the value 64 (before including
                             // any header files) will turn off_t into a 64-bit type
#define _LARGEFILE_SOURCE
#define _LARGEFILE64_SOURCE // off64_t for fseek64
// .................................................

// #define COMPILING_DEBIAN_PACKAGE
#ifdef COMPILING_DEBIAN_PACKAGE
#include <config.h>
#else
#define PACKAGE_NAME "gztool"
#define PACKAGE_VERSION "1.5.1"
#endif

#define _XOPEN_SOURCE 500 // expose <unistd.h>'s pread()
                          // and <stdio.h>'s fileno()

#include <stdint.h> // uint32_t, uint64_t, UINT32_MAX
#include <stdio.h>
#include <stdarg.h> // va_start, va_list, va_end
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <zlib.h>
#include <unistd.h>   // getopt(), access(), sleep(), pread()
#include <getopt.h>   // getopt(): optarg, optopt, optind
#include <ctype.h>    // isprint()
#include <sys/stat.h> // stat()
#include <math.h>     // pow()
#include <stdbool.h>  // bool, false, true

// sets binary mode for stdin in Windows
#define STDIN 0
#define STDOUT 1
#ifdef _WIN32
#include <io.h>
#include <fcntl.h>
#define SET_BINARY_MODE(handle) setmode(handle, O_BINARY)
#else
#define SET_BINARY_MODE(handle) ((void)0)
#endif

#define local static

#define SPAN 10485760L                             /* desired distance between access points */
#define WINSIZE 32768U                             /* sliding window size */
#define CHUNK 16384                                /* file input buffer size */
#define UNCOMPRESSED_WINDOW UINT32_MAX             // window is an uncompressed WINSIZE size window
#define GZIP_INDEX_IDENTIFIER_STRING "gzipindx"    // default index version (v0)
#define GZIP_INDEX_IDENTIFIER_STRING_V1 "gzipindX" // index version with line number info
#define GZIP_INDEX_HEADER_SIZE 16                  // header size in bytes of gztool's .gzi files
#define GZIP_HEADER_SIZE_BY_ZLIB 10                // header size in bytes of gzip files created by zlib:
                                                   // github.com/madler/zlib/blob/master/zlib.h
//      If deflateSetHeader is not used, the default gzip header has text false,
//    the time set to zero, and os set to 255, with no extra, name, or comment
//    fields.
// default waiting time in seconds when supervising a growing gzip file:
#define WAITING_TIME 4
// how many CHUNKs will be decompressed in advance if it is needed (parameter gzip_stream_may_be_damaged, `-p`)
#define CHUNKS_TO_DECOMPRESS_IN_ADVANCE 3
// how many CHUNKs will be look to backwards for a new good gzip reentry point after an error is found (with `-p`)
#define CHUNKS_TO_LOOK_BACKWARDS 3

/* access point entry */
struct point
{
    uint64_t out;              /* corresponding offset in uncompressed data */
    uint64_t in;               /* offset in input file of first full byte */
    uint32_t bits;             /* number of bits (1-7) from byte at in - 1, or 0 */
    uint64_t window_beginning; /* offset at index file where this compressed window is stored */
    uint32_t window_size;      /* size of (compressed) window */
    unsigned char *window;     /* preceding 32K of uncompressed data, compressed */
    // index v1:
    uint64_t line_number; /* if index_version == 1 stores line number at this index point */
};
// NOTE: window_beginning is not stored on disk, it's an on-memory-only value

/* access point list */
struct access
{
    uint64_t have;      /* number of list entries filled in */
    uint64_t size;      /* number of list entries allocated */
    uint64_t file_size; /* size of uncompressed file (useful for bgzip files) */
    struct point *list; /* allocated list */
    char *file_name;    /* path to index file */
    int index_complete; /* 1: index is complete; 0: index is (still) incomplete */
    // index v1:
    int index_version;           /* 0: default; 1: index with line numbers */
    uint32_t line_number_format; /* 0: linux \r | windows \n\r; 1: mac \n */
    uint64_t number_of_lines;    /* number of lines (only used with v1 index format) */
};
// NOTE: file_name, index_complete and index_version are not stored on disk (on-memory-only values)

/* generic struct to return a function error code and a value */
struct returned_output
{
    uint64_t value;
    int error;
};

enum EXIT_RETURNED_VALUES
{
    // used for app exit values:
    EXIT_OK = 0,
    EXIT_GENERIC_ERROR = 1,
    EXIT_INVALID_OPTION = 2,

    // used with returned_output.error, not in app exit values:
    // +100 not to crush with Z_* values (zlib): //www.zlib.net/manual.html
    // (and also not to crush with GZIP_MARK_FOUND_TYPE values... though
    // they are used only for decompress_in_advance()'s ret.error value)
    EXIT_FILE_OVERWRITTEN = 100,
};

enum INDEX_AND_EXTRACTION_OPTIONS
{
    JUST_CREATE_INDEX = 1,
    EXTRACT_FROM_BYTE,
    COMPRESS_AND_CREATE_INDEX,
    EXTRACT_FROM_LINE
};

enum ACTION
{
    ACT_NOT_SET,
    ACT_EXTRACT_FROM_BYTE,
    ACT_CREATE_INDEX,
    ACT_EXTRACT_FROM_LINE
};

// enum VERBOSITY_LEVEL { VERBOSITY_NONE = 0, VERBOSITY_NORMAL = 1, VERBOSITY_EXCESSIVE = 2,
//                        VERBOSITY_MANIAC = 3, VERBOSITY_CRAZY = 4, VERBOSITY_NUTS = 5 };

// enum VERBOSITY_LEVEL verbosity_level = VERBOSITY_NORMAL;

// values returned by decompress_in_advance() in ret.error value:
enum GZIP_MARK_FOUND_TYPE
{
    GZIP_MARK_ERROR        // GZIP_MARK_ERROR informs that recovery is not possible, but
                           // also that process will try to continue without recovery.
    = 8,                   // All values greater than Z_* zlib's return codes, because
                           // a ret.error is used at decompress_in_advance() for both
                           // zlib's outputs and the function output (GZIP_MARK_FOUND_TYPE).
    GZIP_MARK_FATAL_ERROR, // This value aborts process, 'cause a compulsory fseeko() failed.
    GZIP_MARK_NONE,        // Decompress_in_advance() didn't find an error in gzip stream (all ok!).
    GZIP_MARK_BEGINNING,   // An error was found, and a new complete gzip stream was found
                           // to reinitiate process at ret.value byte.
    GZIP_MARK_FULL_FLUSH   // An error was found, and a new gzip-Z_FULL_FLUSH was found
                           // to reinitiate process at ret.value byte.
};

// values used to initialize or continue processing with decompress_in_advance() function:
enum DECOMPRESS_IN_ADVANCE_INITIALIZERS
{
    DECOMPRESS_IN_ADVANCE_RESET,     // initial total reset
    DECOMPRESS_IN_ADVANCE_CONTINUE,  // no reset, just continue processing
    DECOMPRESS_IN_ADVANCE_SOFT_RESET // reset all but last_correct_reentry_point_returned
                                     // in order to continue processing the same gzip stream.
};

// shared string for printing unit conversions without dealing with pointers:
// Always %.2f : "1234.67 Bytes\0" = 14 chars maximum
#define MAX_giveMeSIUnits_RETURNED_LENGTH 14
char number_output[MAX_giveMeSIUnits_RETURNED_LENGTH];

// `fprintf` substitute for printing with VERBOSITY_LEVEL
void printToStderr(const char *format, ...)
{

    va_list args;
    va_start(args, format);
    vfprintf(stderr, format, args);
    va_end(args);
}

// Convert an integer to a short string (2 decimal places) using SI units.
// Please, note that this function uses always "number_output" global variable as output.
// INPUT:
// uint64_t input_number        : number to convert to binary SI units
// int binary                   : 0: decimal suffixes, 1: binary suffixes
// OUTPUT:
// pointer to buffer where string will be written:
// It is ALWAYS the "number_output" global variable.
char *giveMeSIUnits(uint64_t input_number, int binary)
{

    int i;
    double number = (double)input_number;
    double BASE = ((binary == 1) ? 1024.0 : 1000.0);

    char *SI_UNITS[7] =
        {"", "k", "M", "G", "T", "P", "E"};
    char *SI_BINARY_UNITS[7] =
        {"Bytes", "kiB", "MiB", "GiB", "TiB", "PiB", "EiB"};

    // totally empty string so that
    // end-of-string char is always correctly set no matter the number
    for (i = 0; i < MAX_giveMeSIUnits_RETURNED_LENGTH; i++)
        number_output[i] = '\0';

    for (i = 0; number >= BASE; i++)
        number /= BASE;

    snprintf(number_output, MAX_giveMeSIUnits_RETURNED_LENGTH,
             "%.2f %s", number, ((binary == 1) ? SI_BINARY_UNITS[i] : SI_UNITS[i]));

    return number_output;
}

/**************
 * Endianness *
 **************/
static inline int endiannes_is_big(void)
{
    long one = 1;
    return !(*((char *)(&one)));
}
static inline uint32_t endiannes_swap_4(uint32_t v)
{
    v = ((v & 0x0000FFFFU) << 16) | (v >> 16);
    return ((v & 0x00FF00FFU) << 8) | ((v & 0xFF00FF00U) >> 8);
}
static inline void *endiannes_swap_4p(void *x)
{
    *(uint32_t *)x = endiannes_swap_4(*(uint32_t *)x);
    return x;
}
static inline uint64_t endiannes_swap_8(uint64_t v)
{
    v = ((v & 0x00000000FFFFFFFFLLU) << 32) | (v >> 32);
    v = ((v & 0x0000FFFF0000FFFFLLU) << 16) | ((v & 0xFFFF0000FFFF0000LLU) >> 16);
    return ((v & 0x00FF00FF00FF00FFLLU) << 8) | ((v & 0xFF00FF00FF00FF00LLU) >> 8);
}
static inline void *endiannes_swap_8p(void *x)
{
    *(uint64_t *)x = endiannes_swap_8(*(uint64_t *)x);
    return x;
}

// fread() substitute for endianness management of 4 and 8 bytes words
// Data in *ptr will be received as this target system waits them
// independently of how they were stored (always as big endian).
// INPUT:
// void * ptr   : pointer to buffer where data will be received
// size_t size  : size of data in bytes
// FILE * stream: stream to read
// OUTPUT:
// number of bytes read
local size_t fread_endian(void *ptr, size_t size, FILE *stream)
{

    size_t output = fread(ptr, size, 1, stream);

    if (endiannes_is_big())
    {
        ;
    }
    else
    {
        // machine is little endian: flip bytes
        switch (size)
        {
        case 4:
            endiannes_swap_4p(ptr);
            break;
        case 8:
            endiannes_swap_8p(ptr);
            break;
        default:
            assert(0);
        }
    }

    return output;
}

// fwrite() substitute for endianness management of 4 and 8 bytes words
// Data in *ptr will be stored as big endian
// independently of how they were stored in this system.
// INPUT:
// void * ptr   : pointer to buffer where data is stored
// size_t size  : size of data in bytes
// FILE * stream: stream to write
// OUTPUT:
// number of bytes written
local size_t fwrite_endian(void *ptr, size_t size, FILE *stream)
{

    size_t output;
    void *endian_ptr;

    endian_ptr = malloc(size);

    if (endiannes_is_big())
    {
        memcpy(endian_ptr, ptr, size);
    }
    else
    {
        // machine is little endian: flip bytes
        switch (size)
        {
        case 4:
            *(uint32_t *)endian_ptr = *(uint32_t *)ptr;
            endiannes_swap_4p(endian_ptr);
            break;
        case 8:
            *(uint64_t *)endian_ptr = *(uint64_t *)ptr;
            endiannes_swap_8p(endian_ptr);
            break;
        default:
            assert(0);
        }
    }

    output = fwrite(endian_ptr, size, 1, stream);

    free(endian_ptr);
    return output;
}
/*******************
 * end: Endianness *
 *******************/

// Compress source stream using zlib format.
// Used internally to compress the index' windows.
// based on def() from zlib/examples/zpipe.c by Mark Adler :
/* Compress from file source to file dest until EOF on source.
   def() returns Z_OK on success, Z_MEM_ERROR if memory could not be
   allocated for processing, Z_STREAM_ERROR if an invalid compression
   level is supplied, Z_VERSION_ERROR if the version of zlib.h and the
   version of the library linked do not match, or Z_ERRNO if there is
   an error reading or writing the files. */
// INPUT:
// unsigned char *source: pointer to data
// uint64_t *size       : size of data in bytes
// int level            : level of compression
// OUTPUT:
// pointer to compressed data (NULL on error)
local unsigned char *compress_chunk(unsigned char *source, uint64_t *size, int level)
{
    int ret, flush;
    unsigned have;
    z_stream strm;
    uint64_t i = 0;
    uint64_t output_size = 0;
    uint64_t input_size = *size;
    unsigned char *in, *out, *out_complete;

    /* allocate deflate state */
    strm.zalloc = Z_NULL;
    strm.zfree = Z_NULL;
    strm.opaque = Z_NULL;
    ret = deflateInit(&strm, level);
    if (ret != Z_OK)
        return NULL;

    in = malloc(CHUNK);
    out = malloc(CHUNK);
    out_complete = malloc(CHUNK);

    /* compress until end of input */
    do
    {
        strm.avail_in = ((i + CHUNK) < input_size) ? CHUNK : (input_size - i);
        if (memcpy(in, source + i, strm.avail_in) == NULL)
        {
            (void)deflateEnd(&strm);
            goto compress_chunk_error;
        }
        flush = ((i + CHUNK) >= input_size) ? Z_FINISH : Z_NO_FLUSH;
        strm.next_in = in;
        i += strm.avail_in;

        /* run deflate() on input until output buffer not full, finish
           compression if all of source has been read in */
        do
        {
            strm.avail_out = CHUNK;
            strm.next_out = out;
            ret = deflate(&strm, flush);   /* no bad return value */
            assert(ret != Z_STREAM_ERROR); /* state not clobbered */
            have = CHUNK - strm.avail_out;
            if (have != 0 && (NULL == (out_complete = realloc(out_complete, output_size + have)) ||
                              NULL == memcpy(out_complete + output_size, out, have)))
            {
                (void)deflateEnd(&strm);
                goto compress_chunk_error;
            }
            output_size += have;
        } while (strm.avail_out == 0);
        assert(strm.avail_in == 0); /* all input will be used */

        /* done when last data in source processed */
    } while (flush != Z_FINISH);
    assert(ret == Z_STREAM_END); /* stream will be complete */

    /* clean up and return */
    (void)deflateEnd(&strm);
    free(in);
    free(out);
    // return size of returned char array in size pointer parameter
    *size = output_size;
    return out_complete;

compress_chunk_error:
    free(in);
    free(out);
    free(out_complete);
    return NULL;
}

// Decompress a source stream with zlib format.
// Used internally to decompress the index' windows.
// based on inf() from zlib/examples/zpipe.c by Mark Adler
/* Decompress from file source to file dest until stream ends or EOF.
   inf() returns Z_OK on success, Z_MEM_ERROR if memory could not be
   allocated for processing, Z_DATA_ERROR if the deflate data is
   invalid or incomplete, Z_VERSION_ERROR if the version of zlib.h and
   the version of the library linked do not match, or Z_ERRNO if there
   is an error reading or writing the files. */
// INPUT:
// unsigned char *source: pointer to data
// uint64_t *size       : size of data in bytes
// OUTPUT:
// pointer to decompressed data (NULL on error)
local unsigned char *decompress_chunk(unsigned char *source, uint64_t *size)
{
    int ret;
    unsigned have;
    z_stream strm;
    uint64_t i = 0;
    uint64_t output_size = 0;
    uint64_t input_size = *size;
    unsigned char *in, *out, *out_complete;

    /* allocate inflate state */
    strm.zalloc = Z_NULL;
    strm.zfree = Z_NULL;
    strm.opaque = Z_NULL;
    strm.avail_in = 0;
    strm.next_in = Z_NULL;
    ret = inflateInit(&strm);
    if (ret != Z_OK)
        return NULL;

    in = malloc(CHUNK);
    out = malloc(CHUNK);
    out_complete = malloc(CHUNK);

    /* decompress until deflate stream ends or end of file */
    do
    {
        strm.avail_in = (i + CHUNK < input_size) ? CHUNK : (input_size - i);
        if (memcpy(in, source + i, strm.avail_in) == NULL)
        {
            (void)inflateEnd(&strm);
            goto decompress_chunk_error;
        }
        if (strm.avail_in == 0)
            break;
        strm.next_in = in;
        i += strm.avail_in;

        /* run inflate() on input until output buffer not full */
        do
        {
            strm.avail_out = CHUNK;
            strm.next_out = out;
            ret = inflate(&strm, Z_NO_FLUSH);
            assert(ret != Z_STREAM_ERROR); /* state not clobbered */
            switch (ret)
            {
            case Z_NEED_DICT:
                ret = Z_DATA_ERROR;
                (void)inflateEnd(&strm);
                goto decompress_chunk_error;
            case Z_DATA_ERROR:
            case Z_MEM_ERROR:
                (void)inflateEnd(&strm);
                goto decompress_chunk_error;
            }
            have = CHUNK - strm.avail_out;
            if (have != 0 && (NULL == (out_complete = realloc(out_complete, output_size + have)) ||
                              NULL == memcpy(out_complete + output_size, out, have)))
            {
                (void)inflateEnd(&strm);
                goto decompress_chunk_error;
            }
            output_size += have;
        } while (strm.avail_out == 0);

        /* done when inflate() says it's done */
    } while (ret != Z_STREAM_END);

    /* clean up and return */
    (void)inflateEnd(&strm);
    free(in);
    free(out);
    if (ret != Z_OK && ret != Z_STREAM_END)
    {
        printToStderr("Decompression of index' chunk terminated with error (%d).\n", ret);
    }
    // return size of returned char array in size pointer parameter
    *size = output_size;
    return out_complete;

decompress_chunk_error:
    free(in);
    free(out);
    free(out_complete);
    return NULL;
}

// Deallocate from memory
// an index built by decompress_and_build_index()
// INPUT:
// struct access *index: pointer to index
local void free_index(struct access *index)
{
    uint64_t i;
    if (index != NULL)
    {
        for (i = 0; i < index->have; i++)
        {
            free(index->list[i].window);
        }
        free(index->list);
        free(index->file_name);
        free(index);
    }
}

// fill pointers of and index->list with NULL windows
// so that free_index() can proceed on any contingency
// INPUT:
// struct access *index : pointer to index
// uint64_t from        : initial point to fill
// uint64_t to          : last point to fill
local void empty_index_list(struct access *index, uint64_t from, uint64_t to)
{
    uint64_t i;
    for (i = from; i < to; i++)
    {
        index->list[i].window = NULL;
        index->list[i].window_size = 0;
    }
    return;
}

// Allocate an index structure
// and fill it with empty values
// OUTPUT:
// pointer to index
local struct access *create_empty_index()
{
    struct access *index;
    if (NULL == (index = malloc(sizeof(struct access))))
    {
        return NULL;
    }
    index->index_version = 0;
    index->file_size = 0;
    index->number_of_lines = 0;
    index->list = NULL;
    index->file_name = NULL;

    index->list = malloc(sizeof(struct point) << 3);
    empty_index_list(index, 0, 8);
    if (index->list == NULL)
    {
        free(index);
        return NULL;
    }
    index->size = 8;
    index->have = 0;
    index->index_complete = 0;

    return index;
}

/* Add an entry to the access point list.  If out of memory, deallocate the
   existing list and return NULL. */
// INPUT:
// struct access *index : pointer to original index
// uint32_t bits        : (new entry data)
// uint64_t in          : (new entry data)
// uint64_t out         : (new entry data)
// unsigned left        : (new entry data)
// unsigned char *window: window data, already compressed with size window_size,
//                        or uncompressed with size WINSIZE,
//                        or store an empty window (NULL) because it resides on file.
// uint32_t window_size : size of passed *window (may be already compressed o not)
// uint64_t line_number : actual line number if index v1, 0 otherwise
// int compress_window  : 0: store window of size window_size, as it is, in point structure
//                        1: compress passed window of size window_size
// OUTPUT:
// pointer to (new) index (NULL on error)
local struct access *addpoint(struct access *index, uint32_t bits,
                              uint64_t in, uint64_t out, unsigned left, unsigned char *window, uint32_t window_size,
                              uint64_t line_number, int compress_window)
{
    struct point *next;
    uint64_t size = window_size;
    unsigned char *compressed_chunk;

    /* if list is empty, create it (starts with eight points) */
    if (index == NULL)
    {
        index = create_empty_index();
        if (index == NULL)
            return NULL;
    }

    /* if list is full, make it bigger */
    else if (index->have == index->size)
    {
        index->size <<= 1;
        next = realloc(index->list, sizeof(struct point) * index->size);
        if (next == NULL)
        {
            free_index(index);
            return NULL;
        }
        index->list = next;
        empty_index_list(index, index->have, index->size);
    }

    /* fill in entry and increment how many we have */
    next = index->list + index->have;
    next->bits = bits;
    next->in = in;
    next->out = out;
    next->line_number = line_number;

    if (compress_window == 1)
    {
        next->window_size = window_size;
        next->window = NULL;
        if (window_size > 0)
        {
            // compress window if window_size > 0: if not, a zero-length window is stored.
            next->window = malloc(window_size);
            // check left value with windows of size != WINSIZE
            if (left > window_size ||
                window_size != WINSIZE)
                left = 0;
            if (left)
                memcpy(next->window, window + window_size - left, left);
            if (left < window_size)
                memcpy(next->window + left, window, window_size - left);
            // compress window
            compressed_chunk = compress_chunk(next->window, &size, Z_DEFAULT_COMPRESSION);
            if (compressed_chunk == NULL)
            {
                printToStderr("Error whilst compressing index chunk\nProcess aborted\n.");
                return NULL;
            }
            free(next->window);
            next->window = compressed_chunk;
            /* uint64_t size and uint32_t window_size, but windows are small, so this will always fit */
            next->window_size = size;
        }
    }
    else
    {
        // if NULL == window, this creates a NULL window: it resides on file,
        // and can/will later loaded on memory with its correct ->window_size;
        // if passed window is already compressed it will be stored as it is with size "window_size"
        next->window_size = window_size;
        next->window = window;
    }

    index->have++;

    /* return list, possibly reallocated */
    return index;
}

// serialize index into a file
// Note that the function writes and empty header when called with index_last_written_point = 0
// and also writes next points from index_last_written_point to index->have.
// To create a valid index, this function must be called a last time (at least, a 2nd time)
// to write the correct header values (and the (optional) tail (size of uncompressed file)).
// INPUT:
// FILE *output_file    : output stream
// struct access *index : pointer to index
// uint64_t index_last_written_point : last index point already written to file: its values
//                                     go from 1 to index->have, so that 0 has special value "None".
// OUTPUT:
// 0 on error, 1 on success
int serialize_index_to_file(FILE *output_file, struct access *index, uint64_t index_last_written_point)
{
    struct point *here = NULL;
    uint64_t temp;
    uint64_t offset;
    uint64_t i;

    /* proceed only if there's something reasonable to do */
    if (NULL == output_file || NULL == index)
    {
        return 0;
    }

    /*if (index->have <= 0)
        return 0;*/
    /* writing and empty index is allowed: writes the header (of size 4*8 = 32 bytes) */

    if (index_last_written_point == 0)
    {

        /* write header */
        fseeko(output_file, 0, SEEK_SET);
        /* 0x0 8 bytes (to be compatible with .gzi for bgzip format: */
        /* the initial uint32_t is the number of bgzip-idx registers) */
        temp = 0;
        fwrite_endian(&temp, sizeof(temp), output_file);
        /* a 64 bits readable identifier */
        if (index->index_version == 0)
            fprintf(output_file, GZIP_INDEX_IDENTIFIER_STRING);
        else
        {
            fprintf(output_file, GZIP_INDEX_IDENTIFIER_STRING_V1);
            // there's an additional register on v1: line_number_format
            fwrite_endian(&(index->line_number_format), sizeof(index->line_number_format), output_file);
        }

        /* and now the raw data: the access struct "index" */

        // as this may be a growing index
        // values will be filled with special, not actual, values:
        // 0x0..0 , 0xf..f
        // Last write operation will overwrite these with correct values, that is, when
        // serialize_index_to_file() be called with index_last_written_point = index->have
        temp = 0;
        fwrite_endian(&temp, sizeof(temp), output_file); // have
        temp = UINT64_MAX;
        fwrite_endian(&temp, sizeof(temp), output_file); // size
    }

    offset = GZIP_INDEX_HEADER_SIZE + sizeof(temp) +
             ((index->index_version == 1) ? sizeof(index->line_number_format) : 0);

    // update index->size on disk with index->have data
    // ( if index->have == 0, no index points still, maintain UINT64_MAX )
    if (index->have > 0)
    {
        // seek to index->have position
        fseeko(output_file, offset, SEEK_SET);
        // write index->have value; (when the index be closed, index->size on disk will be >0)
        fwrite_endian(&(index->have), sizeof(index->have), output_file);
    }

    // fseek to index position of index_last_written_point
    offset += sizeof(temp);
    for (i = 0; i < index_last_written_point; i++)
    {
        here = &(index->list[i]);
        offset += sizeof(here->out) + sizeof(here->in) +
                  sizeof(here->bits) + sizeof(here->window_size) +
                  ((here->window_size == UNCOMPRESSED_WINDOW) ? WINSIZE : (here->window_size)) +
                  ((index->index_version == 1) ? sizeof(here->line_number) : 0);
    }
    fseeko(output_file, offset, SEEK_SET);

    if (index_last_written_point != index->have)
    {
        for (i = index_last_written_point; i < index->have; i++)
        {
            here = &(index->list[i]);
            fwrite_endian(&(here->out), sizeof(here->out), output_file);
            fwrite_endian(&(here->in), sizeof(here->in), output_file);
            fwrite_endian(&(here->bits), sizeof(here->bits), output_file);
            if (here->window_size == UNCOMPRESSED_WINDOW)
            {
                temp = WINSIZE;
                fwrite_endian(&(temp), sizeof(here->window_size), output_file);
            }
            else
            {
                fwrite_endian(&(here->window_size), sizeof(here->window_size), output_file);
            }
            here->window_beginning = ftello(output_file);
            if (NULL == here->window &&
                here->window_size != 0)
            {
                printToStderr("Index incomplete! - index writing aborted.\n");
                return 0;
            }
            else
            {
                if (here->window_size > 0)
                    fwrite(here->window, here->window_size, 1, output_file);
            }
            // once written, point's window CAN (and will now) BE DELETED from memory
            free(here->window);
            here->window = NULL;
            if (index->index_version == 1)
            {
                // there's an additional register on v1: line_number
                fwrite_endian(&(here->line_number), sizeof(here->line_number), output_file);
            }
        }
    }
    else
    {
        // Last write operation:
        // tail must be written:
        /* write size of uncompressed file (useful for bgzip files) */
        fwrite_endian(&(index->file_size), sizeof(index->file_size), output_file);
        if (index->index_version == 1)
        {
            // there's an additional register on v1: number_of_lines
            fwrite_endian(&(index->number_of_lines), sizeof(index->number_of_lines), output_file);
        }
        // and header must be updated:
        // for this, move position to header:
        fseeko(output_file,
               GZIP_INDEX_HEADER_SIZE + ((1 == index->index_version) ? sizeof(index->line_number_format) : 0),
               SEEK_SET);
        fwrite_endian(&index->have, sizeof(index->have), output_file);
        /* index->size is not written as only filled entries are usable */
        fwrite_endian(&index->have, sizeof(index->have), output_file);
        index->index_complete = 1; /* index is now complete */
    }

    // flush written content to disk
    fflush(output_file);

    return 1;
}

/* Basic checks of existing index file:
   - Checks that last index point ->in isn't greater than gzip file size
*/
// INPUT:
// struct access *index         : pointer to index. Can be NULL => no check.
// char *file_name     : gzip file name. Can be NULL or "" => no check.
// char *index_filename: index file name. Must be != NULL, but can be "". Only used to print warning.
// OUTPUT:
// 0 on error, 1 on success
int check_index_file(struct access *index, char *file_name, char *index_filename)
{
    if (NULL != file_name &&
        strlen(file_name) > 0)
    {
        if (NULL != index &&
            index->have > 0)
        {
            // size of input file
            struct stat st;
            stat(file_name, &st);
            if (index->have > 1 &&
                (uint64_t)st.st_size < (index->list[index->have - 1].in))
            {
                printToStderr("WARNING: Index file '%s' corresponds to a file bigger than '%s'\n",
                              index_filename, file_name);
                return 0;
            }
        }
    }

    return 1;
}

// pread() does not exist in Windows, so in this platform
// pread() is substitued by a fseeko() + fread() operations.
// ReadFile() in Windows would also be feasible in decompress_in_advance()
// context as it will explicitly restore file pointer at the end,
// but it doesn't seem to worth the effort of using non-POSIX functions.
ssize_t PREAD(
    FILE *fildes, void *buf, size_t nbyte, off_t offset)
{

#ifdef _WIN32
    fseeko(fildes, offset, SEEK_SET);
    return fread(buf, 1, nbyte, fildes);
#else
    return pread(fileno(fildes), buf, nbyte, offset);
#endif
}

// decompress at least CHUNKS_TO_DECOMPRESS_IN_ADVANCE chunks
// in front of actual decompression stream (decompress_and_build_index()),
// to assure that no errors will be found, and if an error is found in advance,
// try to find a reason in these CHUNKS_TO_DECOMPRESS_IN_ADVANCE chunks
// that explain it as an incorrectly terminated gzip stream and the
// beginning of a new one immediately attached after it:
// and so either a GZIP header or a full flush point are found there.
// If an error is found and a feasible restart is found, its gzip stream
// byte position is returned for decompress_and_build_index() to decompress
// ONLY until that point, and RESTART decompression just in it later.
// If errors aren't detected with decompression in advance, incorrect but
// technically viable (will not launch zlib errors) data can/will be extracted,
// and the point of error will be already inserted in another gzip stream
// than the original.
// INPUT:
// z_stream strm_original: shallow copy of strm from caller decompress_and_build_index()
//                         only used if local static strm structure hasn't been initialized
// FILE *file_in  : file to be used for reading. Its position pointer will be restored
//                  to actual position when this function returns.
//                  (stdin is not valid as it doesn't allow fseeko())
// char *file_name: name of the input file associated with FILE *in, to check the size
//                  of the input file (if not stdin) to check shrinking.
//                  Can be "" (no file name: stdin used; though decompress_in_advance() MUST NOT be called then,
//                             because this function MUST be able to seek backwards in gzip stream),
//                  or NULL if initialize_function == DECOMPRESS_IN_ADVANCE_RESET.
// uint64_t totin0: total number of input gzip stream bytes already "correctly" processed:
//                  it is set on first call, with initialize_function == DECOMPRESS_IN_ADVANCE_RESET, AND ALSO in all later calls.
// int chunks_in_advance0  : indicates if a decompression in advance will be used and how many CHUNKs
//                           will be decompressed in advance then (CHUNKS_TO_DECOMPRESS_IN_ADVANCE).
//                           If decompress_in_advance is called, this value MUST be GREATER than 0
//                           AND it MUST be set ONLY on first call with initialize_function == DECOMPRESS_IN_ADVANCE_RESET.
// int chunks_to_look_backwards0  : indicates how many CHUNKs can look the function backwards in order
//                           to find a new good gzip entry point to patch an error found in the stream.
//                           If decompress_in_advance is called, this value MUST be GREATER than 0
//                           AND it MUST be set ONLY on first call with initialize_function == DECOMPRESS_IN_ADVANCE_RESET.
// enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts0: value of indx_n_extraction_opts on caller.
//                                            only set on first call, with initialize_function == DECOMPRESS_IN_ADVANCE_RESET.
// int decompressing_with_gzip_headers0: 1 if inflateInit2(&strm, 47) on actual state on first
//                                       call with initialize_function == DECOMPRESS_IN_ADVANCE_RESET. 0 otherwise.
// int end_on_first_proper_gzip_eof0   : end file processing on first proper (at feof()) GZIP EOF.
//                                       Set with initialize_function == DECOMPRESS_IN_ADVANCE_RESET. 0 otherwise.
// uint64_t output_data_counter  : used to know if caller has already output data or not: depending on this
//                                  the final warning is about data collision or about different data beginning.
// uint64_t totout               : passed on each DECOMPRESS_IN_ADVANCE_CONTINUE call to inform it on overlapping warning.
// int waiting_time0             : waiting time in seconds between reads when `-[ST]` (always >0)
// bool lazy_gzip_stream_patching_at_eof0: when used with `-[ST]` implies that checking
//                                  for errors in stream is made as quick as possible as the gzip file
//                                  grows, not respecting then at EOF the CHUNKS_TO_DECOMPRESS_IN_ADVANCE value.
//                                  Set with initialize_function == DECOMPRESS_IN_ADVANCE_RESET. 0 otherwise.
// uint64_t expected_first_byte0 : indicates that the first byte on compressed input is this, not 1,
//                                and so truncated compressed inputs can be used. Used in decompress_in_advance()
//                                only to not check "Detected gzip file overwriting, so ending process" condition:
//                                this check must not be done if using `-n` (because then st.st_size < totin in general)
// DECOMPRESS_IN_ADVANCE_INITIALIZERS initialize_function:
//                          if DECOMPRESS_IN_ADVANCE_RESET, internal value strm_initialized is set to false
//                          marking this way the beginning of a new file processing.
//                          See DECOMPRESS_IN_ADVANCE_INITIALIZERS.
// OUTPUT:
// struct returned_output: contains two values:
//      .error: one of GZIP_MARK_FOUND_TYPE:
//              GZIP_MARK_ERROR, GZIP_MARK_NONE, GZIP_MARK_BEGINNING, GZIP_MARK_FULL_FLUSH.
//      .value: gzip stream byte position of a feasible restart gzip-point.
local struct returned_output decompress_in_advance(
    FILE *file_in, 
    const uint64_t totin0,
    const int chunks_in_advance0,
    const int chunks_to_look_backwards0,
    const enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts0,
    const int decompressing_with_gzip_headers0,
    const int end_on_first_proper_gzip_eof0,
    const int waiting_time0,
    const bool lazy_gzip_stream_patching_at_eof0,
    const uint64_t expected_first_byte0,
    const enum DECOMPRESS_IN_ADVANCE_INITIALIZERS initialize_function)
{
    struct returned_output returned_value; // returned structure
    returned_value.value = 0LLU;
    returned_value.error = GZIP_MARK_NONE;
    off_t initial_file_position = 0;            // Store actual FILE *file_in position for later restore.
                                                // This value won't be needed if pread() would be cross-platform
                                                // as it doesn't modify the file pointer, but unfortunately it
                                                // doesn't exist in Windows platforms.
    int initial_file_EOF = 0;                   // It is important to keep the feof(file_in) state intact after all fseek()
    uint64_t caller_last_processed_byte = 0LLU; // store parameter totin0 when initialize_function == DECOMPRESS_IN_ADVANCE_CONTINUE
    uint64_t process_until_this_byte = 0LLU;    // inflate() until this input byte position is reached
    int gzip_eof_detected = 0;                  // 1 if a GZIP file format EOF is detected, and so, feof() is not trustworthy
    static z_stream strm;
    // internal structure to store ret.error from inflate() (cloning decompress_and_build_index()'s code):
    static struct returned_output ret =
        {.value = 0LLU,  // ret.value is never used
         .error = Z_OK}; // ret.error is used, to maintain this code as an-almost-clone of decompress_and_build_index()
                         // thanks to using the same name.
    // ( note: DO NOT initialize values out of the static declaration, as they'd be initialized on every call! )
    static bool strm_initialized = false;
    static enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts = JUST_CREATE_INDEX;
    static uint64_t totin = 0LLU;
    static uint64_t last_processed_byte = 0LLU; // maintains internal ftello() between calls to this function.
    static int chunks_in_advance = 0;           // set to chunks_in_advance0 from caller on decompress_in_advance()
                                                // initialization (..., DECOMPRESS_IN_ADVANCE_*RESET).
                                                // Also internally set to 0 to mark a correct EOF found and
                                                // so that then no more processing of this function is needed.
    static int chunks_to_look_backwards = 0;
    static int decompressing_with_gzip_headers = 0;
    static int end_on_first_proper_gzip_eof = 0;
    static int waiting_time = 0;
    static bool lazy_gzip_stream_patching_at_eof = false;
    static uint64_t expected_first_byte = 1LLU;
    static unsigned char input[CHUNK];                          // TODO: convert to malloc
    static unsigned char window[WINSIZE];                       // TODO: convert to malloc
    static uint64_t last_correct_reentry_point_returned = 0LLU; // this function will never go backwards beyond the
                                                                // last reentry point (It doesn't seem to have sense
                                                                // as it implies to discard last extracted bytes); anyway,
                                                                // what it's clear is that this last point MUST NOT be
                                                                // used again, or possible infinite loops could appear.
    static bool first_call = true;                              // to know if this is the first call to this function,
                                                                // and so strm already contains previous valid data!

    // strm_initialized must be set to false in decompress_in_advance()
    // when the file processing ends, no matter what...
    // As there may be errors in the middle of the process, the only
    // way to guarantee this is to explicitly reset this function prior to each processing,
    // via decompress_in_advance( ...,     DECOMPRESS_IN_ADVANCE_*RESET ) from decompress_and_build_index()
    if (true == strm_initialized)
    {
        inflateEnd(&strm);
        // ( input[] and window[] must not be emptied
        //   as they're filled on (next) first call. )
    }
    strm_initialized = false;
    indx_n_extraction_opts = indx_n_extraction_opts0;
    totin = totin0;
    last_processed_byte = totin0;
    // ret.value = 0LLU; // ret.value is never used:
    ret.error = Z_OK; // ret.error is used, to maintain this code as an-almost-clone of decompress_and_build_index()
                        // thanks to using the same name.
    chunks_in_advance = chunks_in_advance0;
    chunks_to_look_backwards = chunks_to_look_backwards0;
    decompressing_with_gzip_headers = decompressing_with_gzip_headers0;
    end_on_first_proper_gzip_eof = end_on_first_proper_gzip_eof0;
    expected_first_byte = expected_first_byte0;
    first_call = true;
    if (DECOMPRESS_IN_ADVANCE_RESET == initialize_function)
    {
        waiting_time = waiting_time0;
        lazy_gzip_stream_patching_at_eof = lazy_gzip_stream_patching_at_eof0;
        last_correct_reentry_point_returned = 0LLU;
    }
    if (chunks_in_advance0 == 0 ||
        chunks_to_look_backwards0 == 0)
    {
        // these two values cannot be zero!
        returned_value.error = GZIP_MARK_FATAL_ERROR;
    }
    return returned_value;

    // If this function found a licit EOF on a previous call,
    // then no more decompress_in_advance() processing is needed:
    if (0 == chunks_in_advance)
    {
        return returned_value;
    }

    // store actual FILE *file_in position for later restore
    initial_file_position = ftello(file_in);
    initial_file_EOF = feof(file_in);

    process_until_this_byte = caller_last_processed_byte + chunks_in_advance * CHUNK;

    // move file read position to last processed byte by this function:
    if (0 != fseeko(file_in, last_processed_byte, SEEK_SET))
    {
        printToStderr("ERROR: Cannot seek input file to @%llu Byte.\n", totin);
        returned_value.error = GZIP_MARK_FATAL_ERROR;
        return returned_value;
    }

    // if local strm has not been initialized
    // copy *strm_original on it, using inflateCopy()
    if (false == strm_initialized)
    {
        // deep copy:
        returned_value.value = 0LLU;
        returned_value.error = GZIP_MARK_ERROR;
        return returned_value;
    }

    // Note that all code from here on, which is not involved in the
    //      if ( Z_OK != ret.error &&
    //           Z_STREAM_END != ret.error ) { ... }
    // loop to manage errors in gzip stream, is copied from
    // decompress_and_build_index(), with some lines commented out with "////"
    // and not removed, in order to maintain some one-to-one identity with
    // the original code.

    // initiate decompression "in advance":
    do
    {

        // bgzip-compatible-streams code:
        if (ret.error == Z_STREAM_END &&
            strm.avail_in > 0)
        { // if strm.avail_in is casually 0, this block of code isn't needed.
            // readjust input data moving it to the beginning of buffer:
            // (strm.next_in floats from input to input+CHUNK)
            memmove(input, strm.next_in, strm.avail_in); // memmove() is overlapping-safe
        }
        // .................................................
        // note that here strm.avail_in > 0 only if ret.error == Z_STREAM_END
        int strm_avail_in0 = strm.avail_in;
        // .................................................
        // OK... previous comment:
        //  "note that here strm.avail_in > 0 only if ret.error == Z_STREAM_END"
        // is correct on caller decompress_and_build_index(),
        // :<#but not here in decompress_in_advance()> : here, strm.avail_in can be greater
        // than zero on first call due to the fact that on caller, call to this function occurs
        // after fread() has taken place, so strm.avail_in is full of bytes not yet processed
        // (a CHUNK or till EOF) - as strm is cloned here, also this input buffer is inherited full.
        // This has the side effect that first call to fread() here in +2 lines, is empty (returns no data)
        // and so totin will be greater than ftello(file_in) during first loop processing: this
        // curious but correct behaviour is replicated on every call from decompress_and_build_index()
        // that ocurrs after a reinitiation of the gzip stream: that is, on first index entry point
        // or beginning of gzip stream, and after a decompress_in_advance() returned new-valid-entry-point
        // is executed.
        // Another side effect is that in practice one call to fread()+disk is saved, which cannot be bad.
        // Nonetheless, it must be noted that after the first CHUNK is processed with strm.avail_in > 0,
        // the loop returns here with totin > ftello(file_in), which is not correct in general, as this
        // would result in another fread() over the same data that has been just processed! so fseeko()
        // must be set to totin position in order to correctly read next input from gzip stream:
        if (totin > (uint64_t)ftello(file_in)
            // this condition's code must run (even if ret.error==Z_STREAM_END here, (see <#Z_STREAM_END note>))
            // to adjust file_in read position TO AVOID READ DATA ALREADY IN "input" BUFFER [*] which,
            // btw, ends at ( input + strm.avail_in ) if the memmove() was executed because ret.error == Z_STREAM_END
            // or ends at ( input + CHUNK - strm.avail_in ) if ret.error != Z_STREAM_END,
            // but in both cases that is equal to ( totin + strm.avail_in ):
            // ( note "[*]":
            //    see <#but not here in decompress_in_advance()> comment about why buffer is ahead of ftello(). )
        )
        {
            if (0 != fseeko(file_in, totin + strm.avail_in, SEEK_SET))
            {
                printToStderr("ERROR: Cannot seek input file to @%llu Byte.\n", totin);
                returned_value.error = GZIP_MARK_FATAL_ERROR;
                return returned_value;
            }
        }
        // .................................................
        if (!feof(file_in) &&
            ((uint64_t)ftello(file_in) == totin || initial_file_EOF == 0) // as we're using the same initial buffer with ftello(file_in)>totin
                                                                          // this precaution is compulsory
        )
        { // on last block, strm.avail_in > 0 is possible with feof(file_in)==1 already!
            if (!first_call
                // !first_call ensure that no there's no DOUBLE fread over the same contents:
                // on entering here for the first time (first caller call after DECOMPRESS_IN_ADVANCE_*RESET!)
                // input ALREADY contains strm_avail_in0 valid data,
                // so we cannot enter here to read the same data (fseeko() before the "do" loop) after it (input + strm_avail_in0),
                // which would result in AN INCORRECT input buffer.
                // Note than on first (first_call) "do" loop iteration always feof(file_in)==0 even with small inputs,
                // because there's an fseeko() before the "do" loop - These small inputs (<CHUNK) are the ones that would result
                // in AN INCORRECT input buffer, otherwise if file contents are still bigger than CHUNK passed this point
                // in each call there's no problem as strm_avail_in0 == CHUNK and so this fread() reads 0 bytes.
            )
            {
                strm.avail_in = fread(input + strm_avail_in0, 1, CHUNK - strm_avail_in0, file_in); // here I do not use pread() just to maintain the same code base as in decompress_and_build_index()
                strm.avail_in += strm_avail_in0;
            }
        }

        // .................................................
        //      It is not necessary to reinitiate decompress_in_advance() after Z_STREAM_END
        //      in case of returned_value.error == GZIP_MARK_ERROR && returned_value.value > 0
        //      just because that means that the process will find here at its moment
        //      an unrecoverable error and will stop :-)
        // .................................................
        // decompressor state MUST be reinitiated after Z_STREAM_END
        if (ret.error == Z_STREAM_END)
        {
            strm_avail_in0 = strm.avail_in;
            (void)inflateEnd(&strm);
            strm.zalloc = Z_NULL;
            strm.zfree = Z_NULL;
            strm.opaque = Z_NULL;
            strm.avail_in = 0;
            strm.next_in = Z_NULL;
            ret.error = inflateInit2(&strm, 47); /* automatic zlib or gzip decoding (15 + automatic header detection) */
            decompressing_with_gzip_headers = 1;
            if (ret.error != Z_OK)
                goto decompress_in_advance_ret;
            strm.avail_in = strm_avail_in0;
            // it is compulsory to reinitiate also output data:
            strm.avail_out = WINSIZE;
            strm.next_out = window;
            ////avail_out_0 = strm.avail_out;
            ////window_size = 0;
        }
        // end of bgzip-compatible-streams code

        first_call = false; // before any "continue" on this "do" loop
            //// ...

        // sleep and retry
        if (true == lazy_gzip_stream_patching_at_eof)
        {
            // return to caller as soon as possible, ignoring
            // CHUNKS_TO_DECOMPRESS_IN_ADVANCE at EOF when `-P[ST]`,
            // to print out current caller input buffer,
            // which, as we've reached until here, is CORRECT!
            // because fread() MUST have read it also and so the
            // inflate() has occurred because this code is ONLY
            // REACHED WITH strm.avail_in == 0
            ret.error = GZIP_MARK_NONE;
            goto decompress_in_advance_ret;
        }
        else
        {
            sleep(waiting_time);
        }
        clearerr(file_in);
        continue;

        if (indx_n_extraction_opts == JUST_CREATE_INDEX ||
            indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
            indx_n_extraction_opts == EXTRACT_FROM_LINE)
        {
            // with not Supervising options, strm.avail_in == 8 + eof is equivalent to Correct END OF GZIP at EOF
            if ((feof(file_in) || initial_file_EOF != 0) &&
                strm.avail_in == 8)
            {
                break;
            }
        }

        if (ferror(file_in))
        {
            ret.error = Z_ERRNO;
            goto decompress_in_advance_ret;
        }
        if (strm.avail_in == 0)
        {
            ret.error = Z_DATA_ERROR;
            goto decompress_in_advance_ret;
        }
        strm.next_in = input;

        do
        {

            /* reset sliding window if necessary */
            if (strm.avail_out == 0)
            {
                strm.avail_out = WINSIZE;
                strm.next_out = window;
                ////avail_out_0 = strm.avail_out;
            }

            /* inflate until out of input, output, or at end of block --
               update the total input and output counters */
            totin += strm.avail_in;
            ////totout += strm.avail_out;
            ////window_size += strm.avail_out; // update window_size available size
            ret.error = inflate(&strm, Z_BLOCK); /* return at end of block */
            totin -= strm.avail_in;
            ////totout -= strm.avail_out;

            // .................................................
            // treat possible gzip tail:
            if (ret.error == Z_STREAM_END &&
                strm.avail_in >= 8 &&
                decompressing_with_gzip_headers == 0)
            {
                // discard this data as it is probably the 8 gzip-tail bytes (4 CRC + 4 size%2^32)
                // and zlib is not able to consume it if inflateInit2(&strm, -15) (raw decompressing)
                strm.avail_in -= 8;
                strm.next_in += 8;
                totin += 8; // data is discarded from strm input, but it MUST be counted in total input
                ////if ( offset_in >= 8 )
                ////    offset_in -= 8; // data is discarded from strm input, and also from offset_in
                ////else
                ////    offset_in = 0;
                if (end_on_first_proper_gzip_eof == 1 ||
                    ((indx_n_extraction_opts == JUST_CREATE_INDEX ||
                      indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
                      indx_n_extraction_opts == EXTRACT_FROM_LINE) &&
                     (feof(file_in) || initial_file_EOF != 0)))
                {
                    gzip_eof_detected = 0; // to exit outer loop with gzip_eof_detected == 0
                                           // if the rest of loop conditions hold true.
                    chunks_in_advance = 0; // decompress_in_advance() ceases to have sense in this gzip stream
                                           // so that is marked here with static chunks_in_advance == 0.
                }
                else
                {
                    gzip_eof_detected = 1;
                }
                break;
                //// avail_in_0 doesn't need to be decremented as it tries to count raw stream input bytes
            }
            // end of treat possible gzip tail
            // .................................................

            if (Z_OK != ret.error &&
                Z_STREAM_END != ret.error)
            { // [B]
                // This is the point of error in the gzip stream!
                // Now we must obtain current position in gzip stream to figure out
                // if we can go backwards, and then look for GZIP init stream markers.
                returned_value.error = GZIP_MARK_NONE;
                // Which position exactly has the error been found at:
                off_t file_position_of_error = totin;
                printToStderr("PATCHING: Gzip stream error %d found @ %llu Byte.\n", ret.error, file_position_of_error);
                if (file_position_of_error == 0)
                {
                    // there's no room for more backwards searching for gzip initial markers
                    // [A]
                    returned_value.error = GZIP_MARK_ERROR;
                    returned_value.value = 0LLU;
                    goto decompress_in_advance_ret;
                }
                else
                {
                    // in order to honor chunks_to_look_backwards,
                    // the reverse search will be partitioned in chunks of size CHUNK
                    int i = 0;
                    uint64_t backwards_until_this_byte = 0LLU;
                    int MAX_HEADER_SIZE = 3;
                    int LOCAL_CHUNK = CHUNK + MAX_HEADER_SIZE; // CHECK GZIP HEADERS SPREAD BETWEEN CHUNKS' BORDERS!!!
                                                               // so read 3 bytes more than a CHUNK each time.
                    unsigned char buffer[LOCAL_CHUNK];
                    int buffer_position = 0;
                    int bytes_read = 0;

                    do
                    { // while ( ++i <= chunks_to_look_backwards &&
                      //         backwards_until_this_byte >= CHUNK );

                        backwards_until_this_byte =
                            ((file_position_of_error / CHUNK) - i) * CHUNK; // integer operations

                        if (backwards_until_this_byte <= last_correct_reentry_point_returned)
                        {
                            // never pass "last_correct_reentry_point_returned" byte
                            // backwards as it has been already used:
                            backwards_until_this_byte = last_correct_reentry_point_returned + 1;
                        }

                        // I use pread() to avoid multiple syscalls with fseeko()+fread()
                        if (!(bytes_read =
                                  PREAD(file_in, buffer,
                                        LOCAL_CHUNK, // read CHUNK even @ i==0, to be able to search even a little
                                                     // after buffer_position in that case (markers has 4 bytes in length)
                                        backwards_until_this_byte)))
                        {
                            returned_value.error = GZIP_MARK_ERROR;
                            returned_value.value = 0LLU;
                            goto decompress_in_advance_ret;
                        }

                        buffer_position = bytes_read - MAX_HEADER_SIZE; // do not read pass the buffer's limits!

                        if (i == 0 &&
                            (backwards_until_this_byte + buffer_position) > totin)
                        {
                            buffer_position = totin - backwards_until_this_byte;
                        }

                        if (buffer_position < 0)
                        {
                            buffer_position = 0; // do not read pass the buffer's limits!
                        }

                        // loop to run over all buffer long in case
                        // multiple "false" restart points, at possible gzip markers, are found:
                        do
                        { // while ( buffer_position >= 0 );
                            // so now it's the time to search for GZIP-beggining markers:
                            // //tools.ietf.org/html/rfc1952#section-2.2
                            //  1F 8B + compressionMethod (08: deflate) + flags + 4x modificationTime + ...
                            //  or
                            //  00 00 FF FF, for a full flush point (Z_FULL_FLUSH)
                            //
                            do
                            {
                                if (buffer[buffer_position] == 0x1F &&
                                    buffer[buffer_position + 1] == 0x8B &&
                                    buffer[buffer_position + 2] == 0x08 &&
                                    buffer[buffer_position + 3] < 32 // first three bits are always reserved as 0
                                )
                                {
                                    returned_value.value = backwards_until_this_byte + buffer_position;
                                    returned_value.error = GZIP_MARK_BEGINNING;
                                    break;
                                }
                                if (buffer[buffer_position] == 0x00 &&
                                    buffer[buffer_position + 1] == 0x00 &&
                                    buffer[buffer_position + 2] == 0xFF &&
                                    buffer[buffer_position + 3] == 0xFF)
                                {
                                    returned_value.value = backwards_until_this_byte + buffer_position + 4; // hopefully this +4 position will exist even after end of "buffer"
                                    returned_value.error = GZIP_MARK_FULL_FLUSH;
                                    break;
                                }
                            } while (--buffer_position >= 0);

                            if (returned_value.error == GZIP_MARK_BEGINNING ||
                                returned_value.error == GZIP_MARK_FULL_FLUSH)
                            {
                                buffer_position--;
                                // now we must try to see if the point found is
                                // a valid starting point of a GZIP compressed stream!
                                /* initialize file and inflate state to start there */
                                z_stream strm;
                                struct returned_output ret2;
                                unsigned char buffer[CHUNK];   // input
                                unsigned char window[WINSIZE]; // output
                                // initialize zlib's strm:
                                strm.zalloc = Z_NULL;
                                strm.zfree = Z_NULL;
                                strm.opaque = Z_NULL;
                                strm.avail_in = 0;
                                strm.next_in = Z_NULL;
                                if (returned_value.error == GZIP_MARK_FULL_FLUSH)
                                {
                                    // raw inflate
                                    ret2.error = inflateInit2(&strm, -15);
                                    decompressing_with_gzip_headers = 0;
                                }
                                else
                                {
                                    // automatic zlib or gzip decoding (15 + automatic header detection)
                                    ret2.error = inflateInit2(&strm, 47);
                                    decompressing_with_gzip_headers = 1;
                                }
                                if (Z_OK != ret2.error)
                                {
                                    returned_value.error = ret2.error;
                                    returned_value.value = 0LLU;
                                    // caller must know that an unrecoverable error occurred
                                    goto decompress_in_advance_ret;
                                }
                                // once zlib's strm is initialized, provide content to it:
                                if (!PREAD(file_in, buffer,
                                           CHUNK,
                                           returned_value.value))
                                {
                                    (void)inflateEnd(&strm); // empty strm structure appropriately
                                    returned_value.error = GZIP_MARK_ERROR;
                                    returned_value.value = 0LLU;
                                    // caller must known that an unrecoverable error occurred
                                    goto decompress_in_advance_ret;
                                }
                                strm.next_in = buffer;
                                strm.avail_in = CHUNK;
                                strm.next_out = window;
                                strm.avail_out = WINSIZE;
                                ret2.error = inflate(&strm, Z_BLOCK);
                                (void)inflateEnd(&strm); // empty strm structure appropriately
                                if (Z_OK != ret2.error)
                                {
                                    // this is not a valid restarting point for gzip:
                                    // so let's try another one!
                                    continue;
                                }
                                else
                                {
                                    // this is a valid restarting point for gzip:
                                    // communicate this to caller: it'll take appropriate measures
                                    goto decompress_in_advance_ret;
                                }

                            } // if ( returned_value.error == GZIP_MARK_BEGINNING ||
                              //      returned_value.error == GZIP_MARK_FULL_FLUSH )

                        } while (buffer_position >= 0);

                    } while (++i <= chunks_to_look_backwards &&
                             backwards_until_this_byte >= CHUNK);

                    // there has been no luck looking for a valid gzip restart point:
                    returned_value.error = GZIP_MARK_ERROR;
                    returned_value.value = totin;
                    goto decompress_in_advance_ret;

                } // if ( file_position_of_error <= 0 ||
                  //      file_position_of_error - CHUNK + strm.avail_in < 0 )

            } // if ( Z_OK != ret.error &&
              //      Z_STREAM_END != ret.error )

            if (ret.error == Z_STREAM_END)
            {
                if (indx_n_extraction_opts == JUST_CREATE_INDEX ||
                    indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
                    indx_n_extraction_opts == EXTRACT_FROM_LINE)
                {
                    // with not Supervising options, a Z_STREAM_END at feof() is correct!
                    if ((feof(file_in) ||
                         initial_file_EOF != 0) // as we're using the same initial buffer with ftello(file_in)>totin
                                                // this precaution is compulsory
                        && strm.avail_in == 0)
                    {
                        gzip_eof_detected = 0; // to exit loop, as "gzip_eof_detected == 1" is one ORed condition
                                               // and now this variable is not needed anymore.
                        break;
                    }
                }
                if (end_on_first_proper_gzip_eof == 0)
                    gzip_eof_detected = 1;
                if (gzip_eof_detected == 0)
                {
                    if (end_on_first_proper_gzip_eof == 1 &&
                        (feof(file_in) || initial_file_EOF != 0))
                    {
                        gzip_eof_detected = 0;
                    }
                    else
                    {
                        gzip_eof_detected = 1;
                        ////printToStderr("Warning: GZIP end detected in the middle of data: deactivating `-E`\n" );
                        end_on_first_proper_gzip_eof = 0;
                    }
                }
                ////if ( offset_in >= 8 ) {
                ////    offset_in -= 8; // data is discarded from strm input, and also from offset_in
                ////} else {
                ////    offset_in = 0;
                ////}
                break;
            }

        } while (strm.avail_in != 0);

        // Test if loop
        // do { ... } while ( ret.error != Z_STREAM_END || strm.avail_in > 0 || gzip_eof_detected == 1 );
        // must end.
        // This condition outside the while() condition of the loop is only possible
        // if "continue;" is not used affecting this loop.
        if (totin >= process_until_this_byte /*&&
            ret.error != Z_STREAM_END*/
        )
        { // :<#Z_STREAM_END note>
            // Z_STREAM_END conditions can be treated in the loop
            // before returning to caller as they are very sensible:
            // there's a lot of code in the beginning of the loop
            // waiting to correctly treat this condition. Entering
            // there from a new call, is possible, preserving the state
            //   { totin, ftello(), ret.error }
            // which is what I've finally decided to do. But uncommenting
            // the condition "&& ret.error != Z_STREAM_END" is totally fine.
            break;
        }

    } while (ret.error != Z_STREAM_END || strm.avail_in > 0 || gzip_eof_detected == 1);

    // ok, decompression in advance has correctly ended
    returned_value.value = 0LLU;
    returned_value.error = GZIP_MARK_NONE;

/* return value */
decompress_in_advance_ret:

    // store actual ftello() to fseeko() file_in on next calls
    last_processed_byte = ftello(file_in);

    // strm structure deletion is called by a decompress_in_advance(...,true)
    // from caller, when it estimates it is time to do it: that is (should be) after
    // a successful GZIP_MARK_FULL_FLUSH or GZIP_MARK_BEGINNING returned ret.error value.

    // before returning, a fseeko() is needed if native pread() (which doesn't
    // change file pointer value) hasn't been used
    // OR if fread() has been used, which is always the case (just to maintain the
    // same code base as in decompress_and_build_index()).
    if (0 != fseeko(file_in, initial_file_position, SEEK_SET))
    {
        printToStderr("ERROR: Cannot seek input file to @%llu Byte.\n", initial_file_position);
        returned_value.error = GZIP_MARK_FATAL_ERROR;
    }
    if (0 != initial_file_EOF)
    {
        if (0 != fread(input, 1, 1, file_in))
        {
            // If file_in has grown in the interim, it doesn't matter if feof() is
            // not maintained because then the feof() won't be "correct" anyway...
            if (0 != fseeko(file_in, initial_file_position, SEEK_SET))
            {
                printToStderr("ERROR: Cannot seek input file to @%llu Byte.\n", initial_file_position);
                returned_value.error = GZIP_MARK_FATAL_ERROR;
            }
            // and feof() == 0 but as said, it shouldn't be important.
        }
    }

    // set last_correct_reentry_point_returned
    if (returned_value.error == GZIP_MARK_BEGINNING ||
        returned_value.error == GZIP_MARK_FULL_FLUSH)
    {
        last_correct_reentry_point_returned = returned_value.value;
    }

    // use caller_last_processed_byte to issue data collision warning if needed:
    if ((returned_value.error == GZIP_MARK_BEGINNING ||
         returned_value.error == GZIP_MARK_FULL_FLUSH) &&
        returned_value.value < caller_last_processed_byte)
    {
        // if caller has not yet extracted data, the warning is to indicate
        // that the point of extraction will be BEFORE indicated `-[bL]` value:
        if (JUST_CREATE_INDEX != indx_n_extraction_opts &&
            COMPRESS_AND_CREATE_INDEX != indx_n_extraction_opts)
        {
            printToStderr("PATCHING WARNING:\n"
                            "    Beginning of extracted data will begin BEFORE indicated\n"
                            "    `-[bL]` value, due to an unfinished gzip-erroneous-stream.\n");
        }
        else
        {
            // unless there was nothing to extract!
            printToStderr("PATCHING WARNING:\n"
                            "    Data extracted around the patching point may overlap.\n");
        }
    }

    return returned_value;
}

// Returns the index position in char 'buffer' array in which ends
// the line number 'number_of_lines'
//  - OR -
// if number_of_lines == 0, returns the number of lines seen in
// passed 'buffer' of size 'size_of_buffer'
// INPUT:
// const unsigned char *buffer: array of chars
// int size_of_buffer   : size of buffer (at most, WINSIZE)
// int number_of_lines  : number of lines to find
// int line_number_format   : 0: '\n', 1: '\r'
// int *there_are_more_chars: totlines may need to be decremented at the end depending on last stream char;
//                            "passed by reference", may be NULL, in which case it is not treated.
// OUTPUT:
// int giveMeNumberOfLinesInChars: index position in 'buffer' at which ends line number number_of_lines. This
//                                 coincides with number of bytes in buffer before next line 'number_of_lines + 1'.
//                                 OR if number_of_lines == 0, number of lines seen in
//                                 passed 'buffer' of size 'size_of_buffer'
int giveMeNumberOfLinesInChars(
    const unsigned char *buffer,
    int size_of_buffer,
    int number_of_lines,
    int line_number_format,
    int *passed_there_are_more_chars)
{
    const unsigned char *original_buffer_beginning = buffer;
    unsigned char *pos;
    int there_are_more_chars = 0; /* totlines may need to be decremented at the end depending on last stream char */
    int have_lines = 0;
    do
    {
        pos = memchr(buffer, ((0 == line_number_format) ? '\n' : '\r'), size_of_buffer);
        if (NULL != pos)
        {
            size_of_buffer -= (pos - buffer) + 1;
            buffer = pos + 1;
            have_lines++;
            if (size_of_buffer > 0)
                there_are_more_chars = 1;
            else
                there_are_more_chars = 0;
        }
        if (have_lines == number_of_lines)
        {
            break;
        }
    } while (NULL != pos && pos < (buffer + size_of_buffer));

    if (NULL != passed_there_are_more_chars)
    {
        *passed_there_are_more_chars = there_are_more_chars;
    }

    if (0 == number_of_lines)
    {
        return have_lines;
    }
    else
    {
        return (int)(buffer - original_buffer_beginning);
    }
}

// Limit buffer output based on range_number_of_bytes or range_number_of_lines,
// if necessary.
// Used in decompress_and_build_index() only @
//      if ( indx_n_extraction_opts == EXTRACT_FROM_BYTE )
// and  if ( indx_n_extraction_opts == EXTRACT_FROM_LINE )
// INPUT:
// uint64_t *range_number_of_bytes: range_number_of_bytes "passed by reference"
// uint64_t *range_number_of_lines: range_number_of_lines "passed by reference"
// unsigned int *have   : have "passed by reference"
// uint64_t offset      : offset in buffer
// uint64_t *output_data_counter  : output_data_counter "passed by reference"
// uint64_t *output_lines_counter : output_lines_counter "passed by reference"
// uint64_t *have_lines : have_lines "passed by reference"
// const unsigned char *WINDOW: WINDOW value on caller, already adjusted;
//                              It is a const because it is not changed.
// int line_number_format: index->line_number_format on caller
// OUTPUT:
// bool limitBufferOutput: if true, process is terminated on caller because range
// (range_number_of_bytes or range_number_of_lines) has been reached
bool limitBufferOutput(
    uint64_t *range_number_of_bytes, uint64_t *range_number_of_lines,
    unsigned int *have, uint64_t offset,
    uint64_t *output_data_counter, uint64_t *output_lines_counter,
    uint64_t *have_lines, const unsigned char *WINDOW, int line_number_format)
{
    bool end_processing_because_of_range = false;

    if (*range_number_of_bytes > 0)
    {
        if (*range_number_of_bytes > (*have - offset))
        {
            *range_number_of_bytes -= (*have - offset);
        }
        else
        {
            // decrement counters to take *range_number_of_bytes into account:
            *output_data_counter -= ((*have - offset) - *range_number_of_bytes);
            *have -= ((*have - offset) - *range_number_of_bytes);
            *output_lines_counter += -*have_lines +
                                     giveMeNumberOfLinesInChars(
                                         WINDOW,
                                         *have - offset, 0,
                                         line_number_format, NULL);
            // process will end after this last fwrite!
            end_processing_because_of_range = true;
        }
    }
    if (*range_number_of_lines > 0)
    {
        if (*range_number_of_lines > *have_lines)
        {
            *range_number_of_lines -= *have_lines;
        }
        else
        {
            // decrement counters to take *range_number_of_lines into account:
            *output_lines_counter += *range_number_of_lines - *have_lines;
            *have_lines = *range_number_of_lines;
            // giveMeNumberOfLinesInChars() won't be never greater than (*have - offset):
            int numberOfLinesInChars = giveMeNumberOfLinesInChars(
                WINDOW,
                *have - offset, *range_number_of_lines,
                line_number_format, NULL);
            *output_data_counter -= ((*have - offset) - numberOfLinesInChars);
            *have -= ((*have - offset) - numberOfLinesInChars);
            // process will end after this last fwrite!
            end_processing_because_of_range = true;
        }
    }

    return end_processing_because_of_range;
}

// Creates index for a gzip stream (file or STDIN);
// action_create_index() always calls this function with an incomplete index file
// if it already exists.
// If an incomplete index is passed, it will be completed from the last
// available index point so the whole gzip stream is not processed again.
// Original (zran.c) comments:
/* Make one entire pass through the compressed stream and build an index, with
   access points about every span bytes of uncompressed output -- span is
   chosen to balance the speed of random access against the memory requirements
   of the list, about 32K bytes per access point.  Note that data after the end
   of the first zlib or gzip stream in the file is ignored.  build_index()
   returns the number of access points on success (>= 1), Z_MEM_ERROR for out
   of memory, Z_DATA_ERROR for an error in the input file, or Z_ERRNO for a
   file read error.  On success, *built points to the resulting index. */
// INPUT:
// FILE *file_in            : input stream
// FILE *file_out           : output stream
// char *file_name : name of the input file associated with FILE *in.
//                            Can be "" (no file name: stdin used), but not NULL.
//                            Used only if there's no usable index && input (FILE *in)
//                            is associated with a file (not stdin) &&
//                            indx_n_extraction_opts == *_TAIL, for the use of the file
//                            size as approximation of the size of the tail to be output.
// uint64_t span            : span
// struct access **built: address of index pointer, equivalent to passed by reference.
//                        Note that index may be received with some (all) points already set
//                        from caller, if an index file was already available - and so this
//                        function must use it or create new points from the last available one,
//                        if needed.
// enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts:
//                      = JUST_CREATE_INDEX: usual behaviour
//                      = SUPERVISE_DO  : supervise a growing "in" gzip stream
//                      = SUPERVISE_DO_AND_EXTRACT_FROM_TAIL: like SUPERVISE_DO but
//                          this will also extract data to stdout, starting from
//                          the last available bytes (tail) on gzip when called.
//                      = EXTRACT_FROM_BYTE: extract from indicated offset byte, to stdout
//                      = EXTRACT_FROM_LINE: extract from indicated line, to stdout
// uint64_t offset      : if indx_n_extraction_opts == EXTRACT_FROM_BYTE, this is the offset byte
//                        in the uncompressed stream from which to extract to stdout.
//                        0 otherwise.
// uint64_t line_number_offset : if indx_n_extraction_opts == EXTRACT_FROM_LINE, this is the offset line
//                               in the uncompressed stream from which to extract to stdout.
//                               0 otherwise.
// char *index_filename   : index will be read/written on-the-fly
//                                    to this index file name.
// int write_index_to_disk  : 1: will write/update the index as usual;
//                            0: will just read, but do not write nor update (overwrite) the index on disk,
//                           and also, do not create/update the index in memory (**built and returned_output.value):
//                           This is done because if index is not written to disk, windows would need to be
//                           maintained in memory, increasing the memory footprint unnecessarily as the index
//                           is not (actually) used later in the app. (Windows could be also emptied, but again,
//                           index points are not used later.)
// int end_on_first_proper_gzip_eof : end file processing on first proper (at feof()) GZIP EOF
//                                    (to be used when the file contains surely only one gzip stream)
// int always_create_a_complete_index : create a 'complete' index file even in case of decompressing errors.
//                                      Also an index pointer (**built) is returned, instead of NULL.
// int waiting_time             : waiting time in seconds between reads when `-[ST]` (always >0)
// int extend_index_with_lines  : 0: create index without line numbers (v0 index)
//                                1: create index WITH line numbers (v1 index) using Unix format (\n)
//                                2: create index WITH line numbers (v1 index) using old Mac format (\r) (compatible with Windows \n\r)
//                                Nonetheless, note that if index previously exists and is
//                                index->index_version == 0 it cannot be extended to v1, and
//                                if it is index->index_version == 1, it cannot be made v0,
//                                so extend_index_with_lines IS RESPECTED ONLY IF INDEX DOES NOT EXIST YET.
// uint64_t expected_first_byte  : indicates that the first byte on compressed input is this, not 1,
//                                and so truncated compressed inputs can be used. Only can be used (>1) if
//                                indx_n_extraction_opts is EXTRACT_FROM_BYTE or EXTRACT_FROM_LINE.
// int gzip_stream_may_be_damaged: indicates if a decompression in advance will be used and how many CHUNKs
//                                will be decompressed in advance then (CHUNKS_TO_DECOMPRESS_IN_ADVANCE).
//                                If 0, no decompression in advance is made.
// bool lazy_gzip_stream_patching_at_eof: when used with `-[ST]` implies that checking
//                                for errors in stream is made as quick as possible as the gzip file
//                                grows, not respecting then at EOF the CHUNKS_TO_DECOMPRESS_IN_ADVANCE value.
// uint64_t range_number_of_bytes: indicates how many bytes to extract when using `-[bL]` (offset, line_number_offset)
// uint64_t range_number_of_lines: indicates how many lines to extract when using `-[bL]` (offset, line_number_offset)
// OUTPUT:
// struct returned_output: contains two values:
//      .error: Z_* error code or Z_OK if everything was ok
//      .value: size of built index (index->have)
local struct returned_output decompress_and_build_index(
    FILE *file_in, FILE *file_out, char *file_name, uint64_t span, struct access **built,
    enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts, uint64_t offset, uint64_t line_number_offset,
    char *index_filename,
    int end_on_first_proper_gzip_eof, int always_create_a_complete_index,
    int waiting_time, int extend_index_with_lines, uint64_t expected_first_byte,
    int gzip_stream_may_be_damaged, bool lazy_gzip_stream_patching_at_eof,
    uint64_t range_number_of_bytes, uint64_t range_number_of_lines)
{
    struct returned_output ret;
    uint64_t totin = 0;           /* our own total counters to avoid 4GB limit */
    uint64_t totout = 0;          /* our own total counters to avoid 4GB limit */
    uint64_t totlines = 1;        /* counts total line numbers in uncompressed data from file_in */
    int there_are_more_chars = 0; /* totlines may need to be decremented at the end depending on last stream char */
    uint64_t last = 0;            /* totout value of last access point */
    uint64_t offset_in = 0;       /* offset in compressed data to reach (opposed to "offset" in uncompressed data) */
    uint64_t have_lines = 0;      /* number of lines in last chunk of uncompressed data */
    uint64_t avail_in_0;          /* because strm.avail_in may not exhausts every cycle! */
    uint64_t avail_out_0;         /* because strm.avail_out may not exhausts every cycle! */
    struct access *index = NULL;  /* access points being generated */
    struct point *here = NULL;
    uint64_t actual_index_point = 0;                       // only set initially to >0 if NULL != *built
    uint64_t output_data_counter = 0;                      // counts uncompressed bytes extracted to stdout
    uint64_t output_lines_counter = 0;                     // counts lines extracted to stdout
    uint64_t decompress_until_this_point_byte = 0LLU;      // used if gzip_stream_may_be_damaged == 1 &&
                                                           // decompress_in_advance() found a valid new
                                                           // GZIP-beginning at this byte in the file_in stream.
    int decompress_until_this_point_type = GZIP_MARK_NONE; // stores type of beginning:
                                                           // ( GZIP_MARK_FULL_FLUSH or GZIP_MARK_BEGINNING )
                                                           // when decompress_until_this_point_byte > 0. Also
                                                           // GZIP_MARK_ERROR && decompress_until_this_point_byte > 0
                                                           // when an unrecoverable error was detected.
    unsigned char *decompressed_window = NULL;
    z_stream strm;
    FILE *index_file = NULL;
    size_t index_last_written_point = 0;
    int extraction_from_offset_in = 0;           // 1: extract data using offset_in value
    int start_extraction_on_first_depletion = 0; // 0: extract - no depletion interaction.
                                                 // 1: start extraction on first depletion.
    int decompressing_with_gzip_headers = 0;     // 1 if inflateInit2(&strm, 47);
    int gzip_eof_detected = 0;                   // 1 if a GZIP file format EOF is detected, and so, feof() is not trustworthy
    unsigned char input[CHUNK];                  // TODO: convert to malloc
    unsigned char window[WINSIZE];               // TODO: convert to malloc
    unsigned window_size = 0;                    // restarted on every reinitiation of strm data
    unsigned char window2[WINSIZE];              // TODO: convert to malloc
    unsigned window2_size = 0;                   // size of data stored in window2 buffer
    uint64_t index_points_0 = 0;                 // marker to detect index updates (and to not write if index isn't updated)
    uint64_t index_file_size_0 = 0;              // marker to detect index updates (and to not write if index isn't updated)

    ret.value = 0LLU;
    ret.error = Z_OK;

    // Inform index filename
    if (NULL != (*built) &&
        // if index->have == 0 index is superfluous
        (*built)->have > 0 &&
        // this condition is never true because index is passed always as incomplete
        (*built)->index_complete == 1)
    {
        // even though index is complete, processing will occur because gzip file may have changed!
        printToStderr("Using index '%s'...\n", index_filename);
    }
    else
    {
        if (strlen(index_filename) > 0)
            printToStderr("Processing index to '%s'...\n", index_filename);
        else
            printToStderr("Processing index to STDOUT...\n");
    }

    /* open index_filename for binary reading & writing */
    if (strlen(index_filename) > 0 &&
        (NULL == (*built) || // if passed index doesn't exists yet,
                             // or it's an incomplete index in which case it may need to be completed
         (*built)->index_complete == 0))
    {
        if (access(index_filename, F_OK) != -1)
        {
            // index_filename already exist:
            // "r+": Open a file for update (both for input and output). The file must exist.
            // r+, because the index may be incomplete, and so decompress_and_build_index() will
            // append new data and complete it (->have & ->size to correct values, not 0x0..0, 0xf..f).
            index_file = fopen(index_filename, "r+b");
        }
        else
        {
            // index_filename does not exist:
            index_file = fopen(index_filename, "w+b");
        }
    }
    else
    {
        // restrictions to not collide index output with data output to stdout
        // MUST have been made on caller.
        SET_BINARY_MODE(STDOUT); // sets binary mode for stdout in Windows
        index_file = stdout;
    }
    if (NULL == index_file)
    {
        printToStderr("Could not write index to file '%s'.\n", index_filename);
        ret.error = Z_ERRNO;
        return ret;
    }

    /* inflate the input, maintain a sliding window, and build an index -- this
       also validates the integrity of the compressed data using the check
       information at the end of the gzip or zlib stream */

    // if an index is already passed, use it:
    if (NULL != (*built) &&
        (*built)->have > 0)
    {
        // NULL != *built (there is a previous index available: use it!)
        // if index->have == 0 index is superfluous

        index = *built;
        index_points_0 = index->have;         // marker to detect index updates (and to not write if index isn't updated)
        index_file_size_0 = index->file_size; // marker to detect index updates (and to not write if index isn't updated)

        /* initialize file and inflate state to start there */
        strm.zalloc = Z_NULL;
        strm.zfree = Z_NULL;
        strm.opaque = Z_NULL;
        strm.avail_in = 0;
        strm.next_in = Z_NULL;
        ret.error = inflateInit2(&strm, -15); /* raw inflate */
        if (ret.error != Z_OK)
            return ret;

        index_last_written_point = index->have;

        //
        // Select an index point to start, depending on indx_n_extraction_opts
        //
        if (indx_n_extraction_opts == JUST_CREATE_INDEX)
        {
            // move to last available index point, and continue from it
            actual_index_point = index->have - 1;
            // this index must be completed from last point: index->list[index->have-1]
            totin = index->list[actual_index_point].in;
            totout = index->list[actual_index_point].out;
            totlines = index->list[actual_index_point].line_number;
            last = totout;
            here = &(index->list[actual_index_point]);
        }

        if (indx_n_extraction_opts == EXTRACT_FROM_BYTE)
        {
            // move to the point needed for positioning on offset, or
            // move to last available point if offset can't be reached
            // with actually available index
            here = index->list;
            actual_index_point = 0;
            while (
                ++actual_index_point &&
                actual_index_point < index->have &&
                here[1].out <= offset)
                here++;
            actual_index_point--;
            totin = index->list[actual_index_point].in;
            totout = index->list[actual_index_point].out;
            totlines = index->list[actual_index_point].line_number;
            last = totout;
            // offset value comes from caller as parameter
        }

        if (indx_n_extraction_opts == EXTRACT_FROM_LINE)
        {
            // move to the point needed for positioning on a line, or
            // move to last available point if offset can't be reached
            // with actually available index
            here = index->list;
            actual_index_point = 0;
            while (
                ++actual_index_point &&
                actual_index_point < index->have &&
                // if index point is on the desired line number, we must start
                // on the previous one to retrieve the line from its beginning:
                here[1].line_number < line_number_offset)
                here++;
            actual_index_point--;
            totin = index->list[actual_index_point].in;
            totout = index->list[actual_index_point].out;
            totlines = index->list[actual_index_point].line_number;
            last = totout;
            // offset value comes from caller as parameter
        }

        assert(NULL != here);

        // fseek in data for correct position
        // using here index data:
        {
            uint64_t position = here->in - (here->bits ? 1 : 0);

            // checking expected_first_byte value:
            // (although this has already been done for `-b`, `-L` could not be checked until now)
            if ((expected_first_byte - 1) > position)
            {
                printToStderr("ERROR: `-n %llu` is beyond required index point @%llu byte.\n",
                              expected_first_byte, position);
                ret.error = Z_ERRNO;
                // strm and index are already filled, so a simple return isn't possible:
                goto decompress_and_build_index_error;
            }
            else
            {
                position -= (expected_first_byte - 1);
            }

            if (stdin == file_in)
            {
                // read input until here->in - (here->bits ? 1 : 0)
                uint64_t pos = 0;
                ret.error = 0;
                while (pos < position)
                {
                    if (!fread(input, 1, (pos + CHUNK < position) ? CHUNK : (position - pos), file_in))
                    {
                        ret.error = -1;
                        break;
                    }
                    pos += CHUNK;
                }
            }
            else
            {
                ret.error = fseeko(file_in, position, SEEK_SET);
            }

            if (ret.error == -1)
                goto decompress_and_build_index_error;

            if (here->bits)
            {
                int i;
                i = getc(file_in);
                if (i == -1)
                {
                    ret.error = ferror(file_in) ? Z_ERRNO : Z_DATA_ERROR;
                    goto decompress_and_build_index_error;
                }
                (void)inflatePrime(&strm, here->bits, i >> (8 - here->bits));
            }
        }

        // obtain window and initialize with it zlib's Dictionary
        if (here->window == NULL && here->window_beginning != 0)
        {
            /* index' window data is not on memory,
            but we have position and size on index file, so we load it now */
            FILE *index_file;
            if (index->file_name == NULL ||
                strlen(index->file_name) == 0)
            {
                printToStderr("Error while opening index file.\nAborted.\n");
                ret.error = Z_ERRNO;
                goto decompress_and_build_index_error;
            }
            if (NULL == (index_file = fopen(index->file_name, "rb")) ||
                0 != fseeko(index_file, here->window_beginning, SEEK_SET))
            {
                printToStderr("Error while opening index file.\nAborted.\n");
                ret.error = Z_ERRNO;
                goto decompress_and_build_index_error;
            }
            // here->window_beginning = 0; // this is not needed
            if (here->window_size > 0 &&
                (NULL == (here->window = malloc(here->window_size)) ||
                 !fread(here->window, here->window_size, 1, index_file)))
            {
                printToStderr("Error while reading index file.\nAborted.\n");
                ret.error = Z_ERRNO;
                goto decompress_and_build_index_error;
            }
            fclose(index_file);
        }

        // Windows are ALWAYS stored compressed, so decompress it now:
        /* decompress() use uint64_t counters, but index->list->window_size is smaller */
        // local_window_size in order to avoid deleting the on-memory here->window_size,
        // that may be needed later if index must be increased and written to disk (fseeko).
        window_size = 0;
        if (here->window_size > 0)
        {
            uint64_t local_window_size = here->window_size;
            /* window is compressed on memory, so decompress it */
            decompressed_window = decompress_chunk(here->window, &local_window_size);
            if (NULL == decompressed_window)
            {
                ret.error = Z_ERRNO;
                goto decompress_and_build_index_error;
            }
            (void)inflateSetDictionary(&strm, decompressed_window, local_window_size); // (local_window_size may not be WINSIZE)
            free(decompressed_window);
            decompressed_window = NULL;
            window_size = local_window_size;
        }

    } // end if ( NULL != (*built) && (*built)->have > 0 ) {

    // decrement offset_in, offset and line_number_offset by actual position:
    if (offset_in > 0 &&
        NULL != here)
    {
        if (here->in > offset_in)
            offset_in = 0;
        else
            offset_in -= here->in;
    }
    if (offset > 0 &&
        NULL != here)
    {
        if (here->out > offset)
            offset = 0;
        else
            offset -= here->out;
    }
    if (line_number_offset > 0)
    {
        if (NULL != here)
        {
            // there's an index loaded
            if (here->line_number > line_number_offset)
                line_number_offset = 0;
            else
                line_number_offset -= here->line_number;
        }
        else
        {
            // there's no index available
            line_number_offset--;
        }
    }

    // default zlib initialization
    // when no index entry points have been found:
    if (NULL == (*built) ||
        NULL == here)
    {
        // NULL != *built (there is no previous index available: build it from scratch)

        /* initialize inflate */
        strm.zalloc = Z_NULL;
        strm.zfree = Z_NULL;
        strm.opaque = Z_NULL;
        strm.avail_in = 0;
        strm.next_in = Z_NULL;
        ret.error = inflateInit2(&strm, 47); /* automatic zlib or gzip decoding (15 + automatic header detection) */
        decompressing_with_gzip_headers = 1;
        if (ret.error != Z_OK)
            goto decompress_and_build_index_error;
        totin = totout = last = 0;
        totlines = 1;
        index = NULL; /* will be allocated on first addpoint() */

        if (extend_index_with_lines > 0)
        {
            // mark index as index_version = 1 to store line numbers when serialize();
            // in order to do this, index must be created now (empty)
            index = create_empty_index();
            if (index == NULL)
            { // Oops!?
                ret.error = Z_MEM_ERROR;
                goto decompress_and_build_index_error;
            }
            index->index_version = 1;
            // here extend_index_with_lines can be 3 (implicit `-x`)
            if (extend_index_with_lines == 2)
                index->line_number_format = 1;
            else
            {
                index->line_number_format = 0;
                // end now possible extend_index_with_lines == 3 (implicit `-x`)
                extend_index_with_lines = 1;
            }
        }
    }

    // initialize decompress_in_advance() internal (static) processing variables:
    if (gzip_stream_may_be_damaged > 0)
    {
        decompress_in_advance(NULL, 
                              totin, CHUNKS_TO_DECOMPRESS_IN_ADVANCE, CHUNKS_TO_LOOK_BACKWARDS,
                              indx_n_extraction_opts,
                              decompressing_with_gzip_headers,
                              end_on_first_proper_gzip_eof,
                              waiting_time,
                              lazy_gzip_stream_patching_at_eof,
                              expected_first_byte,
                              DECOMPRESS_IN_ADVANCE_RESET); // returned value is always ok when called with this value
    }

    // initialize output window
    strm.next_out = window;
    strm.avail_out = WINSIZE;
    avail_out_0 = strm.avail_out;
    do
    {
        /* get some compressed data from input file */

        // bgzip-compatible-streams code:
        if (ret.error == Z_STREAM_END &&
            strm.avail_in > 0)
        { // if strm.avail_in is casually 0, this block of code isn't needed.
            // readjust input data moving it to the beginning of buffer:
            // (strm.next_in floats from input to input+CHUNK)
            memmove(input, strm.next_in, strm.avail_in); // memmove() is overlapping-safe
        }
        // .................................................
        // note that here strm.avail_in > 0 only if ret.error == Z_STREAM_END
        int strm_avail_in0 = strm.avail_in;
        if (!feof(file_in))
        { // on last block, strm.avail_in > 0 is possible with feof(file_in)==1 already!
            strm.avail_in = fread(input + strm_avail_in0, 1, CHUNK - strm_avail_in0, file_in);
            strm.avail_in += strm_avail_in0;
        }
        if (feof(file_in))
        {
            // generic check for growing files (not related to bgzip-compatible-streams code):
            // check that file hasn't shrunk, what would mean that the file
            // has been overwritten from the beginning (possible with rsyslog logs, for example)
            if (strlen(file_name) > 0 &&    // this check cannot be done on STDIN
                expected_first_byte == 1LLU // this check must not be done if using `-n` (because then st.st_size < totin in general)
            )
            {
                struct stat st;
                stat(file_name, &st);
                if ((uint64_t)(st.st_size + GZIP_HEADER_SIZE_BY_ZLIB) < totin)
                {
                    // file has shrunk! so do a correct finish of the action_create_index (whatever it is)
                    // (Note that it is not possible that this condition arises when accessing a truncated gzip
                    // file with its corresponding (not-truncated) complete index file, because in this case
                    // fseeko() previously failed. Also if gzip-file size is 0 (which is possible and allowed)
                    // this condition won't arise because (10 < 0) is false (index doesn't exist) and
                    // (10 < 10) is false (index contains always the first access point))
                    ret.error = EXIT_FILE_OVERWRITTEN;
                    break;
                }
            }
        }
        // .................................................
        // decompressor state MUST be reinitiated after Z_STREAM_END
        if (ret.error == Z_STREAM_END)
        {
            strm_avail_in0 = strm.avail_in;
            (void)inflateEnd(&strm);
            strm.zalloc = Z_NULL;
            strm.zfree = Z_NULL;
            strm.opaque = Z_NULL;
            strm.avail_in = 0;
            strm.next_in = Z_NULL;
            ret.error = inflateInit2(&strm, 47); /* automatic zlib or gzip decoding (15 + automatic header detection) */
            decompressing_with_gzip_headers = 1;
            if (ret.error != Z_OK)
                goto decompress_and_build_index_error;
            strm.avail_in = strm_avail_in0;
            // it is compulsory to reinitiate also output data:
            strm.avail_out = WINSIZE;
            strm.next_out = window;
            avail_out_0 = strm.avail_out;
            window_size = 0;
        }
        // end of bgzip-compatible-streams code

        avail_in_0 = strm.avail_in;

        if (indx_n_extraction_opts == JUST_CREATE_INDEX ||
            indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
            indx_n_extraction_opts == EXTRACT_FROM_LINE)
        {
            // with not Supervising options, strm.avail_in == 8 + eof is equivalent to Correct END OF GZIP at EOF
            if (feof(file_in) && strm.avail_in == 8)
            {
                break;
            }
        }

        if (ferror(file_in))
        {
            ret.error = Z_ERRNO;
            goto decompress_and_build_index_error;
        }
        if (strm.avail_in == 0)
        {
            ret.error = Z_DATA_ERROR;
            goto decompress_and_build_index_error;
        }
        strm.next_in = input;

        //
        // INSERTION POINT FOR GZIP RECOVERY CODE (gzip_stream_may_be_damaged)
        // IS PROBABLY HERE
        //
        if (gzip_stream_may_be_damaged > 0 &&
            decompress_until_this_point_type != GZIP_MARK_BEGINNING &&
            decompress_until_this_point_type != GZIP_MARK_FULL_FLUSH &&
            decompress_until_this_point_byte == 0LLU)
        {
            struct returned_output ret;
            // an internal and compulsory fseeko() failed, so process
            // MUST stop here, as read won't be correct at this ftello(file_in):
            printToStderr("PATCHING ERROR: Fatal Read error of input file obliges to stop process.\n");
            ret.error = Z_DATA_ERROR;
            goto decompress_and_build_index_error;
        }

        /* process all of strm.next_in (size strm.avail_in), or until end of stream */
        do
        {

            /* reset sliding window if necessary */
            if (strm.avail_out == 0)
            {
                strm.avail_out = WINSIZE;
                strm.next_out = window;
                avail_out_0 = strm.avail_out;
            }

            // input data MUST (also) be checked against decompress_until_this_point_byte
            // BEFORE inflate() in order to assure
            // that no invalid data is fed to the process !!!
            // ( The other point of checking is [C]. )
            if (gzip_stream_may_be_damaged > 0 &&
                decompress_until_this_point_byte > 0LLU &&
                (GZIP_MARK_FULL_FLUSH == decompress_until_this_point_type ||
                 GZIP_MARK_BEGINNING == decompress_until_this_point_type) &&
                (totin + strm.avail_in) >= decompress_until_this_point_byte)
            {
                if (totin > decompress_until_this_point_byte)
                {
                    // inflate() process cannot continue as we are well beyond the
                    // estimated good point of reentry due to a forthcoming gzip error,
                    // so we must diverge to set a new entire inflateInit():
                    // ( This goto point (which I know it'd not exist) must be inside this
                    // do { ... } while ( strm.avail_in != 0 ); cycle
                    // not to disturb (too much) the proper progression of the algorithm )
                    goto decompress_and_build_index_Set_next_good_decompression_point;
                }
                else
                {
                    // We are still in the good data buffer, but we must process with
                    // caution and chop the entry before the next good reentry point, so
                    // cut input data to just the byte before decompress_until_this_point_byte
                    strm.avail_in -= (totin + strm.avail_in) - decompress_until_this_point_byte - 1;
                }
            }

            /* inflate until out of input, output, or at end of block --
               update the total input and output counters */
            totin += strm.avail_in;
            totout += strm.avail_out;
            window_size += strm.avail_out;       // update window_size available size
            ret.error = inflate(&strm, Z_BLOCK); /* return at end of block */
            totin -= strm.avail_in;
            totout -= strm.avail_out;
            { // update window_size available size
                window_size -= strm.avail_out;
                if (window_size > WINSIZE)
                    window_size = WINSIZE;
            }
            // count lines in this decompressed chunk
            if (NULL != index && // if lines must be counted, index passed must be !=NULL
                                 // to mark index->index_version == 1
                index->index_version == 1)
            {

                have_lines = giveMeNumberOfLinesInChars(
                    // output data is in window + an offset
                    window + (WINSIZE - avail_out_0),
                    avail_out_0 - strm.avail_out,
                    0, index->line_number_format,
                    &there_are_more_chars);

                totlines += have_lines;
            }

            // .................................................
            // treat possible gzip tail:
            if (ret.error == Z_STREAM_END &&
                strm.avail_in >= 8 &&
                decompressing_with_gzip_headers == 0)
            {
                // discard this data as it is probably the 8 gzip-tail bytes (4 CRC + 4 size%2^32)
                // and zlib is not able to consume it if inflateInit2(&strm, -15) (raw decompressing)
                strm.avail_in -= 8;
                strm.next_in += 8;
                totin += 8; // data is discarded from strm input, but it MUST be counted in total input
                if (offset_in >= 8)
                    offset_in -= 8; // data is discarded from strm input, and also from offset_in
                else
                    offset_in = 0;
                if (end_on_first_proper_gzip_eof == 1 ||
                    ((indx_n_extraction_opts == JUST_CREATE_INDEX ||
                      indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
                      indx_n_extraction_opts == EXTRACT_FROM_LINE) &&
                     feof(file_in)))
                {
                    gzip_eof_detected = 0; // to exit outer loop with gzip_eof_detected == 0
                }
                else
                {
                    gzip_eof_detected = 1;
                }
                break;
                // avail_in_0 doesn't need to be decremented as it tries to count raw stream input bytes
            }
            // end of treat possible gzip tail
            // .................................................

            if (ret.error == Z_NEED_DICT)
            {
                ret.error = Z_DATA_ERROR;
            }

            if (ret.error == Z_STREAM_END)
            {
                if (indx_n_extraction_opts == JUST_CREATE_INDEX ||
                    indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
                    indx_n_extraction_opts == EXTRACT_FROM_LINE)
                {
                    // with not Supervising options, a Z_STREAM_END at feof() is correct!
                    if (feof(file_in) && strm.avail_in == 0)
                    {
                        gzip_eof_detected = 0; // to exit loop, as "gzip_eof_detected == 1" is one ORed condition
                                               // and now this variable is not needed anymore.
                        break;
                    }
                }
                if (end_on_first_proper_gzip_eof == 0)
                    gzip_eof_detected = 1;
                if (gzip_eof_detected == 0)
                {
                    if (end_on_first_proper_gzip_eof == 1 &&
                        feof(file_in))
                    {
                        gzip_eof_detected = 0;
                    }
                    else
                    {
                        gzip_eof_detected = 1;
                        printToStderr("Warning: GZIP end detected in the middle of data: deactivating `-E`\n");
                        end_on_first_proper_gzip_eof = 0;
                    }
                }
                if (offset_in >= 8)
                {
                    offset_in -= 8; // data is discarded from strm input, and also from offset_in
                }
                else
                {
                    offset_in = 0;
                }
                break;
            }

            //
            // if required by passed indx_n_extraction_opts option, extract to stdout:
            //
            // EXTRACT_FROM_BYTE: extract all:
            if (indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
                indx_n_extraction_opts == EXTRACT_FROM_LINE)
            {

                // end processing based on range_number_of_bytes or range_number_of_lines:
                bool end_processing_because_of_range = false;

                if (indx_n_extraction_opts == EXTRACT_FROM_BYTE)
                {
                    unsigned have = avail_out_0 - strm.avail_out;
                    if (offset > have)
                    {
                        offset -= have;
                    }
                    else
                    {
                        if ((offset > 0 && offset <= have) ||
                            offset == 0)
                        {
                            // print have - offset bytes
                            // If offset==0 (from offset byte on) this prints always all bytes:
                            output_data_counter += have - offset;
                            /*
                             * also count lines, if necessary:
                             */
                            if (offset != 0 &&   // Count lines ONLY in offset block, not in all window.
                                NULL != index && // If lines must be counted, index passed must be !=NULL
                                                 // to mark index->index_version == 1
                                                 // and use index->line_number_format
                                index->index_version == 1)
                            {
                                have_lines = giveMeNumberOfLinesInChars(
                                    // output data is in window + an offset
                                    window + offset + (WINSIZE - avail_out_0), have - offset,
                                    0, index->line_number_format,
                                    NULL);
                            }
                            // if index->index_version == 0 then output_lines_counter is simply not used
                            output_lines_counter += have_lines;
                            /*
                             * end of "also count lines, if necessary"
                             */
                            // limit buffer output based on range_number_of_bytes or range_number_of_lines,
                            // if necessary:
                            end_processing_because_of_range = limitBufferOutput(
                                &range_number_of_bytes, &range_number_of_lines, &have, offset,
                                &output_data_counter, &output_lines_counter, &have_lines,
                                window + offset + (WINSIZE - avail_out_0),
                                index->line_number_format);
                            if (fwrite(window + offset + (WINSIZE - avail_out_0), 1, have - offset, file_out) != (have - offset) ||
                                ferror(file_out))
                            {
                                (void)inflateEnd(&strm);
                                ret.error = Z_ERRNO;
                                goto decompress_and_build_index_error;
                            }
                            offset = 0;
                            fflush(file_out);
                        }
                    }
                    avail_out_0 = strm.avail_out;
                }
                else
                {
                    // indx_n_extraction_opts == EXTRACT_FROM_LINE
                    unsigned have = avail_out_0 - strm.avail_out;
                    if (line_number_offset > have_lines)
                    {
                        line_number_offset -= have_lines;
                    }
                    else
                    {
                        if ((line_number_offset > 0 && line_number_offset <= have_lines) ||
                            line_number_offset == 0)
                        {
                            output_lines_counter += have_lines - line_number_offset;
                            // If line_number_offset==0 (from line_number_offset byte on) this prints always all bytes:
                            if (line_number_offset != 0)
                            {
                                // calculate at which byte to start fwrite output:
                                offset = giveMeNumberOfLinesInChars(
                                    window + (WINSIZE - avail_out_0), avail_out_0 - strm.avail_out,
                                    line_number_offset, index->line_number_format,
                                    NULL);
                                if ( // giveMeNumberOfLinesInChars() only if range_number_of_* > 0
                                     // just to spare some CPU cycles (as limitBufferOutput() doesn't
                                     // change anything if ! range_number_of_* > 0)
                                    range_number_of_bytes > 0 ||
                                    range_number_of_lines > 0)
                                {
                                    have_lines = giveMeNumberOfLinesInChars(
                                        window + offset + (WINSIZE - avail_out_0),
                                        have - offset, 0,
                                        index->line_number_format, NULL);
                                }
                            }
                            else
                            {
                                offset = 0;
                            }
                            output_data_counter += have - offset;
                            // limit buffer output based on range_number_of_bytes or range_number_of_lines,
                            // if necessary:
                            end_processing_because_of_range = limitBufferOutput(
                                &range_number_of_bytes, &range_number_of_lines, &have, offset,
                                &output_data_counter, &output_lines_counter, &have_lines,
                                window + offset + (WINSIZE - avail_out_0),
                                index->line_number_format);
                            if (fwrite(window + offset + (WINSIZE - avail_out_0), 1, have - offset, file_out) != (have - offset) ||
                                ferror(file_out))
                            {
                                (void)inflateEnd(&strm);
                                ret.error = Z_ERRNO;
                                goto decompress_and_build_index_error;
                            }
                            line_number_offset = 0;
                            offset = 0;
                            fflush(file_out);
                        }
                    }
                    avail_out_0 = strm.avail_out;
                }

                // end process if end_processing_because_of_range == true
                if (true == end_processing_because_of_range)
                {
                    goto decompress_and_build_index_error;
                }
            }
            else
            {
                // extraction_from_offset_in marks the use of "offset_in"
                if (extraction_from_offset_in == 1)
                {
                    unsigned have = avail_out_0 - strm.avail_out;
                    unsigned have_in = avail_in_0 - strm.avail_in;
                    avail_in_0 = strm.avail_in;
                    if ((offset_in > 0 && offset_in <= have_in) ||
                        offset_in <= 0)
                    {
                        // print all "have" bytes as with offset_in it is not possible
                        // to know how much output discard (uncompressed != compressed)
                        output_data_counter += have;
                        if (fwrite(window + (WINSIZE - avail_out_0), 1, have, file_out) != have ||
                            ferror(file_out))
                        {
                            (void)inflateEnd(&strm);
                            ret.error = Z_ERRNO;
                            goto decompress_and_build_index_error;
                        }
                        offset_in = 0;
                        fflush(file_out);
                    }
                    else
                    {
                        offset_in -= have_in;
                    }
                }
                avail_out_0 = strm.avail_out;
            }

        decompress_and_build_index_Set_next_good_decompression_point:
            // Check decompress_until_this_point_byte to see if decompression has reached
            // or surpassed the point, and restart the strm structure again in this case.
            //
            // Gzip-reinitiation check MUST be done BEFORE adding a new point to the index,
            // because the (possible) point at decompress_until_this_point_byte Byte
            // will be a zero-length window one, so actual window(s) must be emptied.
            //
            if ((GZIP_MARK_BEGINNING == decompress_until_this_point_type ||
                 GZIP_MARK_FULL_FLUSH == decompress_until_this_point_type) &&
                totin >= decompress_until_this_point_byte)
            { // [C]
                // gzip-stream MUST be reinitiated at byte decompress_until_this_point_byte
                // in order to avoid an error after the actual unfinished gzip stream
                // previously detected by decompress_in_advance().
                // Reinitiation is complete, so possible strm.avail_in bytes remaining (is
                // this possible?) will be discarded.
                // All counters will remain the same as the stream will be considered "correct".
                totin = decompress_until_this_point_byte; // do not count duplicate input bytes, even if that is what occurred
                (void)inflateEnd(&strm);
                strm.zalloc = Z_NULL;
                strm.zfree = Z_NULL;
                strm.opaque = Z_NULL;
                strm.avail_in = 0;
                strm.next_in = Z_NULL;
                if (GZIP_MARK_FULL_FLUSH == decompress_until_this_point_type)
                {
                    // raw inflate
                    ret.error = inflateInit2(&strm, -15);
                    decompressing_with_gzip_headers = 0;
                }
                else
                {
                    // automatic zlib or gzip decoding (15 + automatic header detection)
                    ret.error = inflateInit2(&strm, 47);
                    decompressing_with_gzip_headers = 1;
                }
                if (Z_OK != ret.error)
                {
                    goto decompress_and_build_index_error;
                }
                strm.avail_in = 0; // == 0 in order to oblige
                                   // to begin a new cycle with fread()
                                   // at the end of this cycle
                                   // "do{...}while ( strm.avail_in != 0 );".
                                   // This will also set strm.next_in = input;
                                   // at its proper time.
                strm.next_out = window;
                strm.avail_out = WINSIZE;
                // adjacent counters:
                avail_out_0 = strm.avail_out;
                window_size = 0;
                avail_in_0 = strm.avail_in; // 0

                //
                // set ret.error to a Z_OK value, to always force a new cycle
                // in the cycle
                // "do{...} while ( ret.error != Z_STREAM_END || strm.avail_in > 0 || gzip_eof_detected == 1 );".
                // (I think here it should be Z_OK always, but who knows!)
                //
                ret.error = Z_OK;
                //
                // last step is to ensure that file_in pointer is set to next gzip valid byte:
                //
                if (0 != fseeko(file_in, decompress_until_this_point_byte, SEEK_SET))
                {
                    goto decompress_and_build_index_error;
                }
                //
                // and reset decompress_until_this_point_* variables:
                //
                decompress_until_this_point_byte = 0LLU;
                decompress_until_this_point_type = GZIP_MARK_NONE;
                //
                // and reset decompress_in_advance() internal state
                //
                decompress_in_advance(NULL, 
                                      totin, CHUNKS_TO_DECOMPRESS_IN_ADVANCE, CHUNKS_TO_LOOK_BACKWARDS,
                                      indx_n_extraction_opts,
                                      decompressing_with_gzip_headers,
                                      end_on_first_proper_gzip_eof,
                                      0,
                                      false,
                                      0LLU,
                                      DECOMPRESS_IN_ADVANCE_SOFT_RESET); // returned value is always ok when called with this value
                //
                // all done!
                //
            }

            /* if at end of block, consider adding an index entry (note that if
               data_type indicates an end-of-block, then all of the
               uncompressed data from that block has been delivered, and none
               of the compressed data after that block has been consumed,
               except for up to seven bits) -- the totout == 0 provides an
               entry point after the zlib or gzip header, and assures that the
               index always has at least one access point; we avoid creating an
               access point after the last block by checking bit 6 of data_type
             */
            if ((strm.data_type & 128) && !(strm.data_type & 64) &&
                (totout == 0 || totout - last > span))
            {

                // check actual_index_point to see if we've passed
                // the end of the passed previous index, and so
                // we must addpoint() from now on :
                if (actual_index_point > 0)
                    ++actual_index_point;
                if (NULL != index &&
                    actual_index_point > (index->have - 1))
                {
                    actual_index_point = 0; // 0: actual_index_point won't be incremented or checked anymore
                }
                if (actual_index_point == 0 &&
                    // addpoint() only if index doesn't yet exist or it is incomplete
                    (NULL == index || index->index_complete == 0) &&
                    // do not add points if position is lower than the last index point available:
                    // (this can happen if -s is less now, than when the index was created!)
                    (NULL == index ||
                     index->have == 0 || // if index->index_version == 1 index has been previously created (!=NULL)
                     (index->have > 0 &&
                      index->list[index->have - 1].in < totin &&
                      (index->list[index->have - 1].out + span) <= totout)))
                {

                    index = addpoint(index, strm.data_type & 7, totin,
                                        totout, strm.avail_out, window, window_size, totlines, 1);
                    if (NULL == index)
                    {
                        ret.error = Z_MEM_ERROR;
                        goto decompress_and_build_index_error;
                    }

                    // write added point!
                    // note that points written are automatically emptied of its window values
                    // in order to use as less memory as possible
                    if (!serialize_index_to_file(index_file, index, index_last_written_point))
                        goto decompress_and_build_index_error;

                    if (NULL != index)
                        index_last_written_point = index->have;
                }

                last = totout;
            }

        } while (strm.avail_in != 0);

    } while (ret.error != Z_STREAM_END || strm.avail_in > 0 || gzip_eof_detected == 1);

    /* clean up */
    (void)inflateEnd(&strm);
    /* and return index (release unused entries in list) */
    if (NULL != index &&
        index->have > 0)
    {
        struct point *next = realloc(index->list, sizeof(struct point) * index->have);
        if (NULL == next)
        {
            ret.error = Z_MEM_ERROR;
            ret.value = 0;
            goto decompress_and_build_index_error;
        }
        index->list = next;
        index->size = index->have;
        index->file_size = totout; /* size of uncompressed file (useful for bgzip files) */
        if (0 == there_are_more_chars && totlines > 1)
        {
            totlines--;
        }
        index->number_of_lines = totlines; /* lines in uncompressed file */
    }

    // once all index values are filled, close index file: a last call must be done
    // with index_last_written_point = index->have
    if (NULL != index &&
        index->index_complete == 0)
    {
        // use markers to detect index updates and to not write if index isn't updated
        if (index_points_0 != index->have ||
            index_file_size_0 != index->file_size)
        {
            if (!serialize_index_to_file(index_file, index, index->have))
                goto decompress_and_build_index_error;
            if (strlen(index_filename) > 0)
                printToStderr("Index written to '%s'.\n", index_filename);
            else
                printToStderr("Index written to STDOUT.\n");
            /* index is now complete */
        }
    }

    if (NULL != index_file)
        fclose(index_file);

    // print output_data_counter info
    if (output_data_counter > 0)
    {
        printToStderr("%s (%llu Bytes) of data extracted.\n",
                      giveMeSIUnits(output_data_counter, 1), output_data_counter);
    }

    // print output_lines_counter info
    if ((indx_n_extraction_opts == EXTRACT_FROM_LINE ||
         (NULL != index && index->index_version > 0)) &&
        output_lines_counter > 0)
    {
        printToStderr("%llu lines extracted", output_lines_counter);
        if (output_lines_counter > 1000)
        {
            printToStderr(" (%s).\n", giveMeSIUnits(output_lines_counter, 0));
        }
        else
        {
            printToStderr(".\n");
        }
    }

    *built = index;
    if (NULL != index)
    {
        ret.value = index->have;
    }
    else
    {
        ret.value = 0;
    }
    return ret;

    /*
     * return error point,
     * or point of exit when end_processing_because_of_range == true
     */
decompress_and_build_index_error:

    // print output_data_counter info
    if (output_data_counter > 0)
    {
        printToStderr("%s (%llu Bytes) of data extracted.\n",
                      giveMeSIUnits(output_data_counter, 1), output_data_counter);
    }

    // print output_lines_counter info
    if ((indx_n_extraction_opts == EXTRACT_FROM_LINE ||
         (NULL != index && index->index_version > 0)) &&
        output_lines_counter > 0)
    {
        printToStderr("%llu lines extracted", output_lines_counter);
        if (output_lines_counter > 1000)
        {
            printToStderr(" (%s).\n", giveMeSIUnits(output_lines_counter, 0));
        }
        else
        {
            printToStderr(".\n");
        }
    }

    (void)inflateEnd(&strm);

    if (always_create_a_complete_index == 1 &&
        NULL != index)
    {
        index->file_size = totout; /* size of uncompressed file (useful for bgzip files) */
        if (0 == there_are_more_chars && totlines > 1)
        {
            totlines--;
        }
        index->number_of_lines = totlines; /* lines in uncompressed file */
        // return index pointer and write index to index file, ignoring the decompression error
        *built = index;
        if (!serialize_index_to_file(index_file, index, index->have))
            printToStderr("ERROR whilst writing index file '%s'.\n", index_file);
    }
    else
    {
        if (index != NULL)
            free_index(index);
        *built = NULL;
    }
    if (index_file != NULL)
        fclose(index_file);
    return ret;

    // there's no need to free(here),
    // because it pointed to index's values, and they
    // will be freed with free_index().
}

// read index from a file
// INPUT:
// FILE *input_file         : input stream
// int load_windows         : 0 do not yet load windows on memory; 1 to load them
// char *file_name : file path to index file, needed in case
//                            load_windows==0, so a later extract() can access the
//                            index file again to read the window data.
//                            Can be "" if using stdin, but not NULL.
// int extend_index_with_lines: if >0 (index v1) but index is v0, operation cannot be performed
//                              unless:
//                              * the index is an **empty file**, in which case the index
//                                pointer is correctly initialized with corresponding ->index_version
//                              * extend_index_with_lines == 3, in which case extend_index_with_lines
//                                is implicit, not compulsory, so process can transparently proceed
// OUTPUT:
// struct access *index : pointer to index, or NULL on error
struct access *deserialize_index_from_file(
    FILE *input_file, int load_windows, char *file_name,
    int extend_index_with_lines)
{
    struct point here;
    struct access *index = NULL;
    uint32_t index_complete = 1;
    uint64_t number_of_index_points = 0;
    uint64_t index_have, index_size, file_size;
    uint64_t position_at_file = 0;
    char header[GZIP_INDEX_HEADER_SIZE];
    struct stat st;
    int index_version = 0; /* 0: default; 1: index with line numbers */

    // get index file size to calculate on-the-fly how many registers are
    // in order to being able to read still-growing index files (see `-S`)
    if (strlen(file_name) > 0)
    {
        stat(file_name, &st);
        file_size = st.st_size;
    }
    else
    {
        // for stdin use max value
        file_size = UINT64_MAX;
    }

    // check index size == 0
    if (file_size != 0)
    {
        if (fread(header, 1, GZIP_INDEX_HEADER_SIZE, input_file) == GZIP_INDEX_HEADER_SIZE &&
            *((uint64_t *)header) == 0)
        {
            if (strncmp(&header[GZIP_INDEX_HEADER_SIZE / 2], GZIP_INDEX_IDENTIFIER_STRING, GZIP_INDEX_HEADER_SIZE / 2) != 0)
            {
                if (strncmp(&header[GZIP_INDEX_HEADER_SIZE / 2], GZIP_INDEX_IDENTIFIER_STRING_V1, GZIP_INDEX_HEADER_SIZE / 2) != 0)
                {
                    printToStderr("ERROR: File is not a valid gzip index file.\n");
                    return NULL;
                }
                else
                {
                    index_version = 1;
                }
            }
            else
            {
                index_version = 0;
            }
        }
        else
        {
            printToStderr("ERROR: File is not a valid gzip index file.\n");
            return NULL;
        }
    }
    else
    {
        // for an empty index, return a pointer with zero data
        index = create_empty_index();
        if (extend_index_with_lines > 0)
            index->index_version = 1;
        return index;
    }

    if (0 == index_version &&
        extend_index_with_lines > 0)
    {
        if (3 != extend_index_with_lines)
        {
            printToStderr("ERROR: Existing index has no line number information.\n");
            printToStderr("ERROR: Aborting on `-[LRxX]` parameter(s).\n");
            goto deserialize_index_from_file_error;
        }
        else
        {
            // transparently handle v0 files, as `-x` was implicitly tried (v>1.4.2), but it is not compulsory
            extend_index_with_lines = 0; // this value is not used - set here only for clarity
        }
    }

    index = create_empty_index();
    index->index_version = index_version;

    // if index is v1, there's a previous register with line separator format info:
    if (index_version == 1)
    {
        fread_endian(&(index->line_number_format), sizeof(index->line_number_format), input_file);
    }

    fread_endian(&(index_have), sizeof(index_have), input_file);
    fread_endian(&(index_size), sizeof(index_size), input_file);

    number_of_index_points = index_have;

    // index->size equals index->have when the index file is correctly closed,
    // and index->have on disk == 0 && index->have on disk = index->size whilst the index is growing:
    if (index_have != index_size)
    {
        printToStderr("Index file is incomplete.\n");
        index_complete = 0;
        number_of_index_points = index_size;
    }

    position_at_file = GZIP_INDEX_HEADER_SIZE +
                       ((1 == index->index_version) ? sizeof(index->line_number_format) : 0) +
                       sizeof(index_have) * 2;

    // read the list of points
    do
    {

        fread_endian(&(here.out), sizeof(here.out), input_file);
        fread_endian(&(here.in), sizeof(here.in), input_file);
        fread_endian(&(here.bits), sizeof(here.bits), input_file);
        fread_endian(&(here.window_size), sizeof(here.window_size), input_file);
        position_at_file += sizeof(here.out) + sizeof(here.in) + sizeof(here.bits) + sizeof(here.window_size);

        if (here.bits > 8)
        {
            break; // exit do loop
        }

        if (load_windows == 0)
        {
            // do not load window on here.window, but leave it on disk: this is marked with
            // a here.window_beginning to the position of the window on disk
            here.window = NULL;
            here.window_beginning = position_at_file;
            // and position into file as if read had occur
            if (stdin == input_file)
            {
                // read input until here.window_size
                uint64_t pos = 0;
                uint64_t position = here.window_size;
                unsigned char *input = malloc(CHUNK);
                if (NULL == input)
                {
                    printToStderr("Not enough memory to load index from STDIN.\n");
                    goto deserialize_index_from_file_error;
                }
                while (pos < position)
                {
                    if (!fread(input, 1, (pos + CHUNK < position) ? CHUNK : (position - pos), input_file))
                    {
                        printToStderr("Could not read index from STDIN.\n");
                        goto deserialize_index_from_file_error;
                    }
                    pos += CHUNK;
                }
                free(input);
            }
            else
            {
                if (here.window_size > 0)
                    fseeko(input_file, here.window_size, SEEK_CUR);
            }
        }
        else
        {
            // load compressed window on memory:
            if (here.window_size > 0)
            {
                here.window = malloc(here.window_size);
                // load window on here.window: this is marked with
                // a here.window_beginning = 0 (which is impossible with gzipindx format)
                here.window_beginning = 0;
                if (here.window == NULL)
                {
                    printToStderr("Not enough memory to load index from file.\n");
                    goto deserialize_index_from_file_error;
                }
                if (!fread(here.window, here.window_size, 1, input_file))
                {
                    printToStderr("Error while reading index file.\n");
                    goto deserialize_index_from_file_error;
                }
            }
            else
            {
                here.window = NULL;
            }
        }
        position_at_file += here.window_size;

        if (index_version == 1)
        {
            fread_endian(&(here.line_number), sizeof(here.line_number), input_file);
            position_at_file += sizeof(here.line_number);
        }
        else
        {
            here.line_number = 0;
        }

        // increase index structure with a new point
        // (here.window can be NULL if load_windows==0)
        index = addpoint(index, here.bits, here.in, here.out, 0, here.window, here.window_size, here.line_number, 0);

        // after increasing index, copy values which were not passed to addpoint():
        index->list[index->have - 1].window_beginning = here.window_beginning;
        // note that even if (here.window != NULL) it MUST NOT be free() here, because
        // the pointer has been copied in a point of the index structure.

    } while (
        (file_size - position_at_file) >=
            // at least an empty window must enter, otherwise end loop:
            (sizeof(here.out) + sizeof(here.in) + sizeof(here.bits) +
             sizeof(here.window_size)) &&
        --number_of_index_points > 0);

    index->file_size = 0;

    if (index_complete == 1)
    {
        /* read size of uncompressed file (useful for bgzip files) */
        /* this field may not exist (maybe useful for growing gzip files?) */
        fread_endian(&(index->file_size), sizeof(index->file_size), input_file);
        /* read number of lines in uncompressed file */
        /* this field does not exist with v0 of index file */
        fread_endian(&(index->number_of_lines), sizeof(index->number_of_lines), input_file);
    }

    index->file_name = malloc(strlen(file_name) + 1);
    if (NULL == index->file_name ||
        NULL == memcpy(index->file_name, file_name, strlen(file_name) + 1))
    {
        printToStderr("Not enough memory to load index from file.\n");
        goto deserialize_index_from_file_error;
    }

    if (index_complete == 1)
        index->index_complete = 1; /* index is now complete */
    else
        index->index_complete = 0;

    return index;

deserialize_index_from_file_error:
    free_index(index);
    return NULL;
}

// Decompress source stream to dest stream using zlib compression format
// that is, with 2-byte header and adler-32 crc, or raw format.
// based on inf from zlib/examples/zpipe.c by Mark Adler
/* Decompress from file source to file dest until stream ends or EOF.
   inf() returns Z_OK on success, Z_MEM_ERROR if memory could not be
   allocated for processing, Z_DATA_ERROR if the deflate data is
   invalid or incomplete, Z_VERSION_ERROR if the version of zlib.h and
   the version of the library linked do not match, or Z_ERRNO if there
   is an error reading or writing the files. */
// INPUT:
// FILE *source : source stream
// FILE *dest   : destination stream
// int raw_method: 0: zlib decompression (header+Adler-32)
//                 1: raw decompression (no header nor tail)
// OUTPUT:
// Z_* error code or Z_OK on success
local int decompress_file(FILE *source, FILE *dest)
{
    int ret;
    unsigned have;
    z_stream strm;
    unsigned char *in;
    unsigned char *out;

    /* allocate inflate state */
    strm.zalloc = Z_NULL;
    strm.zfree = Z_NULL;
    strm.opaque = Z_NULL;
    strm.avail_in = 0;
    strm.next_in = Z_NULL;
    ret = inflateInit(&strm);
    if (ret != Z_OK)
        return ret;

    in = malloc(CHUNK);
    out = malloc(CHUNK);

    /* decompress until deflate stream ends or end of file */
    do
    {
        strm.avail_in = fread(in, 1, CHUNK, source);
        if (ferror(source))
        {
            (void)inflateEnd(&strm);
            ret = Z_ERRNO;
            goto decompress_file_error;
        }
        if (strm.avail_in == 0)
            break;
        strm.next_in = in;

        /* run inflate() on input until output buffer not full */
        do
        {
            strm.avail_out = CHUNK;
            strm.next_out = out;
            ret = inflate(&strm, Z_NO_FLUSH);
            assert(ret != Z_STREAM_ERROR); /* state not clobbered */
            switch (ret)
            {
            case Z_NEED_DICT:
                ret = Z_DATA_ERROR;
                (void)inflateEnd(&strm);
                goto decompress_file_error;
            case Z_DATA_ERROR:
            case Z_MEM_ERROR:
                (void)inflateEnd(&strm);
                goto decompress_file_error;
            }
            have = CHUNK - strm.avail_out;
            if (fwrite(out, 1, have, dest) != have || ferror(dest))
            {
                (void)inflateEnd(&strm);
                ret = Z_ERRNO;
                goto decompress_file_error;
            }
        } while (strm.avail_out == 0);

        /* done when inflate() says it's done */
    } while (ret != Z_STREAM_END);

    /* clean up and return */
    (void)inflateEnd(&strm);
    free(in);
    free(out);
    return ret == Z_STREAM_END ? Z_OK : Z_DATA_ERROR;

decompress_file_error:
    free(in);
    free(out);
    return ret;
}

// GZIP compress the file_in stream to file_out
// creating a .gzi index to index_filename file name,
// and returning it on *built.
// Note that the index will be of minimum size as this function
// creates Z_FULL_FLUSH marks on compression stream every span bytes,
// so the window size will be always 0 bytes. This produces indexes
// almost as small as with bgzip compression, with better compression factor.
// Compression based on def() from zlib/examples/zpipe.c by Mark Adler
/* Compress from file source to file dest until EOF on source.
   def() returns Z_OK on success, Z_MEM_ERROR if memory could not be
   allocated for processing, Z_STREAM_ERROR if an invalid compression
   level is supplied, Z_VERSION_ERROR if the version of zlib.h and the
   version of the library linked do not match, or Z_ERRNO if there is
   an error reading or writing the files. */
// INPUT:
// FILE *file_in : source stream
// FILE *file_out   : destination stream
// int level    : level of compression
// char *file_name : name of the input file associated with FILE *in.
//                            Can be "" (no file name: stdin used), but not NULL.
//                            Used only if input (FILE *in)
//                            is associated with a file (not stdin) to detect a
//                            possible overwriting of the source file (and then stop).
// uint64_t span    : span
// struct access **built: address of index pointer, equivalent to passed by reference.
//                        Note that *built == NULL on call ALWAYS, because index is
//                        constructed from scratch here.
// char *index_filename  : index will be read/written on-the-fly
//                                  to this index file name.
// int write_index_to_disk  : 1: will write/update the index as usual;
//                            0: do not write the index on disk,
//                           and also, do not create/update the index in memory (**built and returned_output.value):
//                           This is done because if index is not written to disk, windows would need to be
//                           maintained in memory, increasing the memory footprint unnecessarily as the index
//                           is not (actually) used later in the app. (Windows could be also emptied, but again,
//                           index points are not used later.)
// int end_on_first_eof : end file processing on first feof():
//                        to be used when we are not compressing a growing file.
// int always_create_a_complete_index : create a 'complete' index file even in case of compressing errors.
//                                      Also an index pointer (**built) is returned, instead of NULL.
// int waiting_time     : waiting time in seconds between reads when `-[S]`
// int extend_index_with_lines  : 0: create index without line numbers (v0 index)
//                                1: create index WITH line numbers (v1 index) using Unix format (\n)
//                                2: create index WITH line numbers (v1 index) using old Mac format (\r) (compatible with Windows \n\r)
// OUTPUT:
// struct returned_output: contains two values:
//      .error: Z_* error code or Z_OK if everything was ok
//      .value: size of built index (index->have)
local struct returned_output compress_and_build_index(
    FILE *file_in, FILE *file_out, char *file_name, uint64_t span,
    struct access **built, char *index_filename,
    int end_on_first_eof, int always_create_a_complete_index,
    int waiting_time, int extend_index_with_lines)
{
    struct returned_output ret;
    uint64_t last = 0;            /* uncompressed byte position of last access point */
    struct access *index = NULL;  /* access points being generated */
    uint64_t totin = 0;           // counts uncompressed bytes read from file_in
    uint64_t totout = 0;          // counts compressed bytes deflated to file_out
    uint64_t totlines = 1;        // counts total line numbers in file_in
    int there_are_more_chars = 0; // totlines may need to be decremented at the end depending on last stream char
    int flush = Z_NO_FLUSH;
    unsigned have;
    z_stream strm;
    FILE *index_file = NULL;
    size_t index_last_written_point = 0;
    unsigned char *input;
    unsigned char *output;

    ret.value = 0;
    ret.error = Z_OK;

    /* allocate deflate state */
    strm.zalloc = Z_NULL;
    strm.zfree = Z_NULL;
    strm.opaque = Z_NULL;
    ret.error = deflateInit2( // www.zlib.net/manual.html
                              // github.com/madler/zlib/blob/master/zlib.h
                              //  Add 16 to windowBits to write a simple gzip header and trailer
                              //  around the compressed data instead of a zlib wrapper
        &strm, 0, Z_DEFLATED, 15 + 16,
        8, Z_DEFAULT_STRATEGY);
    if (ret.error != Z_OK)
        return ret;

    input = malloc(CHUNK);
    output = malloc(CHUNK);

    if (extend_index_with_lines > 0)
    {
        // mark index as index_version = 1 to store line numbers when serialize();
        // in order to do this, index must be created now (empty)
        index = create_empty_index();
        if (index == NULL)
        { // Oops!?
            ret.error = Z_MEM_ERROR;
            return ret;
        }
        index->index_version = 1;
        if (extend_index_with_lines == 2)
            index->line_number_format = 1;
        else
            index->line_number_format = 0;
    }

    /* open index_filename for binary writing */
    // index_filename DOES NOT previously exists: this has has been checked on caller
    if (strlen(index_filename) > 0)
    {
        if (access(index_filename, F_OK) != -1)
        {
            // index_filename already exist:
            // Abort, as we would overwrite it:
            printToStderr("Index file '%s' already exist. Aborted.\n", index_filename);
            goto compress_and_build_index_error;
        }
        else
        {
            // index_filename does not exist:
            index_file = fopen(index_filename, "w+b");
        }
    }
    else
    {
        SET_BINARY_MODE(STDOUT); // sets binary mode for stdout in Windows
        index_file = stdout;
    }
    if (NULL == index_file)
    {
        printToStderr("Could not write index to file '%s'.\n", index_filename);
        goto compress_and_build_index_error;
    }

    /* compress until end of file */
    do
    {

        // check if a new index point must be created
        if ( // create an index point at uncompressed byte 0 (just before data)
            (totin == 0 &&
             index_last_written_point == 0) ||
            // create index points after Z_FULL_FLUSH marks on compressed data
            (totin - last >= span &&
             Z_FULL_FLUSH == flush))
        {

            // write index point
            index = addpoint(index, 0,
                                ((0 == totout) ? GZIP_HEADER_SIZE_BY_ZLIB : totout), // compressed byte after header <=> uncompressed byte 0
                                totin, 0, NULL, 0, totlines, 0);

            // write added point!
            // note that points written are automatically emptied of its window values
            // in order to use as less memory a s possible
            if (!serialize_index_to_file(index_file, index, index_last_written_point))
                goto compress_and_build_index_error;
            }

            if (NULL != index)
                index_last_written_point = index->have;

            last = totin;

        strm.avail_in = fread(input, 1, CHUNK, file_in);
        totin += strm.avail_in;
        // count lines in this source chunk
        if (extend_index_with_lines > 0)
        {
            totlines += giveMeNumberOfLinesInChars(
                input, strm.avail_in,
                0, extend_index_with_lines - 1,
                &there_are_more_chars);
        }

        if (ferror(file_in))
        {
            (void)deflateEnd(&strm);
            goto compress_and_build_index_error;
        }

        if (feof(file_in))
        {
            if (end_on_first_eof == 1)
                flush = Z_FINISH;
            else
                flush = Z_SYNC_FLUSH; // Z_SYNC_FLUSH doesn't occupy space
        }
        else
        {
            flush = Z_NO_FLUSH;
        }
        if (totin - last >= span)
        {
            // create a FLUSH point for an index point with window of size zero
            // With gzip compression this should not affect compression performance
            flush = Z_FULL_FLUSH;
        }

        strm.next_in = input;

        /* run deflate() on input until output buffer not full, finish
           compression if all of file_in has been read in */
        do
        {

            strm.avail_out = CHUNK;
            strm.next_out = output;
            ret.error = deflate(&strm, flush);   /* no bad return value */
            assert(ret.error != Z_STREAM_ERROR); /* state not clobbered */

            have = CHUNK - strm.avail_out;
            totout += have;

            if (fwrite(output, 1, have, file_out) != have || ferror(file_out))
            {
                (void)deflateEnd(&strm);
                goto compress_and_build_index_error;
            }

            // flush written content to disk
            // fflush( file_out );

        } while (strm.avail_out == 0);
        assert(strm.avail_in == 0); /* all input will be used */

        // generic check for growing files:
        // check that file hasn't shrunk, what would mean that the file
        // has been overwritten from the beginning (possible with logs, for example)
        if (feof(file_in) &&
            end_on_first_eof == 0)
        { // if end_on_first_eof == 1, ret.error == Z_STREAM_END already
            if (strlen(file_name) > 0)
            { // this check cannot be done on STDIN
                struct stat st;
                stat(file_name, &st);
                if ((uint64_t)st.st_size < totin)
                {
                    // file has shrunk! so do a correct finish of the action_create_index (whatever it is)
                    // (Note that it is not possible that this condition arises when accessing a file
                    // with size 0 (which is possible and allowed) because (0 < 0) is false )
                    end_on_first_eof = 1;
                    // loop will continue and because feof() will activate again (because source file
                    // won't be overwritten so quick as to surpase actual total_in bytes) a Z_FINISH
                    // will be written, and a Z_STREAM_END will be returned to end the process.
                }
            }
        }

        if (end_on_first_eof == 0 &&
            strm.avail_in == 0 &&
            feof(file_in))
        {
            // flush written content to disk
            fflush(file_out);
            // wait for file to grow
            sleep(waiting_time);
            clearerr(file_in);
        }

        /* done when last data in file processed */

    } while (flush != Z_FINISH);

    assert(ret.error == Z_STREAM_END); /* stream will be complete */

    /* clean up and return */
    (void)deflateEnd(&strm);

    /* and return index (release unused entries in list) */
    if (NULL != index &&
        index->have > 0)
    {
        struct point *next = realloc(index->list, sizeof(struct point) * index->have);
        if (NULL == next)
        {
            ret.error = Z_MEM_ERROR;
            ret.value = 0;
            goto compress_and_build_index_error;
        }
        index->list = next;
        index->size = index->have;
        index->file_size = totin; /* size of uncompressed file */
        if (0 == there_are_more_chars && totlines > 1)
        {
            totlines--;
        }
        index->number_of_lines = totlines; /* lines in uncompressed file */
    }

    // once all index values are filled, close index file: a last call must be done
    // with index_last_written_point = index->have
    if (NULL != index &&
        index->index_complete == 0)
    {
        if (!serialize_index_to_file(index_file, index, index->have))
            goto compress_and_build_index_error;
        if (strlen(index_filename) > 0)
            printToStderr("Index written to '%s'.\n", index_filename);
        else
            printToStderr("Index written to STDOUT.\n");
        /* index is now complete */
    }

    if (NULL != index_file)
        fclose(index_file);

    // print totin info
    if (totin > 0)
    {
        printToStderr("%s (%llu Bytes) of data compressed.\n",
                      giveMeSIUnits(totin, 1), totin);
        if (extend_index_with_lines > 0 &&
            totlines > 0)
        {
            printToStderr("%llu lines compressed.\n", totlines);
            if (totlines > 1000)
            {
                printToStderr(" (%s).\n", giveMeSIUnits(totlines, 0));
            }
            else
            {
                printToStderr(".\n");
            }
        }
        if (totout > 0)
            printToStderr("%llu bytes of gzip data (%.2f%%).\n",
                          totout, 100.0 - (double)totout / (double)totin * 100.0);
    }

    *built = index;
    if (NULL != index)
        ret.value = index->have;
    else
        ret.value = 0;

    free(input);
    free(output);

    return ret;

compress_and_build_index_error:
    free(input);
    free(output);

    // flush written content to disk
    fflush(file_out);

    // print totin info
    if (totin > 0)
    {
        printToStderr("%s (%llu Bytes) of data compressed.\n",
                      giveMeSIUnits(totin, 1), totin);
        if (extend_index_with_lines > 0 &&
            totlines > 0)
        {
            printToStderr("%llu lines compressed", ((0 == there_are_more_chars) ? totlines - 1 : totlines));
            if (totlines > 1000)
            {
                printToStderr(" (%s).\n", giveMeSIUnits(totlines, 0));
            }
            else
            {
                printToStderr(".\n");
            }
        }
        if (totout > 0)
            printToStderr("%s (%llu Bytes) of gzip data (%.2f%%).\n",
                          giveMeSIUnits(totout, 1), totout, 100.0 - (double)totout / (double)totin * 100.0);
    }

    (void)deflateEnd(&strm);

    if (always_create_a_complete_index == 1)
    {
        if (NULL != index)
        {
            index->file_size = totin; /* size of uncompressed file */
            if (0 == there_are_more_chars && totlines > 1)
            {
                totlines--;
            }
            index->number_of_lines = totlines; /* lines in uncompressed file */
            // return index pointer and write index to index file, ignoring the compression error
            *built = index;
            if (!serialize_index_to_file(index_file, index, index->have))
                printToStderr("ERROR whilst writing index file '%s'.\n", index_file);
        }
    }
    else
    {
        if (NULL != index)
            free_index(index);
        *built = NULL;
    }

    if (NULL != index_file)
        fclose(index_file);
    if (Z_OK == ret.error)
        ret.error = Z_ERRNO;
    return ret;
}

// Creates an index for a gzip file.
// If index file already exists, it is completed if it weren't complete,
// or directly used if it is complete.
// INPUT:
// char *file_name     : file name of gzip file for which index will be calculated,
//                                or that will be decompressed, depending on indx_n_extraction_opts.
//                                If strlen(file_name) == 0 stdin is used as gzip file.
// struct access **index        : memory address of index pointer (passed by reference)
// char *index_filename: file name where index will be written
//                                If strlen(index_filename) == 0 stdout is used as output for index.
// enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts:
//                                  value passed to decompress_and_build_index();
//                                  in case of SUPERVISE_DO* (not *DONT), wait here until gzip file exists.
// uint64_t offset                 : if supervise == EXTRACT_FROM_BYTE, this is the offset byte in
//                                the uncompressed stream from which to extract to stdout.
//                                0 otherwise.
// uint64_t line_number_offset  : if indx_n_extraction_opts == EXTRACT_FROM_LINE, this is the offset line
//                                in the uncompressed stream from which to extract to stdout.
//                                0 otherwise.
// uint64_t span_between_points    : span between index points in bytes
// int write_index_to_disk      : 1: will write/update the index as usual;
//                                0: will just read, but do not write nor update (overwrite) it
// int end_on_first_proper_gzip_eof: end file processing on first proper (at feof()) GZIP EOF
//                                   (to be used when the file contains surely only one gzip stream)
// int always_create_a_complete_index : create a 'complete' index file even in case of decompressing errors.
//                                      Also an index pointer (**built) is returned, instead of NULL.
// int waiting_time             : waiting time in seconds between reads when `-[ST]`
// int force_action             : with `-[cd]`, force destination file overwriting if it exists
// int wait_for_file_creation   : 0: exit with error if source file doesn't exist
//                                1: wait for file creation if it doesn't yet exist, when using `-[STcd]`
// int extend_index_with_lines  : create index with (1,2) or without (0) line numbers (v1 or v0 index):
//                                decision is make at de/compress_and_build_index(), not here, unless
//                                index file already exists and extend_index_with_lines == 3, in which
//                                case if index is v0 => extend_index_with_lines = 0 and if index is
//                                v1 => extend_index_with_lines = 1, to transparently handle v0 indexes
//                                with implicit `-x` (v>1.4.2).
// uint64_t expected_first_byte  : indicates that the first byte on compressed input is this, not 1,
//                                and so truncated compressed inputs can be used. Only can be used (>1) if
//                                indx_n_extraction_opts is EXTRACT_FROM_BYTE or EXTRACT_FROM_LINE.
//                                Directly passed here to decompress_and_build_index(), w/o further processing.
// int gzip_stream_may_be_damaged: indicates if a decompression in advance will be used and how many CHUNKs
//                                will be decompressed in advance then (CHUNKS_TO_DECOMPRESS_IN_ADVANCE).
//                                If 0, no decompression in advance is made.
// bool lazy_gzip_stream_patching_at_eof: when used with `-[ST]` implies that checking
//                                for errors in stream is made as quick as possible as the gzip file
//                                grows, not respecting then at EOF the CHUNKS_TO_DECOMPRESS_IN_ADVANCE value.
// uint64_t range_number_of_bytes: indicates how many bytes to extract when using `-[bL]` (offset, line_number_offset)
// uint64_t range_number_of_lines: indicates how many lines to extract when using `-[bL]` (offset, line_number_offset)
// int compression_factor        : a value from 1 to 9 (Z_BEST_SPEED to Z_BEST_COMPRESSION),
//                                 defaulting to Z_DEFAULT_COMPRESSION which though it's -1, means 6 internally for zlib.
//                                 Used here only when calling compress_and_build_index().
// OUTPUT:
// EXIT_* error code or EXIT_OK on success
local int action_create_index(
    char *file_name, struct access **index,
    char *index_filename, enum INDEX_AND_EXTRACTION_OPTIONS indx_n_extraction_opts,
    uint64_t offset, uint64_t line_number_offset, uint64_t span_between_points,
    int end_on_first_proper_gzip_eof, int always_create_a_complete_index,
    int waiting_time, int wait_for_file_creation,
    int extend_index_with_lines, uint64_t expected_first_byte, int gzip_stream_may_be_damaged,
    bool lazy_gzip_stream_patching_at_eof,
    uint64_t range_number_of_bytes, uint64_t range_number_of_lines)
{

    FILE *file_in = NULL;
    FILE *file_out = NULL;
    struct returned_output ret;
    uint64_t number_of_index_points = 0;
    int waiting = 0;

    // First of all, check that data output and index output do not collide:
    if (strlen(file_name) == 0 &&
        strlen(index_filename) == 0 &&
        (indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
         indx_n_extraction_opts == EXTRACT_FROM_LINE))
    {
        // input is stdin, output is stdout, and no file name has been
        // indicated for index output, so action is not possible:
        printToStderr("ERROR: Please, note that extracted data will be output to STDOUT\n");
        printToStderr("       so an index file name is needed (`-I`).\nAborted.\n");
        return EXIT_GENERIC_ERROR;
    }

    // open <FILE>:
    if (strlen(file_name) > 0)
    {
    action_create_index_wait_for_file_creation:
        file_in = fopen(file_name, "rb");
        if (NULL == file_in)
        {
            if (wait_for_file_creation == 1 && 
                indx_n_extraction_opts == COMPRESS_AND_CREATE_INDEX)
            {
                if (waiting == 0)
                {
                    printToStderr("Waiting for creation of file '%s'\n", file_name);
                    waiting = 1;
                }
                sleep(waiting_time);
                goto action_create_index_wait_for_file_creation;
            }
            printToStderr("Could not open '%s' for reading.\nAborted.\n", file_name);
            return EXIT_GENERIC_ERROR;
        }
        printToStderr("Processing '%s' ...\n", file_name);
    }
    else
    {
        // stdin
        SET_BINARY_MODE(STDIN); // sets binary mode for stdin in Windows
        file_in = stdin;
        printToStderr("Processing STDIN ...\n");
    }

    if (indx_n_extraction_opts == COMPRESS_AND_CREATE_INDEX)
    {
        // compression !

        // an output filename is necessary:
        // this block directly provides de FILE *file_out pointer
        if (strlen(file_name) > 0)
        {
            char *out_filename;
            out_filename = malloc(strlen(file_name) + 3 + 1);
            sprintf(out_filename, "%s.gz", file_name);
            // check that destination file doesn't exist
            if (access(out_filename, F_OK) != -1)
            {
                printToStderr("Using `-f` force option: Deleting '%s' ...\n", out_filename);
            }
            file_out = fopen(out_filename, "w+b");
            if (NULL == file_out)
            {
                printToStderr("Could not open '%s' for writing.\nAborted.\n", out_filename);
                free(out_filename);
                return EXIT_GENERIC_ERROR;
            }
            printToStderr("Compressing to '%s'\n", out_filename);
            free(out_filename);
        }
        else
        {
            // stdin
            // If using STDIN, the output will be STDOUT
            SET_BINARY_MODE(STDOUT); // sets binary mode for stdout in Windows
            file_out = stdout;
            printToStderr("Compressing to STDOUT ...\n");
        }

        if (extend_index_with_lines == 3)
        {
            extend_index_with_lines = 1; // in case of COMPRESS_AND_CREATE_INDEX, direct v1 index
                                         // as no previous index is possible, so there's no doubt.
        }

        ret = compress_and_build_index(file_in, file_out,
                                       file_name, span_between_points, index, index_filename,
                                       ((end_on_first_proper_gzip_eof == 0) ? 1 : 0), // `-E` behaviour is inverted with `-c` for ease of use
                                       always_create_a_complete_index, waiting_time,
                                       extend_index_with_lines);

        if (NULL != index && NULL != *index)
        {
            // it is important to set (*index)->file_name, if the index is to be reused,
            // because the window data must be read from disk to be used.
            if (NULL == (*index)->file_name)
            {
                (*index)->file_name = malloc(strlen(index_filename) + 1);
                if (NULL == (*index)->file_name ||
                    NULL == memcpy((*index)->file_name, index_filename, strlen(index_filename) + 1))
                {
                    printToStderr("Not enough memory to load index from file.\n");
                    if (NULL != (*index)->file_name)
                        free((*index)->file_name);
                    return EXIT_GENERIC_ERROR;
                }
            }
        }
    }
    else
    {
        // decompression!

        // compute index:
        // if index_filename already exist, load it and use it
        // (if it is complete, it'll be used directly, if not it'll be
        // completed from last point - all in decompress_and_build_index() ).
        if (strlen(index_filename) > 0 && access(index_filename, F_OK) != -1)
        {
            if (NULL == index || NULL == (*index))
            {
                // index_filename already exist: try to load it
                FILE *index_file;
                index_file = fopen(index_filename, "rb");
                if (NULL != index_file)
                {
                    *index = deserialize_index_from_file(index_file, 0, index_filename, extend_index_with_lines);
                    fclose(index_file);
                    if (NULL == *index)
                    {
                        printToStderr("Could not load index from file '%s'.\nAborted.\n", index_filename);
                        return EXIT_GENERIC_ERROR;
                    }
                    // index ok, continue
                    number_of_index_points = (*index)->have;
                    (*index)->index_complete = 0; // every index can be updated with new points in case gzip data grows!
                    // set local "extend_index_with_lines" to the curated value returned by deserialize_index_from_file()
                    // (this way a possible extend_index_with_lines == 3 is reconverted to a usable value {0,1,2})
                    if (0 == (*index)->index_version)
                    {
                        extend_index_with_lines = 0;
                    }
                    else
                    {
                        extend_index_with_lines = (*index)->line_number_format;
                    }
                }
                else
                {
                    printToStderr("Could not open '%s' for reading.\nAborted.\n", index_filename);
                    return EXIT_GENERIC_ERROR;
                }
            }
            else
            {
                number_of_index_points = (*index)->have;
                (*index)->index_complete = 0; // every index can be updated with new points in case gzip data grows!
            }
            // there's an index now, continue:
            // it is important to set (*index)->file_name, if the index is to be reused,
            // because the window data must be read from disk to be used.
            if (NULL == (*index)->file_name)
            {
                (*index)->file_name = malloc(strlen(index_filename) + 1);
                if (NULL == (*index)->file_name ||
                    NULL == memcpy((*index)->file_name, index_filename, strlen(index_filename) + 1))
                {
                    printToStderr("Not enough memory to load index from file.\n");
                    if (NULL != (*index)->file_name)
                        free((*index)->file_name);
                    return EXIT_GENERIC_ERROR;
                }
            }
        }

        // checks on index read from file:
        if (NULL != (*index) &&
            strlen(file_name) > 0)
        {
            // (here, index_filename exists and index exists)
            check_index_file((*index), file_name, index_filename);
            // returned value is not used - only warn user and continue
        }

        // stdout to binary mode if needed
        if (indx_n_extraction_opts == EXTRACT_FROM_BYTE ||
            indx_n_extraction_opts == EXTRACT_FROM_LINE)
        {
            SET_BINARY_MODE(STDOUT); // sets binary mode for stdout in Windows
        }

        file_out = stdout;

        ret = decompress_and_build_index(file_in, file_out, file_name, span_between_points, index,
                                            indx_n_extraction_opts, offset, line_number_offset, index_filename,
                                            end_on_first_proper_gzip_eof, always_create_a_complete_index,
                                            waiting_time, extend_index_with_lines, expected_first_byte,
                                            gzip_stream_may_be_damaged, lazy_gzip_stream_patching_at_eof,
                                            range_number_of_bytes, range_number_of_lines);

        if (NULL != index && NULL != *index)
        {
            // it is important to set (*index)->file_name, if the index is to be reused,
            // because the window data must be read from disk to be used.
            if (NULL == (*index)->file_name)
            {
                (*index)->file_name = malloc(strlen(index_filename) + 1);
                if (NULL == (*index)->file_name ||
                    NULL == memcpy((*index)->file_name, index_filename, strlen(index_filename) + 1))
                {
                    printToStderr("Not enough memory to load index from file.\n");
                    if (NULL != (*index)->file_name)
                        free((*index)->file_name);
                    return EXIT_GENERIC_ERROR;
                }
            }
        }
    }

    if (file_in != stdin)
        fclose(file_in);

    if (NULL != file_out &&
        file_out != stdout)
        fclose(file_out);

    if (ret.error < 0)
    {
        switch (ret.error)
        {
        case Z_MEM_ERROR:
            printToStderr("ERROR: Out of memory.\n");
            break;
        case Z_DATA_ERROR:
            if (strlen(file_name) > 0)
                printToStderr("ERROR: Compressed data error in '%s'.\n", file_name);
            else
                printToStderr("ERROR: Compressed data error in STDIN.\n");
            break;
        case Z_ERRNO:
            if (strlen(file_name) > 0)
                printToStderr("ERROR: Read error on '%s'.\n", file_name);
            else
                printToStderr("ERROR: Read error on STDIN.\n");
            break;
        default:
            printToStderr("ERROR: Error %d while applying action.\n", ret.error);
        }
        return EXIT_GENERIC_ERROR;
    }

    if (NULL != index && NULL != *index &&
        number_of_index_points != (*index)->have)
    {
        if (number_of_index_points > 0)
        {
            printToStderr("Updated index with %llu new access points.\n", ret.value - number_of_index_points);
            printToStderr("Now index has %llu access points.\n", ret.value);
        }
        else
        {
            printToStderr("Built index with %llu access points.\n", ret.value);
        }
    }

    if (EXIT_FILE_OVERWRITTEN <= ret.error)
    {
        return ret.error;
    }

    return EXIT_OK;
}

// obtain an integer from a string that may used decimal point,
// with valid suffixes: kmgtpe (powers of 10) and KMGTPE (powers of 2),
// and valid prefixes: "0" (octal), "0x" or "0X" (hexadecimal).
// Examples:
// "83m" == 83*10^6, "9G" == 9*2^30, "0xa" == 10, "010k" = 8000, "23.5k" = 23500
// INPUT:
// char *original_input: string containing the data (only read)
// OUTPUT:
// 64 bit long integer number
uint64_t giveMeAnInteger(const char *original_input)
{

    unsigned i = 0;
    char *PowerSuffixes = "kmgtpeKMGTPE";
    unsigned PowerSuffixesLength = strlen(PowerSuffixes) / 2;
    char *input = NULL;
    uint64_t result = 1;

    if (NULL == original_input)
        return 0LLU;

    input = malloc(strlen(original_input) + 1);
    memcpy(input, original_input, strlen(original_input) + 1);

    if (strlen(input) > 1)
    {
        // look for suffixes of size

        for (i = 0; i < strlen(PowerSuffixes); i++)
        {
            if ((char *)strchr(input, PowerSuffixes[i]) == (char *)(input + strlen(input) - 1))
            {
                if (i >= PowerSuffixesLength)
                {
                    result = pow(2.0, 10.0 + 10.0 * (double)(i - PowerSuffixesLength));
                }
                else
                {
                    result = pow(10.0, 3.0 + 3.0 * (double)i);
                }
                input[strlen(input) - 1] = '\0';
                break;
            }
        }
    }

    if (strlen(input) > 1)
    {
        // look fo prefixes of base

        if (input[0] == '0')
        {
            if (strlen(input) > 2 &&
                input[1] == '.')
            {
                // this is a float-point number
                ;
            }
            else
            {
                // hexadecimal or octal number:
                if (strlen(input) > 2 &&
                    (input[1] == 'x' || input[1] == 'X'))
                {
                    // hexadecimal
                    result = strtoll(input + 2, NULL, 16) * result;
                }
                else
                {
                    // octal
                    result = strtoll(input + 1, NULL, 8) * result;
                    // Note: 0 is zero decimal and zero octal :-)
                }
                input[0] = '1';
                input[1] = '\0';
            }
        }
    }

    result = (uint64_t)(strtod(input, NULL) * (double)result);

    if (NULL != input)
        free(input);

    return result;
}

// .................................................

//// print help
// local void print_help() {
//     fprintf( stderr, "\n" );
//     fprintf( stderr, "  %s (v%s)\n", PACKAGE_NAME, PACKAGE_VERSION );
//     fprintf( stderr, "  GZIP files indexer, compressor and data retriever.\n" );
//     fprintf( stderr, "  Create small indexes for gzipped files and use them\n" );
//     fprintf( stderr, "  for quick and random-positioned data extraction.\n" );
//     fprintf( stderr, "  No more waiting when the end of a 10 GiB gzip is needed!\n" );
//     fprintf( stderr, "  //github.com/circulosmeos/gztool (by Roberto S. Galende)\n\n" );
//     fprintf( stderr, "  $ gztool [-[abLnsv] #] [-[1..9]AcCdDeEfFhilpPrRStTwWxXz|u[cCdD]] [-I <INDEX>] <FILE>...\n\n" );
//     fprintf( stderr, "  Note that actions `-bcStT` proceed to an index file creation (if\n" );
//     fprintf( stderr, "  none exists) INTERLEAVED with data flow. As data flow and\n" );
//     fprintf( stderr, "  index creation occur at the same time there's no waste of time.\n" );
//     fprintf( stderr, "  Also you can interrupt actions at any moment and the remaining\n" );
//     fprintf( stderr, "  index file will be reused (and completed if necessary) on the\n" );
//     fprintf( stderr, "  next gztool run over the same data.\n\n" );
//     fprintf( stderr, " -[1..9]: compression factor to use with `-[c|u[cC]]`, from\n" );
//     fprintf( stderr, "     best speed (`-1`) to best compression (`-9`). Default is `-6`.\n" );
//     fprintf( stderr, " -a #: Await # seconds between reads when `-[ST]|Ec`. Default is 4 s.\n" );
//     fprintf( stderr, " -A: Modifier for `-[rR]` to indicate the range of bytes/lines in\n" );
//     fprintf( stderr, "     absolute values, instead of the default incremental values.\n" );
//     fprintf( stderr, " -b #: extract data from indicated uncompressed byte position of\n" );
//     fprintf( stderr, "     gzip file (creating or reusing an index file) to STDOUT.\n" );
//     fprintf( stderr, "     Accepts '0', '0x', and suffixes 'kmgtpe' (^10) 'KMGTPE' (^2).\n" );
//     fprintf( stderr, " -C: always create a 'Complete' index file, ignoring possible errors.\n" );
//     fprintf( stderr, " -c: compress a file like with gzip, creating an index at the same time.\n" );
//     fprintf( stderr, " -d: decompress a file like with gzip.\n" );
//     fprintf( stderr, " -D: do not delete original file when using `-[cd]`.\n" );
//     fprintf( stderr, " -e: if multiple files are indicated, continue on error (if any).\n" );
//     fprintf( stderr, " -E: end processing on first GZIP end of file marker at EOF.\n" );
//     fprintf( stderr, "     Nonetheless with `-c`, `-E` waits for more data even at EOF.\n" );
//     fprintf( stderr, " -f: force file overwriting if index file already exists.\n" );
//     fprintf( stderr, " -F: force index creation/completion first, and then action: if\n" );
//     fprintf( stderr, "     `-F` is not used, index is created interleaved with actions.\n" );
//     fprintf( stderr, " -h: print brief help; `-hh` prints this help.\n" );
//     fprintf( stderr, " -i: create index for indicated gzip file (For 'file.gz' the default\n" );
//     fprintf( stderr, "     index file name will be 'file.gzi'). This is the default action.\n" );
//     fprintf( stderr, " -I string: index file name will be the indicated string.\n" );
//     fprintf( stderr, " -l: check and list info contained in indicated index file.\n" );
//     fprintf( stderr, "     `-ll` and `-lll` increase the level of index checking detail.\n" );
//     fprintf( stderr, " -L #: extract data from indicated uncompressed line position of\n" );
//     fprintf( stderr, "     gzip file (creating or reusing an index file) to STDOUT.\n" );
//     fprintf( stderr, "     Accepts '0', '0x', and suffixes 'kmgtpe' (^10) 'KMGTPE' (^2).\n" );
//     fprintf( stderr, " -n #: indicates that the first byte on compressed input is #, not 1,\n" );
//     fprintf( stderr, "     and so truncated compressed inputs can be used if an index exists.\n" );
//     fprintf( stderr, " -p: indicates that the gzip input stream may be composed of various\n" );
//     fprintf( stderr, "     incorrectly terminated GZIP streams, and so then a careful\n" );
//     fprintf( stderr, "     Patching of the input may be needed to extract correct data.\n" );
//     fprintf( stderr, " -P: like `-p`, but when used with `-[ST]` implies that checking\n" );
//     fprintf( stderr, "     for errors in stream is made as quick as possible as the gzip file\n" );
//     fprintf( stderr, "     grows. Warning: this may lead to some errors not being patched.\n" );
//     fprintf( stderr, " -r #: (range): Number of bytes to extract when using `-[bL]`.\n" );
//     fprintf( stderr, "     Accepts '0', '0x', and suffixes 'kmgtpe' (^10) 'KMGTPE' (^2).\n" );
//     fprintf( stderr, " -R #: (Range): Number of lines to extract when using `-[bL]`.\n" );
//     fprintf( stderr, "     Accepts '0', '0x', and suffixes 'kmgtpe' (^10) 'KMGTPE' (^2).\n" );
//     fprintf( stderr, " -s #: span in uncompressed MiB between index points when\n" );
//     fprintf( stderr, "     creating the index. By default is `10`.\n" );
//     fprintf( stderr, " -S: Supervise indicated file: create a growing index,\n" );
//     fprintf( stderr, "     for a still-growing gzip file. (`-i` is implicit).\n" );
//     fprintf( stderr, " -t: tail (extract last bytes) to STDOUT on indicated gzip file.\n" );
//     fprintf( stderr, " -T: tail (extract last bytes) to STDOUT on indicated still-growing\n" );
//     fprintf( stderr, "     gzip file, and continue Supervising & extracting to STDOUT.\n" );
//     fprintf( stderr, " -u [cCdD]: utility to compress (`-u c`) or decompress (`-u d`)\n" );
//     fprintf( stderr, "          zlib-format files to STDOUT. Use `-u C` and `-u D`\n" );
//     fprintf( stderr, "          to manage raw compressed files. No index involved.\n" );
//     fprintf( stderr, " -v #: output verbosity: from `0` (none) to `5` (nuts).\n" );
//     fprintf( stderr, "     Default is `1` (normal).\n" );
//     fprintf( stderr, " -w: wait for creation if file doesn't exist, when using `-[cdST]`.\n" );
//     fprintf( stderr, " -W: do not Write index to disk. But if one is already available\n" );
//     fprintf( stderr, "     read and use it. Useful if the index is still under an `-S` run.\n" );
//     fprintf( stderr, " -x: create index with line number information (win/*nix compatible).\n" );
//     fprintf( stderr, "     (Index counts last line even w/o newline char (`wc` does not!)).\n" );
//     fprintf( stderr, "     This is implicit unless `-X` or `-z` are indicated.\n" );
//     fprintf( stderr, " -X: like `-x`, but newline character is '\\r' (old mac).\n" );
//     fprintf( stderr, " -z: create index without line number information.\n" );
//     fprintf( stderr, "\n" );
//     fprintf( stderr, "  EXAMPLE: Extract data from 1 GiB byte (byte 2^30) on,\n" );
//     fprintf( stderr, "  from `myfile.gz` to the file `myfile.txt`. Also gztool will\n" );
//     fprintf( stderr, "  create (or reuse, or complete) an index file named `myfile.gzi`:\n" );
//     fprintf( stderr, "  $ gztool -b 1G myfile.gz > myfile.txt\n" );
//     fprintf( stderr, "\n" );
// }

// OUTPUT:
// EXIT_* error code or EXIT_OK on success
int main(int argc, char **argv)
{
    // variables for used for the different actions:
    char *file_name = NULL;
    FILE *in = NULL;
    struct access *index = NULL;

    // variables for grabbing the options:
    uint64_t extract_from_byte = 0LLU;
    uint64_t extract_from_line = 0LLU;
    uint64_t expected_first_byte = 1LLU;
    uint64_t span_between_points = SPAN;
    uint64_t range_number_of_bytes = 0LLU;
    uint64_t range_number_of_lines = 0LLU;
    char *index_filename = NULL;
    int index_filename_indicated = 0;
    int force_strict_order = 0;
    int end_on_first_proper_gzip_eof = 0;
    int always_create_a_complete_index = 0;
    int wait_for_file_creation = 0;
    int extend_index_with_lines = 0;
    bool force_index_without_lines = false; // marks `-z` use
    int gzip_stream_may_be_damaged = 0;
    bool lazy_gzip_stream_patching_at_eof = false;
    bool indicate_range_in_absolute_value = false;

    enum EXIT_RETURNED_VALUES ret_value;
    enum ACTION action;

    int opt = 0;
    uint64_t i = 0;
    int actions_set = 0;

    action = ACT_NOT_SET;
    ret_value = EXIT_OK;
    while ((opt = getopt(argc, argv, "123456789a:Ab:cCdDeEfFhiI:lL:n:pPr:R:s:StTu:v:wWxXz")) != -1)
    {
        switch (opt)
        {
        // `-A` modifies `-[rR]` to indicate the range of bytes/lines in
        // absolute values, instead of the default incremental values.
        case 'A':
            indicate_range_in_absolute_value = true;
            break;
        // `-b #` extracts data from indicated position byte in uncompressed stream of <FILE>
        case 'b':
            extract_from_byte = (uint64_t)giveMeAnInteger(optarg);
            action = ACT_EXTRACT_FROM_BYTE;
            actions_set++;
            break;
        // `-C` generates always a complete index file, ignoring possible decompression errors
        case 'C':
            always_create_a_complete_index = 1;
            break;
        // `-E` ends gzip processing on first proper (at feof()) GZIP EOF,
        // or with `-c` continue waiting for data even at eof()
        case 'E':
            end_on_first_proper_gzip_eof = 1;
            break;
        // First create index, the process indicated action
        case 'F':
            force_strict_order = 1;
            break;
        // `-i` create index for <FILE>
        case 'i':
            action = ACT_CREATE_INDEX;
            actions_set++;
            break;
        // list number of bytes, but not 0 valued
        // `-I INDEX_FILE` creates index for <FILE> with name <INDEX_FILE>
        case 'I':
            // action = ACT_CREATE_INDEX but only if no other option indicated
            index_filename_indicated = 1;
            index_filename = malloc(strlen(optarg) + 1);
            memcpy(index_filename, optarg, strlen(optarg) + 1);
            break;
        // `-L #` extracts data from indicated line in uncompressed stream of <FILE>
        case 'L':
            extract_from_line = (uint64_t)giveMeAnInteger(optarg);
            action = ACT_EXTRACT_FROM_LINE;
            actions_set++;
            if (extend_index_with_lines == 0)
            {
                // if `-[xX]` is not indicated, newline format is Unix
                extend_index_with_lines = 1;
            }
            break;
        // `-n #` indicates that the first byte on compressed input is #, not 1
        // and so truncated compressed inputs can be used. This is subject to checks later on.
        case 'n':
            expected_first_byte = (uint64_t)giveMeAnInteger(optarg);
            if (expected_first_byte == 0)
            {
                printToStderr("ERROR: Option `-n 0` invalid: must be >= 1.\n");
                return EXIT_INVALID_OPTION;
            }
            break;
        // `-p` indicates that the gzip input stream may be composed of various
        // incorrectly terminated GZIP streams, and so then a careful
        // Patching of the input may be needed to extract correct data.
        case 'p':
            gzip_stream_may_be_damaged = CHUNKS_TO_DECOMPRESS_IN_ADVANCE;
            break;
        // `-P` is like `-p`, but when used with `-[ST]` implies that checking
        // for errors in stream is made as quick as possible as the gzip file
        // grows, not respecting then at EOF the CHUNKS_TO_DECOMPRESS_IN_ADVANCE value.
        case 'P':
            gzip_stream_may_be_damaged = CHUNKS_TO_DECOMPRESS_IN_ADVANCE;
            lazy_gzip_stream_patching_at_eof = true;
            break;
        // `-r` is used to indicate how many bytes to extract when using `-[bL]`
        // Accepts SI suffixes.
        case 'r':
            range_number_of_bytes = (uint64_t)giveMeAnInteger(optarg);
            if (range_number_of_bytes == 0LLU)
            {
                printToStderr("ERROR: Option `-r 0` invalid: must be >= 1.\n");
                return EXIT_INVALID_OPTION;
            }
            break;
        // `-R` is used to indicate how many lines to extract when using `-[bL]`
        // Accepts SI suffixes.
        case 'R':
            range_number_of_lines = (uint64_t)giveMeAnInteger(optarg);
            if (range_number_of_lines == 0LLU)
            {
                printToStderr("ERROR: Option `-R 0` invalid: must be >= 1.\n");
                return EXIT_INVALID_OPTION;
            }
            if (extend_index_with_lines == 0)
            {
                // if `-[xX]` is not indicated, newline format is Unix
                extend_index_with_lines = 1;
            }
            break;
        // `-s #` span between index points, in MiB
        case 's':
            // span is converted from MiB to bytes for internal use
            span_between_points = strtoll(optarg, NULL, 10);
            if (span_between_points > UINT64_MAX / 1024 / 1024)
            {
                printToStderr("ERROR: Option `-s %llu` MiB value too high!\n", span_between_points);
                return EXIT_INVALID_OPTION;
            }
            span_between_points = span_between_points * 1024 * 1024;
            break;
        // `-w` wait for file creation with `-[STcd]`
        case 'w':
            wait_for_file_creation = 1;
            break;
        // `-x` create extended index with line number information
        // using Unix newline format ('\n')  (compatible with Windows \r\n)
        case 'x':
            extend_index_with_lines = 1;
            break;
        // `-z` create index without line number information
        case 'z':
            force_index_without_lines = true;
            break;
        default:
            printToStderr("\n");
            abort();
        }
    }

    if (actions_set > 1)
    {
        printToStderr("ERROR: do not merge parameters `-[bcdilLStTu]`.\n");
        return EXIT_INVALID_OPTION;
    }

    if (wait_for_file_creation == 1)
    {
        printToStderr("ERROR: `-w` only apply to `-[cdST]`\n");
        return EXIT_INVALID_OPTION;
    }

    if (span_between_points <= 0)
    {
        printToStderr("ERROR: Invalid `-s` parameter value: '%llu'\n", span_between_points);
        return EXIT_INVALID_OPTION;
    }

    if (0 == actions_set)
    {
        // `-I <FILE>` is equivalent to `-i -I <FILE>`
        if (action == ACT_NOT_SET && index_filename_indicated == 1)
        {
            action = ACT_CREATE_INDEX;
            if ((optind + 1) < argc)
            {
                // too much files indicated to use `-I`
                printToStderr("ERROR: `-I` is incompatible with multiple input files.\nAborted.\n\n");
                return EXIT_INVALID_OPTION;
            }
        }
        else
        {
            // default action is `-i`
            action = ACT_CREATE_INDEX;
            actions_set = 1;
        }
    }

    if ( // these are the actions that can create an index
        action == ACT_EXTRACT_FROM_BYTE ||
        action == ACT_CREATE_INDEX ||
        action == ACT_EXTRACT_FROM_LINE
        // it's stated that "DECOMPRESS does not require index" @ action_create_index() BUT
        // also that "`gztool -d` is just an alias for `gztool -b0` when using STDIN" @ main()
    )
    {
        // if `-z`, honor it, if not, make `-x` implicit unless, in both cases, `-[xX]` was used:
        if (true == force_index_without_lines)
        {
            if (extend_index_with_lines > 0)
            { // unless `-[xX]` was used
                printToStderr("ERROR: `-[xX]` and `-z` cannot be mixed.\n");
                return EXIT_INVALID_OPTION;
            }
            extend_index_with_lines = 0;
        }
        else
        {
            // make `-x` implicit
            if (extend_index_with_lines == 0)
            {                                // only if `-[xX]` was not used
                extend_index_with_lines = 3; // by default use '\n' as newline char
            }
        }
    }
    else
    {
        if (true == force_index_without_lines)
        {
            printToStderr("ERROR: `-z` is not applicable to these options.\n");
            return EXIT_INVALID_OPTION;
        }
    }

    if (expected_first_byte != 1 &&
        (action != ACT_EXTRACT_FROM_BYTE && action != ACT_EXTRACT_FROM_LINE &&
         action != ACT_CREATE_INDEX))
    {
        printToStderr("ERROR: `-n` parameter can only be used with `-[biL]`.\n");
        return EXIT_INVALID_OPTION;
    }

    if ((range_number_of_bytes > 0LLU || range_number_of_lines > 0LLU) &&
        (action != ACT_EXTRACT_FROM_BYTE && action != ACT_EXTRACT_FROM_LINE))
    {
        printToStderr("ERROR: `-[rR]` parameter can only be used with `-[bL]`.\n");
        return EXIT_INVALID_OPTION;
    }

    if (true == indicate_range_in_absolute_value)
    {
        if (range_number_of_bytes == 0LLU &&
            range_number_of_lines == 0LLU)
        {
            printToStderr("ERROR: `-A` parameter can only be used with `-[rR]`.\n");
            return EXIT_INVALID_OPTION;
        }
        // ( yes, `-r` AND `-R` can be simultaneosuly indicated: the 1st reached is the one applied. )
        if (range_number_of_bytes > 0LLU)
        {
            if (action == ACT_EXTRACT_FROM_BYTE &&
                extract_from_byte > 0LLU)
            {
                if (range_number_of_bytes > extract_from_byte)
                {
                    range_number_of_bytes -= extract_from_byte;
                }
                else
                {
                    printToStderr("ERROR: `-Ar` implies a value greater than `-b`.\n");
                    return EXIT_INVALID_OPTION;
                }
            }
            else
            {
                printToStderr("ERROR: `-Ar` (bytes) cannot be used with `-L` (lines).\n");
                return EXIT_INVALID_OPTION;
            }
        }
        if (range_number_of_lines > 0LLU)
        {
            if (action == ACT_EXTRACT_FROM_LINE &&
                extract_from_line > 0LLU)
            {
                if (range_number_of_lines > extract_from_line)
                {
                    range_number_of_lines -= extract_from_line;
                }
                else
                {
                    printToStderr("ERROR: `-AR` implies a value greater than `-L`.\n");
                    return EXIT_INVALID_OPTION;
                }
            }
            else
            {
                printToStderr("ERROR: `-AR` (lines) cannot be used with `-b` (bytes).\n");
                return EXIT_INVALID_OPTION;
            }
        }
    }

    if (1 == force_strict_order)
    {
        printToStderr("ERROR: Cannot use `-F` with `-[dlSTu]`.\n");
        return EXIT_INVALID_OPTION;
    }

    if (true == lazy_gzip_stream_patching_at_eof)
    {
        printToStderr("ERROR: without `-[ST]`, use `-p` instead of `-P`.\n");
        return EXIT_INVALID_OPTION;
    }

    if (gzip_stream_may_be_damaged > 0 &&
        !(action == ACT_EXTRACT_FROM_BYTE ||
          action == ACT_EXTRACT_FROM_LINE ||
          action == ACT_CREATE_INDEX))
    {
        printToStderr("ERROR: Cannot use `-p` without `-[bdiLST]`.\n");
        return EXIT_INVALID_OPTION;
    }

    /*
     * end of "Checking parameter merging and absence"
     */

    // inform action on stderr:
    char *action_string = NULL;
    switch (action)
    {
    case ACT_EXTRACT_FROM_BYTE:
        action_string = "Extract from byte = ";
        break;
    case ACT_CREATE_INDEX:
        action_string = "Create index for a gzip file";
        break;
    case ACT_EXTRACT_FROM_LINE:
        action_string = "Extract from line = ";
        break;
    default:
        printToStderr("ERROR: Unexpected error! No action specified.\n");
        return EXIT_GENERIC_ERROR;
    }
    switch (action)
    {
    case ACT_EXTRACT_FROM_BYTE:
        printToStderr("ACTION: %s%llu", action_string, extract_from_byte);
        if (expected_first_byte > 1)
        {
            printToStderr(" (input - %llu bytes)", expected_first_byte - 1);
        }
        printToStderr("\n\n");
        break;
    case ACT_EXTRACT_FROM_LINE:
        printToStderr("ACTION: %s%llu\n\n", action_string, extract_from_line);
        break;
    default:
        printToStderr("ACTION: %s\n\n", action_string);
    }

    file_name = argv[optind];

    // if no index filename is set (`-I`), it is derived from each <FILE> parameter
    if (0 == index_filename_indicated)
    {
        if (NULL != index_filename)
        {
            free(index_filename);
            index_filename = NULL;
        }
        index_filename = malloc(strlen(argv[i]) + 4 + 1);
        sprintf(index_filename, "%s", argv[i]);

        if (strlen(argv[i]) > 3 && // avoid out-of-bounds
            (char *)strstr(index_filename, ".gz") ==
                (char *)(index_filename + strlen(argv[i]) - 3))
            // if gzip-file name is 'FILE.gz', index file name will be 'FILE.gzi'
            sprintf(index_filename, "%si", argv[i]);
        else
            // otherwise, the complete extension '.gzi' is appended
            sprintf(index_filename, "%s.gzi", argv[i]);
    }

    if ((action == ACT_CREATE_INDEX ||
            action == ACT_EXTRACT_FROM_BYTE || action == ACT_EXTRACT_FROM_LINE) &&
        access(index_filename, F_OK) != -1)
    {
        // index file already exists

        /*if ( ( extend_index_with_lines > 0 && action != ACT_EXTRACT_FROM_LINE ) &&
                ( force_action == 0 || ( force_action == 1 && write_index_to_disk == 0 ) ) )
                printToStderr("WARNING: `-[Xx]` will be ignored because index already exists.\n" );*/

        // delete index file
        printToStderr("Using `-f` force option: Deleting '%s' ...\n", index_filename);
        if (remove(index_filename) != 0)
        {
            printToStderr("ERROR: Could not delete '%s'.\nAborted.\n", index_filename);
            ret_value = EXIT_GENERIC_ERROR;
        }
    }

    // check possible errors and `-e` before proceed
    if (ret_value != EXIT_OK)
    {
        goto end_of_using_index;
    }

    // create index first if `-F`
    // (checking of conformity between `-F` and action has been done before)
    if (force_strict_order == 1)
    {
        ret_value = action_create_index(file_name, &index, index_filename,
                                        JUST_CREATE_INDEX, 0, 0, span_between_points,
                                        end_on_first_proper_gzip_eof,
                                        always_create_a_complete_index, WAITING_TIME,
                                        wait_for_file_creation, extend_index_with_lines,
                                        expected_first_byte, gzip_stream_may_be_damaged,
                                        lazy_gzip_stream_patching_at_eof,
                                        range_number_of_bytes, range_number_of_lines);
        if (ret_value != EXIT_OK)
        {
            printToStderr("ERROR: Could not create index '%s'.\nAborted.\n", index_filename);
            goto end_of_using_index;
        }
    }

    // "-bil" options can accept multiple files
    switch (action)
    {

    case ACT_EXTRACT_FROM_BYTE:
        ret_value = action_create_index(file_name, &index, index_filename,
                                        EXTRACT_FROM_BYTE, extract_from_byte, 0, span_between_points,
                                        end_on_first_proper_gzip_eof,
                                        always_create_a_complete_index, WAITING_TIME,
                                        wait_for_file_creation, extend_index_with_lines,
                                        expected_first_byte, gzip_stream_may_be_damaged,
                                        lazy_gzip_stream_patching_at_eof,
                                        range_number_of_bytes, range_number_of_lines);
        break;

    case ACT_EXTRACT_FROM_LINE:
        ret_value = action_create_index(file_name, &index, index_filename,
                                        EXTRACT_FROM_LINE, 0, extract_from_line, span_between_points,
                                        end_on_first_proper_gzip_eof,
                                        always_create_a_complete_index, WAITING_TIME,
                                        wait_for_file_creation, extend_index_with_lines,
                                        expected_first_byte, gzip_stream_may_be_damaged,
                                        lazy_gzip_stream_patching_at_eof,
                                        range_number_of_bytes, range_number_of_lines);
        break;

    case ACT_CREATE_INDEX:
        if (force_strict_order == 0)
            // if force_strict_order == 1 action has already been done!
            ret_value = action_create_index(file_name, &index, index_filename,
                                            JUST_CREATE_INDEX, 0, 0, span_between_points,
                                            end_on_first_proper_gzip_eof,
                                            always_create_a_complete_index, WAITING_TIME,
                                            wait_for_file_creation, extend_index_with_lines,
                                            expected_first_byte, gzip_stream_may_be_damaged,
                                            lazy_gzip_stream_patching_at_eof,
                                            range_number_of_bytes, range_number_of_lines);
        break;

    default:
        printToStderr("ERROR: action not specified.\n");
        ret_value = EXIT_GENERIC_ERROR;
    }

    if (ret_value != EXIT_OK)
    {
        printToStderr("Aborted.\n");
        // break the for loop
        printToStderr("file processed with errors!\n");
        printToStderr("\n");
    }

end_of_using_index:

    // final freeing of resources
    if (NULL != in)
    {
        fclose(in);
    }
    if (NULL != index)
    {
        free_index(index);
    }
    if (NULL != index_filename)
    {
        free(index_filename);
    }

    return ret_value;
}
