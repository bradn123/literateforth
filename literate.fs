

vocabulary literate also literate definitions


: assert ( n -- ) 0= if abort then ;

: once! ( n a -- ) dup @ 0= assert ! ;


: allocate' ( n -- a ) allocate 0= assert ;
: zero ( a n -- ) 0 fill ;
: allocate0 ( n -- a ) dup allocate' swap 2dup zero drop ;

: chain-new ( n -- a ) 1+ cells allocate0 ;
: chain-fillout ( .. a n -- a ) 0 do dup i 1+ cells + swap >r ! r> loop ;
: chain-link ( ..n -- a ) dup chain-new swap chain-fillout ;
: chain-first ( ..n head[t] -- ) >r chain-link r> 2dup ! cell+ ! ;
: chain-rest ( ..n head[t] -- ) >r chain-link r> 2dup cell+ @ ! cell+ ! ;
: chain ( ..n head[t] -- ) dup @ if chain-rest else chain-first then ;

: ->next ( a -- a' ) @ ;
: linked-list   create 0 , 0 , ;


: $clone ( $ - $ ) dup allocate 0= assert swap 2dup >r >r move r> r> ;

: 3dup ( xyz -- xyzxyz ) >r 2dup r> dup >r swap >r swap r> r> ;


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
: $atom-new ( $ -- A ) >r >r 0 0 r> r> 4 atom-root chain
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
 
: atom-append ( A n Ad -- ) atom-def-head 2 swap chain ;
: atom+=$ ( A Ad -- ) 0 swap atom-append ;
: atom+=ref ( A Ad -- ) 1 swap atom-append ;



\ Test using atoms.
atom" foo" atom" foo" = assert
atom" bar" atom" foo" <> assert



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


\ Test means.
atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert


: atom>>$ ( A d -- d' ) 2dup >r atom-string@ r> swap move swap atom-length@ + ;
: atom+ ( A A -- A ) swap 2dup atom-length@ swap atom-length@ + dup >r
                     allocate 0= assert dup >r
                     atom>>$ atom>>$ drop r> r> $atom ;
: atom-ch ( ch -- A ) 1 allocate 0= assert 2dup c! nip 1 atom ;
10 atom-ch constant atom-cr
: atom-cr+ ( A -- A ) atom-cr atom+ ;




: file! ( A A -- )
    atom-string@ w/o bin create-file 0= assert
    swap over >r atom-string@ r> write-file 0= assert
    close-file 0= assert
;




: source@ source drop >in @ + ;

: source-remaining source nip >in @ - ;

: drop| ( -- ) source@ 1- c@ [char] | = if -1 >in +! then ;

: need-refill? ( -- f) source nip >in @ <= ;

: on|? ( -- f ) need-refill? if false exit then source@ c@ [char] | = ;

: replenish ( -- f ) need-refill? if refill else true then ;

: ?atom-cr+ ( A -- A ) on|? 0= if atom-cr+ then ;

: eat| ( -- ) [char] | parse drop| atom atom+ ?atom-cr+ ;

: parse-cr ( -- A ) source@ source-remaining atom   source nip >in ! ;

: parse..| ( -- A ) atom"" begin replenish 0=

                    if exit then eat| on|? until ;

: skip| ( -- ) on|?  need-refill? 0= and if 1 >in +! then ;

: |-constant ( create atom constant ) constant ;


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
: here! ( a -- ) here - allot ;
: escape ( A -- A ) here dup >r swap escape-each here over - atom r> here! ;



atom" ~~~blackhole" constant blackhole

variable documentation-chunk   blackhole documentation-chunk !

: documentation ( -- A ) documentation-chunk @ ;



variable chunk

: doc! ( back to documentation) documentation chunk ! ;

doc!

: doc? ( -- f) documentation chunk @ = ;

: chunk+=$ ( A -- ) chunk @ atom+=$ ;

: chunk+=ref ( A -- ) chunk @ atom+=ref ;

: doc+=$ ( A -- ) documentation atom+=$ ;

: .d{ ( -- ) postpone atom{ postpone doc+=$ ; immediate

: .d| ( -- ) parse..| ; immediate

: |.d ( -- ) postpone literal postpone doc+=$ ; immediate

: .dcr   atom-cr doc+=$ ;

: doc+=ref ( A -- ) documentation atom+=ref ;

: ?doc+=$ ( A -- ) doc? 0= if escape doc+=$ else drop then ;

: feed ( read into current chunk ) parse..| dup ?atom-cr+ ?doc+=$ atom-cr+ chunk+=$ ;

: doc+=use ( A -- ) .d{ <u><b>} doc+=$ .d{ </b></u>} ;

: doc+=def ( A -- )

    .d{ </p><tt><u><b>} doc+=$

    .d{ </b></u> +&equiv;</tt><div class="chunk"><pre>} ;

: |@ ( use a chunk ) parse-cr dup chunk+=ref doc+=use .dcr feed ;

: |: ( add to a chunk ) parse-cr dup chunk ! doc+=def feed ;

: || ( escaped | ) atom" |" chunk+=$ feed ;

: |; ( documentation ) doc? 0= if .d{ </pre></div><p>} then doc! feed ;

: |$ ( paragraph ) .d{ </p><p>} feed ;

: |\ ( whole line) parse-cr atom-cr+ dup chunk+=$ ?doc+=$ feed ;



: |TeX .d{ <span>T<sub><big>E</big></sub>X</span>} feed ;


: |LaTeX
    .d{ <span>L<sup><small>A</small></sup>T<sub><big>E</big></sub>X</span>}
    feed
;



linked-list out-files

: |file: ( add a new output file )

   parse-cr dup 1 out-files chain

   .d{ <tt><i>} doc+=$ .d{ </i></tt>} feed ;



variable title

: |title:   parse-cr title once! feed ;


variable author

: |author:   parse-cr author once! feed ;




variable chapter-count
linked-list chapters
: chapter-finish   .d{ </p></body></html>} ;


: |chapter:

    chapter-finish

    parse-cr 

    chapter-count @   1 chapter-count +!

    over 2 chapters chain

    dup documentation-chunk ! doc!


.d| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"

"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">

<html>

<head>

<style type="text/css">

div.chunk {

  margin: 0em 0.5em;

}

pre {

  margin: 0em 0em;

}

</style>

<title>|.d


    dup doc+=$

    .d{ </title></head><body><h1>}

    doc+=$

    .d{ </h1><p>}


    feed

;


: |section:   parse-cr .d{ </p><h2>} doc+=$ .d{ </h2><p>} feed ;

: |page   parse-cr .d{ </p><p style="page-break-before:always;">} feed ;


: |{-   .d{ <ul><li>} feed ;

: |--   .d{ </li><li>} feed ;

: |-}   .d{ </li></ul>} feed ;

: chapter-name ( chp -- A ) cell+ @ ;
: chapter-text ( chp -- A ) cell+ @ means ;
: chapter-number ( chp -- n ) 2 cells + @ ;
atom" .html" constant .html
: chapter-filename ( chp -- A )
     chapter-number s>d <# # # # #s #> atom .html atom+ ;




\ Decide if we're weaving or tangling.
: literate-env ( -- $ ) s" LITERATE" getenv ;
literate-env s" weave" compare 0= constant weaving?
literate-env s" tangle" compare 0= constant tangling?
literate-env s" " compare 0= constant running?
\ Require we are in one of the modes.
weaving? tangling? or running? or assert




atom" ~~~OPF" constant atom-opf

atom" index.opf" constant opf-filename


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


.d| <?xml version="1.0" encoding="utf-8"?>

<package xmlns="http://www.idpf.org/2007/opf" version="2.0"

unique-identifier="BookId">

<metadata xmlns:dc="http://purl.org/dc/elements/1.1/"

xmlns:opf="http://www.idpf.org/2007/opf">

  <dc:title>Test1</dc:title>

  <dc:language>en-us</dc:language>

  <dc:identifier id="BookId" opf:scheme="ISBN">9999999999</dc:identifier>

  <dc:creator>me</dc:creator>

  <dc:publisher>Self</dc:publisher>

  <dc:subject>Article</dc:subject>

  <dc:date>2012-02-15</dc:date>

  <dc:description>My short description.</dc:description>

</metadata>



<manifest>

  <item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml"

   href="index.ncx"/>

  <item id="toc" media-type="application/xhtml+xml" href="index.html"></item>

|.d


   chapters @ begin dup while

        dup chapter-filename opf-chapter ->next repeat drop


.d|

</manifest>

<spine toc="My_Table_of_Contents">

  <itemref idref="toc"/>

|.d


   chapters @ begin dup while dup chapter-filename opf-chapter' ->next repeat drop


.d|

</spine>

<guide>

  <reference type="toc" title="Table of Contents"

   href="index.html"></reference>

</guide>

</package>

|.d


   documentation means opf-filename file!

;




atom" ~~~NCX" constant atom-ncx

atom" index.ncx" constant ncx-filename


: weave-ncx-chapter ( chapter -- )

   .d{ <navPoint class="chapter" id="}

   dup chapter-filename doc+=$

   .d{ " playOrder="}

   dup chapter-filename doc+=$

   .d| ">

      <navLabel>

        <text>|.d

   dup chapter-name doc+=$

   .d| </text>

      </navLabel>

      <content src="|.d

   chapter-filename doc+=$

   .d| "/>

    </navPoint>

|.d

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

<docTitle><text>Test1</text></docTitle>

<docAuthor><text>me</text></docAuthor>

  <navMap>

    <navPoint class="toc" id="toc" playOrder="1">

      <navLabel>

        <text>Table of Contents</text>

      </navLabel>

      <content src="index.html"/>

    </navPoint>

|.d


   chapters @ begin dup while dup weave-ncx-chapter ->next repeat drop


.d|

  </navMap>

</ncx>

|.d


   documentation means ncx-filename file!

;




atom" ~~~TOC" constant atom-toc

atom" index.html" constant toc-filename


: weave-toc-chapter ( chapter -- )

   .d{ <h3><b><a href="}

   dup chapter-filename doc+=$

   .d{ ">}

   chapter-name doc+=$

   .d{ </a></b></h3>} .dcr

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


   chapters @ begin dup while dup weave-toc-chapter ->next repeat drop


.d|

</div>

</body>

</html>

|.d


   documentation means toc-filename file!

;


: weave-chapter ( chapter -- ) dup chapter-text swap chapter-filename file! ;
: weave-chapters
    chapters @ begin dup while dup weave-chapter ->next repeat drop ;

: weave ( -- ) weave-chapters weave-toc weave-opf weave-ncx ;


: tangle-file ( file -- ) cell+ @ dup means swap file! ;
: tangle   out-files @ begin dup while dup tangle-file ->next repeat drop ;


atom" literate_running.tmp" constant run-filename
: run-cleanup   run-filename atom-string@ delete-file drop ;
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




