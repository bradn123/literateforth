all: test1.stamp literate.stamp

.SECONDARY:

%.stamp: %.fs %.mobi
	touch $@

%.fs: %_lit.fs
	LITERATE=tangle gforth $<

%.opf: %_lit.fs
	LITERATE=weave gforth $<

%.mobi: %.opf
	~/kindle/KindleGen_Mac_i386_v2/kindlegen $<

install:
	cp literate.fs literate_tangled.fs

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
	rm -f *.opf *.ncx *.mobi test1_power4.fs test1.fs *.html literate.fs *.bmp *.zip *.stamp
	rm -rf snap
