

: assert ( n -- ) 0= if abort then ;
: assert= ( a b -- ) = assert ;


\c #include <assert.h>
\c #include <fcntl.h>
\c #include <pthread.h>
\c #include <stdio.h>
\c #include <stdlib.h>
\c #include <string.h>
\c #include <unistd.h>


\c #define WORKERS 10
\c static pthread_t g_worker_pool[WORKERS];


\c typedef union {
\c   int number;
\c   void *pointer;
\c } VARIANT;
\c
\c typedef struct _REQUEST {
\c   struct _REQUEST *next;
\c   enum {
       
\c     SHUTDOWN,

\c     OPEN,

\c     CLOSE,

\c     READ,

\c     WRITE,

\c     SYSTEM,

\c   } operation;
\c   VARIANT args[4];
\c   void *callback;
\c   int result;
\c } REQUEST;


\c static pthread_mutex_t g_lock;
\c static int g_pending_count;


\c static pthread_cond_t g_requests_ready;
\c static REQUEST *g_requests_head;
\c static REQUEST *g_requests_tail;


\c static pthread_cond_t g_results_ready;
\c static REQUEST *g_results_head;
\c static REQUEST *g_results_tail;


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

       
\c     switch (req->operation) {
       
\c     case SHUTDOWN:
\c       free(req);
\c       return;

\c     case OPEN:
\c       tmp = malloc(req->args[1].number + 1);
\c       assert(tmp);
\c       memcpy(tmp, req->args[0].pointer, req->args[1].number);
\c       tmp[req->args[1].number] = 0;
\c       req->result = open(tmp, req->args[2].number, req->args[3].number);
\c       free(tmp);
\c       break;

\c     case CLOSE:
\c       req->result = close(req->args[0].number);
\c       break;

\c     case READ:
\c       req->result = read(req->args[0].number, req->args[1].pointer,
\c                          req->args[2].number);
\c       break;

\c     case WRITE:
\c       req->result = write(req->args[0].number, req->args[1].pointer,
\c                           req->args[2].number);
\c       break;

\c     case SYSTEM:
\c       tmp = malloc(req->args[1].number + 1);
\c       assert(tmp);
\c       memcpy(tmp, req->args[0].pointer, req->args[1].number);
\c       tmp[req->args[1].number] = 0;
\c       req->result = system(tmp);
\c       free(tmp);
\c       break;

\c     default:
\c       assert(0);
\c       break;
\c     }

       
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

\c   for (i = 0; i < WORKERS; ++i) {
\c     if (pthread_create(&g_worker_pool[i], NULL, Worker, NULL)) {
\c       assert(0);
\c     }
\c   }

\c }


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

\c void async_close(int fd, void *callback) {
\c   REQUEST *req;
\c   req = (REQUEST*) calloc(1, sizeof(REQUEST));
\c   assert(req);
\c   req->operation = CLOSE;
\c   req->args[0].number = fd;
\c   req->callback = callback;
\c   async_request_enqueue(req);
\c }

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


\c #define DEFINT(name) int name##_int(void) { return name; }
\c DEFINT(O_CREAT)
\c DEFINT(O_TRUNC)
\c DEFINT(O_WRONLY)
\c DEFINT(O_RDONLY)
c-function O_CREAT O_CREAT_int -- n
c-function O_TRUNC O_TRUNC_int -- n
c-function O_WRONLY O_WRONLY_int -- n
c-function O_RDONLY O_RDONLY_int -- n

: octal 8 base ! ;
octal
777 constant rwx
decimal


c-function async-startup async_startup -- void

c-function async-shutdown async_shutdown -- void

c-function async-open async_open a n n n a -- void

c-function async-close async_close n a -- void

c-function async-read async_read n a n a -- void

c-function async-write async_write n a n a -- void

c-function async-system async_system a n a -- void

c-function async-wait async_wait a a -- void



4 constant colon-sys-size

: colon-sys-drop ( colon-sys -- ) colon-sys-size 0 do drop loop ;


colon-sys-size 20 * constant scope-cells
: scope-alloc ( -- s) scope-cells allocate 0= assert
              1 cells over ! ;
variable myscope
scope-alloc myscope !

: scope+! ( n -- ) cells myscope @ +! ;
: scope-ptr ( -- n ) myscope @ @ myscope @ + ;
: >s ( n -- ) scope-ptr !  1 scope+! ;
: s> ( -- n ) -1 scope+!  scope-ptr @ ;

: scope. ( s -- ) ." scope(" dup @ cell / 1- . ." ) "
    dup @ cell ?do dup i + @ . cell +loop drop cr ;

: scope-clone ( s -- s' )
    scope-alloc dup >r scope-cells cmove r>
;
: scope-free ( s -- ) free 0= assert ;


: :noname2 ( -- xt )
    :noname colon-sys-drop ;


: bind ( xt -- closure )
    >s myscope @ scope-clone s> drop
;
: invoke ( closure -- )
    myscope @ >r ( leak ) scope-clone myscope !
    s> execute
    myscope @ scope-free r> myscope !
;


: [:   postpone ahead postpone exit
       postpone [ :noname2 >s ; immediate
: ;]   postpone exit postpone then
       s> postpone literal postpone bind ; immediate



variable result
variable callback
: async-run
  begin
    result callback async-wait
    callback @ 0= if exit then
    result @ callback @ invoke
  again
;


async-startup


: scope-test 1 [: 2 [: 3 ;] 4 ;] 5 ;
scope-test 5 assert=
invoke 4 assert=
invoke 3 assert=
2 assert=
1 assert=

: test-adder >s [: s> + ;] ;
5 4 test-adder invoke 9 assert=

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

: special-adder dup 8 = if
     drop [: 256 ;]
   else
     >s [: s> + ;]
   then
;
5 4 special-adder invoke 9 assert=
5 8 special-adder invoke 256 assert=


async-shutdown



