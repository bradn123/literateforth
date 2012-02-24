

vocabulary literate also literate definitions



: assert ( n -- )
    0= if abort then ;


: linked-list
    create 0 , 0 , ;

: allocate' ( n -- a )
    allocate 0= assert ;

: zero ( a n -- )
    0 fill ;
: allocate0 ( n -- a )
    dup allocate' swap 2dup zero drop ;

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

: ->next ( a -- a' ) @ ;

: $clone ( $ - $ )
    dup allocate 0= assert swap 2dup >r >r move r> r> ;

: 3dup ( xyz -- xyzxyz )
    >r 2dup r> dup >r swap >r swap r> r> ;


: atom-length@ ( A -- n )
    1 cells + @ ;
: atom-data@ ( A -- a )
    2 cells + @ ;
: atom-string@ ( A -- $ )
    dup atom-data@ swap atom-length@ ;
: atom-meaning-head ( A -- A[head] )
    3 cells + ;

linked-list atom-root

: $atom-new ( $ -- A )
    >r >r 0 0 r> r> 4 atom-root chain atom-root cell+ @ ;

: atom-new ( $ -- A )
    $clone $atom-new ;

: atom= ( $ A -- f )
    atom-string@ compare 0= ;

: atom-find' ( $ A -- A )
    begin
       dup 0= if nip nip exit then
       3dup atom= if nip nip exit then
       ->next
    again ;
: atom-find ( $ -- A )
    atom-root @ atom-find' ;

: $atom ( $ -- A )
    2dup atom-find dup if nip nip else drop $atom-new then ;

: atom ( $ -- A )
    2dup atom-find dup if nip nip else drop atom-new then ;

: atom. ( A -- )
    atom-string@ type ;

: atoms. ( -- )
    atom-root @ begin dup while
    dup atom. cr ->next repeat drop ;

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


atom" foo" atom" foo" = assert

atom" bar" atom" foo" <> assert

atom" testing" atom" 123" atom+ atom" testing123" = assert

atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert


: file! ( A A -- )
    atom-string@ w/o bin create-file 0= assert
    swap over >r atom-string@ r> write-file 0= assert
    close-file 0= assert
;



: source@ source ( -- a )
    drop >in @ + ;
: source-remaining ( -- n )
   source nip >in @ - ;

: drop| ( -- )

    source@ 1- c@ [char] | = if -1 >in +! then ;
: need-refill? ( -- f)
    source nip >in @ <= ;

: on|? ( -- f )

    need-refill? if false exit then source@ c@ [char] | = ;
: replenish ( -- f )
    need-refill? if refill else true then ;

: ?atom-cr+ ( A -- A )

    on|? 0= if atom-cr+ then ;

: eat| ( -- )

    [char] | parse drop| atom atom+ ?atom-cr+ ;
: parse-cr ( -- A )
    source@ source-remaining atom   source nip >in ! ;

: parse..| ( -- A )

    atom"" begin replenish 0=

    if exit then eat| on|? until ;

: skip| ( -- )

    on|?  need-refill? 0= and if 1 >in +! then ;


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

: .d| ( -- )

    parse..| ; immediate

: |.d ( -- )
    postpone literal postpone doc+=$ ; immediate
: .dcr   atom-cr doc+=$ ;
: doc+=ref ( A -- )
    documentation atom+=ref ;
: ?doc+=$ ( A -- )
    doc? 0= if escape doc+=$ else drop then ;
: feed ( read into current chunk )

    parse..| dup ?atom-cr+ ?doc+=$ atom-cr+ chunk+=$ ;
: doc+=use
    ( A -- ) .d{ <b>} doc+=$ .d{ </b>} ;
: doc+=def ( A -- )
    .d{ </p><tt><b>} doc+=$
    .d{ </b> +&equiv;</tt><div class="chunk"><pre>} ;



: |@ ( use a chunk )
    parse-cr dup chunk+=ref doc+=use .dcr feed ;

: |: ( add to a chunk )
    parse-cr dup chunk ! doc+=def feed ;

: |; ( documentation )
    doc? 0= if .d{ </pre></div><p>} then doc! feed ;

: |$ ( paragraph )
    .d{ </p><p>} feed ;

: |\ ( whole line)
    parse-cr atom-cr+ dup chunk+=$ ?doc+=$ feed ;


variable doc-base
atom" index" doc-base !

: |document-base:   parse-cr doc-base ! feed ;

variable title
atom" Untitled" title !

: |title:   parse-cr title ! feed ;

variable author
atom" Anonymous" author !

: |author:   parse-cr author ! feed ;

variable isbn
atom" 9999999999" isbn !

: |isbn:   parse-cr isbn ! feed ;

variable subject
atom" Article" subject !

: |subject:   parse-cr subject ! feed ;

variable doc-date
atom" Unknown" doc-date !

: |date:   parse-cr doc-date ! feed ;

variable description
atom" No description available." description !

: |description:   parse-cr description ! feed ;



linked-list out-files

: |file: ( add a new output file )
    parse-cr dup 1 out-files chain
    .d{ <tt><i>} doc+=$ .d{ </i></tt>} feed ;
: file-name@ ( file -- A )
    cell+ @ ;



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


.d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
 "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html>
<head>

|.d

slide-chapter @ if


.d|
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

|.d

then


.d|
<style type="text/css">
  div.chunk {
    margin: 0em 0.5em;
  }

|.d

slide-chapter @ if

.d|
  div.section {
    page-break-before: always;
  }

|.d
then


.d|
  pre {
    margin: 0em 0em;
  }
</style>

<title>|.d

    dup doc+=$
    .d{ </title></head>}
    slide-chapter @ if .d{ <body onload="Load()">} else .d{ <body>} then
    .d{ <div class="section"><h1>}
    doc+=$
    .d{ </h1><p>}

    feed
;


: |chapter:   false slide-chapter !  raw-chapter ;

: |slide-chapter:   true slide-chapter !  raw-chapter ;


: |section:   parse-cr .d{ </p></div><div class="section"><h2>} doc+=$
                 .d{ </h2><p>} feed ;

: |page   parse-cr .d{ </p><p style="page-break-before:always;">} feed ;

variable bullet-depth
: bullet+   1 bullet-depth +!   bullet-depth @ 1 = if .d{ </p>} then ;
: bullet-   -1 bullet-depth +!   bullet-depth @ 0 = if .d{ <p>} then ;

: |{-   bullet+ .d{ <ul><li>} feed ;

: |--   .d{ </li><li>} feed ;

: |-}   .d{ </li></ul>} bullet- feed ;


: |b{   .d{ <b>} feed ;

: |}b   .d{ </b>} feed ;

: |i{   .d{ <i>} feed ;

: |}i   .d{ </i>} feed ;

: |u{   .d{ <u>} feed ;

: |}u   .d{ </u} feed ;

: |tt{   .d{ <tt>} feed ;

: |}tt   .d{ </tt>} feed ;

: |sup{   .d{ <sup>} feed ;

: |}sup   .d{ </sup>} feed ;

: |sub{   .d{ <sub>} feed ;

: |}sub   .d{ </sub>} feed ;

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





: |TeX .d{ <span>T<sub><big>E</big></sub>X</span>} feed ;


: |LaTeX
    .d{ <span>L<sup><small>A</small></sup>T<sub><big>E</big></sub>X</span>}
    feed
;


: |<-| .d{ &larr;} feed ;

: |->| .d{ &rarr;} feed ;

: |^| .d{ &uarr;} feed ;

: |v| .d{ &darr;} feed ;






: literate-env ( -- $ ) s" LITERATE" getenv ;
: literate-mode ( $ -- )
    literate-env compare 0= constant ;

s" weave" literate-mode weaving?
s" tangle" literate-mode tangling?
s" " literate-mode running?

weaving? tangling? or running? or assert



atom" ~~~TOC" constant atom-toc
: toc-filename doc-base @ atom" .html" atom+ ;

: weave-toc-chapter ( chapter -- )
    .d{ <h4><b><a href="}
    dup chapter-filename doc+=$
    .d{ ">}
    chapter-name doc+=$
    .d{ </a></b></h4>} .dcr
 ;

: weave-toc
    atom-toc documentation-chunk ! doc!


.d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head><title>Table of Contents</title></head>
<body>
<div>
  <h1><b>TABLE OF CONTENTS</b></h1>

|.d

    chapters @ begin dup while
    dup weave-toc-chapter ->next repeat drop

    .d{ </div></body></html>} .dcr

    documentation means toc-filename file!
;


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


.d| <?xml version="1.0" encoding="UTF-8"?>
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


|.d
.d{ <docTitle><text>} title @ doc+=$

.d| </text></docTitle>
<docAuthor><text>me</text></docAuthor>

  <navMap>
    <navPoint class="toc" id="toc" playOrder="1">
      <navLabel>
        <text>Table of Contents</text>
      </navLabel>


     <content src="|.d toc-filename doc+=$ .d| "/>
     </navPoint>

|.d

    chapters @ begin dup while
    dup weave-ncx-chapter ->next repeat drop

    .d{ </navMap></ncx>}
    documentation means ncx-filename file!
;


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
    .d{ <itemref idref="} doc+=$ .d{ "/>} .dcr ;

: weave-opf
    atom-opf documentation-chunk ! doc!


.d| <?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" version="2.0"
unique-identifier="BookId">
<metadata xmlns:dc="http://purl.org/dc/elements/1.1/"
xmlns:opf="http://www.idpf.org/2007/opf">

|.d

    .d{ <dc:title>} title @ doc+=$ .d{ </dc:title>} .dcr
    .d{ <dc:language>en-us</dc:language>} .dcr
    .d{ <dc:identifier id="BookId" opf:scheme="ISBN">}
    isbn @ doc+=$ .d{ </dc:identifier>} .dcr
    .d{ <dc:creator>} author @ doc+=$ .d{ </dc:creator>} .dcr
    .d{ <dc:publisher>} author @ doc+=$ .d{ </dc:publisher>} .dcr
    .d{ <dc:subject>} subject @ doc+=$ .d{ </dc:subject>} .dcr
    .d{ <dc:date>} doc-date @ doc+=$ .d{ </dc:date>} .dcr
    .d{ <dc:description>} description @ doc+=$ .d{ </dc:description>} .dcr

.d|
</metadata>

<manifest>
   <item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml"

   href="|.d ncx-filename doc+=$ .d| "/>

  <item id="toc" media-type="application/xhtml+xml" href="|.d
    toc-filename doc+=$ .d{ "></item>}
    chapters @ begin dup while
        dup chapter-filename opf-chapter ->next
    repeat drop
    .d{ </manifest>}

    .d{ <spine toc="My_Table_of_Contents"><itemref idref="toc"/>}
    chapters @ begin dup while
        dup chapter-filename opf-chapter' ->next
    repeat drop
   .d{ </spine>}


.d|
<guide>
  <reference type="toc" title="Table of Contents"

   href="|.d toc-filename doc+=$ .d| "></reference>
</guide>
</package>

|.d

   documentation means opf-filename file!
;


: weave-chapter ( chapter -- )
    dup chapter-text swap chapter-filename file! ;
: weave-chapters
    chapters @ begin dup while
    dup weave-chapter ->next repeat drop ;

: weave ( -- )
    weave-chapters weave-toc weave-opf weave-ncx ;


: tangle-file ( file -- )
    file-name@ dup means swap file! ;

: tangle
    out-files @ begin dup while
    dup tangle-file ->next repeat drop ;


: run-filename ( -- A )
    doc-base @ atom" _running.tmp" atom+ ;

: run-cleanup
    run-filename atom-string@ delete-file drop ;

: bye   run-cleanup bye ;

: run   atom" *" means run-filename file!
        run-filename atom-string@ included
        run-cleanup
;



: |. ( exit literate mode )
     chapter-finish
     weaving? if weave bye then
     tangling? if tangle bye then
     running? if run then ;




