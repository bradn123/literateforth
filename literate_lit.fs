s" literate.fs" included

|title: Literate Forth
|author: Brad Nelson
|subject: Literate Programming in Forth
|description: Literate programming implementation in Forth.
|date: 2012-02-23
|document-base: literate

|chapter: Overview
|section: Introduction

This document is a literate programming exposition of a Forth program
designed to allow literate programming directly in Forth.
|$
Literate programming is a technique, conceived of by Donald Knuth, in which the
documentation of a program is emphasised in precedence over the code that
implements it.
Rather than being linearly presented, code is interspersed inside documentation.
Prior to evaluation by the target language, a special pre-processor is used
to "tangle" the source code into a machine readable form.
|$
The Forth programming language has the relatively unique flexibility of
a dynamically re-definable parser. This allows the possibility of applying
literate programming techniques to Forth, without the need of external
pre-processors.
|$
eBook readers such as the Amazon Kindle are pleasant tools for reading
documentation.
Most literate programming tools such as WEB, CWEB, and noweb are
designed to emit |TeX  and |LaTeX  output, targeting printed output.
While these tools produce high quality printed output,
they produce eBooks which badly matched the limited feature set of eBook
formats.
Thus, the system presented emits documents in a format ready for processing
by the kindlegen MOBI document processor.
The Kindle's native format (MOBI) restricts documents to a minimalistic
format that emphasizes user text preferences over document designer layout.
While the Adobe PDF format is also supported, such documents are second class
citizens which hi-lite the wisdom of MOBI's restrictions, particularly on
eInk devices.


|section: Comment Conventions
When useful, Forth style stack effect comments
|tt{ ( xyz -- abc ) |}tt  will be used
to describe stack effects.
|$
The capital letter A will be used throughout indicate the "atomic string"
type (described later). (e.g. |tt{ ( A -- f )|}tt )
|$
The dollar sign $ will be used to indicate an address count pair
referencing a string.
So |tt{ ( -- $ )|}tt  will be used in place of |tt{ ( -- a n )|}tt .
|$
Other typical Forth stack effect abbreviations will be used.
|$

|{- f = flag
|-- a = address
|-- n = number (cell)
|-- A = atomic string
|-- $ = string in two element: address, length
|-}


|section: Generated Files

When generating runnable code (weaving),
|file: literate_out.fs
 is emitted. This file should typically be renamed to
literate.fs and included in other literate programs to active the syntax
described herein.
|$
It will contain a single file expansion of all the code described in this
document.
|: literate_out.fs
|@ *
|;


|section: Program Overview

This is the basic structure of the literate programming parser:
|: *
|@ isolate in wordlist
|@ assertion support
|@ utility words
|@ implement atoms
|@ pipe parsing
|@ escaping atoms
|@ chunks
|@ global fields
|@ tex and latex shortcuts
|@ output files
|@ file writing implementation
|@ chapters
|@ chapters and sections
|@ chapter structure
|@ primary program flow
|;



|chapter: Weaving, Tangling, and Running

|section: Modes of operation

There are three actions typically taken on literate programs:
weave, tangle, and running.
Weaving is the generation of documentation from a literate program.
Tangling is the generation of macro expanded source code from
a literate program.
Running is the act of tangling followed by evaluation of the tangled
output.
Most of the plumbing to handle these modes is executed on each run,
for simplicity and to ensure failures are detected early.
The pieces look like this:

|: primary program flow
|@ setup mode flags
|@ weaving implementation
|@ tangle implementation
|@ run implementation
|@ apply literate mode
|;


|section: Mode selection

We will need to decide which mode in which to operate.
For the moment we will use the value of the LITERATE
environment variable to select which mode.
|: setup mode flags
: literate-env ( -- $ ) s" LITERATE" getenv ;
: literate-mode ( $ -- )
    literate-env compare 0= constant ;
|;
|$

Running is selected by having LITERATE unset or empty.
Anything else is considered an error.
|: setup mode flags
s" weave" literate-mode weaving?
s" tangle" literate-mode tangling?
s" " literate-mode running?
|;

As a sanity check, we will insist we are in at least one mode.
|: setup mode flags
weaving? tangling? or running? or assert
|;


|section: Tangling

The process of tangling can generate one or more files depending on user input.
At the point we are doing final tangling, all filenames will have a
"meaning" associated with them that is their desired content.

