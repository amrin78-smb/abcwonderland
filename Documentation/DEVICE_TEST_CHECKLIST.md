# Device Test Checklist (dev APK 0.1.x)

Install: open the GitHub Release link on the phone → download `ABCWonderworld-dev-*.apk` → allow "Install unknown apps" for the browser → install. (Or `adb install -r`.)
The Parent Zone is the small lock top-right on Home: **hold ~2 s**, then tap the two numbers it asks for. Settings → Graphics shows Auto/Low/Medium/High (Auto should pick **Low on the Redmi**, **High on the S25 Ultra**).

Note for each phone: model, Android version, does it launch, rough smoothness (and any hitches), heat/battery after 15 min.

## Flow
- [ ] Launches (also relaunch a 2nd and 3rd time — the Redmi PowerVR Vulkan crash from Little Burger World must NOT recur)
- [ ] Home: characters animate, Play / Book / Music buttons speak and work, star counter
- [ ] World map → each of the 5 worlds loads; tap scenery (trees, flowers, critters) — everything reacts
- [ ] Tap a character → greeting + 6 activity buttons

## Activities (try a few letters, incl. B, I, Q, X, Y)
- [ ] Meet the Letter: name and sound are different; tap glyphs + pictures
- [ ] Tracing: finger follows the path; wobbly tracing still completes; can't skip around O; small letter after big
- [ ] Find the Letter: wrong taps give gentle help, never a "fail"
- [ ] Match: both dragging and tap-then-tap work
- [ ] Puzzle: jigsaw pieces snap (drag or tap), missing letter
- [ ] Adventures: one per world and archetype (B balloons, E water, H house, K kite, P penguin, T turtle, X xylophone, Y yo-yo)
- [ ] Exit an activity: the Home button needs a **hold**; a quick tap does nothing

## Android-specific
- [ ] Back button / edge-swipe inside an activity does nothing; on Home it does not close the app
- [ ] Notch / punch-hole: no buttons hidden under the camera (S25 Ultra) in both landscape directions
- [ ] Home button to background mid-activity, return → still works, progress kept
- [ ] Force-close (recents swipe) and relaunch → stars/stickers/progress kept
- [ ] Airplane mode: everything works
- [ ] Volume: Parent Zone sliders + Music on/off; voice is never drowned by music

## Toddler stress
- [ ] Mash buttons quickly, palm on screen, tap during loading screens — no double navigation, no stuck screens
- [ ] Two fingers at once (v0.1.1+): rest a palm while tracing / dragging a puzzle or match piece — only the first finger acts; the hold-Home button still works
- [ ] Leave the ABC song playing without touching: screen does not dim during the song
