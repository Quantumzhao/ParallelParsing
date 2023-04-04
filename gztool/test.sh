#!/bin/bash
if [ `bash ./compile.sh` ]; then
	exit 1;
fi

rm "./tests/gplv3.txt.gzi"

# ---------- INDEX CREATION -------------

OUTPUT_FILE=./tests/gplv3.txt.gzi

CKSUM0=`md5sum "./tests/gplv3.txt.gzi.ORIG" | awk '{print $1;}'`

`./gztool -xi -I "./tests/gplv3.txt.gzi" "./tests/gplv3.txt.gz"`

CKSUM1=`md5sum "./tests/gplv3.txt.gzi" | awk '{print $1;}'`

if [ "$CKSUM0" = "$CKSUM1" ]; then
	echo "Indexes check: PASS"
else
	echo $CKSUM0 != $CKSUM1;
	echo "Indexes check: FAIL"
	exit 1;
fi

# ----------- DECOMPRESSION W/ INDEX -----------

# CKSUMF0=`md5sum "./tests/gplv3.txt.ORIG" | awk '{print $1;}'`

# ln ./tests/gplv3.txt.gz ./tests/gplv3.txt.cp.gz &&
# `./gztool -d -I ./tests/gplv3.txt.gzi ./tests/gplv3.txt.cp.gz`

# CKSUMF1=`md5sum "./tests/gplv3.txt.cp" | awk '{print $1;}'`

# if [ "$CKSUMF0" = "$CKSUMF1" ]; then
# 	rm "./tests/gplv3.txt.cp"
# 	echo "Decompression check: PASS"
# else
# 	echo $CKSUM0 != $CKSUM1;
# 	echo "Decompression check: FAIL"
# 	exit 1;
# fi

# ----------- DECOMPRESSION W/ STARTING POS -------------
# set the position to 400

`./gztool -b 400  -I ./tests/gplv3.txt.gzi ./tests/gplv3.txt.gz > test_400.txt`
