0 s" literate_lit.fs" included

|title: Event Driven Programming in Forth
|author: Brad Nelson
|document-base: events

|chapter: Introduction
This document describes an approach to event driven programming in Forth, in
the literate programming style.
Unlike a typical literate program written in full prose, this document is
was written to be used as a slide deck for a deep drive code tour.
While the presentation attempts to be comprehensive,
some supplemental information is included in a non-slide chapter
at the end of this document.

|slide-chapter: Event Driven Programming in Forth

Brad Nelson
|$
August 25, 2012
|$
|i{
In your browser, press |<-|  and |->|  to move through the slides,
|^|  and |v|  jump to the beginning and end.
|}i
|$
|i{
(On eBook readers, browse normally.)
|}i

|section: Overview
|{- Intro to Event Driven Programming
|-- Asynchronous I/O in Forth
|-- Closures in Forth
|-- Putting it Together
|-}

|section: Event Driven Programming
|{- Computers spend a lot of time waiting
|-- Real concurrency can be HARD
|-- Lots of threads can be expensive
|-}

|section: Node.js
|{- Server side Javascript
|-- Chrome's V8 engine for speed
|-- Closures used to chain asynchronous events
|-- Standard library mostly asynch instead of an afterthought
|-- Event loop implicit (program runs until nothing pending)
|-}

|section: Node.js (example)
|code{
function handleRequest(request, done) {
  if (request.style == 1) {
    getThing(request.name, function(result, err) {
      getThing(result, function(result, err) {
        done(result);
      });
    });
  } else {
    getThing('default', function(result, err) {
      done(result);
    });
  }
}
|}code

|section: Twisted
|{- Python framework for event drive programming
|-- Closer to explicit event handling
|-- Uses reactor + deferred
|-- Attach callbacks to promises (to be called when ready)
|-}

|section: Twisted (example)
|code{
from twisted.internet import reactor, defer
def getSlowSquare(x):
  d = defer.Defered()
  reactor.callLater(100, d.callback, x * x)
def printValue(x):
  print d
d = getSlowSquare(4)
d.addCallback(printValue)
reactor.callLater(200, reactor.stop)
reactor.run()
|}code

|section: Traditional Forth Approach
|{- Tasks
|-- User variables for per task state
|-- Global variables for shared state
|-- Blocked tasks mitigate waiting
|-}
PROs:
|{- Flow of a task is in one place
|-- Tasks can spawn subtasks
|-- Easier synchronization than threads
|-}
CONs:
|{- Tasks have thread-like overhead
|-- Cross task communication is ad-hoc
|-}

|section: Closures
|: closures
|@ carnal knowledge
|@ scope stack
|@ scope flow control
|@ bind and invoke
|@ start and end scope
|;

|section: Carnal Knowledge
For gforth we know colon-sys is on the data stack and 4 cells:
|: carnal knowledge
4 constant colon-sys-size
|;
Add a word to drop a colon-sys.
|: carnal knowledge
: colon-sys-drop ( colon-sys -- ) colon-sys-size 0 do drop loop ;
|;


|section: Scope Stack
|{- Nested scopes
|-- Can't use dstack or rstack as flow control is in the way
|-- Define our own
|-- Assume it won't get too deep
|-- Store the size in the first cell
|-- Leak like mad for now
|-}

|section: Scope Stack (implementation)
|: scope stack
colon-sys-size 20 * constant scope-cells
: scope-alloc ( -- s) scope-cells allocate 0= assert
              1 cells over ! ;
variable myscope
scope-alloc myscope !
|;
Add some push / pop operations.
|: scope stack
: scope+! ( n -- ) cells myscope @ +! ;
: scope-ptr ( -- n ) myscope @ @ myscope @ + ;
: >s ( n -- ) scope-ptr !  1 scope+! ;
: s> ( -- n ) -1 scope+!  scope-ptr @ ;
|;

|section: Printing a Scope
|: scope stack
: scope. ( s -- ) ." scope(" dup @ cell / 1- . ." ) "
    dup @ cell ?do dup i + @ . cell +loop drop cr ;
|;