|: tangle implementation
: tangle-file ( file -- )
    file-name@ dup means swap file! ;
|;

Each file is then iterated thru.

|: tangle implementation
: tangle   out-files @ begin dup while dup tangle-file ->next repeat drop ;
|;


|section: Running

Running involves tangling followed by evaluation.
Ideally, evaluation could happen in memory. Unfortunately,
ANSFORTH's EVALUATE word can only be used to fill in one "line"
in the input buffer. This precludes the use of multi-line parsing words
which are line aware (such as \). Since we would like to support Forth's
full syntax, we will instead output a temporary file and use INCLUDED.
|$
We will select a temporary filename based on the document base.
This can cause problems if multiple instances are running at once from the
same directory. However, pre-tangling can be used in this case.
|: run implementation
: run-filename ( -- A )
    doc-base @ atom" _running.tmp" atom+ ;
|;

After evaluation we will want to cleanup the temporary file.
|: run implementation
: run-cleanup
    run-filename atom-string@ delete-file drop ;
|;

We will override bye to attempt to make sure cleanup happens even
if the evaluated program exits early.
|: run implementation
: bye   run-cleanup bye ;
|;

When running, as there can be many tangled output files,
we adopt noweb's convention that the root for evaluation is
the chunk named "*".
|: run implementation
: run   atom" *" means run-filename file!
        run-filename atom-string@ included
        run-cleanup
;
|;

|section: Commence operation

|: apply literate mode
|\ : |. ( exit literate mode )
     chapter-finish
     weaving? if weave bye then
     tangling? if tangle bye then
     running? if run then ;
|;


|chapter: Foundations

|section: Assertions
We will often want to check if certain conditions are true,
halting if they are not.
|: assertion support
: assert ( n -- )
    0= if abort then ;
|;


|section: Linked lists

In several places in this program, singely linked lists are useful.
As we are interested primarily in inserting in elements at the end of a
list (or are indifferent as to the order). We will standardize on
a list root with this structure:
|{- pointer to the first element (head) of the list (0 on empty)
|-- pointer to the last element (tail) of the list (0 on empty)
|-}

We will need a word to create list roots in a variable:
|: utility words
: linked-list
    create 0 , 0 , ;
|;

In allocating memory for lists, we will assume sufficient memory is available.
|: utility words
: allocate' ( n -- a )
    allocate 0= assert ;
|;

We will also the allocated memory for simplicity.
|: utility words
: zero ( a n -- )
    0 fill ;
: allocate0 ( n -- a )
    dup allocate' swap 2dup zero drop ;
|;

|: utility words
: chain-new ( n -- a )
    1+ cells allocate0 ;
: chain-fillout ( .. a n -- a )
    0 do dup i 1+ cells + swap >r ! r> loop ;
: chain-link ( ..n -- a )
    dup chain-new swap chain-fillout ;
: chain-first ( ..n head[t] -- )
    >r chain-link r> 2dup ! cell+ ! ;
: chain-rest ( ..n head[t] -- )
    >r chain-link r> 2dup cell+ @ ! cell+ ! ;
: chain ( ..n head[t] -- )
    dup @ if chain-rest else chain-first then ;
|;

