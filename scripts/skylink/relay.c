/* TCP <-> Win32 named pipe relay. Runs inside the game's Wine/Proton prefix. */
#include <winsock2.h>
#include <windows.h>
#include <stdio.h>

typedef struct { SOCKET s; HANDLE p; } Ctx;

static DWORD WINAPI sock_to_pipe(LPVOID a) {
    Ctx *c = (Ctx*)a; char buf[8192]; int n; DWORD w;
    while ((n = recv(c->s, buf, sizeof buf, 0)) > 0)
        if (!WriteFile(c->p, buf, n, &w, NULL)) break;
    return 0;
}
static DWORD WINAPI pipe_to_sock(LPVOID a) {
    Ctx *c = (Ctx*)a; char buf[8192]; DWORD n;
    while (ReadFile(c->p, buf, sizeof buf, &n, NULL) && n > 0)
        if (send(c->s, buf, n, 0) <= 0) break;
    return 0;
}

int main(int argc, char **argv) {
    const char *pipe_name = argc > 1 ? argv[1] : "\\\\.\\pipe\\SkyrimMCP";
    int port = argc > 2 ? atoi(argv[2]) : 8770;

    WSADATA wsa; WSAStartup(MAKEWORD(2,2), &wsa);
    SOCKET ls = socket(AF_INET, SOCK_STREAM, 0);
    int opt = 1; setsockopt(ls, SOL_SOCKET, SO_REUSEADDR, (char*)&opt, sizeof opt);
    struct sockaddr_in a = {0};
    a.sin_family = AF_INET; a.sin_port = htons(port);
    a.sin_addr.s_addr = inet_addr("127.0.0.1");
    if (bind(ls, (struct sockaddr*)&a, sizeof a) || listen(ls, 4)) {
        fprintf(stderr, "relay: bind/listen failed on port %d\n", port); return 1;
    }
    fprintf(stderr, "relay: listening 127.0.0.1:%d -> %s\n", port, pipe_name);
    fflush(stderr);

    for (;;) {
        SOCKET cs = accept(ls, NULL, NULL);
        if (cs == INVALID_SOCKET) continue;
        HANDLE p = CreateFileA(pipe_name, GENERIC_READ|GENERIC_WRITE, 0, NULL,
                               OPEN_EXISTING, 0, NULL);
        if (p == INVALID_HANDLE_VALUE) {
            fprintf(stderr, "relay: pipe not available (err %lu)\n", GetLastError());
            fflush(stderr);
            closesocket(cs); continue;
        }
        DWORD mode = PIPE_READMODE_BYTE;
        SetNamedPipeHandleState(p, &mode, NULL, NULL);
        fprintf(stderr, "relay: client connected\n"); fflush(stderr);

        Ctx c = { cs, p };
        HANDLE t1 = CreateThread(NULL,0,sock_to_pipe,&c,0,NULL);
        HANDLE t2 = CreateThread(NULL,0,pipe_to_sock,&c,0,NULL);
        HANDLE ts[2] = { t1, t2 };
        WaitForMultipleObjects(2, ts, FALSE, INFINITE);
        shutdown(cs, SD_BOTH); closesocket(cs); CloseHandle(p);
        WaitForMultipleObjects(2, ts, TRUE, 2000);
        CloseHandle(t1); CloseHandle(t2);
        fprintf(stderr, "relay: client disconnected\n"); fflush(stderr);
    }
}
