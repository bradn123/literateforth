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
|@ data structures and tools
|@ user facing tags
|@ primary program flow
|;


|section: Tags
A variety of user facing formatting tags are provided.
|: user facing tags
|@ chapters
|@ chapter structure
|@ formatting tags
|@ bullet lists
|@ tex and latex shortcuts
|@ arrow symbols
|@ chunk tags
|;


|section: Data structures and Tools
The data structures and tools needed to put this together are detailed.
|: data structures and tools
|@ assertion support
|@ utility words
|@ implement atoms
|@ parsing
|@ chunks
|@ global fields
|@ output files
|@ document boundaries
|;


|chapter: Tags

|section: Overview
Tags are the user facing set of words provided by this tool.
|\ All tags are preceded by the pipe (|) character.
They are cataloged herein.

|section: Global Fields
Most documents will begin with a number of document wide field
tags.
|$

The document base is the prefix put in front of each output file in weaving
mode.
|: global fields
variable doc-base
atom" index" doc-base !
|\ : |document-base:   parse-cr doc-base ! feed ;
|;

The document has a title.
|: global fields
variable title
atom" Untitled" title !
|\ : |title:   parse-cr title ! feed ;
|;

And an author (used for the publisher for now too).
|: global fields
variable author
atom" Anonymous" author !
|\ : |author:   parse-cr author ! feed ;
|;

A frivolous ISBN number
|: global fields
variable isbn
atom" 9999999999" isbn !
|\ : |isbn:   parse-cr isbn ! feed ;
|;

A subject.
|: global fields
variable subject
atom" Article" subject !
|\ : |subject:   parse-cr subject ! feed ;
|;

A date of authorship.
|: global fields
variable doc-date
atom" Unknown" doc-date !
|\ : |date:   parse-cr doc-date ! feed ;
|;

And a description.
|: global fields
variable description
atom" No description available." description !
|\ : |description:   parse-cr description ! feed ;
|;

|section: Formatting Tags

Tags are provided for bold, italics, underline, fixed width (teletype),
superscript, and subscript.
|: formatting tags
|\ : |b{   .d{ <b>} feed ;
|\ : |}b   .d{ </b>} feed ;
|\ : |i{   .d{ <i>} feed ;
|\ : |}i   .d{ </i>} feed ;
|\ : |u{   .d{ <u>} feed ;
|\ : |}u   .d{ </u} feed ;
|\ : |tt{   .d{ <tt>} feed ;
|\ : |}tt   .d{ </tt>} feed ;
|\ : |sup{   .d{ <sup>} feed ;
|\ : |}sup   .d{ </sup>} feed ;
|\ : |sub{   .d{ <sub>} feed ;
|\ : |}sub   .d{ </sub>} feed ;
|;


|section: Bullet Lists

Tags are provided for bullet lists.
|: bullet lists
variable bullet-depth
: bullet+   1 bullet-depth +!   bullet-depth @ 1 = if .d{ </p>} then ;
: bullet-   -1 bullet-depth +!   bullet-depth @ 0 = if .d{ <p>} then ;
|\ : |{-   bullet+ .d{ <ul><li>} feed ;
|\ : |--   .d{ </li><li>} feed ;
|\ : |-}   .d{ </li></ul>} bullet- feed ;
|;


|section: TeX and LaTeX

