#!/usr/bin/env python3
import argparse
import os
import subprocess
import sys

# Add venv site-packages to path if needed, or just run within venv.
# Since we are called from ModForge, it's better to use the venv python.

def main():
    print(f"DEBUG voicegen.py received args: {sys.argv}", file=sys.stderr)
    parser = argparse.ArgumentParser()
    parser.add_argument("--engine", required=True)
    parser.add_argument("--text", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--ref-wav")
    parser.add_argument("--ref-text")
    parser.add_argument("--model")
    parser.add_argument("--rvc")
    parser.add_argument("--seed", type=int)
    parser.add_argument("--speed", type=float, default=1.0)
    # Engine-specific knobs: accepted for every engine so callers can always pass them;
    # engines that have no use for one simply ignore it (with a stderr note) instead of crashing.
    parser.add_argument("--exaggeration", type=float, default=None)
    parser.add_argument("--language", default=None)
    args = parser.parse_args()

    engine = args.engine.lower()

    if engine == "f5":
        # F5-TTS only supports speed; exaggeration has no equivalent and the language
        # is inferred from the reference clip/text. Note + ignore rather than fail.
        if args.exaggeration is not None:
            print("NOTE: f5 engine has no exaggeration control; ignoring --exaggeration", file=sys.stderr)
        if args.language is not None and args.language not in ("en", ""):
            print(f"NOTE: f5 engine infers language from the reference; ignoring --language {args.language}", file=sys.stderr)
        try:
            from f5_tts.api import F5TTS
            f5 = F5TTS()

            # F5 auto-transcribes the reference clip (its own ASR) when ref_text is "".
            # We pass "" rather than None so a vanilla-extracted clip needs no hand transcript.
            ref_text = args.ref_text if args.ref_text else ""

            f5.infer(
                ref_file=args.ref_wav,
                ref_text=ref_text,
                gen_text=args.text,
                file_wave=args.out,
                seed=args.seed,
                speed=args.speed
            )
            print(f"Generated: {args.out}")
        except Exception as e:
            print(f"ERROR in F5-TTS: {e}", file=sys.stderr)
            import traceback
            traceback.print_exc()
            sys.exit(1)

    elif engine in ("fish", "fish-s2", "fishspeech", "fish-speech"):
        fish_bin = os.environ.get("MODFORGE_FISH_SPEECH_BIN")
        if not fish_bin:
            print(
                "ERROR: fish-s2 requires MODFORGE_FISH_SPEECH_BIN. Point it at a local Fish Speech "
                "wrapper that accepts --text/--out/--ref-audio/--ref-text and writes a WAV.",
                file=sys.stderr,
            )
            sys.exit(1)

        cmd = [fish_bin, "--text", args.text, "--out", args.out]
        if args.ref_wav:
            cmd += ["--ref-audio", args.ref_wav]
        if args.ref_text:
            cmd += ["--ref-text", args.ref_text]
        if args.model:
            cmd += ["--model", args.model]
        if args.seed is not None:
            cmd += ["--seed", str(args.seed)]
        if args.speed is not None:
            cmd += ["--speed", str(args.speed)]
        if args.exaggeration is not None:
            cmd += ["--exaggeration", str(args.exaggeration)]
        if args.language:
            cmd += ["--language", args.language]

        print(f"    Fish Speech command: {' '.join(cmd)}", file=sys.stderr)
        try:
            result = subprocess.run(cmd, check=False, text=True, capture_output=True)
        except FileNotFoundError:
            print(f"ERROR: MODFORGE_FISH_SPEECH_BIN not found: {fish_bin}", file=sys.stderr)
            sys.exit(1)

        if result.stdout:
            print(result.stdout, file=sys.stderr)
        if result.stderr:
            print(result.stderr, file=sys.stderr)
        if result.returncode != 0:
            print(f"ERROR: Fish Speech wrapper failed with exit code {result.returncode}", file=sys.stderr)
            sys.exit(result.returncode)
        if not os.path.exists(args.out):
            print(f"ERROR: Fish Speech wrapper did not create output WAV: {args.out}", file=sys.stderr)
            sys.exit(1)
        print(f"Generated: {args.out}")

    else:
        print(f"Unknown engine: {args.engine}")
        sys.exit(1)

if __name__ == "__main__":
    main()
