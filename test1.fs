s" literate.fs" included

|
Our goal is to print the square of 4.
Like this:
|+! *|
  |@ define square|
  4 square .
  bye
|

Here's how we define square:
|+! define square|
  : square ( n -- n^2 ) dup * ; 
|

That's it!

|;
bye
