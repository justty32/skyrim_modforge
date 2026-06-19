#!/usr/bin/env python3
"""Generate the zh-TW HTML doc bundle from the canonical Markdown in docs/zh-TW/.

Each page is a thin shell: the *current* Markdown is embedded verbatim in a hidden
<textarea> and rendered client-side by marked.js. The only transform applied is
rewriting in-doc `*.md` links to their flat `.html` equivalents — so the rendered
bundle can never drift from the source docs. Re-run after editing any zh-TW markdown:

    python docs/zh-TW/html/generate.py

The page set mirrors the semantic Markdown split 1:1 (SPEC = index + 11 parts under
docs/zh-TW/spec/, cookbook = index + 6 parts), tracking the EN docs/ structure.
"""
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent   # docs/zh-TW/html
ZH = HERE.parent                         # docs/zh-TW

# Ordered sections -> pages. Each page: (markdown path relative to docs/zh-TW, output .html, nav label).
SECTIONS = [
    ("代理工作流程", [
        ("for_agent.md",               "for-agent.html",               "概覽"),
        ("for_agent_cli.md",           "for-agent-cli.html",           "CLI 驅動"),
        ("for_agent_lib.md",           "for-agent-lib.html",           "函式庫 API"),
        ("external_assets.md",         "external-assets.html",         "外部資源"),
        ("engine-internals.md",        "engine-internals.html",        "引擎內部原理"),
        ("local-skyrim-extraction.md", "local-skyrim-extraction.html", "本地 Skyrim 抽取"),
    ]),
    ("規格說明（SPEC）", [
        ("spec/SPEC-index.md",       "spec-index.html",       "總覽 · 目錄"),
        ("spec/SPEC-intro.md",       "spec-intro.html",       "介紹 · 記錄類型"),
        ("spec/SPEC-magic.md",       "spec-magic.html",       "魔法 · 附魔"),
        ("spec/SPEC-dialogue.md",    "spec-dialogue.html",    "對話 · 場景"),
        ("spec/SPEC-quests.md",      "spec-quests.html",      "任務 · Story Manager"),
        ("spec/SPEC-identities.md",  "spec-identities.html",  "身分系統"),
        ("spec/SPEC-world.md",       "spec-world.html",       "Cell · 放置 · 光照"),
        ("spec/SPEC-worldspaces.md", "spec-worldspaces.html", "世界空間 · 清單 · 商販"),
        ("spec/SPEC-items.md",       "spec-items.html",       "配方 · 天賦 · 資源"),
        ("spec/SPEC-packages.md",    "spec-packages.html",    "AI 套件 · 天氣"),
        ("spec/SPEC-animation.md",   "spec-animation.html",   "動作系統 · OAR/BDI/PIE"),
        ("spec/SPEC-distribution.md","spec-distribution.html","SKSE 分發器 · SPID/MCM"),
        ("spec/SPEC-workflow.md",    "spec-workflow.html",    "工作流程"),
        ("spec/SPEC-refs.md",        "spec-refs.html",        "$ref · 參數化"),
    ]),
    ("讓 NPC 更有生命力", [
        ("lifelike/README.md",                "lifelike.html",             "總覽"),
        ("lifelike/cookbook-index.md",        "cookbook-index.html",       "食譜手冊 · 目錄"),
        ("lifelike/cookbook-npc-basics.md",   "cookbook-npc-basics.html",  "食譜：基礎 NPC"),
        ("lifelike/cookbook-followers.md",    "cookbook-followers.html",   "食譜：跟隨者"),
        ("lifelike/cookbook-world-items.md",  "cookbook-world-items.html", "食譜：世界 · 物品"),
        ("lifelike/cookbook-presets.md",      "cookbook-presets.html",     "食譜：預設片段"),
        ("lifelike/cookbook-magic.md",        "cookbook-magic.html",       "食譜：魔法"),
        ("lifelike/cookbook-social-quest.md", "cookbook-social-quest.html","食譜：社交 · 任務"),
        ("lifelike/cookbook-advanced.md",     "cookbook-advanced.html",    "食譜：進階"),
        ("lifelike/cheatsheets.md",           "cheatsheets.html",          "速查表"),
        ("lifelike/gotchas.md",               "gotchas.html",              "常見陷阱"),
        ("lifelike/formid-reference.md",      "formid-ref.html",           "原版 FormID 參考"),
    ]),
]

# basename (without .md) -> output .html, for link rewriting (links may use ../ or lifelike/ prefixes).
MD_TO_HTML = {}
for _title, pages in SECTIONS:
    for md_rel, html_name, _label in pages:
        MD_TO_HTML[Path(md_rel).stem] = html_name

LINK_RE = re.compile(r"\]\((?P<url>[^)\s]+)\)")


def rewrite_links(md_text):
    """Rewrite `](something.md#anchor)` -> `](flat.html#anchor)`; leave non-.md links alone."""
    def repl(m):
        url = m.group("url")
        if ".md" not in url:
            return m.group(0)
        anchor = ""
        if "#" in url:
            url, anchor = url.split("#", 1)
            anchor = "#" + anchor
        if not url.endswith(".md"):
            return m.group(0)
        stem = Path(url).stem  # strips ../, lifelike/, .md
        html = MD_TO_HTML.get(stem)
        if html is None:
            print(f"  ! unmapped link target: {url}", file=sys.stderr)
            return m.group(0)
        return f"]({html}{anchor})"
    return LINK_RE.sub(repl, md_text)


