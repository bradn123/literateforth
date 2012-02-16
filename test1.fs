s" literate.fs" included

|

|title: fun^4|

|section: Overview|

Our goal is to print the 4 raised to the 4th power.
Like this:
|+! *|
  |@ define 4^|
  4 4^ . cr
  bye
|


|section: Detailed of the implementation|

Here's how we define 4^:
|+! define 4^|
  |@ define square|
  : 4^ ( n -- n^4 ) square dup * ; 
|

|$|

Here's how we define square:
|+! define square|
  : square ( n -- n^2 ) dup * ; 
|

|$|

That's it!

|;

bye
