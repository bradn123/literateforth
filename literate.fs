\ Literate Programming Words

\ Decide if we're weaving or tangling.
s" WEAVE" getenv nip constant weaving?


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
: atomch ( ch -- A ) here c! here cell allot 1 atom ;
10 atomch constant atomcr
: atomcr+ ( A -- A ) atomcr atom+ ;
                     

: assert ( n -- ) 0= if abort then ;


: source@ source drop >in @ + ;
: drop| ( -- ) source@ 1- c@ [char] | = if -1 >in +! then ;
: need-refill? ( -- f) source nip >in @ <= ;
: on|? ( -- f ) need-refill? if false exit then source@ c@ [char] | = ;
: replenish ( -- f ) need-refill? if refill else true then ;
: ?atomcr+ ( A -- A ) on|? 0= if atomcr+ then ;
: eat| ( -- ) [char] | parse drop| atom atom+ ?atomcr+ ;
: parse| ( -- A ) atom"" begin replenish 0= if exit then eat| on|? until ;

: |@ ( use a chunk ) parse| atom. ;
: |+! ( add to a chunk ) parse| atom. ;
: || ( escaped | ) parse| atom. ;
: | ( documentation ) parse| atom. ;
: |; ( exit literate mode ) ;


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
parse| testing
Hello there
123|;
atom" testing" atomcr+
atom" Hello there" atom+ atomcr+
atom" 123" atom+ = assert
parse| testing
Hello there
123|;


(

|

This is a test.

|+! *|
|;


@<title: fsdfsdfsdfssdfd fsd fsdf s>
@<author: foo bar>
@<section: foo>
@<: variable blah>



@{

\title{ fdsfsdfsds }
\author{ fsdfsd }
\maketitle

\tableofcontents
\section{ foo }

blah blah this is messed b{ up }
\emph{ hi }

\subsection{ bar }

: test1
  weaving? if ." We're weaving" else ." We're tangling" then cr
;

test1
)
