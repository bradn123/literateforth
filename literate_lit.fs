s" literate.fs" included

|title: Literate Forth
|author: Brad Nelson

|chapter: Overview
|section: Major Structure

|file: literate_out.fs
|: literate_out.fs
|@ *
|;

|section: program structure

|: *
|@ isolate in wordlist
|@ assertion support
|@ setup mode flags
|@ utility words
|@ linked lists
|@ implement atoms
|@ pipe parsing
|@ escaping atoms
|@ chunks
|@ tex and latex shortcuts
|@ output files
|@ global fields
|@ chapters
|@ opf
|@ ncx
|@ toc
|@ chapters and sections
|@ file writing implementation
|@ chapter structure
|@ weaving implementation
|@ tangle implementation
|@ run implementation
|@ apply literate mode
|; 


|section: isolate in wordlist

|: isolate in wordlist
vocabulary literate also literate definitions
|;


|section: Modes of operation

We will need to decide which mode in which to operate.
For the moment we will use the value of the LITERATE
environment variable to select which mode.
|$
Modes currently include: weave, tangle, running.
Running is selected by having LITERATE unset or empty.
Anything else is considered an error.
|: setup mode flags
\ Decide if we're weaving or tangling.
: literate-env ( -- $ ) s" LITERATE" getenv ;
literate-env s" weave" compare 0= constant weaving?
literate-env s" tangle" compare 0= constant tangling?
literate-env s" " compare 0= constant running?
\ Require we are in one of the modes.
weaving? tangling? or running? or assert
|;

|: apply literate mode
|\ : |. ( exit literate mode )
|\     chapter-finish
|\     weaving? if weave bye then
|\     tangling? if tangle bye then
|\     running? if run then ;
|;


|chapter: Foundations

|section: Assertions
We will often want to check if certain conditions are true,
halting if they are not.
|: assertion support
: assert ( n -- ) 0= if abort then ;
|;
Additionally we may want to set a variable to a non-zero value,
ensuring that this happens only once.
|: assertion support
: once! ( n a -- ) dup @ 0= assert ! ;
|;


|section: Linked lists

|: linked lists
: chain-link ( head[t] -- a head[t] ) here 0 , swap ;
: chain-first ( head[t] -- ) chain-link 2dup ! cell+ ! ;
: chain-rest ( head[t] -- ) chain-link cell+ 2dup @ ! ! ;
: chain ( head[t] -- ) dup @ if chain-rest else chain-first then ;

: allocate' ( n -- a ) allocate 0= assert ;
: zero ( a n -- ) 0 fill ;
: allocate0 ( n -- a ) dup allocate' swap 2dup zero drop ;

: nchain-new ( n -- a ) 1+ cells allocate0 ;
: nchain-fillout ( .. a n -- a ) 0 do dup i 1+ cells + swap >r ! r> loop ;
: nchain-link ( ..n -- a ) dup nchain-new swap nchain-fillout ;
: nchain-first ( ..n head[t] -- ) >r nchain-link r> 2dup ! cell+ ! ;
: nchain-rest ( ..n head[t] -- ) >r nchain-link r> 2dup cell+ @ ! cell+ ! ;
: nchain ( ..n head[t] -- ) dup @ if nchain-rest else nchain-first then ;

