all:
	LITERATE=weave gforth test1.fs
	LITERATE=tangle gforth test1.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen test1.opf || true
	gforth test1.fs

lit:
	LITERATE=tangle gforth literate_lit.fs
	LITERATE=weave gforth literate_lit.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen literate.opf || true

install:
	cp literate_out.fs literate.fs

deploy:
	cp literate.mobi /Volumes/Kindle/documents
	diskutil eject Kindle

snapshot:
	rm -rf snap
	mkdir snap
	cp *.mobi snap
	cp *.html snap
	cp *.fs snap
	zip -r literate.zip snap/

clean :
	rm -f *.opf *.ncx *.mobi power4.fs main.fs *.html literate_out.fs
	rm -rf snap
