OUT=out
VPATH=src

all: $(OUT)/test1.stamp $(OUT)/literate.stamp

.SECONDARY:

$(OUT):
	mkdir -p $@

$(OUT)/%.stamp: $(OUT)/%.fs $(OUT)/%.mobi | $(OUT)
	cd $(@D) && touch ../$@

$(OUT)/%.fs: %_lit.fs | $(OUT)
	cd $(@D) && LITERATE=tangle gforth ../$<

$(OUT)/%.opf: %_lit.fs | $(OUT)
	cd $(@D) && LITERATE=weave gforth ../$<

$(OUT)/%.mobi: $(OUT)/%.opf | $(OUT)
	cd $(@D) && ~/kindle/KindleGen_Mac_i386_v2/kindlegen ../$<

install: $(OUT)/literate.fs
	cp $< src/literate_tangled.fs

deploy:
	cp $(OUT)/literate.mobi /Volumes/Kindle/documents
	diskutil eject Kindle

snapshot: all
	rm -rf snap
	mkdir snap
	cp -r src snap
	cp -r out snap
	zip -r literate.zip snap/

clean:
	rm -rf out snap literate.zip