: ->next ( a -- a' ) @ ;
: linked-list   create 0 , 0 , ;

|;


|section: Odds and ends
We will need to clone strings occasionally.
|: utility words
: $clone ( $ - $ ) dup allocate 0= assert swap 2dup >r >r move r> r> ;
|;
We will also need to duplicate three items off the stack.
|: utility words
: 3dup ( xyz -- xyzxyz ) >r 2dup r> dup >r swap >r swap r> r> ;
|;


|chapter: Atoms

|section: Implementing Atoms

|: testing atoms
\ Test using atoms.
atom" foo" atom" foo" = assert
atom" bar" atom" foo" <> assert
|;

|: testing atom+ 
\ Test atom+.
atom" testing" atom" 123" atom+ atom" testing123" = assert
|;

|: testing means
\ Test means.
atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert
|;

|: implement atoms
\ Atomic strings.
\ Layout of an atom (in cells):
\   - next atom
\   - string length
\   - string start
\   - definition head
\   - definition tail
\ Layout of a definition link (in cells):
\   - next link
\   - is_reference?
\   - atom
: atom-length@ ( A -- n ) 1 cells + @ ;
: atom-data@ ( A -- a ) 2 cells + @ ;
: atom-string@ ( A -- $ ) dup atom-data@ swap atom-length@ ;
: atom-def-head ( A -- A[head] ) 3 cells + ;

linked-list atom-root
: $atom-new ( $ -- A ) >r >r 0 0 r> r> 4 atom-root nchain
                       atom-root cell+ @ ;
: atom-new ( $ -- A ) $clone $atom-new ;

: atom. ( A -- ) atom-string@ type ;

: atoms. ( -- ) atom-root @ begin dup while dup atom. cr ->next repeat drop ;

: atom= ( $ A -- f ) atom-string@ compare 0= ;

: atom-find' ( $ A -- A ) dup 0= if nip nip exit then
                          3dup atom= if nip nip exit then
                          ->next recurse ;
: atom-find ( $ -- A ) atom-root @ atom-find' ;

: atom ( $ -- A ) 2dup atom-find dup if nip nip else drop atom-new then ;
: $atom ( $ -- A ) 2dup atom-find dup if nip nip else drop $atom-new then ;
: atom" ( -- A ) [char] " parse
                  state @ if postpone sliteral postpone atom
                          else atom then ; immediate
: atom"" ( -- A ) 0 0 atom ;
: atom{ ( -- A ) [char] } parse
                 state @ if postpone sliteral postpone atom
                         else atom then ; immediate
 
: atom-append ( A n Ad -- ) atom-def-head chain , , ;
: atom+=$ ( A Ad -- ) 0 swap atom-append ;
: atom+=ref ( A Ad -- ) 1 swap atom-append ;


|@ testing atoms


: ref-parts ( ref -- A ref? ) cell+ dup cell+ @ swap @ ;
: atom-walk ( fn A -- )
     atom-def-head @ begin dup while
         2dup >r >r
         ref-parts if recurse else swap execute then
         r> r>
         ->next
     repeat 2drop ;
: tally-length ( n A -- n ) atom-length@ + ;
: gather-string ( a A -- a' ) 2dup atom-string@ >r swap r> move tally-length ;
: atom-walk-length ( A -- n ) 0 swap ['] tally-length swap atom-walk ;
: atom-walk-gather ( a A -- ) swap ['] gather-string swap atom-walk drop ;
: means ( A -- A' ) dup atom-walk-length dup allocate 0= assert
                    swap 2dup >r >r drop
                    atom-walk-gather r> r> $atom ;

|@ testing means

: atom, ( A -- ) atom-string@ dup here swap allot swap move ;
: atom>>$ ( A d -- d' ) 2dup >r atom-string@ r> swap move swap atom-length@ + ;
: atom+ ( A A -- A ) swap 2dup atom-length@ swap atom-length@ + dup >r
                     allocate 0= assert dup >r
                     atom>>$ atom>>$ drop r> r> $atom ;
: atom-ch ( ch -- A ) 1 allocate 0= assert 2dup c! nip 1 atom ;
10 atom-ch constant atom-cr
: atom-cr+ ( A -- A ) atom-cr atom+ ;

|@ testing atom+
|;

|section: html escaping

|: escaping atoms
: escape-ch ( ch -- )
   dup [char] < = if [char] & c, [char] l c, [char] t c,
                     [char] ; c, drop exit then
   dup [char] > = if [char] & c, [char] g c, [char] t c,
                     [char] ; c, drop exit then
   dup [char] " = if [char] & c, [char] q c, [char] u c, [char] o c,
                     [char] t c, [char] ; c, drop exit then
   dup [char] & = if [char] & c, [char] a c, [char] m c, [char] p c,
                     [char] ; c, drop exit then
   c, ;
: escape-each ( A -- ) atom-string@ 0 ?do dup i + c@ escape-ch loop drop ;
: escape ( A -- A ) here swap escape-each here over - align $atom ;
|;


|chapter: Parsing

|section: parsing pipe

|: testing parsing
\ Test parsing.
|\ : |halt! ;
|\ parse..| testing
|\ Hello there
|\ 123|halt!
|\ atom" testing" atom-cr+
|\ atom" Hello there" atom+ atom-cr+
|\ atom" 123" atom+ = assert
|;


|: pipe parsing
|\ : source@ source drop >in @ + ;
|\ : source-remaining source nip >in @ - ;
|\ : drop| ( -- ) source@ 1- c@ [char] | = if -1 >in +! then ;
|\ : need-refill? ( -- f) source nip >in @ <= ;
|\ : on|? ( -- f ) need-refill? if false exit then source@ c@ [char] | = ;
|\ : replenish ( -- f ) need-refill? if refill else true then ;
|\ : ?atom-cr+ ( A -- A ) on|? 0= if atom-cr+ then ;
|\ : eat| ( -- ) [char] | parse drop| atom atom+ ?atom-cr+ ;
|\ : parse-cr ( -- A ) source@ source-remaining atom   source nip >in ! ;
|\ : parse..| ( -- A ) atom"" begin replenish 0=
|\                     if exit then eat| on|? until ;
|\ : skip| ( -- ) on|?  need-refill? 0= and if 1 >in +! then ;
|\ : |-constant ( create atom constant ) constant ;
|;


|chapter: Tags

|section: tex and latex

|: tex and latex shortcuts
|\ : |TeX
    .d{ <span style="font-family:cmr10, LMRoman10-Regular, Times, serif;">T<span style="text-transform:uppercase; vertical-align:-0.5ex; margin-left:-0.1667em; margin-right:-0.125em;">e</span>X</span>}
    feed
;
|;

|: tex and latex shortcuts
|\ : |LaTeX
    .d{ <span style="font-family:cmr10, LMRoman10-Regular, Times, serif;">L<span style="text-transform: uppercase; font-size: 70%; margin-left: -0.36em; vertical-align: 0.3em; line-height: 0; margin-right: -0.15em;">a</span>T<span style="text-transform: uppercase; margin-left: -0.1667em; vertical-align: -0.5ex; line-height: 0; margin-right: -0.125em;">e</span>X</span>}
    feed
;
|;


|section: document chunks

|: chunks
|\ atom" ~~~blackhole" constant blackhole
|\ variable documentation-chunk   blackhole documentation-chunk !
|\ : documentation ( -- A ) documentation-chunk @ ;
|\ 
|\ variable chunk
|\ : doc! ( back to documentation) documentation chunk ! ;
|\ doc!
|\ : doc? ( -- f) documentation chunk @ = ;
|\ : chunk+=$ ( A -- ) chunk @ atom+=$ ;
|\ : chunk+=ref ( A -- ) chunk @ atom+=ref ;
|\ : doc+=$ ( A -- ) documentation atom+=$ ;
|\ : .d{ ( -- ) postpone atom{ postpone doc+=$ ; immediate
|\ : .dcr   atom-cr doc+=$ ;
|\ : doc+=ref ( A -- ) documentation atom+=ref ;
|\ : ?doc+=$ ( A -- ) doc? 0= if escape doc+=$ else drop then ;
|\ : feed ( read into current chunk ) parse..| dup ?atom-cr+ ?doc+=$ atom-cr+ chunk+=$ ;
|\ : doc+=use ( A -- ) .d{ <u><b>} doc+=$ .d{ </b></u>} ;
|\ : doc+=def ( A -- )
|\     .d{ </p><tt><u><b>} doc+=$
|\     .d{ </b></u> +&equiv;</tt><div class="chunk"><pre>} ;
|\ : |@ ( use a chunk ) parse-cr dup chunk+=ref doc+=use .dcr feed ;
|\ : |: ( add to a chunk ) parse-cr dup chunk ! doc+=def feed ;
|\ : || ( escaped | ) atom" |" chunk+=$ feed ;
|\ : |; ( documentation ) doc? 0= if .d{ </pre></div><p>} then doc! feed ;
|\ : |$ ( paragraph ) .d{ </p><p>} feed ;
|\ : |\ ( whole line) parse-cr atom-cr+ dup chunk+=$ ?doc+=$ feed ;
|;


|chapter: Chapters

|section: chapter handling
|: chapter structure
: chapter-name ( chp -- A ) cell+ @ ;
: chapter-text ( chp -- A ) cell+ @ means ;
: chapter-number ( chp -- n ) 2 cells + @ ;
atom" .html" constant .html
: chapter-filename ( chp -- A )
     chapter-number s>d <# # # # #s #> atom .html atom+ ;
|;


|: chapters
|\ parse..| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
|\ "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
|\ <html>
|\ <head>
|\ <style type="text/css">
|\ div.chunk {
|\   margin: 0em 0.5em;
|\ }
|\ pre {
|\   margin: 0em 0em;
|\ }
|\ </style>
|\ <title>|-constant chapter-pre1
|\ 
|\ parse..| </title>
|\ </head>
|\ <body>
|\ <h1>|-constant chapter-pre2
|\ 
|\ parse..| </h1>
|\ <p>
|\ |-constant chapter-pre3
|\ 
|\ parse..|
|\ </p>
|\ </body>
|\ </html>
|\ |-constant chapter-post
|;


|chapter: MOBI Format

|section: OPF files
|: opf
|\ atom" ~~~OPF" constant atom-opf
|\ atom" index.opf" constant opf-filename

|\ parse..| <?xml version="1.0" encoding="utf-8"?>
|\ <package xmlns="http://www.idpf.org/2007/opf" version="2.0"
|\ unique-identifier="BookId">
|\ <metadata xmlns:dc="http://purl.org/dc/elements/1.1/"
|\ xmlns:opf="http://www.idpf.org/2007/opf">
|\   <dc:title>Test1</dc:title>
|\   <dc:language>en-us</dc:language>
|\   <dc:identifier id="BookId" opf:scheme="ISBN">9999999999</dc:identifier>
|\   <dc:creator>me</dc:creator>
|\   <dc:publisher>Self</dc:publisher>
|\   <dc:subject>Article</dc:subject>
|\   <dc:date>2012-02-15</dc:date>
|\   <dc:description>My short description.</dc:description>
|\ </metadata>
|\ 
|\ <manifest>
|\   <item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml"
|\    href="index.ncx"/>
|\   <item id="toc" media-type="application/xhtml+xml" href="index.html"></item>
|\ |-constant opf-pre1
|\ 
|\ parse..| <item id="|-constant opf-chapter-pre1
|\ parse..| " media-type="application/xhtml+xml" href="|-constant opf-chapter-pre2
|\ parse..| "></item>
|\ |-constant opf-chapter-post
|\ : opf-chapter ( A -- )
|\   opf-chapter-pre1 doc+=$ 
|\   dup doc+=$
|\   opf-chapter-pre2 doc+=$ 
|\   doc+=$
|\   opf-chapter-post doc+=$ 
|\ ;
|\ 
|\ parse..|
|\ </manifest>
|\ <spine toc="My_Table_of_Contents">
|\   <itemref idref="toc"/>
|\ |-constant opf-pre2
|\ 
|\ parse..| <itemref idref="|-constant opf-chapter'-pre1
|\ parse..| "/>
|\ |-constant opf-chapter'-post
|\ : opf-chapter' ( A -- )
|\   opf-chapter'-pre1 doc+=$ 
|\   doc+=$
|\   opf-chapter'-post doc+=$ 
|\ ;
|\ 
|\ 
|\ 
|\ parse..|
|\ </spine>
|\ <guide>
|\   <reference type="toc" title="Table of Contents"
|\    href="index.html"></reference>
|\ </guide>
|\ </package>
|\ |-constant opf-post
|;


|section: NCX files
|: ncx
|\ atom" ~~~NCX" constant atom-ncx
|\ atom" index.ncx" constant ncx-filename

|\ parse..| <?xml version="1.0" encoding="UTF-8"?>
|\ <!DOCTYPE ncx PUBLIC "-//NISO//DTD ncx 2005-1//EN"
|\ "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd">
|\ <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/"
|\  version="2005-1" xml:lang="en-US">
|\ <head>
|\ <meta name="dtb:uid" content="BookId"/>
|\ <meta name="dtb:depth" content="2"/>
|\ <meta name="dtb:totalPageCount" content="0"/>
|\ <meta name="dtb:maxPageNumber" content="0"/>
|\ </head>
|\ <docTitle><text>Test1</text></docTitle>
|\ <docAuthor><text>me</text></docAuthor>
|\   <navMap>
|\     <navPoint class="toc" id="toc" playOrder="1">
|\       <navLabel>
|\         <text>Table of Contents</text>
|\       </navLabel>
|\       <content src="index.html"/>
|\     </navPoint>
|\ |-constant ncx-pre1
|\ 
|\ parse..| <navPoint class="chapter" id="|-constant ncx-chapter-pre1
|\ parse..| " playOrder="|-constant ncx-chapter-pre2
|\ parse..| ">
|\       <navLabel>
|\         <text>|-constant ncx-chapter-pre3
|\ parse..| </text>
|\       </navLabel>
|\       <content src="|-constant ncx-chapter-pre4
|\ parse..| "/>
|\     </navPoint>
|\ |-constant ncx-chapter-post
|\ 
|\ parse..|
|\   </navMap>
|\ </ncx>
|\ |-constant ncx-post
|;


|section: table of contents
|: toc
|\ atom" ~~~TOC" constant atom-toc
|\ atom" index.html" constant toc-filename

|\ parse..| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
|\ "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
|\ <html xmlns="http://www.w3.org/1999/xhtml">
|\ <head><title>Table of Contents</title></head>
|\ <body>
|\ <div>
|\  <h1><b>TABLE OF CONTENTS</b></h1>
|\ |-constant toc-pre
|\ 
|\ parse..|
|\ </div>
|\ </body>
|\ </html>
|\ |-constant toc-post
|\ 
|\ parse..| <h3><b><a href="|-constant toc-chapter-pre1
|\ parse..| ">|-constant toc-chapter-pre2
|\ parse..| </a></b></h3>
|\ |-constant toc-chapter-post
|;


|chapter: Weaving

|section: weaving details
|: weaving implementation
|\ : weave-chapter ( chapter -- ) dup chapter-text swap chapter-filename file! ;
|\ : weave-chapters
|\    chapters @ begin dup while dup weave-chapter ->next repeat drop ;
|\ 
|\ : weave-toc-chapter ( chapter -- )
|\    toc-chapter-pre1 doc+=$
|\    dup chapter-filename doc+=$
|\    toc-chapter-pre2 doc+=$
|\    chapter-name doc+=$
|\    toc-chapter-post doc+=$
|\ ;
|\ : weave-toc
|\    atom-toc documentation-chunk ! doc!
|\    toc-pre doc+=$
|\    chapters @ begin dup while dup weave-toc-chapter ->next repeat drop
|\    toc-post doc+=$
|\    documentation means toc-filename file!
|\ ;
|\ 
|\ : weave-ncx-chapter ( chapter -- )
|\    ncx-chapter-pre1 doc+=$
|\    dup chapter-filename doc+=$
|\    ncx-chapter-pre2 doc+=$
|\    dup chapter-filename doc+=$
|\    ncx-chapter-pre3 doc+=$
|\    dup chapter-name doc+=$
|\    ncx-chapter-pre4 doc+=$
|\    chapter-filename doc+=$
|\    ncx-chapter-post doc+=$
|\ ;
|\ : weave-ncx
|\    atom-ncx documentation-chunk ! doc!
|\    ncx-pre1 doc+=$
|\    chapters @ begin dup while dup weave-ncx-chapter ->next repeat drop
|\    ncx-post doc+=$
|\    documentation means ncx-filename file!
|\ ;
|\ 
|\ : weave-opf
|\    atom-opf documentation-chunk ! doc!
|\    opf-pre1 doc+=$
|\    chapters @ begin dup while dup chapter-filename opf-chapter ->next repeat drop
|\    opf-pre2 doc+=$
|\    chapters @ begin dup while dup chapter-filename opf-chapter' ->next repeat drop
|\    opf-post doc+=$
|\    documentation means opf-filename file!
|\ ;
|\ 
|\ 
|\ 
|\ : weave    weave-chapters weave-toc weave-opf weave-ncx ;
|;


|chapter: Tangling and Runnning

|section: tangling

|: tangle implementation
: tangle-file ( file -- ) cell+ @ dup means swap file! ;
: tangle   out-files @ begin dup while dup tangle-file ->next repeat drop ;
|;


|section: running

|: run implementation
atom" literate_running.tmp" constant run-filename
: run-cleanup   run-filename atom-string@ delete-file drop ;
: bye   run-cleanup bye ;
: run   atom" *" means run-filename file!
        run-filename atom-string@ included
        run-cleanup
;
|;


|chapter: Odds and Ends

|section: file writing
|: file writing implementation
: file! ( A A -- )
    atom-string@ w/o bin create-file 0= assert
    swap over >r atom-string@ r> write-file 0= assert
    close-file 0= assert
;
|;


|section: Chapters and Sections
|: chapters and sections
|\ variable chapter-count
|\ linked-list chapters
|\ : chapter-finish   chapter-post doc+=$ ;
|\ : |chapter:
|\     chapter-finish
|\     parse-cr chapters chain dup ,
|\     chapter-count @ ,   1 chapter-count +!
|\     dup documentation-chunk ! doc!
|\     chapter-pre1 doc+=$
|\     dup doc+=$
|\     chapter-pre2 doc+=$
|\     doc+=$
|\     chapter-pre3 doc+=$
|\     feed
|\ ;
|;

|: chapters and sections
|\ : |section:   parse-cr .d{ </p><h2>} doc+=$ .d{ </h2><p>} feed ;
|\ : |page   parse-cr .d{ </p><p style="page-break-before:always;">} feed ;
|;

|: chapters and sections
|\ : |{-   .d{ <ul><li>} feed ;
|\ : |--   .d{ </li><li>} feed ;
|\ : |-}   .d{ </li></ul>} feed ;
|;


|section: Global Fields

|: global fields
|\ variable title
|\ : |title:   parse-cr title once! feed ;
|;

|: global fields
|\ variable author
|\ : |author:   parse-cr author once! feed ;
|;


|section: output files

|: output files
|\ linked-list out-files
|\ : |file: ( add a new output file )
|\    parse-cr dup 1 out-files nchain
|\    .d{ <tt><i>} doc+=$ .d{ </i></tt>} feed ;
|;

|chapter: Slides

The follow are slides from an SVFIG presentation.

|page

|section: Literate Programming in Forth
Brad Nelson

|page

Motivations:
|{- Literate programming is cool
|-- Forth is cool
|-- ebooks are cool
|-}

|page

How:
|{- Use the Forth parser
|-- Generate MOBI files
   |{- table of contents
   |-- ncx file
   |-- opf file
   |-}
|-- Other cool stuff.
|-}

|page

The last slide.

|.
