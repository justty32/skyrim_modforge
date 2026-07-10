/* Inject a DirectInput scancode into the game's own Wine input queue.
 *
 * Why this exists: nothing on the Linux side can press a key in Skyrim.
 * The Wayland compositor refuses XTest (xdotool's mousemove does not even
 * move the pointer), and Skyrim reads raw input, so synthetic X events are
 * ignored too. Run inside the game's wineserver via protontricks-launch,
 * SendInput() feeds the same queue dinput8 reads from.
 *
 * SkyLink covers state queries and console commands, which execute inside the
 * game process and bypass input entirely. This covers the remaining hole:
 * SKSE plugins whose only trigger is a hotkey.
 *
 * Built by skylink-bridge.sh; see workflows/skylink/bridge.md.
 */
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char** argv) {
    /* DirectInput scancode, NOT a virtual-key code. 0x44 = F10. */
    WORD scan = (argc > 1) ? (WORD)strtol(argv[1], NULL, 16) : 0x44;
    /* Give the caller time to raise/focus the game window. */
    DWORD delay = (argc > 2) ? (DWORD)atoi(argv[2]) : 1500;
    Sleep(delay);

    INPUT in[2];
    ZeroMemory(in, sizeof(in));
    in[0].type = INPUT_KEYBOARD;
    in[0].ki.wVk = 0;
    in[0].ki.wScan = scan;
    in[0].ki.dwFlags = KEYEVENTF_SCANCODE;
    in[1] = in[0];
    in[1].ki.dwFlags |= KEYEVENTF_KEYUP;

    UINT down = SendInput(1, &in[0], sizeof(INPUT));
    Sleep(80);
    UINT up = SendInput(1, &in[1], sizeof(INPUT));

    printf("sendkey scan=0x%02X down=%u up=%u\n", scan, down, up);
    fflush(stdout);
    return (down && up) ? 0 : 1;
}
