OUT=out
VPATH=src

all: $(OUT)/index.html test1 literate events

.SECONDARY:

test1: $(OUT)/test1.stamp
literate: $(OUT)/literate.stamp
events: $(OUT)/events.stamp
	gforth src/events_lit.fs -e bye

LITERATE_TOOL=src/literate_lit.fs

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

# literate uses literate_tangle.fs to avoid circularity.
$(OUT)/literate.fs: src/literate_tangled.fs
$(OUT)/literate.opf: src/literate_tangled.fs

install: $(OUT)/literate.fs
	cp $< src/literate_tangled.fs

uninstall:
	git checkout src/literate_tangled.fs

$(OUT)/index.html: src/index.html | $(OUT)
	cp $< $@

deploy:
	cp $(OUT)/literate.mobi /Volumes/Kindle/documents
	diskutil eject Kindle

publish:
	make clean
	git checkout gh-pages
	git merge master
	make clean
	make
	git commit -a -m "Publish"
	git checkout master

snap: all
	rm -rf snapshot
	mkdir snapshot
	cp -r src snapshot
	cp -r out snapshot
	zip -r literate.zip snapshot/

clean:
	rm -rf out snapshot literate.zip
