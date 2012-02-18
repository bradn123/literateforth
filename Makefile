all:
	LITERATE=weave gforth test1.fs
	LITERATE=tangle gforth test1.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen index.opf || true
	gforth test1.fs

lit:
	LITERATE=weave gforth literate_lit.fs
	LITERATE=tangle gforth literate_lit.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen index.opf || true

clean :
	rm -f index.* power4.fs main.fs *.html literate_out.fs
