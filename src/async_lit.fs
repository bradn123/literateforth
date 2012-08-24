0 s" literate_lit.fs" included

|title: Asynchronous Programming in Forth
|author: Brad Nelson
|document-base: async

|chapter: Introduction

|section: Overview

|chapter: Asynchronous System I/O

|section: Overview
As Forth's built-in I/O primitives are either blocking or polling based,
we will need to define some other primitives. With gforth, we are able to
define additional functionality in C.

|section: The Basics

|: required headers
\c #include <assert.h>
\c #include <fcntl.h>
\c #include <pthread.h>
\c #include <stdio.h>
\c #include <stdlib.h>
\c #include <string.h>
\c #include <unistd.h>
|;

Some standard constants will be brought over from C.
|: relevant constants
c-function O_CREAT O_CREAT -- n
c-function O_TRUNC O_TRUNC -- n
c-function O_WRONLY O_WRONLY -- n
c-function O_RDONLY O_RDONLY -- n
|;

Others declared to avoid switching base (for permissions).

|: relevant constants
: octal 8 base ! ;
octal
777 constant 0777
decimal
|;

|chapter: Requests

|: Request Structure
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
\c   VARIANT args[10];
\c   int callback;
\c   int result;
\c } REQUEST;
|;

|section: Shutdown
|: event types
\c     SHUTDOWN,
|;
|: forth to c declarations
c-function async-shutdown async_shutdown -- void
|;
|: handle requests
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
c-function async-open async_open a n n n n -- void
|;
|: handle requests
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
\c                 int oflag, int mode, int callback) {
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
c-function async-close async_close n n -- void
|;
|: handle requests
\c     case CLOSE:
\c       req->result = close(req->args[0].number);
\c       break;
|;
|: issue requests
\c void async_close(int fd, int callback) {
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
c-function async-read async_read n a n n -- void
|;
|: handle requests
\c     case READ:
\c       req->result = read(req->args[0].number, req->args[1].pointer,
\c                          req->args[2].number);
\c       break;
|;
|: issue requests
\c void async_read(int fd, void *buf, int len, int callback) {
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
c-function async-write async_write n a n n -- void
|;
|: handle requests
\c     case WRITE:
\c       req->result = write(req->args[0].number, req->args[1].pointer,
\c                           req->args[2].number);
\c       break;
|;
|: issue requests
\c void async_write(int fd, void *buf, int len, int callback) {
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
c-function async-system async_system a n n -- void
|;
|: handle requests
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
\c void async_system(char *cmd, int cmd_len, int callback) {
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

|chapter: Workers and Queues

|section: Workers

A number of worker threads will be started so that they can block on pending
requests. We will for now assume a fixed number of workers.
|: worker count
\c #define WORKERS 10
\c static pthread_t g_worker_pool[WORKERS];
|;

The workers are started when the system is initialized.
|: start all workers
\c   for (i = 0; i < WORKERS; ++i) {
\c     if (pthread_create(&g_worker_pool[i], NULL, Worker, NULL)) {
\c       assert(0);
\c     }
\c   }
|;

A worker draws requests from a single queue, executes it, then posts the result
to another queue.

|: worker implementation
\c void *Worker(void *arg) {
\c   REQUEST *req;
\c   char *tmp;
\c
\c   for (;;) {
\c     pthread_mutex_lock(&g_lock);
\c     while (!g_requests_head) {
\c       pthread_cond_wait(&g_requests_ready, &g_lock);
\c     }
\c     req = g_requests_head;
\c     g_requests_head = req->next;
\c     if (!g_requests_head) { g_requests_tail = 0; }
\c     pthread_mutex_unlock(&g_lock);
\c
\c     switch (req->operation) {
|@ handle requests
\c     default:
\c       assert(0);
\c       break;
\c     }
\c
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
\c   }
\c }
|;

|section: Queues

Two queues are involved in the system.
Guarded by a single lock so that a single pending count for the complete
pipeline can be kept.
|: lock and count
\c static pthread_mutex_t g_lock;
\c static int g_pending_count;
|;

One to receive pending requests.
|: requests queue
\c static pthread_cond_t g_requests_ready;
\c static REQUEST *g_requests_head;
\c static REQUEST *g_requests_tail;
|;

Another to gather processed requests for processing in the main event loop.
|: results queue
\c static pthread_cond_t g_results_ready;
\c static REQUEST *g_results_head;
\c static REQUEST *g_results_tail;
|;

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

Waiting then occurs on the main thread.
|: implement waiting
\c void async_wait(int *result, int *callback) {
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


|: *
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

variable result
variable callback

: async-run
  begin
    result callback async-wait
\    result @ . callback @ . cr
    callback @ 0=
  until
;

: test
    async-startup
\    10 0 do s" ls >/dev/null" i 1+ async-system loop
\    s" test1.txt" O_CREAT O_TRUNC or O_WRONLY or 0777 1234 async-open
\    1 s" Hello world!" 5555 async-write
    async-run
    async-shutdown
;

test
bye
|;

|.
