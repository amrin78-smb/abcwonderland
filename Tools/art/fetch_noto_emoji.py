"""Download placeholder vocabulary/prop pictures from Google's Noto Emoji (Apache-2.0).

Usage: python3 -I Tools/art/fetch_noto_emoji.py <alphabet.json> <out_dir>
Fetches every 'emoji' code in alphabet.json plus the shared prop list below, at 512 px.
Codes prefixed 'draw:' are drawn procedurally by the Unity setup instead.
Records provenance in <out_dir>/SOURCES.txt. Re-runnable: existing files are skipped.
"""
import json, os, sys, urllib.request

BASE = "https://raw.githubusercontent.com/googlefonts/noto-emoji/main/2D/png/512/emoji_u{}.png"
README_URL = "https://raw.githubusercontent.com/googlefonts/noto-emoji/main/README.md"
LICENSE_URL = "https://raw.githubusercontent.com/googlefonts/noto-emoji/main/LICENSE"
# Shared props, scenery and rewards (keys must match ArtKeys.cs).
EXTRA = ["1f388", "1f95a", "1f33c", "1f41a", "2601", "1faa8", "2b50", "1f4a7", "1f451", "1f41b", "1f96d", "1f955",
         "1f333", "1f98b", "1f426", "1f33b", "1f337", "1f344", "1f41e", "1f41d", "1f31f", "1f380", "1f576", "1f3a9",
         "1f9e2", "1f451", "1f3b5", "1f308", "1f30a", "1fab8", "1f420", "1f42c", "1f36d", "1f36c", "1f9c1", "1f369",
         "1f334", "1f996", "1f43e", "1f30b", "1fa90", "1f680", "1f319", "2728", "1f9f8", "1f3e0", "1f6f8", "1f97d",
         "1f3b8", "1f3b7", "1f3ba", "1fa87", "1f941", "1f3b9", "1f60a", "1f3c6", "1f9e9", "1f4d6", "1f3a8"]

def main(src, out):
    os.makedirs(out, exist_ok=True)
    data = json.load(open(src, encoding="utf-8"))
    codes = []
    for l in data["letters"]:
        for v in l["vocabulary"]:
            codes.append(v["emoji"])
        codes.append(l["adventure"]["prop"])
    codes += EXTRA
    codes = sorted({c for c in codes if c and not c.startswith("draw:") and all(ch in "0123456789abcdef_" for ch in c)})
    ok = failed = 0
    for c in codes:
        dst = os.path.join(out, f"emoji_u{c}.png")
        if os.path.exists(dst) and os.path.getsize(dst) > 0:
            ok += 1; continue
        try:
            with urllib.request.urlopen(BASE.format(c), timeout=30) as r:
                body = r.read()
            if not body.startswith(b"\x89PNG"):
                raise ValueError("not a PNG")
            open(dst, "wb").write(body); ok += 1
        except Exception as e:
            failed += 1; print("FAILED", c, e)
    lic = os.path.join(out, "..", "..", "Art", "Vocabulary", "LICENSE-noto-emoji.txt")
    if not os.path.exists(lic):
        with urllib.request.urlopen(LICENSE_URL, timeout=30) as r:
            body = r.read()
        with urllib.request.urlopen(README_URL, timeout=30) as r:
            readme = r.read().decode("utf-8")
        note = readme[readme.find("## License"):readme.find("## Contributing")]
        open(lic, "wb").write(b"=== Licence statement from noto-emoji README.md (fetched by fetch_noto_emoji.py) ===\n" +
                              note.encode("utf-8") + b"\n=== Repository root LICENSE ===\n" + body)
    with open(os.path.join(out, "..", "..", "Art", "Vocabulary", "SOURCES.txt"), "w") as f:
        f.write("Placeholder pictures: Noto Emoji by Google, https://github.com/googlefonts/noto-emoji\n"
                "Licence: README states image resources are Apache-2.0; repository root LICENSE is OFL-1.1 (both in LICENSE-noto-emoji.txt).\n"
                "Both permit commercial use with notice. Confirm with legal review before release (Documentation/ASSET_REGISTER.md).\n"
                "Status: PLACEHOLDER. Replace with commissioned 3D/illustrated art before release.\n")
    print(f"noto: {ok} ok, {failed} failed")
    return 1 if failed else 0

if __name__ == "__main__":
    sys.exit(main(sys.argv[1], sys.argv[2]))
