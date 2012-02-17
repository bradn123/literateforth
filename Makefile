all:
	LITERATE=weave gforth test1.fs
	LITERATE=tangle gforth test1.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen index.opf
	gforth test1.fs

clean :
	rm -f index.* power4.fs main.fs *.html
