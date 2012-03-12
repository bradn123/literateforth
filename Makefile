OUT=out
VPATH=src

all: $(OUT)/test1.stamp $(OUT)/literate.stamp

.SECONDARY:

LITERATE_TOOL=src/literate_tangled.fs

$(OUT):
	mkdir -p $@

$(OUT)/%.stamp: $(OUT)/%.fs $(OUT)/%.mobi | $(OUT)
	cd $(@D) && touch ../$@

$(OUT)/%.fs: %_lit.fs $(LITERATE_TOOL) | $(OUT)
	@echo "-- Tangling $<"
	cd $(@D) && gforth -e 1 ../$<

$(OUT)/%.opf: %_lit.fs $(LITERATE_TOOL) | $(OUT)
	@echo "-- Weaving $<"
	cd $(@D) && gforth -e 2 ../$<

$(OUT)/%.mobi: $(OUT)/%.opf | $(OUT)
	cd $(@D) && ~/kindle/KindleGen_Mac_i386_v2/kindlegen ../$<

# test1 uses literate_lit.fs directly.
$(OUT)/test1.mobi: src/literate_lit.fs
$(OUT)/test1.opf: src/literate_lit.fs

install: $(OUT)/literate.fs
	cp $< src/literate_tangled.fs

uninstall:
	git checkout src/literate_tangled.fs

deploy:
	cp $(OUT)/literate.mobi /Volumes/Kindle/documents
	diskutil eject Kindle

snap: all
	rm -rf snapshot
	mkdir snapshot
	cp -r src snapshot
	cp -r out snapshot
	zip -r literate.zip snapshot/

clean:
	rm -rf out snapshot literate.zip
