#!/bin/bash
OUTPUT_FILE=./tests/gplv3.txt.gzi

CKSUM0=`md5sum "./tests/gplv3.txt.gzi.ORIG" | awk '{print $1;}'`
echo gplv3.txt.gzi.ORIG CHECKSUM = $CKSUM0

`./gztool -fxi -I "${OUTPUT_FILE}" "./tests/gplv3.txt.gz"`

CKSUM1=`md5sum "${OUTPUT_FILE}" | awk '{print $1;}'`
echo gplv3.txt.gzi CHECKSUM = $CKSUM1

if [ "$CKSUM0" = "$CKSUM1" ]; then
	rm "${OUTPUT_FILE}"
	exit 0;
else
	echo $CKSUM0 != $CKSUM1;
	exit 1;
fi
