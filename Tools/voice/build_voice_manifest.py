"""Builds the voice manifest (every required voice line) from alphabet.json + static instruction lines.

Usage: python3 -I Tools/voice/build_voice_manifest.py
Writes UnityProject/Assets/ABCWonderworld/Content/voice_manifest.json, read by both the TTS generator and the
Unity ContentBuilder (which attaches clips to the AudioLibrary and reports missing ones).
All synthesised lines are PROVISIONAL. Isolated phoneme clips especially must be re-recorded by a voice actor
and checked by an early-literacy reviewer (Documentation/AUDIO_AND_VOICE_GUIDELINES.md).
"""
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT = os.path.join(ROOT, "UnityProject", "Assets", "ABCWonderworld", "Content")

# Letter names in General American, espeak-style IPA (the Kokoro phoneme alphabet).
NAME_IPA = {"A": "ˈeɪ", "B": "bˈiː", "C": "sˈiː", "D": "dˈiː", "E": "ˈiː", "F": "ˈɛf", "G": "dʒˈiː", "H": "ˈeɪtʃ",
            "I": "ˈaɪ", "J": "dʒˈeɪ", "K": "kˈeɪ", "L": "ˈɛl", "M": "ˈɛm", "N": "ˈɛn", "O": "ˈoʊ", "P": "pˈiː",
            "Q": "kjˈuː", "R": "ˈɑːɹ", "S": "ˈɛs", "T": "tˈiː", "U": "jˈuː", "V": "vˈiː", "W": "dˈʌbəljˌuː",
            "X": "ˈɛks", "Y": "wˈaɪ", "Z": "zˈiː"}

# Beginner sounds. Stops get the shortest possible release (ᵊ), continuants are held, vowels lengthened.
SOUND_IPA = {"A": "ˈææ", "B": "bᵊ", "C": "kᵊ", "D": "dᵊ", "E": "ˈɛɛ", "F": "fff", "G": "ɡᵊ", "H": "hᵊ",
             "I": "ˈɪɪ", "J": "dʒᵊ", "K": "kᵊ", "L": "lll", "M": "mmm", "N": "nnn", "O": "ˈɑɑ", "P": "pᵊ",
             "Q": "kwᵊ", "R": "ɹɹɹ", "S": "sss", "T": "tᵊ", "U": "ˈʌʌ", "V": "vvv", "W": "wᵊ", "X": "ks",
             "Y": "jᵊ", "Z": "zzz"}

CHARACTER_VOICES = ["af_bella", "am_puck", "af_nicole", "am_michael", "af_sky"]

STATIC = [
    ("vo.welcome", "Welcome to ABC Wonderworld!"),
    ("vo.lets_play", "Let's play!"),
    ("vo.choose_world", "Pick a world to explore!"),
    ("vo.choose_activity", "What shall we play?"),
    ("vo.this_is_letter", "This is the letter"),
    ("vo.big", "Big"),
    ("vo.small", "Small"),
    ("vo.says", "says"),
    ("vo.like", "like"),
    ("vo.like_end_of", "like at the end of"),
    ("vo.tap_pictures", "Tap the pictures to hear their names!"),
    ("vo.can_you_find", "Can you find"),
    ("vo.find_big", "Can you find the big letter"),
    ("vo.find_small", "Can you find the small letter"),
    ("vo.find_sound", "Which letter makes the sound"),
    ("vo.you_found_it", "You found it!"),
    ("vo.great_job", "Great job!"),
    ("vo.hooray", "Hooray!"),
    ("vo.wonderful", "Wonderful!"),
    ("vo.you_did_it", "You did it!"),
    ("vo.lets_look_again", "Let's look again!"),
    ("vo.try_another", "Hmm, try another one!"),
    ("vo.here_it_is", "Look! Here it is!"),
    ("vo.trace_intro", "Put your finger on the green dot, and follow the path!"),
    ("vo.trace_start_here", "Start at the green dot!"),
    ("vo.trace_keep_going", "Keep going!"),
    ("vo.trace_great", "Beautiful tracing!"),
    ("vo.trace_now_small", "Now let's trace the small letter!"),
    ("vo.match_pictures", "Match each picture to its letter!"),
    ("vo.match_case", "Match the big letter to the small letter!"),
    ("vo.match_tip", "Tap a picture, then tap where it goes!"),
    ("vo.puzzle_intro", "Let's finish the puzzle!"),
    ("vo.puzzle_missing", "Which letter is missing?"),
    ("vo.music_intro", "Let's sing the alphabet song!"),
    ("vo.music_play", "Tap the bells to make music!"),
    ("vo.free_play", "Let's explore! Tap anything you like."),
    ("vo.act_meet", "Meet the letter!"),
    ("vo.act_trace", "Let's trace!"),
    ("vo.act_find", "Find the letter!"),
    ("vo.act_match", "Let's match!"),
    ("vo.act_adventure", "Adventure time!"),
    ("vo.act_puzzle", "Puzzle time!"),
    ("vo.star", "You got a star!"),
    ("vo.sticker", "A new sticker for your book!"),
    ("vo.grown_ups", "This part is for grown-ups."),
    ("vo.break", "You played so well! Time for a little rest?"),
    ("vo.alphabet_book", "Your alphabet book!"),
]


def main():
    data = json.load(open(os.path.join(CONTENT, "alphabet.json"), encoding="utf-8"))
    lines = [{"key": k, "text": t, "kind": "instruction"} for k, t in STATIC]
    for w in data["worlds"]:
        lines.append({"key": f"world.{w['id']}.name", "text": w["name"] + "!", "kind": "world"})
    for i, l in enumerate(data["letters"]):
        up, lo = l["upper"], l["lower"]
        lines.append({"key": f"letter.{lo}.name", "text": l["nameSpoken"], "phonemes": NAME_IPA[up], "kind": "letter-name"})
        lines.append({"key": f"letter.{lo}.sound", "text": l["phoneme"], "phonemes": SOUND_IPA[up], "kind": "phoneme",
                      "note": "Synthesised isolated sound: MUST be re-recorded and reviewed."})
        ch = l["character"]
        lines.append({"key": f"letter.{lo}.intro", "text": f"Hi! I'm {ch['name']}!", "kind": "character",
                      "voice": CHARACTER_VOICES[i % len(CHARACTER_VOICES)]})
        for v in l["vocabulary"]:
            vid = "vocab_" + v["word"].lower().replace(" ", "_").replace("-", "_")
            text = "X-ray" if v["word"] == "x-ray" else ("Yo-yo" if v["word"] == "yo-yo" else v["word"].capitalize())
            lines.append({"key": f"vocab.{vid}", "text": text + "!", "kind": "vocabulary"})
        lines.append({"key": f"adventure.{lo}.instruction", "text": l["adventure"]["instruction"], "kind": "instruction"})
    out = os.path.join(CONTENT, "voice_manifest.json")
    json.dump({"_readme": "Generated by Tools/voice/build_voice_manifest.py. Do not edit by hand.",
               "language": "en", "narrator": "af_heart", "speed": 0.88, "lines": lines},
              open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"voice manifest: {len(lines)} lines -> {out}")


if __name__ == "__main__":
    main()
