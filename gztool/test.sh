#!/bin/bash
if [ `bash ./compile.sh` ]; then
	exit 1;
fi

# ---------- INDEX CREATION -------------

OUTPUT_FILE=./tests/gplv3.txt.gzi

CKSUM0=`md5sum "./tests/gplv3.txt.gzi.ORIG" | awk '{print $1;}'`
echo gplv3.txt.gzi.ORIG CHECKSUM = $CKSUM0

`./gztool -fxi -I "./tests/gplv3.txt.gzi" "./tests/gplv3.txt.gz"`

CKSUM1=`md5sum "./tests/gplv3.txt.gzi" | awk '{print $1;}'`
echo gplv3.txt.gzi CHECKSUM = $CKSUM1

if [ "$CKSUM0" = "$CKSUM1" ]; then
	rm "./tests/gplv3.txt.gzi"
	echo "Indexes check: PASS"
else
	echo $CKSUM0 != $CKSUM1;
	echo "Indexes check: FAIL"
	exit 1;
fi

# ----------- DECOMPRESSION W/ INDEX -----------

CKSUMF0=`md5sum "./tests/gplv3.txt.ORIG" | awk '{print $1;}'`
echo gplv3.txt.ORIG CHECKSUM = $CKSUMF0

ln ./tests/gplv3.txt.gz ./tests/gplv3.txt.cp.gz &&
`./gztool -d -I ./tests/gplv3.txt.gzi ./tests/gplv3.txt.cp.gz`

CKSUMF1=`md5sum "./tests/gplv3.txt.cp" | awk '{print $1;}'`
echo gplv3.txt CHECKSUM = $CKSUM1

if [ "$CKSUMF0" = "$CKSUMF1" ]; then
	rm "./tests/gplv3.txt.cp"
	echo "Decompression check: PASS"
else
	echo $CKSUM0 != $CKSUM1;
	echo "Decompression check: FAIL"
	exit 1;
fi
