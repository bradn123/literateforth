all:
	LITERATE=weave gforth test1_lit.fs
	LITERATE=tangle gforth test1_lit.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen test1.opf
	gforth test1_lit.fs

lit:
	LITERATE=tangle gforth literate_lit.fs
	LITERATE=weave gforth literate_lit.fs
	~/kindle/KindleGen_Mac_i386_v2/kindlegen literate.opf

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

clean:
	rm -f *.opf *.ncx *.mobi power4.fs main.fs *.html literate_out.fs *.bmp *.zip
	rm -rf snap
