\ Literate Programming Words


: assert ( n -- ) 0= if abort then ;

\ Decide if we're weaving or tangling.
s" LITERATE" getenv s" weave" compare 0= constant weaving?
s" LITERATE" getenv s" tangle" compare 0= constant tangling?
s" LITERATE" getenv s" " compare 0= constant running?
weaving? tangling? or running? or assert


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

: chain-link ( head[t] -- a head[t] ) here 0 , swap ;
: chain-first ( head[t] -- ) chain-link 2dup ! cell+ ! ;
: chain-rest ( head[t] -- ) chain-link cell+ 2dup @ ! ! ;
: chain ( head[t] -- ) dup @ if chain-rest else chain-first then ;
: ->next ( a -- a' ) @ ;
: $clone ( $ - $ ) here over 1+ allot swap 2dup >r >r cmove r> r> ;
: 3dup ( xyz -- xyzxyz ) >r 2dup r> dup >r swap >r swap r> r> ;

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
: atom" ( -- A ) [char] " parse atom ;
: atom"" ( -- A ) 0 0 atom ;

: atom-append ( A n Ad -- ) atom-head chain , , ;
: atom+=$ ( A Ad -- ) 0 swap atom-append ;
: atom+=ref ( A Ad -- ) 1 swap atom-append ;


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
: parse..| ( -- A ) atom"" begin replenish 0= if exit then eat| on|? until ;

atom" |" constant atom-|
atom" ~~~documentation" constant documentation
atom" <b>( " constant pre-use
atom"  )</b>" constant post-use
atom" </p><pre><b>&lt; " constant pre-def
atom"  &gt;</b> +&equiv; " constant post-def
atom" </p><h2>" constant pre-section
atom" </h2><p>" constant post-section
atom" </pre>" constant post-post-def
atom" </p><p>" constant paragraph
atom" *" constant atom-*
variable chunk
: doc! ( back to documentation) documentation chunk ! ;
doc!
: doc? ( -- f) documentation chunk @ = ;
: chunk+=$ ( A -- ) chunk @ atom+=$ ;
: chunk+=ref ( A -- ) chunk @ atom+=ref ;
: doc+=$ ( A -- ) documentation atom+=$ ;
: doc+=ref ( A -- ) documentation atom+=ref ;
: ?doc+=$ ( A -- ) doc? 0= if doc+=$ else drop then ;
: feed ( read into current chunk ) atom-cr parse..| atom+ dup chunk+=$ ?doc+=$ ;
: doc+=use ( A -- ) pre-use doc+=$ doc+=$ post-use doc+=$ ;
: doc+=def ( A -- ) pre-def doc+=$ doc+=$ post-def doc+=$ ;
: |@ ( use a chunk ) parse-cr dup chunk+=ref doc+=use feed ;
: |: ( add to a chunk ) parse-cr dup chunk ! doc+=def feed ;
: || ( escaped | ) atom-| chunk+=$ feed ;
: |; ( documentation ) doc? 0= if post-post-def doc+=$ then doc! feed ;
: |$ ( paragraph ) paragraph doc+=$ feed ;

: once! ( n a -- ) dup @ 0= assert ! ;

variable title
: |title:   parse-cr title once! feed ;
variable author
: |author:   parse-cr author once! feed ;

: html-preamble ." <html><head><title>" title @ atom.
                ." </title></head><body>" cr
                ." <h1>" title @ atom.
                ."  - <i>" author @ atom. ." </i></h1><p>" cr ;
: html-postamble ." </p></body></html>" cr ;


: |section:   parse-cr pre-section doc+=$ doc+=$ post-section doc+=$ feed ;

: weave   html-preamble documentation means atom. html-postamble ;
: tangle   atom-* means atom. ;
: run   atom-* means atom-string@ evaluate ;
: |. ( exit literate mode ) weaving? if weave then
                            tangling? if tangle then
                            running? if run then ;


\ Test atoms.
atom" foo" atom" foo" = assert
atom" bar" atom" foo" <> assert

\ Test means.
atom" abc" atom" bar" atom+=$
atom" def" atom" bar" atom+=$
atom" 1234" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" 5678 9" atom" foo" atom+=$
atom" bar" atom" foo" atom+=ref
atom" foo" means atom" 1234abcdef5678 9abcdef" = assert

\ Test atom+.
atom" testing" atom" 123" atom+ atom" testing123" = assert

\ Test parse.
: |halt! ;
parse..| testing
Hello there
123|halt!
atom" testing" atom-cr+
atom" Hello there" atom+ atom-cr+
atom" 123" atom+ = assert

