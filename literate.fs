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

: chain ( head -- ) dup @ here swap , swap ! ;
: ->next ( a -- a' ) @ ;
: $clone ( $ - $ ) here over 1+ allot swap 2dup >r >r cmove r> r> ;
: 3dup ( xyz -- xyzxyz ) >r 2dup r> dup >r swap >r swap r> r> ;

variable atom-root
: atom-new ( $ -- A ) $clone atom-root chain , , 0 , 0 , atom-root @ ;

: atom. ( A -- ) atom-string@ type ;

: atoms. ( -- ) atom-root @ begin dup while dup atom. cr ->next repeat drop ;

: atom= ( $ A -- f ) atom-string@ compare 0= ;

: atom-find' ( $ A -- A ) dup 0= if nip nip exit then
                          3dup atom= if nip nip exit then
                          ->next recurse ;
: atom-find ( $ -- A ) atom-root @ atom-find' ;

: atom ( $ -- A ) 2dup atom-find dup if nip nip else drop atom-new then ;



s" hello there" atom dup . atom. cr
s" dude" atom dup . atom. cr
s" hello there" atom dup . atom. cr
s" what up" atom dup . atom. cr
s" dude" atom dup . atom. cr

atoms.


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