As |TeX  and |LaTeX  are widely referenced in material related to
literate programming, we will want to be able to mention them in
way that has some semblance of typographical accuracy.
Unfortunately, precise duplication would require images
(which don't scale).
|$
Use of subscript and big text gives use this for |TeX :
|: tex and latex shortcuts
|\ : |TeX .d{ <span>T<sub><big>E</big></sub>X</span>} feed ;
|;
|$
Adding in small text and superscript then brings us to this for |LaTeX :
|: tex and latex shortcuts
|\ : |LaTeX
    .d{ <span>L<sup><small>A</small></sup>T<sub><big>E</big></sub>X</span>}
    feed
;
|;


|section: Symbols

We provide some tags for arrow symbols.
|: arrow symbols
|\ : |<-| .d{ &larr;} feed ;
|\ : |->| .d{ &rarr;} feed ;
|\ : |^| .d{ &uarr;} feed ;
|\ : |v| .d{ &darr;} feed ;
|;



|section: Document Boundaries
Documents need logical division markers.
|$
We provide tags for regular and slide show chapters.
|: document boundaries
|@ chapter implementation
|\ : |chapter:   false slide-chapter !  raw-chapter ;
|\ : |slide-chapter:   true slide-chapter !  raw-chapter ;
|;

Sections.
|: document boundaries
|\ : |section:   parse-cr .d{ </p></div><div class="section"><h2>} doc+=$
                 .d{ </h2><p>} feed ;
|;

Page breaks.
|: document boundaries
|\ : |page   parse-cr .d{ </p><p style="page-break-before:always;">} feed ;
|;

Paragraph boundaries.
|: document boundaries
|\ : |$ ( paragraph )
    .d{ </p><p>} feed ;
|;

|\ And pipe (|) escaping whole line.
|: document boundaries
|\ : |\ ( whole line)
    parse-cr atom-cr+ dup chunk+=$ escape doc+=$ feed ;
|;


|section: Chunk Tags

Crucial to literate programming.
Tags are provided to manipulate chunks.
|$
Adding to their definition (in a Forth-y way).
|: chunk tags
|\ : |: ( add to a chunk )
    parse-cr dup chunk ! doc+=def feed ;
|\ : |; ( documentation )
    .d{ </pre></div><p>} doc! feed ;
|;

Using them (in a Forth-y way).
|: chunk tags
|\ : |@ ( use a chunk )
    parse-cr dup chunk+=ref doc+=use .dcr feed ;
|;


|section: Output Files

Tangled output can be directed to multiple files using a special tag.
A linked list of output files is kept.
Their "meaning" is consulted to know what to emit.
|: output files
|\ linked-list out-files
|\ : |file: ( add a new output file )
    parse-cr dup 1 out-files chain
    .d{ <tt><i>} doc+=$ .d{ </i></tt>} feed ;
: file-name@ ( file -- A )
    cell+ @ ;
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
: tangle
    out-files @ begin dup while
    dup tangle-file ->next repeat drop ;
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
: run
    atom" *" means run-filename file!
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


|section: Linked Lists

In several places in this program, singly linked lists are useful.
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

Support adding a new link to the end of a chain.
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

And walking down the list.
|: utility words
: ->next ( a -- a' ) @ ;
|;


|section: Ordinary Strings
We will need to clone strings occasionally.
|: utility words
: $clone ( $ - $ )
    dup allocate 0= assert swap 2dup >r >r move r> r> ;
|;


|section: Stack Maneuvers
We will also need to duplicate three items off the stack.
|: utility words
: 3dup ( xyz -- xyzxyz )
    dup 2over rot ;
|;

|section: File Writing
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
|tt{ atom+=$|}tt  (to append a literal string)
or |tt{ atom+=ref|}tt  (to append a reference to the meaning of another atom).
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

A list of all atoms will be kept chained off |tt{ atom-root |}tt .
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
|tt{ $atom |}tt  for atoms based on persistent strings.
|: implement atoms
: $atom ( $ -- A )
    2dup atom-find dup if nip nip else drop $atom-new then ;
|;

And |tt{ atom |}tt  for atoms based on non-persistent strings.
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
    atom-root @ begin dup while
    dup atom. cr ->next repeat drop ;
|;

We provide two different stringing words for atoms.
One based on quotes, the other braces.
|: implement atoms
: atom" ( -- A )
    [char] " parse
    state @ if postpone sliteral postpone atom
    else atom then ; immediate
: atom{ ( -- A )
    [char] } parse
    state @ if postpone sliteral postpone atom
    else atom then ; immediate
|;

As well as a word for an empty atom.
|: implement atoms
: atom"" ( -- A ) 0 0 atom ;
|;

While atoms are fixed, once created,
their "meanings" can be accumulated gradually.
The two words for this are |tt{ atom+=$|}tt  and
|tt{ atom+=ref|}tt .
|: implement atoms
: atom-append ( A n Ad -- )
    atom-meaning-head 2 swap chain ;
: atom+=$ ( A Ad -- )
    0 swap atom-append ;
: atom+=ref ( A Ad -- )
    1 swap atom-append ;
|;

We then provide a way to extract the "meaning" of an atom.
|: implement atoms
|@ implement means tools
: means ( A -- A' )
    dup atom-walk-length dup allocate 0= assert
    swap 2dup >r >r drop
    atom-walk-gather r> r> $atom ;
|;

Using this plumbing.
|: implement means tools
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
|;

We provide atom concatenation.
|: implement atoms
: atom>>$ ( A d -- d' )
    2dup >r atom-string@ r> swap move swap atom-length@ + ;
: atom+ ( A A -- A )
    swap 2dup atom-length@ swap atom-length@ + dup >r
    allocate 0= assert dup >r
    atom>>$ atom>>$ drop r> r> $atom ;
|;

And a way to get an atom from one character.
|: implement atoms
: atom-ch ( ch -- A )
    1 allocate 0= assert 2dup c! nip 1 atom ;
|;

This allows us to add a shorthand for carriage returns
and concatenation of carriage returns.
|: implement atoms
10 atom-ch constant atom-cr
: atom-cr+ ( A -- A )
    atom-cr atom+ ;
|;

We can then apply the tests above.
|: implement atoms
|@ testing atoms
|;

And some words that depend on atoms.
|: implement atoms
|@ post atom utility words
|;

|section: HTML Escaping

A critical feature is to be able to html escape an atom.
We convert the following:
|{- < |->|  &lt;
|-- > |->|  &gt;
|-- " |->|  &quot;
|-- & |->|  &amp;
|-}
|: implement atoms
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

|section: Parsing Pipe

|\ As we will use the pipe (|) character as a divider,
we will need tools to parse around it.
Something like this:
|: testing parsing
|\ : |halt! ;
|\ parse..| testing
|\ Hello there
|\ 123|halt!
atom" testing" atom-cr+
atom" Hello there" atom+ atom-cr+
atom" 123" atom+ = assert
|;

|section: Implement Parsing Pipe

We'll need to manipulate the input buffer.
|: parsing
: source@ source ( -- a )
    drop >in @ + ;
: source-remaining ( -- n )
   source nip >in @ - ;
|;

Then parse through multiple lines until (but not including),
the next pipe character.
|: parsing
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
|\ : parse..| ( -- A )
|\     atom"" begin replenish 0=
|\     if exit then eat| on|? until ;
|;

We'll also have some words that grab input until the end of the line.
|: parsing
: parse-cr ( -- A )
    source@ source-remaining atom   source nip >in ! ;
|;


|chapter: Chunks

|section: Implementing Chunks
As text is parsed it is accumulated into chunks.
|: chunks
variable chunk
: chunk+=$ ( A -- )
    chunk @ dup if atom+=$ else drop then ;
: chunk+=ref ( A -- )
    chunk @ dup if atom+=ref else drop then ;
|;

A special primary chunk is kept for the main document.
|: chunks
atom" ~~~DOC" constant main-documentation
variable documentation-chunk
main-documentation documentation-chunk !
|;

A number of words are provided to write to the current
documentation chunk or to set it.
|: chunks
: documentation ( -- A )
    documentation-chunk @ ;
: doc! ( back to documentation)
    0 chunk ! ;
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
: doc+=use
    ( A -- ) .d{ <b>} doc+=$ .d{ </b>} ;
: doc+=def ( A -- )
    .d{ </p><tt><b>} doc+=$
    .d{ </b> +&equiv;</tt><div class="chunk"><pre>} ;
|;

This is critically use by nearly every tag to parse until the next tag
using the word |tt{ feed |}tt .
|: chunks
: feed ( read into current chunk )
|\     parse..| dup ?atom-cr+ escape doc+=$ atom-cr+ chunk+=$ ;
|;





|chapter: MOBI Format

|section: Mobipocket file format

The Mobipocket format (.mobi) files is a common format for eBook readers.
In particular, it is the primary native format for Amazon's Kindle.
Amazon uses a variant of the format with DRM (Digital Rights Management)
features added. Amazon provides a tool called kindlegen which converts
a human readable set of files into a single .mobi file.
The inputs consist of:

|{- an .opf file (an xml manifest listing all the other files)
|-- an .ncx file (an xml index file listing document divisions)
|-- an XHTML table of contents
|-- one or more XHTML files, each containing a chapter of the book
|-}

Thus the process of weaving to the MOBI format looks like this:
|: weaving implementation
|@ weaving toc
|@ weaving ncx
|@ weaving cover
|@ weaving opf
|@ weaving chapter xhtml
: weave ( -- )
    weave-opf
    weave-ncx
    weave-cover
    weave-toc
    weave-chapters
;
|;

|section: OPF files

The OPF file provided to kindlegen is the primary input file.
In fact, it is the file listed as an argument when running kindlegen
from the command line.
|$
We will assume a single OPF file which will be generated into the
"meaning" of a reserved atom.
|: weaving opf
atom" ~~~OPF" constant atom-opf
|;

We will append .opf to the document base name to select the output file.
|: weaving opf
: opf-filename ( -- A )
    doc-base @ atom" .opf" atom+ ;
|;

Weaving the opf file involves changing the focus
chunk to the opf file.
|: weaving opf
|@ weaving opf manifest chapters
|@ weaving opf chapter itemref
: weave-opf
    atom-opf documentation-chunk ! doc!
|;

Emitting the opf header.
|: weaving opf
|\ .d| <?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" version="2.0"
unique-identifier="BookId">
<metadata xmlns:dc="http://purl.org/dc/elements/1.1/"
xmlns:opf="http://www.idpf.org/2007/opf">
|\ |.d
|;

Add in metadata fields about the document in general like:
title, isbn, author, subject, date, and description.
|: weaving opf
    .d{ <dc:title>} title @ doc+=$ .d{ </dc:title>} .dcr
    .d{ <dc:language>en-us</dc:language>} .dcr
    .d{ <meta name="cover" content="My_Cover"/> } .dcr
    .d{ <dc:identifier id="BookId" opf:scheme="ISBN">}
    isbn @ doc+=$ .d{ </dc:identifier>} .dcr
    .d{ <dc:creator>} author @ doc+=$ .d{ </dc:creator>} .dcr
    .d{ <dc:publisher>} author @ doc+=$ .d{ </dc:publisher>} .dcr
    .d{ <dc:subject>} subject @ doc+=$ .d{ </dc:subject>} .dcr
    .d{ <dc:date>} doc-date @ doc+=$ .d{ </dc:date>} .dcr
    .d{ <dc:description>} description @ doc+=$ .d{ </dc:description>} .dcr
|\ .d|
</metadata>
|;

Then add in a table of contents listing all the files in the book,
including table of contents and chapters.
|: weaving opf
<manifest>
   <item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml"
|\    href="|.d ncx-filename doc+=$ .d| "/>
|\   <item id="toc" media-type="application/xhtml+xml" href="|.d
    toc-filename doc+=$ .d{ "></item>}
    chapters @ begin dup while
        dup chapter-filename opf-chapter ->next
    repeat drop
    .d{ <item id="My_Cover" media-type="image/gif"} .dcr
    .d{  href="} cover-filename doc+=$ .d{ "/>} .dcr
    .d{ </manifest>}
|;

One entry per chapter.
|: weaving opf manifest chapters
: opf-chapter ( A -- )
    .d{ <item id="}
    dup doc+=$
    .d{ " media-type="application/xhtml+xml" href="}
    doc+=$
    .d{ "></item>} .dcr
;
|;

Then list each chapter and TOC again for the spine.
|: weaving opf
    .d{ <spine toc="My_Table_of_Contents"><itemref idref="toc"/>}
    chapters @ begin dup while
        dup chapter-filename opf-chapter' ->next
    repeat drop
   .d{ </spine>}
|;

Each itemref in the spine looks like this.
|: weaving opf chapter itemref
: opf-chapter' ( A -- )
    .d{ <itemref idref="} doc+=$ .d{ "/>} .dcr ;
|;

Finally the guide can just consist of the table of contents.
|: weaving opf
|\ .d|
<guide>
  <reference type="toc" title="Table of Contents"
|\    href="|.d toc-filename doc+=$ .d| "></reference>
</guide>
</package>
|\ |.d
|;

Then write out the file.
|: weaving opf
   documentation means opf-filename file!
;
|;


|section: NCX files

The NCX file relists each chapter to select the navigation points in
the document.
|$
As with the OPF, accumulate into the "meaning" of a reserved atom.
|: weaving ncx
atom" ~~~NCX" constant atom-ncx
|;

Output to the document base with .ncx appended.
|: weaving ncx
: ncx-filename ( -- A )
    doc-base @ atom" .ncx" atom+ ;
|;

We then can write to the reserved atom.
|: weaving ncx
|@ weaving ncx chapter
: weave-ncx
    atom-ncx documentation-chunk ! doc!
|;

Writing out the ncx header.
|: weaving ncx
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
|;

Including the a few fields like title and author.
|: weaving ncx
|\ |.d
.d{ <docTitle><text>} title @ doc+=$
|\ .d| </text></docTitle>
<docAuthor><text>me</text></docAuthor>
|;

Then the main navmap.
|: weaving ncx
  <navMap>
    <navPoint class="toc" id="toc" playOrder="1">
      <navLabel>
        <text>Table of Contents</text>
      </navLabel>
|;

Add in the table of contents.
|: weaving ncx
|\      <content src="|.d toc-filename doc+=$ .d| "/>
     </navPoint>
|\ |.d
|;

And each chapter.
|: weaving ncx
    chapters @ begin dup while
    dup weave-ncx-chapter ->next repeat drop
|;

A chapter looks like this.
|: weaving ncx chapter
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
|;

Then close out the file and write it.
|: weaving ncx
    .d{ </navMap></ncx>}
    documentation means ncx-filename file!
;
|;


|section: table of contents

The table of contents is an XHTML file like the chapters.
XHTML is like HTML but strictly XML like in format.
We use a subset that is constrained by MOBI's limitations.
|$
We will accumulate the table of contents to a reserved atom.
|: weaving toc
atom" ~~~TOC" constant atom-toc
|;

And write this to a filename based on the document base with
the .html extension added.
|: weaving toc
: toc-filename doc-base @ atom" .html" atom+ ;
|;

We change the focus chunk to the TOC.
|: weaving toc
|@ weaving toc chapter
: weave-toc
    atom-toc documentation-chunk ! doc!
|;

Then write out the header for the TOC.
|: weaving toc
|\ .d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head><title>Table of Contents</title></head>
<body>
<div>
  <h1><b>TABLE OF CONTENTS</b></h1>
|\ |.d
|;

Then write out each chapter.
|: weaving toc
    chapters @ begin dup while
    dup weave-toc-chapter ->next repeat drop
|;

Where a chapter looks like this.
|: weaving toc chapter
: weave-toc-chapter ( chapter -- )
    .d{ <h4><b><a href="}
    dup chapter-filename doc+=$
    .d{ ">}
    chapter-name doc+=$
    .d{ </a></b></h4>} .dcr
 ;
|;

Then close out the TOC and write it out.
|: weaving toc
    .d{ </div></body></html>} .dcr

    documentation means toc-filename file!
;
|;


|section: Chapter HTML

Each chapter is accumulated into a link list of chapters.
|: chapter implementation
variable slide-chapter
variable chapter-count
linked-list chapters
|;

Accessors for chapters are provided.
|: chapter implementation
: chapter-name ( chp -- A )
    cell+ @ ;
: chapter-text ( chp -- A )
    cell+ @ means ;
: chapter-number ( chp -- n )
    2 cells + @ ;
|;

Chapters are output to the base document name, followed by an
underscore, then a zero extended number, and .html at the end.
|: chapter implementation
atom" .html" constant .html
: chapter-filename ( chp -- A )
     chapter-number s>d <# # # # #s #> atom
     doc-base @ atom" _" atom+ swap .html atom+ atom+ ;
|;

A raw chapter can be either normal or for slides.
It is added to the list of chapters.
|: chapter implementation
|@ chapter implementation finish
: raw-chapter ( -- )
     chapter-finish
     parse-cr
     chapter-count @   1 chapter-count +!
     over 2 chapters chain
     dup documentation-chunk ! doc!
|;

Then a the xhtml header is written.
|: chapter implementation
|\ .d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
 "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html>
<head>
|\ |.d
|;

Then potentially slide show Javascript.
|: chapter implementation
slide-chapter @ if
|@ slide show logic
then
|;

Then some CSS.
|: chapter implementation
|\ .d|
<style type="text/css">
  div.chunk {
    margin: 0em 0.5em;
  }
  pre {
    margin: 0em 0em;
  }
|\ |.d
|;

Potentially with a page break for slide shows.
|: chapter implementation
slide-chapter @ if
|\ .d|
  div.section {
    page-break-before: always;
  }
|\ |.d
then
|;

Finally chapter headings.
|: chapter implementation
|\ .d|
</style>
|\ <title>|.d
    dup doc+=$
    .d{ </title></head>}
    slide-chapter @ if .d{ <body onload="Load()">} else .d{ <body>} then
    .d{ <div class="section"><h1>}
    doc+=$
    .d{ </h1><p>}

    feed
;
|;

Each chapter also has a short footer.
|: chapter implementation finish
: chapter-finish   .d{ </p></div></body></html>} ;
|;

This allows us to construct each chapter.
|: weaving chapter xhtml
: weave-chapter ( chapter -- )
    dup chapter-text swap chapter-filename file! ;
: weave-chapters
    chapters @ begin dup while
    dup weave-chapter ->next repeat drop ;
|;


|chapter: Odds and Ends

|section: isolate in wordlist
We want to be able apply the literate programming tool from inside itself.
To that end, we need to isolate it from the base vocabulary.
|: isolate in wordlist
vocabulary literate also literate definitions
|;


|chapter: Slide Show
We would like to be able to support a slide show for some chapters.
On eBook readers this is handled by marking each section being preceded
with a page break.
For desktop browsers, we add a small amount of Javascript to selectively
hide each section <div> tag.
|$
We need to be able to count the number of slides.
|: slide show logic
|\ .d|
<script type="text/javascript">
function SlideCount() {
  var sections = document.getElementsByClassName('section');
  return sections.length;
}
|;

Move to a slide.
|: slide show logic
function ShowSlide(index) {
  var sections = document.getElementsByClassName('section');
  for (var i = 0; i < sections.length; i++) {
    sections[i].style.display = ((i == index) ? 'inline' : 'none');
  }
}
|;

Track the current slide.
|: slide show logic
var current_slide = 0;
|;

Then start the show on load and intercept the arrow keys
to control the show.
|: slide show logic
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


|chapter: Images

|section: Colors

In order to represent images, we'll need to handle colors.
We keep a 0-255 value for the red, green, and blue component of the current
color.

|: implement colors
variable red
variable green
variable blue
|;

We'll often use it to setup colors specified as a triple.
Often this triple will be floating point in the 0-1 range.
|: implement colors
: rgb ( r g b -- ) blue ! green ! red ! ;
: f>primary ( f -- n ) 255e f* f>s 0 max 255 min ;
: rgbf ( rf gf bf -- ) f>primary f>primary f>primary rgb ;
|;

We can then define some important common colors.
|: implement colors
: black ( -- ) 0 0 0 rgb ;
: white ( -- ) 255 255 255 rgb ;
|;

And some common color sets.
|: implement colors
: gray ( n -- ) dup dup rgb ;
|;


|section: Images

We will need to implement images.
For now, a single global image will suffice.
We'll store it in 4 byte per pixel form (with the alpha component unused for
now). These fields will be needed.

|: implement images
variable image-width
variable image-height
variable image-data
|;

In manipulating images we'll want to be able to reference the total size of
the image data.
|: implement images
: image-data-size ( -- n )
    image-width @ image-height @ * 4 * ;
|;

We will (re-)allocate image data on setup.
|: implement images
: image-pick-size ( w h -- )
    image-height ! image-width ! ;
: image-free-old
    image-data @ dup if free 0= assert else drop then ;
: image-allocate
    image-data-size allocate 0= assert image-data ! ;
: image-clear
    image-data @ image-data-size 0 fill ;
: image-setup ( w h -- )
    image-pick-size image-free-old image-allocate image-clear ;
|;


The most basic operation will be to plot to a given (x, y) coordinate
with the current color.
|: implement images
|@ implement colors
: image-xy ( x y -- a )
    image-width @ * + 4 *
    image-data @ + ;
: plot ( x y -- )
    image-xy
    red @ over c!
    green @ over 1+ c!
    blue @ over 2 + c!
    0 swap 3 + c! ; 
|;

|section: Writing BMPs.

We will want to output the current image to a windows BMP file.
We will assume we're only writing one BMP at time and keep a global to its
file handle.

|: writing bmp files
variable bmp-file
|;

Opening and closing the file based on an atom name will be useful.
|: writing bmp files
: bmp-begin ( A -- )
    atom-string@ w/o bin create-file 0= assert bmp-file ! ;
: bmp-end ( -- )
    bmp-file @ close-file 0= assert ;
|;

We will halt on failures to write, so we should centralize that.
|: writing bmp files
: bmp-write ( $ -- )
    bmp-file @ write-file 0= assert ;
|;

Additionally writing out little endian bytes, words, and double words will be
needed.

|: writing bmp files
: bmp-byte ( b -- ) here c! here 1 bmp-write ;
: bmp-word ( w -- ) dup 255 and bmp-byte 8 rshift 255 and bmp-byte ;
: bmp-dword ( d -- ) dup 65535 and bmp-word 16 rshift 65535 and bmp-word ;
|;

BMP files have two headers, the main BMP header and a DIB (device
independent header. We'll need to talk about the size of them.
|: writing bmp files
3 2 * 2 4 * + constant bmp-header-size
10 4 * constant dib-header-size
|;

We can then implement writing the bmp file.
Starting with the BMP header.
|: writing bmp files
: bmp-save ( A -- )
  bmp-begin
  \ BMP header
  s" BM" bmp-write
  bmp-header-size
  dib-header-size +
  image-data-size + bmp-dword \ size of bmp file in bytes
  0 bmp-word \ unused
  0 bmp-word \ unused
  bmp-header-size
  dib-header-size + bmp-dword \ offset to start of bitmap image data
|;

Then the DIB header.
|: writing bmp files
  \ DIB header
  dib-header-size bmp-dword \ size of header in bytes
  image-width @ bmp-dword \ width
  image-height @ bmp-dword \ height
  1 bmp-word \ color planes
  32 bmp-word \ bits per pixel
  0 bmp-dword \ BI_RGB (uncompressed)
  image-data-size bmp-dword \ pixel data size
  0 bmp-dword \ horizontal pixels per meter
  0 bmp-dword \ vertical pixels per meter
  0 bmp-dword \ colors in color palette
  0 bmp-dword \ important colors in palette
|;

Then the image data and done.
|: writing bmp files
  \ Image data
  image-data @ image-data-size bmp-write
  bmp-end
;
|;


|chapter: Book Cover

|section: Introduction
We will want to produce interesting images for the book cover.
To this end we will use "Forth Haiku" which produce images with
small snippets of Forth code.


|section: Forth Haiku

Forth Haiku are described in terms of a current coordinate.
We will call them x and y for simplicity.
The Haiku cannot mutate the coordinates, so we'll provide accessors.

|: implement haiku
fvariable xx
fvariable yy
: x ( -- f ) xx f@ ;
: y ( -- f ) yy f@ ;
|;

We will implement haiku by calling a per-pixel execution token for each pixel
in the current image.
|: implement haiku
: haiku ( f -- )
  image-height @ 0 do
    i s>f image-height @ s>f f/ yy f!
    image-width @ 0 do
      i s>f image-width @ s>f f/ xx f!
      dup execute
      rgbf i j plot
    loop
  loop
  drop
;
|;

Sometimes we will want to convert a haiku to grayscale.
We'll want an rgb to grayscale conversion function.
|: implement haiku
: luminance ( rf gf bf -- f )
    0.0722e f* fswap
    0.7152e f* f+ fswap
    0.2126e f* f+ ;
|;

|section: 4spire

My favorite Forth Haiku of my own devising is called |b{ 4spire |}b .
|: 4spire
: 4spire ( -- )
  x x 23e f* fsin 2e f/ y fmax f/ fsin
  y x 23e f* fsin 2e f/ y fmax f/ fsin
  fover fover f/ fsin
;
|;

We will likely want a grayscale version.
|: 4spire
: 4spire-gray
  4spire luminance fdup fdup
;
|;

|section: Weaving the cover

We will have a cover based on the document base.
|: weaving cover
: cover-filename doc-base @ atom" _cover.bmp" atom+ ;
|;

We can now weave the cover, using 4spire.
It is a 600x800 image.
|: weaving cover
|@ implement images
|@ writing bmp files
|@ implement haiku
|@ 4spire
: weave-cover
  600 800 image-setup
  ['] 4spire haiku
  cover-filename bmp-save
;
|;


|slide-chapter: Appendix A - Slides

The follow are slides from an SVFIG presentation on
February 25, 2012.
|$
On eBook readers, browse normally.
On full browsers, |<-|  and |->|  move through the slides,
|^|  and |v|  jump to the beginning and end.


|section: Literate Programming in Forth
Brad Nelson
|$
February 25, 2012.


|section: Literate Programming
|{- Conceived of by Donald Knuth.
|-- Emphasize documentation over code.
|-- Pre-process source code to extract it from documentation
    which may list it in narratively use order.
|-}


|section: Use the Forth Parser
|{- Forth allows words to parse input source in mostly arbitrary ways.
|-- Indirection through an output file is unfortunately needed for ANSFORTH.
|-- Careful use of escaping.
|-- Use the pipe
|\ (|)
    character as the primary divider as it is rare in Forth.
|-}


|section: Target eBook readers
|{- Embrace the limitations and strengths of eBook readers.
|-- Use the MOBI format via kindlegen
   |{- OPF file
   |-- NCX file
   |-- Table of contents
   |-- Chapters in XHTML
   |-}
|-}

|section: Questions?
Document walk through

|.
