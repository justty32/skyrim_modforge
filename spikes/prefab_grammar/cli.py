"""Command-line interface for deterministic prefab grammar generation."""
from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Optional, Sequence

PACKAGE_DIR = Path(__file__).resolve().parent
SPIKES_DIR = PACKAGE_DIR.parent
if str(SPIKES_DIR) not in sys.path:
    sys.path.insert(0, str(SPIKES_DIR))

from prefab_grammar.generator import GenerationError, GeneratorOptions, generate_layout
from prefab_grammar.schema import SchemaError, dump_layout, load_prefab_library


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Generate a deterministic prefab layout.")
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument("--prefabs", type=Path, default=PACKAGE_DIR / "data" / "prefabs")
    parser.add_argument("--rooms", type=int, default=6)
    parser.add_argument("--hall-length", type=int, default=2)
    parser.add_argument("--max-blocks", type=int, default=30)
    parser.add_argument("--out", type=Path)
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = _parser().parse_args(argv)
    try:
        prefabs = load_prefab_library(args.prefabs)
        layout = generate_layout(
            prefabs,
            GeneratorOptions(
                seed=args.seed,
                rooms=args.rooms,
                hall_length=args.hall_length,
                max_blocks=args.max_blocks,
            ),
        )
        output = dump_layout(layout)
        if args.out is None:
            # Write raw bytes: a text-mode stdout on Windows translates every LF
            # into CRLF, so a piped CLI run would no longer be byte-identical to
            # --out, nor to dump_layout() itself.
            sys.stdout.buffer.write(output.encode("utf-8"))
            sys.stdout.buffer.flush()
        else:
            args.out.write_text(output, encoding="utf-8", newline="\n")
        return 0
    except SchemaError as exc:
        print(f"schema error: {exc}", file=sys.stderr)
        return 2
    except GenerationError as exc:
        print(f"generation error: {exc}", file=sys.stderr)
        return 3
    except Exception as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
