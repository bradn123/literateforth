


: assert ( n -- ) 0= if abort then ;

: once! ( n a -- ) dup @ 0= assert ! ;


\ Decide if we're weaving or tangling.
: literate-env ( -- $ ) s" LITERATE" getenv ;
literate-env s" weave" compare 0= constant weaving?
literate-env s" tangle" compare 0= constant tangling?
literate-env s" " compare 0= constant running?
\ Require we are in one of the modes.
weaving? tangling? or running? or assert


: $clone ( $ - $ ) here over 1+ allot swap 2dup >r >r cmove r> r> ;

: 3dup ( xyz -- xyzxyz ) >r 2dup r> dup >r swap >r swap r> r> ;


: chain-link ( head[t] -- a head[t] ) here 0 , swap ;
: chain-first ( head[t] -- ) chain-link 2dup ! cell+ ! ;
: chain-rest ( head[t] -- ) chain-link cell+ 2dup @ ! ! ;
: chain ( head[t] -- ) dup @ if chain-rest else chain-first then ;
: ->next ( a -- a' ) @ ;


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
: atom-head ( A -- A[head] ) 3 cells + ;

create atom-root  0 , 0 ,
: $atom-new ( $ -- A ) atom-root chain , , 0 , 0 , atom-root cell+ @ ;
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
 
: atom-append ( A n Ad -- ) atom-head chain , , ;
: atom+=$ ( A Ad -- ) 0 swap atom-append ;
: atom+=ref ( A Ad -- ) 1 swap atom-append ;



\ Test using atoms.
atom" foo" atom" foo" = assert
atom" bar" atom" foo" <> assert



: ref-parts ( ref -- A ref? ) cell+ dup cell+ @ swap @ ;
: atom-walk ( fn A -- )
     atom-head @ begin dup while
         2dup >r >r
         ref-parts if recurse else swap execute then
         r> r>
         ->next
     repeat 2drop ;
: tally-length ( n A -- n ) atom-length@ + ;
: gather-string ( a A -- a' ) 2dup atom-string@ >r swap r> move tally-length ;
: atom-walk-length ( A -- n ) 0 swap ['] tally-length swap atom-walk ;
: atom-walk-gather ( a A -- ) swap ['] gather-string swap atom-walk drop ;
: means ( A -- A' ) dup atom-walk-length here swap 2dup >r >r allot align
                    atom-walk-gather r> r> $atom ;


\ Test means.
atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert


: atom, ( A -- ) atom-string@ dup here swap allot swap move ;
: atom+ ( A A -- A ) swap here >r atom, atom, r> here over - align $atom ;
: atom-ch ( ch -- A ) here c! here cell allot 1 atom ;
10 atom-ch constant atom-cr
: atom-cr+ ( A -- A ) atom-cr atom+ ;






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
: escape ( A -- A ) here swap escape-each here over - align $atom-new ;



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
: .dcr   atom-cr doc+=$ ;
: doc+=ref ( A -- ) documentation atom+=ref ;
: ?doc+=$ ( A -- ) doc? 0= if escape doc+=$ else drop then ;
: feed ( read into current chunk ) parse..| atom-cr atom+ dup chunk+=$ ?doc+=$ ;
: doc+=use ( A -- ) .d{ <b>( } doc+=$ .d{  )</b>} ;
: doc+=def ( A -- ) .d{ </p><p><b>&lt; } doc+=$
                    .d{  &gt;</b> +&equiv;<pre> } .dcr ;
: |@ ( use a chunk ) parse-cr dup chunk+=ref doc+=use feed ;
: |: ( add to a chunk ) parse-cr dup chunk ! doc+=def feed ;
: || ( escaped | ) atom" |" chunk+=$ feed ;
: |; ( documentation ) doc? 0= if .d{ </pre></p><p>} then doc! feed ;
: |$ ( paragraph ) .d{ </p><p>} feed ;
: |\ ( whole line) parse-cr dup chunk+=$ ?doc+=$ feed ;

\
\


create out-files 0 , 0 ,
: |file: ( add a new output file )
   parse-cr out-files chain dup ,
   .d{ <div><i>} doc+=$ .d{ </i></div>} feed ;


: |-constant ( create atom constant ) constant ;


variable title
: |title:   parse-cr title once! feed ;
variable author
: |author:   parse-cr author once! feed ;



parse..| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html>
<head>
<style>
pre {
  margin: 0em 1em;
}
</style>
<title>|-constant chapter-pre1

parse..| </title>
</head>
<body>
<h1>|-constant chapter-pre2

parse..| </h1>
<p>
|-constant chapter-pre3

parse..|
</p>
</body>
</html>
|-constant chapter-post


atom" ~~~OPF" constant atom-opf
atom" index.opf" constant opf-filename

parse..| <?xml version="1.0" encoding="utf-8"?>
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
|-constant opf-pre1

parse..| <item id="|-constant opf-chapter-pre1
parse..| " media-type="application/xhtml+xml" href="|-constant opf-chapter-pre2
parse..| "></item>
|-constant opf-chapter-post
: opf-chapter ( A -- )
  opf-chapter-pre1 doc+=$ 
  dup doc+=$
  opf-chapter-pre2 doc+=$ 
  doc+=$
  opf-chapter-post doc+=$ 
;

parse..|
</manifest>
<spine toc="My_Table_of_Contents">
  <itemref idref="toc"/>
|-constant opf-pre2

parse..| <itemref idref="|-constant opf-chapter'-pre1
parse..| "/>
|-constant opf-chapter'-post
: opf-chapter' ( A -- )
  opf-chapter'-pre1 doc+=$ 
  doc+=$
  opf-chapter'-post doc+=$ 
;



parse..|
</spine>
<guide>
  <reference type="toc" title="Table of Contents"
   href="index.html"></reference>
</guide>
</package>
|-constant opf-post


atom" ~~~NCX" constant atom-ncx
atom" index.ncx" constant ncx-filename

parse..| <?xml version="1.0" encoding="UTF-8"?>
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
|-constant ncx-pre1

parse..| <navPoint class="chapter" id="|-constant ncx-chapter-pre1
parse..| " playOrder="|-constant ncx-chapter-pre2
parse..| ">
      <navLabel>
        <text>|-constant ncx-chapter-pre3
parse..| </text>
      </navLabel>
      <content src="|-constant ncx-chapter-pre4
parse..| "/>
    </navPoint>
|-constant ncx-chapter-post

parse..|
  </navMap>
</ncx>
|-constant ncx-post


atom" ~~~TOC" constant atom-toc
atom" index.html" constant toc-filename

parse..| <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN"
"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head><title>Table of Contents</title></head>
<body>
<div>
 <h1><b>TABLE OF CONTENTS</b></h1>
|-constant toc-pre

parse..|
</div>
</body>
</html>
|-constant toc-post

parse..| <h3><b><a href="|-constant toc-chapter-pre1
parse..| ">|-constant toc-chapter-pre2
parse..| </a></b></h3>
|-constant toc-chapter-post


variable chapter-count
create chapters 0 , 0 ,
: chapter-finish   chapter-post doc+=$ ;
: |chapter:
    chapter-finish
    parse-cr chapters chain dup ,
    chapter-count @ ,   1 chapter-count +!
    dup documentation-chunk ! doc!
    chapter-pre1 doc+=$
    dup doc+=$
    chapter-pre2 doc+=$
    doc+=$
    chapter-pre3 doc+=$
    feed
;



: file! ( A A -- )
    atom-string@ w/o bin create-file 0= assert
    swap over >r atom-string@ r> write-file 0= assert
    close-file 0= assert
;





: |section:   parse-cr .d{ </p><h2>} doc+=$ .d{ </h2><p>} feed ;



: chapter-name ( chp -- A ) cell+ @ ;
: chapter-text ( chp -- A ) cell+ @ means ;
: chapter-number ( chp -- n ) 2 cells + @ ;
atom" .html" constant .html
: chapter-filename ( chp -- A )
    chapter-number [char] A + atom-ch .html atom+ ;


: weave-chapter ( chapter -- ) dup chapter-text swap chapter-filename file! ;
: weave-chapters
   chapters @ begin dup while dup weave-chapter ->next repeat drop ;

: weave-toc-chapter ( chapter -- )
   toc-chapter-pre1 doc+=$
   dup chapter-filename doc+=$
   toc-chapter-pre2 doc+=$
   chapter-name doc+=$
   toc-chapter-post doc+=$
;
: weave-toc
   atom-toc documentation-chunk ! doc!
   toc-pre doc+=$
   chapters @ begin dup while dup weave-toc-chapter ->next repeat drop
   toc-post doc+=$
   documentation means toc-filename file!
;

: weave-ncx-chapter ( chapter -- )
   ncx-chapter-pre1 doc+=$
   dup chapter-filename doc+=$
   ncx-chapter-pre2 doc+=$
   dup chapter-filename doc+=$
   ncx-chapter-pre3 doc+=$
   dup chapter-name doc+=$
   ncx-chapter-pre4 doc+=$
   chapter-filename doc+=$
   ncx-chapter-post doc+=$
;
: weave-ncx
   atom-ncx documentation-chunk ! doc!
   ncx-pre1 doc+=$
   chapters @ begin dup while dup weave-ncx-chapter ->next repeat drop
   ncx-post doc+=$
   documentation means ncx-filename file!
;

: weave-opf
   atom-opf documentation-chunk ! doc!
   opf-pre1 doc+=$
   chapters @ begin dup while dup chapter-filename opf-chapter ->next repeat drop
   opf-pre2 doc+=$
   chapters @ begin dup while dup chapter-filename opf-chapter' ->next repeat drop
   opf-post doc+=$
   documentation means opf-filename file!
;



: weave    weave-chapters weave-toc weave-opf weave-ncx ;

: tangle-file ( file -- ) cell+ @ dup means swap file! ;
: tangle   out-files @ begin dup while dup tangle-file ->next repeat drop ;


: run   atom" *" means atom-string@ evaluate ;



: |. ( exit literate mode )
    chapter-finish
    weaving? if weave bye then
    tangling? if tangle bye then
    running? if run then ;