|section: Cloning Scopes and Freeing
|: scope stack
: scope-clone ( s -- s' )
    scope-alloc dup >r scope-cells cmove r>
;
: scope-free ( s -- ) free 0= assert ;
|;

|section: :noname
Alternate version of |tt{ :noname|}tt .
|: scope flow control
: :noname2 ( -- xt )
    :noname colon-sys-drop ;
|;

|section: Bind and Invoke
|: bind and invoke
: bind ( xt -- closure )
    >s myscope @ scope-clone s> drop
;
: invoke ( closure -- )
    myscope @ >r ( leak ) scope-clone myscope !
    s> execute
    myscope @ scope-free r> myscope !
;
|;

|section: Start and End Scope
We want:
|code{
: foo a b c [: x y z ;] d e f ;
|}code
|: start and end scope
: [:   postpone ahead postpone exit
       postpone [ :noname2 >s ; immediate
: ;]   postpone exit postpone then
       s> postpone literal postpone bind ; immediate
|;

|section: Try Out Closures
|: general tests
: scope-test 1 [: 2 [: 3 ;] 4 ;] 5 ;
scope-test 5 assert=
invoke 4 assert=
invoke 3 assert=
2 assert=
1 assert=
|;

|section: Closures with Scope
|: general tests
: test-adder >s [: s> + ;] ;
5 4 test-adder invoke 9 assert=
|;

|section: Asynchronous I/O
|{- Use gforth's c-function words
|-- Single threaded forth
|-- C side I/O worker pool
|-}

|section: Required Headers
|: required headers
\c #include <assert.h>
\c #include <fcntl.h>
\c #include <pthread.h>
\c #include <stdio.h>
\c #include <stdlib.h>
\c #include <string.h>
\c #include <unistd.h>
|;

|section: Standard Constants
Some standard constants will be brought over from C.
|: relevant constants
\c #define DEFINT(name) int name##_int(void) { return name; }
\c DEFINT(O_CREAT)
\c DEFINT(O_TRUNC)
\c DEFINT(O_WRONLY)
\c DEFINT(O_RDONLY)
c-function O_CREAT O_CREAT_int -- n
c-function O_TRUNC O_TRUNC_int -- n
c-function O_WRONLY O_WRONLY_int -- n
c-function O_RDONLY O_RDONLY_int -- n
|;

|section: File Permissions
Declare full octal permissions:
|: relevant constants
: octal 8 base ! ;
octal
777 constant rwx
decimal
|;

|section: Workers
We will for now assume a fixed number of workers.
|: worker count
\c #define WORKERS 10
\c static pthread_t g_worker_pool[WORKERS];
|;

|section: Worker Startup
The workers are started when the system is initialized.
|: start all workers
\c   for (i = 0; i < WORKERS; ++i) {
\c     if (pthread_create(&g_worker_pool[i], NULL, Worker, NULL)) {
\c       assert(0);
\c     }
\c   }
|;

|section: Requests
|: request structure
\c typedef union {
\c   int number;
\c   void *pointer;
\c } VARIANT;
\c
\c typedef struct _REQUEST {
\c   struct _REQUEST *next;
\c   enum {
       |@ event types
\c   } operation;
\c   VARIANT args[4];
\c   void *callback;
\c   int result;
\c } REQUEST;
|;

|section: Queues
Two queues are involved in the system.
Guarded by a single lock so that a single pending count for the complete
pipeline can be kept.
|: lock and count
\c static pthread_mutex_t g_lock;
\c static int g_pending_count;
|;

|section: Request Queue
One to receive pending requests.
|: requests queue
\c static pthread_cond_t g_requests_ready;
\c static REQUEST *g_requests_head;
\c static REQUEST *g_requests_tail;
|;

|section: Result Queue
Another to gather processed requests for processing in the main event loop.
|: results queue
\c static pthread_cond_t g_results_ready;
\c static REQUEST *g_results_head;
\c static REQUEST *g_results_tail;
|;

|section: Queue Setup
These will be initialized on startup.
|: startup routine
\c void async_startup(void) {
\c   int i;
\c   pthread_mutex_init(&g_lock, NULL);
\c   pthread_cond_init(&g_requests_ready, NULL);
\c   pthread_mutex_init(&g_lock, NULL);
\c   pthread_cond_init(&g_results_ready, NULL);
\c   g_requests_head = 0;
\c   g_requests_tail = 0;
\c   g_results_head = 0;
\c   g_results_tail = 0;
\c   g_pending_count = 0;
|@ start all workers
\c }
|;

|section: Enqueue Requests
Requests will then be enqueued on demand.
|: enqueue a request
\c void async_request_enqueue(REQUEST *req) {
\c   pthread_mutex_lock(&g_lock);
\c   ++g_pending_count;
\c   if (g_requests_tail) {
\c     g_requests_tail->next = req;
\c   } else {
\c     g_requests_head = req;
\c   }
\c   g_requests_tail = req;
\c   req->next = 0;
\c   pthread_cond_signal(&g_requests_ready);
\c   pthread_mutex_unlock(&g_lock);
\c }
|;
|: forth to c declarations
c-function async-startup async_startup -- void
|;

|section: Shutdown
|: event types
\c     SHUTDOWN,
|;
|: forth to c declarations
c-function async-shutdown async_shutdown -- void
|;
|: handle request types
\c     case SHUTDOWN:
\c       free(req);
\c       return;
|;
|: issue requests
\c void async_shutdown(void) {
\c   REQUEST *req;
\c   int i;
\c   for (i = 0; i < WORKERS; ++i) {
\c     req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c     req->operation = SHUTDOWN;
\c     async_request_enqueue(req);
\c   }
\c   for (i = 0; i < WORKERS; ++i) {
\c     pthread_join(g_worker_pool[i], NULL);
\c     pthread_mutex_destroy(&g_lock);
\c     pthread_cond_destroy(&g_requests_ready);
\c     pthread_mutex_destroy(&g_lock);
\c     pthread_cond_destroy(&g_results_ready);
\c   }
\c }
|;

|section: Open
|: event types
\c     OPEN,
|;
|: forth to c declarations
c-function async-open async_open a n n n a -- void
|;
|: handle request types
\c     case OPEN:
\c       tmp = malloc(req->args[1].number + 1);
\c       assert(tmp);
\c       memcpy(tmp, req->args[0].pointer, req->args[1].number);
\c       tmp[req->args[1].number] = 0;
\c       req->result = open(tmp, req->args[2].number, req->args[3].number);
\c       free(tmp);
\c       break;
|;
|: issue requests
\c void async_open(char *path, int path_len,
\c                 int oflag, int mode, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = OPEN;
\c   req->args[0].pointer = path;
\c   req->args[1].number = path_len;
\c   req->args[2].number = oflag;
\c   req->args[3].number = mode;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }
|;

|section: Close
|: event types
\c     CLOSE,
|;
|: forth to c declarations
c-function async-close async_close n a -- void
|;
|: handle request types
\c     case CLOSE:
\c       req->result = close(req->args[0].number);
\c       break;
|;
|: issue requests
\c void async_close(int fd, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = CLOSE;
\c   req->args[0].number = fd;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }
|;

|section: Read
|: event types
\c     READ,
|;
|: forth to c declarations
c-function async-read async_read n a n a -- void
|;
|: handle request types
\c     case READ:
\c       req->result = read(req->args[0].number, req->args[1].pointer,
\c                          req->args[2].number);
\c       break;
|;
|: issue requests
\c void async_read(int fd, void *buf, int len, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = READ;
\c   req->args[0].number = fd;
\c   req->args[1].pointer = buf;
\c   req->args[2].number = len;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }
|;

|section: Write
|: event types
\c     WRITE,
|;
|: forth to c declarations
c-function async-write async_write n a n a -- void
|;
|: handle request types
\c     case WRITE:
\c       req->result = write(req->args[0].number, req->args[1].pointer,
\c                           req->args[2].number);
\c       break;
|;
|: issue requests
\c void async_write(int fd, void *buf, int len, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = WRITE;
\c   req->args[0].number = fd;
\c   req->args[1].pointer = buf;
\c   req->args[2].number = len;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }
|;

|section: System
|: event types
\c     SYSTEM,
|;
|: forth to c declarations
c-function async-system async_system a n a -- void
|;
|: handle request types
\c     case SYSTEM:
\c       tmp = malloc(req->args[1].number + 1);
\c       assert(tmp);
\c       memcpy(tmp, req->args[0].pointer, req->args[1].number);
\c       tmp[req->args[1].number] = 0;
\c       req->result = system(tmp);
\c       free(tmp);
\c       break;
|;
|: issue requests
\c void async_system(char *cmd, int cmd_len, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = SYSTEM;
\c   req->args[0].pointer = cmd;
\c   req->args[1].number = cmd_len;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }
|;

|section: Worker Implementation
|: worker implementation
\c void *Worker(void *arg) {
\c   REQUEST *req;
\c   char *tmp;
\c
\c   for (;;) {
       |@ get a pending request
       |@ handle a request
       |@ pass on result
\c   }
\c }
|;

|section: Get a Pending Request
|: get a pending request
\c     pthread_mutex_lock(&g_lock);
\c     while (!g_requests_head) {
\c       pthread_cond_wait(&g_requests_ready, &g_lock);
\c     }
\c     req = g_requests_head;
\c     g_requests_head = req->next;
\c     if (!g_requests_head) { g_requests_tail = 0; }
\c     pthread_mutex_unlock(&g_lock);
|;

|section: Handle a Result
|: handle a request
\c     switch (req->operation) {
       |@ handle request types
\c     default:
\c       assert(0);
\c       break;
\c     }
|;

|section: Pass on Result
|: pass on result
\c     pthread_mutex_lock(&g_lock);
\c     if (g_results_tail) {
\c       g_results_tail->next = req;
\c     } else {
\c       g_results_head = req;
\c     }
\c     g_results_tail = req;
\c     req->next = 0;
\c     pthread_cond_signal(&g_results_ready);
\c     pthread_mutex_unlock(&g_lock);
|;

|section: Waiting for Results
Waiting then occurs on the main thread.
|: implement waiting
\c void async_wait(int *result, void **callback) {
\c   REQUEST *req;
\c   pthread_mutex_lock(&g_lock);
\c   if (g_pending_count <= 0) {
\c     *result = 0;
\c     *callback = 0;
\c     pthread_mutex_unlock(&g_lock);
\c     return;
\c   }
\c   while (!g_results_head) {
\c     pthread_cond_wait(&g_results_ready, &g_lock);
\c   }
\c   req = g_results_head;
\c   g_results_head = req->next;
\c   if (!g_results_head) { g_results_tail = 0; }
\c   *result = req->result;
\c   *callback = req->callback;
\c   free(req);
\c   --g_pending_count;
\c   pthread_mutex_unlock(&g_lock);
\c }
|;
|: forth to c declarations
c-function async-wait async_wait a a -- void
|;

|section: Run Loop
|: dispatch events
variable result
variable callback
: async-run
  begin
    result callback async-wait
    callback @ 0= if exit then
    result @ callback @ invoke
  again
;
|;

|section: Putting it together
Some tests are in order.
|: general tests
: test1
    s" ls -l out" [:
      0= assert
      1 s" Hello world!" [:
        drop cr ." And Done!" cr
      ;] async-write
    ;] async-system
    async-run
;
test1
|;

|section: Using Scope
|: general tests
: write-whole-file ( data filename next -- )
    >s >r >r >s >s r> r>
    O_CREAT O_TRUNC or O_WRONLY or rwx [:
      dup 0>= assert
      dup >r s> s> r> >s [:
        drop s> [: s> invoke ;] async-close
      ;] async-write
    ;] async-open
;
: test2
    s" Hello there!" s" out/test1.txt" [:
      ." Written file." cr
    ;] write-whole-file 
    async-run
;
test2
|;

|section: Closures with Conditions
|: general tests
: special-adder dup 8 = if
     drop [: 256 ;]
   else
     >s [: s> + ;]
   then
;
5 4 special-adder invoke 9 assert=
5 8 special-adder invoke 256 assert=
|;

|section: Sugar
|: add sugar
: '[:   postpone ['] postpone [: ; immediate
: ;]'   postpone ;] postpone swap postpone execute ; immediate
|;

|section: Test Sugar
|: general tests
: test3
    s" ls -l out" '[: async-system
      0= assert
      1 s" Sugar Test!" '[: async-write
        drop cr ." And Done with Sugar!" cr
      ;]'
    ;]'
    async-run
;
test3
|;

|section: Question?
Questions?

|chapter: Supplemental Material

This chapter contains things that didn't make sense to include in the
main presentation.

|section: Tangled Output

We would like to generate a tangled (runnable) version of this document.
It should be written to:
|file: events.fs

It will contain everything that is normally run.
|: events.fs
|@ *
|;

|section: Overall Order
|: *
|@ test tools
|@ required headers
|@ worker count
|@ request structure
|@ lock and count
|@ requests queue
|@ results queue
|@ worker implementation
|@ startup routine
|@ enqueue a request
|@ issue requests
|@ implement waiting
|@ relevant constants
|@ forth to c declarations
|@ closures
|@ dispatch events
|@ add sugar
|@ do startup
|@ general tests
|@ do shutdown
|;

|section: Testing Tools
We'll also define some generic test tools.
|: test tools
: assert ( n -- ) 0= if abort then ;
: assert= ( a b -- ) = assert ;
|;

|section: Startup and Shutdown

|: do startup
async-startup
|;

|: do shutdown
async-shutdown
|;

|.