def first_heading(md_text):
    for line in md_text.splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return "ModForge 文件"


def sidebar(active_html):
    rows = ['  <div class="logo"><a href="index.html">📖 ModForge 文件</a></div>']
    for title, pages in SECTIONS:
        rows.append(f'  <div class="nav-section">{title}</div>')
        for _md, html_name, label in pages:
            cls = "nav-item active" if html_name == active_html else "nav-item"
            rows.append(f'  <a class="{cls}" href="{html_name}">{label}</a>')
    return '<nav class="sidebar">\n' + "\n".join(rows) + "\n</nav>"


PAGE_TMPL = """<!DOCTYPE html>
<html lang="zh-TW">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>{title} — ModForge 文件</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
{sidebar}
<div class="content"><div class="content-inner"><div id="md" class="md"></div></div></div>
<textarea id="src" hidden>
{markdown}
</textarea>
<script src="https://cdn.jsdelivr.net/npm/marked@4/marked.min.js"></script>
<script>
marked.setOptions({{gfm:true,breaks:false}});
document.getElementById('md').innerHTML=marked.parse(document.getElementById('src').value);
document.querySelectorAll('.nav-item').forEach(function(a){{
  var p=location.pathname.split('/').pop()||'index.html';
  if(a.getAttribute('href')===p)a.classList.add('active');
}});
</script>
</body>
</html>
"""

INDEX_TMPL = """<!DOCTYPE html>
<html lang="zh-TW">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>ModForge 文件 — 繁體中文</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
{sidebar}
<div class="content">
<div class="content-inner">
  <h1 style="font-size:2em;font-weight:700;color:#1a1a2e;margin-bottom:12px">ModForge 文件</h1>
  <p class="page-intro">
    ModForge 是一個 Skyrim 外掛產生器。你（AI 代理或開發者）撰寫 JSON 規格，
    工具輸出有效的 <code style="background:#f0f0f5;border:1px solid #d8d9e0;padding:1px 5px;border-radius:4px;font-family:monospace;color:#7c3aed">.esp</code>/<code style="background:#f0f0f5;border:1px solid #d8d9e0;padding:1px 5px;border-radius:4px;font-family:monospace;color:#7c3aed">.esl</code>。
    本文件為繁體中文翻譯版本，由 <code style="background:#f0f0f5;border:1px solid #d8d9e0;padding:1px 5px;border-radius:4px;font-family:monospace;color:#7c3aed">generate.py</code> 從 <code style="background:#f0f0f5;border:1px solid #d8d9e0;padding:1px 5px;border-radius:4px;font-family:monospace;color:#7c3aed">docs/zh-TW/*.md</code> 自動產生。
  </p>

  <div class="card-grid">
{cards}
  </div>
</div>
</div>
<script>
document.querySelectorAll('.nav-item').forEach(function(a){{
  var p=location.pathname.split('/').pop()||'index.html';
  if(a.getAttribute('href')===p)a.classList.add('active');
}});
</script>
</body>
</html>
"""

SECTION_ICONS = {
    "代理工作流程": "🤖",
    "規格說明（SPEC）": "📋",
    "讓 NPC 更有生命力": "🧑‍🤝‍🧑",
}


def build():
    produced = set()

    for title, pages in SECTIONS:
        for md_rel, html_name, _label in pages:
            md_path = ZH / md_rel
            md_text = md_path.read_text(encoding="utf-8")
            md_text = rewrite_links(md_text)
            html = PAGE_TMPL.format(
                title=first_heading(md_text),
                sidebar=sidebar(html_name),
                markdown=md_text.rstrip("\n"),
            )
            (HERE / html_name).write_text(html, encoding="utf-8")
            produced.add(html_name)
            print(f"  + {html_name}  <-  zh-TW/{md_rel}")

    # index.html — landing cards built from the same section table
    cards = []
    for title, pages in SECTIONS:
        icon = SECTION_ICONS.get(title, "📄")
        items = "\n".join(
            f'        <li><a href="{html_name}">{label}</a></li>'
            for _md, html_name, label in pages
        )
        cards.append(
            f'    <div class="card">\n'
            f'      <h3>{icon} {title}</h3>\n'
            f'      <ul>\n{items}\n      </ul>\n'
            f'    </div>'
        )
    (HERE / "index.html").write_text(
        INDEX_TMPL.format(sidebar=sidebar("index.html"), cards="\n\n".join(cards)),
        encoding="utf-8",
    )
    produced.add("index.html")
    print("  + index.html")

    # Remove stale generated pages (e.g. the old numbered spec-1..3 / cookbook-1..4).
    for f in HERE.glob("*.html"):
        if f.name not in produced:
            f.unlink()
            print(f"  - removed stale {f.name}")

    print(f"\nDone: {len(produced)} pages generated.")


if __name__ == "__main__":
    build()
