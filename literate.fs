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



: assert ( n -- ) 0= if abort then ;

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
0 assert


: @{ ( start documentation chunk) ;
: =<< ( define a chunk ) ;
: << ( use a chunk ) ;


(
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

bye