|: utility words
: ->next ( a -- a' ) @ ;
|;


|section: strings
We will need to clone strings occasionally.
|: utility words
: $clone ( $ - $ )
    dup allocate 0= assert swap 2dup >r >r move r> r> ;
|;

|section: stack maneuvers
We will also need to duplicate three items off the stack.
|: utility words
: 3dup ( xyz -- xyzxyz )
    >r 2dup r> dup >r swap >r swap r> r> ;
|;

|section: file writing
|: post atom utility words
: file! ( A A -- )
    atom-string@ w/o bin create-file 0= assert
    swap over >r atom-string@ r> write-file 0= assert
    close-file 0= assert
;
|;


|chapter: Atomic Strings

|section: Introduction
We will devise a number of words to implement so called "atomic strings".
This data type augments Forth's more machine level string handling with
something higher level. Hereafter atomic strings will simply be referred
to as atoms. The central properties of atoms are:
|{- occupy a single cell on the stack
|-- have identical numerical value when equal (for one program run)
|-- have a single associative "meaning"
|-}
The utility of atoms will become apparent given some examples.


|section: Using Atoms

Atoms with the same string are equal:
|: testing atoms
atom" foo" atom" foo" = assert
|;

Atoms with different strings are of course, not equal:
|: testing atoms
atom" bar" atom" foo" <> assert
|;

Atoms can be concatenated:
|: testing atoms
atom" testing" atom" 123" atom+ atom" testing123" = assert
|;

Atoms can have a meaning assigned to them using
atom+=$ (to append a literal string)
or atom+=ref (to append a reference to the meaning of another atom).
|: testing atoms
atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert
|;


|section: Structure of an Atom

Conveniently, because atoms have a single numerical value per string value,
we can implement meaning without the need for a lookup data structure.
Each atom's value will be the address of a structure:
|{- address of next atom (in the set of atoms)
|-- string length
|-- address of string start
|-- "meaning" head
|-- "meaning" tail
|-}

Some words to read these values are useful:
|: implement atoms
: atom-length@ ( A -- n )
    1 cells + @ ;
: atom-data@ ( A -- a )
    2 cells + @ ;
: atom-string@ ( A -- $ )
    dup atom-data@ swap atom-length@ ;
: atom-meaning-head ( A -- A[head] )
    3 cells + ;
|;

|$
Off of each atom's primary structure, a chain of "meaning" links.
When determining the "meaning" of an atom, the expansion of each
link in the chain is concatenated.
There are two types of link:
|{- raw strings (atom specifies the literal string)
|-- reference links (atom specifies another atom who's
    meaning should recursively be used)
|-}
|$
The format of the meaning links is:
|{- address of next link (in the meaning list)
|-- flag indicating if this is a reference (rather than a raw string)
|-- an atom (either raw string or a recursive reference)
|-}

|section: Implementing Atoms

A list of all atoms will be kept chained off |ttb{ atom-root |}ttb .
Whenever an atom is needed, this list should be consulted before a
new atoms is created (as an existing one may exist and
|b{ must |}b  be used).
|: implement atoms
linked-list atom-root
|;

We will create new unchained atoms either from a string that can safely
be assumed to persist:
|: implement atoms
: $atom-new ( $ -- A )
    >r >r 0 0 r> r> 4 atom-root chain atom-root cell+ @ ;
|;

Or from one that is transitory (parse region for example).
|: implement atoms
: atom-new ( $ -- A )
    $clone $atom-new ;
|;

Comparison for equality with a normal string is needed in order to seek
out a match from the existing pool of atoms.
|: implement atoms
: atom= ( $ A -- f )
    atom-string@ compare 0= ;
|;

We then need a way to look through all atoms for a match.

|: implement atoms
: atom-find' ( $ A -- A )
    begin
       dup 0= if nip nip exit then
       3dup atom= if nip nip exit then
       ->next
    again ;
: atom-find ( $ -- A )
    atom-root @ atom-find' ;
|;

Now we can implement two versions of atom lookup.
|ttb{ $atom |}ttb  for atoms based on persistent strings.
|: implement atoms
: $atom ( $ -- A )
    2dup atom-find dup if nip nip else drop $atom-new then ;
|;

And |ttb{ atom |}ttb  for atoms based on non-persistent strings.
|: implement atoms
: atom ( $ -- A )
    2dup atom-find dup if nip nip else drop atom-new then ;
|;

Printing an atom is provided (mainly for debugging).
|: implement atoms
: atom. ( A -- )
    atom-string@ type ;
|;

As is printing |b{ all |}b  atoms.
|: implement atoms
: atoms. ( -- )
    atom-root @ begin dup while dup atom. cr ->next repeat drop ;
|;


|: implement atoms
: atom" ( -- A )
    [char] " parse
    state @ if postpone sliteral postpone atom
    else atom then ; immediate
: atom"" ( -- A ) 0 0 atom ;
: atom{ ( -- A )
    [char] } parse
    state @ if postpone sliteral postpone atom
    else atom then ; immediate

: atom-append ( A n Ad -- )
    atom-meaning-head 2 swap chain ;
: atom+=$ ( A Ad -- )
    0 swap atom-append ;
: atom+=ref ( A Ad -- )
    1 swap atom-append ;


: ref-parts ( ref -- A ref? )
    cell+ dup cell+ @ swap @ ;
: atom-walk ( fn A -- )
     atom-meaning-head @ begin dup while
         2dup >r >r
         ref-parts if recurse else swap execute then
         r> r>
         ->next
     repeat 2drop ;
: tally-length ( n A -- n )
    atom-length@ + ;
: gather-string ( a A -- a' )
    2dup atom-string@ >r swap r> move tally-length ;
: atom-walk-length ( A -- n )
    0 swap ['] tally-length swap atom-walk ;
: atom-walk-gather ( a A -- )
    swap ['] gather-string swap atom-walk drop ;
: means ( A -- A' )
    dup atom-walk-length dup allocate 0= assert
    swap 2dup >r >r drop
    atom-walk-gather r> r> $atom ;


: atom>>$ ( A d -- d' )
    2dup >r atom-string@ r> swap move swap atom-length@ + ;
: atom+ ( A A -- A )
    swap 2dup atom-length@ swap atom-length@ + dup >r
    allocate 0= assert dup >r
    atom>>$ atom>>$ drop r> r> $atom ;
: atom-ch ( ch -- A )
    1 allocate 0= assert 2dup c! nip 1 atom ;
10 atom-ch constant atom-cr
: atom-cr+ ( A -- A )
    atom-cr atom+ ;

|@ testing atoms
|@ post atom utility words
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
: escape-each ( A -- )
    atom-string@ 0 ?do dup i + c@ escape-ch loop drop ;
: here! ( a -- )
    here - allot ;
: escape ( A -- A )
    here dup >r swap escape-each here over - atom r> here! ;
|;


|chapter: Parsing

|section: parsing pipe

|: testing parsing
\ Test parsing.
|\ : |halt! ;
|\ parse..| testing
|\ Hello there
|\ 123|halt!
atom" testing" atom-cr+
atom" Hello there" atom+ atom-cr+
atom" 123" atom+ = assert
|;


|: pipe parsing
: source@ source ( -- a )
    drop >in @ + ;
: source-remaining ( -- n )
   source nip >in @ - ;
|\ : drop| ( -- )
|\     source@ 1- c@ [char] | = if -1 >in +! then ;
: need-refill? ( -- f)
    source nip >in @ <= ;
|\ : on|? ( -- f )
|\     need-refill? if false exit then source@ c@ [char] | = ;
: replenish ( -- f )
    need-refill? if refill else true then ;
|\ : ?atom-cr+ ( A -- A )
|\     on|? 0= if atom-cr+ then ;
|\ : eat| ( -- )
|\     [char] | parse drop| atom atom+ ?atom-cr+ ;
: parse-cr ( -- A )
    source@ source-remaining atom   source nip >in ! ;
|\ : parse..| ( -- A )
|\     atom"" begin replenish 0=
|\     if exit then eat| on|? until ;
|\ : skip| ( -- )
|\     on|?  need-refill? 0= and if 1 >in +! then ;
|;


|chapter: Tags

|section: TeX and LaTeX

|: tex and latex shortcuts
|\ : |TeX .d{ <span>T<sub><big>E</big></sub>X</span>} feed ;
|;

|: tex and latex shortcuts
|\ : |LaTeX
    .d{ <span>L<sup><small>A</small></sup>T<sub><big>E</big></sub>X</span>}
    feed
;
|;

|: tex and latex shortcuts
|\ : |<-| .d{ &larr;} feed ;
|\ : |->| .d{ &rarr;} feed ;
|\ : |^| .d{ &uarr;} feed ;
|\ : |v| .d{ &darr;} feed ;
|;


|section: document chunks

|: chunks
atom" ~~~blackhole" constant blackhole
variable documentation-chunk
blackhole documentation-chunk !

: documentation ( -- A )
    documentation-chunk @ ;

variable chunk
: doc! ( back to documentation)
    documentation chunk ! ;
doc!
: doc? ( -- f)
    documentation chunk @ = ;
: chunk+=$ ( A -- )
    chunk @ atom+=$ ;
: chunk+=ref ( A -- )
    chunk @ atom+=ref ;
: doc+=$ ( A -- )
    documentation atom+=$ ;
: .d{ ( -- )
    postpone atom{ postpone doc+=$ ; immediate
|\ : .d| ( -- )
|\     parse..| ; immediate
|\ : |.d ( -- )
    postpone literal postpone doc+=$ ; immediate
: .dcr   atom-cr doc+=$ ;
: doc+=ref ( A -- )
    documentation atom+=ref ;
: ?doc+=$ ( A -- )
    doc? 0= if escape doc+=$ else drop then ;
: feed ( read into current chunk )
|\     parse..| dup ?atom-cr+ ?doc+=$ atom-cr+ chunk+=$ ;
: doc+=use
    ( A -- ) .d{ <u><b>} doc+=$ .d{ </b></u>} ;
: doc+=def ( A -- )
    .d{ </p><tt><u><b>} doc+=$
    .d{ </b></u> +&equiv;</tt><div class="chunk"><pre>} ;


|\ : |@ ( use a chunk )
    parse-cr dup chunk+=ref doc+=use .dcr feed ;
|\ : |: ( add to a chunk )
    parse-cr dup chunk ! doc+=def feed ;
|\ : |; ( documentation )
    doc? 0= if .d{ </pre></div><p>} then doc! feed ;
|\ : |$ ( paragraph )
    .d{ </p><p>} feed ;
|\ : |\ ( whole line)
    parse-cr atom-cr+ dup chunk+=$ ?doc+=$ feed ;
|;




|chapter: MOBI Format

|section: MOBI files

|: weaving implementation
|@ weaving toc
|@ weaving ncx
|@ weaving opf
|@ weaving chapter html
: weave ( -- )
    weave-chapters weave-toc weave-opf weave-ncx ;
|;

|section: OPF files

|: weaving opf
atom" ~~~OPF" constant atom-opf
: opf-filename ( -- A )
    doc-base @ atom" .opf" atom+ ;

: opf-chapter ( A -- )
    .d{ <item id="}
    dup doc+=$
    .d{ " media-type="application/xhtml+xml" href="}
    doc+=$
    .d{ "></item>} .dcr
;

: opf-chapter' ( A -- )
    .d{ <itemref idref="} doc+=$ .d{ "/>} .dcr
 ;

: weave-opf
    atom-opf documentation-chunk ! doc!

|\ .d| <?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" version="2.0"
unique-identifier="BookId">
<metadata xmlns:dc="http://purl.org/dc/elements/1.1/"
xmlns:opf="http://www.idpf.org/2007/opf">
|\ |.d
    .d{ <dc:title>} title @ doc+=$ .d{ </dc:title>} .dcr
    .d{ <dc:language>en-us</dc:language>} .dcr
    .d{ <dc:identifier id="BookId" opf:scheme="ISBN">}
    isbn @ doc+=$ .d{ </dc:identifier>} .dcr
    .d{ <dc:creator>} author @ doc+=$ .d{ </dc:creator>} .dcr
    .d{ <dc:publisher>} author @ doc+=$ .d{ </dc:publisher>} .dcr
    .d{ <dc:subject>} subject @ doc+=$ .d{ </dc:subject>} .dcr
    .d{ <dc:date>} doc-date @ doc+=$ .d{ </dc:date>} .dcr
    .d{ <dc:description>} description @ doc+=$ .d{ </dc:description>} .dcr
|\ .d|
</metadata>

<manifest>
   <item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml"
|\    href="|.d ncx-filename doc+=$ .d| "/>
|\   <item id="toc" media-type="application/xhtml+xml" href="|.d
|\   toc-filename doc+=$ .d| "></item>
|\ |.d

    chapters @ begin dup while
        dup chapter-filename opf-chapter ->next repeat drop

|\ .d|
</manifest>
<spine toc="My_Table_of_Contents">
  <itemref idref="toc"/>
|\ |.d

|\    chapters @ begin dup while dup chapter-filename opf-chapter' ->next repeat drop

|\ .d|
</spine>
<guide>
  <reference type="toc" title="Table of Contents"
|\    href="|.d toc-filename doc+=$ .d| "></reference>
</guide>
</package>
|\ |.d

   documentation means opf-filename file!
;

|;


|section: NCX files
|: weaving ncx
atom" ~~~NCX" constant atom-ncx
: ncx-filename ( -- A )
    doc-base @ atom" .ncx" atom+ ;

: weave-ncx-chapter ( chapter -- )
   .d{ <navPoint class="chapter" id="}
    dup chapter-filename doc+=$
    .d{ " playOrder="}
    dup chapter-filename doc+=$
    .d{ "><navLabel><text>}
    dup chapter-name doc+=$
    .d{ </text></navLabel><content src="}
    chapter-filename doc+=$
    .d{ "/></navPoint>}
;

: weave-ncx
    atom-ncx documentation-chunk ! doc!

|\ .d| <?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE ncx PUBLIC "-//NISO//DTD ncx 2005-1//EN"
"http://www.daisy.org/z3986/2005/ncx-2005-1.dtd">
<ncx xmlns="http://www.daisy.org/z3986/2005/ncx/"
 version="2005-1" xml:lang="en-US">
<head>
<meta name="dtb:uid" content="BookId"/>
<meta name="dtb:depth" content="2"/>
<meta name="dtb:totalPageCount" content="0"/>
<meta name="dtb:maxPageNumber" content="0"/>
</head>
|\ |.d
.d{ <docTitle><text>} title @ doc+=$
|\ .d| </text></docTitle>
<docAuthor><text>me</text></docAuthor>
  <navMap>
    <navPoint class="toc" id="toc" playOrder="1">
      <navLabel>
        <text>Table of Contents</text>
      </navLabel>
|\      <content src="|.d toc-filename doc+=$ .d| "/>
     </navPoint>
|\ |.d

    chapters @ begin dup while dup weave-ncx-chapter ->next repeat drop

    .d{ </navMap></ncx>}
    documentation means ncx-filename file!
;
|;


|section: table of contents
|: weaving toc
|\ atom" ~~~TOC" constant atom-toc
|\ : toc-filename doc-base @ atom" .html" atom+ ;

|\ : weave-toc-chapter ( chapter -- )
|\    .d{ <h4><b><a href="}
|\    dup chapter-filename doc+=$
|\    .d{ ">}
|\    chapter-name doc+=$
|\    .d{ </a></b></h4>} .dcr
|\ ;

|\ : weave-toc
|\    atom-toc documentation-chunk ! doc!

|\ .d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
|\ "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
|\ <html xmlns="http://www.w3.org/1999/xhtml">
|\ <head><title>Table of Contents</title></head>
|\ <body>
|\ <div>
|\  <h1><b>TABLE OF CONTENTS</b></h1>
|\ |.d

|\    chapters @ begin dup while dup weave-toc-chapter ->next repeat drop

|\ .d|
|\ </div>
|\ </body>
|\ </html>
|\ |.d

|\    documentation means toc-filename file!
|\ ;
|;


|section: Chapter HTML

|: weaving chapter html
: weave-chapter ( chapter -- )
    dup chapter-text swap chapter-filename file! ;
: weave-chapters
    chapters @ begin dup while dup weave-chapter ->next repeat drop ;
|;


|chapter: Odds and Ends

|section: isolate in wordlist
|: isolate in wordlist
vocabulary literate also literate definitions
|;


|chapter: Chapters

|section: Chapters and Sections

|: chapters and sections
variable slide-chapter
variable chapter-count
linked-list chapters
: chapter-finish   .d{ </p></div></body></html>} ;

: raw-chapter ( -- )
     chapter-finish
     parse-cr
     chapter-count @   1 chapter-count +!
     over 2 chapters chain
     dup documentation-chunk ! doc!

|\ .d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
|\ "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
|\ <html>
|\ <head>
|\ |.d

slide-chapter @ if
|@ slide show logic
then

|\ .d|
|\ <style type="text/css">
|\ div.chunk {
|\   margin: 0em 0.5em;
|\ }
|\ |.d

slide-chapter @ if
|\ .d|
|\ div.section {
|\   page-break-before: always;
|\ }
|\ |.d
then

|\ .d|
|\ pre {
|\   margin: 0em 0em;
|\ }
|\ </style>
|\ <title>|.d

    dup doc+=$
    .d{ </title></head>}
    slide-chapter @ if .d{ <body onload="Load()">} else .d{ <body>} then
    .d{ <div class="section"><h1>}
    doc+=$
    .d{ </h1><p>}

    feed
;

|\ : |chapter:   false slide-chapter !  raw-chapter ;
|\ : |slide-chapter:   true slide-chapter !  raw-chapter ;
|;

|: chapters and sections
|\ : |section:   parse-cr .d{ </p></div><div class="section"><h2>} doc+=$
                 .d{ </h2><p>} feed ;
|\ : |page   parse-cr .d{ </p><p style="page-break-before:always;">} feed ;
|;

|: chapters and sections
|\ variable bullet-depth
|\ : bullet+   1 bullet-depth +!   bullet-depth @ 1 = if .d{ </p>} then ;
|\ : bullet-   -1 bullet-depth +!   bullet-depth @ 0 = if .d{ <p>} then ;
|\ : |{-   bullet+ .d{ <ul><li>} feed ;
|\ : |--   .d{ </li><li>} feed ;
|\ : |-}   .d{ </li></ul>} bullet- feed ;

|\ : |b{   .d{ <b>} feed ;
|\ : |}b   .d{ </b>} feed ;
|\ : |i{   .d{ <i>} feed ;
|\ : |}i   .d{ </i>} feed ;
|\ : |u{   .d{ <u>} feed ;
|\ : |}u   .d{ </u} feed ;
|\ : |tt{   .d{ <tt>} feed ;
|\ : |}tt   .d{ </tt>} feed ;
|\ : |ttb{   .d{ <tt><b>} feed ;
|\ : |}ttb   .d{ </b></tt>} feed ;
|\ : |sup{   .d{ <sup>} feed ;
|\ : |}sup   .d{ </sup>} feed ;
|\ : |sub{   .d{ <sub>} feed ;
|\ : |}sub   .d{ </sub>} feed ;
|;

|: slide show logic
|\ .d|
<script type="text/javascript">
function SlideCount() {
  var sections = document.getElementsByClassName('section');
  return sections.length;
}

function ShowSlide(index) {
  var sections = document.getElementsByClassName('section');
  for (var i = 0; i < sections.length; i++) {
    sections[i].style.display = ((i == index) ? 'inline' : 'none');
  }
}

var current_slide = 0;

function Load() {
  ShowSlide(0);
  window.onkeydown = function(e) {
    if (e.keyCode == 37) {  // left
      current_slide = Math.max(0, current_slide - 1);
    } else if (e.keyCode == 39) {  // right
      current_slide = Math.min(SlideCount() - 1, current_slide + 1);
    } else if (e.keyCode == 38) {  // up
      current_slide = 0;
    } else if (e.keyCode == 40) {  // down
      current_slide = SlideCount() - 1;
    }
    ShowSlide(current_slide);
  };
}

</script>
|\ |.d
|;

|section: chapter handling
|: chapters and sections
: chapter-name ( chp -- A )
    cell+ @ ;
: chapter-text ( chp -- A )
    cell+ @ means ;
: chapter-number ( chp -- n )
    2 cells + @ ;
atom" .html" constant .html
: chapter-filename ( chp -- A )
     chapter-number s>d <# # # # #s #> atom
     doc-base @ atom" _" atom+ swap .html atom+ atom+ ;
|;




|section: Global Fields

|: global fields
variable doc-base
atom" index" doc-base !
|\ : |document-base:   parse-cr doc-base ! feed ;
|;

|: global fields
variable title
atom" Untitled" title !
|\ : |title:   parse-cr title ! feed ;
|;

|: global fields
variable author
atom" Anonymous" author !
|\ : |author:   parse-cr author ! feed ;
|;

|: global fields
variable isbn
atom" 9999999999" isbn !
|\ : |isbn:   parse-cr isbn ! feed ;
|;

|: global fields
variable subject
atom" Article" subject !
|\ : |subject:   parse-cr subject ! feed ;
|;

|: global fields
variable doc-date
atom" Unknown" doc-date !
|\ : |date:   parse-cr doc-date ! feed ;
|;

|: global fields
variable description
atom" No description available." description !
|\ : |description:   parse-cr description ! feed ;
|;


|section: output files

|: output files
|\ linked-list out-files
|\ : |file: ( add a new output file )
|\    parse-cr dup 1 out-files chain
|\    .d{ <tt><i>} doc+=$ .d{ </i></tt>} feed ;
: file-name@ ( file -- A )
    cell+ @ ;
|;

|slide-chapter: Appendix A - Slides

The follow are slides from an SVFIG presentation on
February 25, 2012.
|$
On ebooks readers, browse normally.
On full browsers, |<-|  and |->|  move through the slides,
|^|  and |v|  jump to the begining and end.


|section: Literate Programming in Forth
Brad Nelson


|section: Motivations
|{- Literate programming is cool
|-- Forth is cool
|-- ebooks are cool
|-}


|section: Approach
|{- Use the Forth parser
|-- Generate MOBI files
   |{- table of contents
   |-- ncx file
   |-- opf file
   |-}
|-- Other cool stuff.
|-}

|section: Questions?
Questions?

|.
