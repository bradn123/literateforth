s" literate.fs" included

|title: fun^4
|author: me
|document-base: test1

|chapter: Introduction

|section: Overview

Our goal is to print the 4 raised to the 4th power.

|section: Source Files

There will be two files:
|file: test1.fs
|file: test1_power4.fs

|$

test1.fs will have the main use case:
|: test1.fs
  s" test1_power4.fs" included
  |@ use it
|;

test1_power4.fs will have the definition of 4^.
|: test1_power4.fs
  |@ define 4^
|;

When run directly it will work like this:
|: *
  |@ define 4^
  |@ use it
|;


|chapter: Implementation

|section: Basic Approach

In total it will work like this:
|: use it
  4 4^ . cr
  bye
|;

|section: Details of the implementation

Here's how we define 4^:
|: define 4^
  |@ define square
  : 4^ ( n -- n^4 ) square dup * ; 
|;

|$

Here's how we define square:
|: define square
  : square ( n -- n^2 ) dup * ; 
|;

|$

That's it!

|.
