#!/usr/bin/env python3
import argparse
import os
import sys
import torch
import torchaudio

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

    if args.engine == "f5":
        # F5-TTS only supports speed; exaggeration has no equivalent and the language
        # is inferred from the reference clip/text. Note + ignore rather than fail.
        if args.exaggeration is not None:
            print("NOTE: f5 engine has no exaggeration control; ignoring --exaggeration", file=sys.stderr)
        if args.language is not None and args.language not in ("en", ""):
            print(f"NOTE: f5 engine infers language from the reference; ignoring --language {args.language}", file=sys.stderr)
        try:
            from f5_tts.api import F5TTS
            f5 = F5TTS()
            
            f5.infer(
                ref_file=args.ref_wav,
                ref_text=args.ref_text,
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
        
    else:
        print(f"Unknown engine: {args.engine}")
        sys.exit(1)

if __name__ == "__main__":
    main()
