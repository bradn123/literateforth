3 2 * 2 4 * + constant bmp-header-size
10 4 * constant dib-header-size

variable bmp-file
variable bmp-width
variable bmp-height
variable bmp-data

: bmp-data-size bmp-width @ bmp-height @ * 4 * ;

: assert ( f -- ) 0= if abort then ;

: bc ( ch -- ) here c! here 1 bmp-file @ write-file 0= assert ;
: bw ( w -- ) dup 255 and bc 8 rshift 255 and bc ;
: bd ( d -- ) dup 65535 and bw 16 rshift 65535 and bw ;

: bmp-resize ( w h -- )
  bmp-height ! bmp-width !
  bmp-data-size allocate 0= assert bmp-data !
  bmp-data @ bmp-data-size 0 fill
;

: bmp-save ( -- )
s" out.bmp" w/o bin create-file 0= assert bmp-file !

\ BMP Header
[char] B bc [char] M bc
bmp-header-size
dib-header-size +
bmp-data-size + bd \ size of bmp file in bytes
0 bw
0 bw
bmp-header-size
dib-header-size + bd \ offset to start of bitmap image data

\ DIB Header
dib-header-size bd \ size of header in bytes
bmp-width @ bd \ width
bmp-height @ bd \ height
1 bw \ color planes
32 bw \ bits per pixel
0 bd \ BI_RGB (uncompressed)
bmp-data-size bd \ pixel data size
0 bd \ horizontal pixels per meter
0 bd \ vertical pixels per meter
0 bd \ colors in color palette
0 bd \ important colors in palette

bmp-data @ bmp-data-size bmp-file @ write-file 0= assert

bmp-file @ close-file 0= assert
;

variable red
variable green
variable blue
: rgb ( r g b -- n ) blue ! green ! red ! ;
: plot ( x y -- ) bmp-width @ * + 4 * bmp-data @ +
                  red @ over c!
                  green @ over 1+ c!
                  blue @ swap 2 + c! ;
: black ( -- ) 0 0 0 rgb ;
: white ( -- ) 255 255 255 rgb ;
: grayscale ( n -- ) dup dup rgb ;


: pretty ( -- )
  bmp-height @ 0 do
    bmp-width @ 0 do
      i j * 255 and j + 2/ grayscale
      i j plot
    loop
  loop
;

fvariable xx
fvariable yy
: x ( -- f ) xx f@ ;
: y ( -- f ) yy f@ ;

: luminance ( r g b -- n )
    0.0722e f* fswap
    0.7152e f* f+ fswap
    0.2126e f* f+
    1e fmin 0e fmax ;

: 4spire ( -- )
  x x 23e f* fsin 2e f/ y fmax f/ fsin
  y x 23e f* fsin 2e f/ y fmax f/ fsin
  fover fover f/ fsin
  luminance fdup fdup
;

: haiku ( f -- )
  bmp-height @ 0 do
    i s>f bmp-height @ s>f f/ yy f!
    bmp-width @ 0 do
      i s>f bmp-width @ s>f f/ xx f!
      dup execute
      255e f* f>s 255e f* f>s 255e f* f>s rgb
      i j plot
    loop
  loop
  drop
;

600 800 bmp-resize
' 4spire haiku
bmp-save
bye
