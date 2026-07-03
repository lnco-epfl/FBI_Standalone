#!/usr/bin/env python3
"""
Generator for FBI Standalone sequence YAML files.

Full-Body Illusion (Baseline + Post) x Meditative Body-Perspective Manipulation

Why this exists:
  - startTime bookkeeping is done automatically (next start = previous start
    + previous duration + gap). Change one duration and everything downstream
    re-times itself.
  - Repeated blocks (FBI 2x2 trials, question batteries, settle+interpolation
    camera phases) are defined once and reused.
  - The FBI condition order can be counterbalanced from the command line
    instead of hand-editing the YAML.

Usage:
    python generate_fbi_sequence.py                          # default order, prints to stdout
    python generate_fbi_sequence.py -o my_sequence.yaml
    python generate_fbi_sequence.py --order back-sync,front-async,front-sync,back-async
    python generate_fbi_sequence.py --participant 7          # Latin-square order from ID
    python generate_fbi_sequence.py --post-order front-async,back-sync,back-async,front-sync

Requires: PyYAML  (pip install pyyaml)
"""

import argparse
import sys
from pathlib import Path

import yaml


# ============================================================================
# Timeline: a sequence builder that tracks the clock for you
# ============================================================================

class Timeline:
    """Accumulates steps and computes startTime values.

    Default rule:
      - Steps WITH a duration (wait, displayCameras, displayText, ...):
        the next step starts exactly when they end (no gap).
      - Steps WITHOUT a duration (likert, question, loadScene, loadConfig):
        the next step starts 1 s later (blocking steps need a placeholder).
      - LSL markers consume NO time at all (see Timeline.marker).
    """

    DEFAULT_GAP = 1.0

    def __init__(self, gap=DEFAULT_GAP):
        self.items = []          # ("comment", text) or ("step", dict)
        self.t = 0.0
        self.gap = gap

    # ---- comments -----------------------------------------------------
    def section(self, title):
        bar = "=" * 73
        self.items.append(("comment", f"{bar}\n# {title}\n# {bar}"))

    def subsection(self, title):
        self.items.append(("comment", f"--- {title} ---"))

    def note(self, text):
        self.items.append(("comment", text))

    # ---- steps ---------------------------------------------------------
    def add(self, step, gap=None, advance=None, at=None):
        """Append a step.

        gap:     override the pause added after this step (default 1 s).
        advance: override the *entire* clock increment (duration + gap),
                 e.g. for a video whose length isn't in the file.
        at:      place the step at an absolute time WITHOUT moving the
                 clock (for overlapping / concurrent steps).
        """
        start = self.t if at is None else at
        ordered = {"stepType": step["stepType"], "startTime": _num(start)}
        for k, v in step.items():
            if k != "stepType":
                ordered[k] = v
        self.items.append(("step", ordered))

        if at is None:
            if advance is None:
                if gap is None:
                    # no gap after timed steps; 1 s after duration-less steps
                    gap = 0.0 if "duration" in step else self.gap
                advance = float(step.get("duration", 0.0)) + gap
            self.t = start + advance
        return start

    def concurrent(self, step):
        """Add a step at the current clock time without advancing it."""
        return self.add(step, at=self.t)

    def marker(self, label, at=None):
        """Emit an LSL event marker. Consumes NO timeline time."""
        return self.add(lsl(label), at=self.t if at is None else at)

    # ---- output ----------------------------------------------------------
    def to_yaml(self, header=""):
        out = []
        if header:
            out.append(header.rstrip() + "\n")
        out.append("steps:\n")
        for kind, payload in self.items:
            if kind == "comment":
                out.append("")
                for line in payload.split("\n"):
                    line = line if line.startswith("#") else "# " + line
                    out.append("  " + line)
            else:
                out.append("")
                block = yaml.dump(
                    [payload],
                    sort_keys=False,
                    default_flow_style=False,
                    allow_unicode=True,
                    width=1000,
                )
                out.extend("  " + line for line in block.rstrip("\n").split("\n"))
        return "\n".join(out) + "\n"


def _num(x):
    """Render 26.0 as 26 (like the hand-written files)."""
    return int(x) if float(x).is_integer() else float(x)

def lsl(label):
    return {"stepType": "sendLSLEvent", "eventName": label}

def slug(text):
    """Config/condition name -> safe marker token: '1PP - Flip' -> '1PP_Flip'."""
    return text.replace(" - ", "_").replace(" ", "_").replace("-", "_")

# Measured lengths of the guidance audio files (ffprobe), used to place
# audio_end markers and to time phase 1 to its audio.
AUDIO_DURATIONS = {
    "NoM_Phase1": 77.1,
    "NoM_Phase2": 63.6,
    "NoM_Phase3": 73.2,
    "NoM_Phase4": 73.3,
    "NoM_Phase5": 46.7,
}

# Phase 1 display is timed to its audio: 5 s settle + main = audio + ~3 s buffer
PHASE1_MAIN_DURATION = round(AUDIO_DURATIONS["NoM_Phase1"] + 3.0 - 5.0)   # = 75 s


# ============================================================================
# Step constructors (one tiny function per stepType)
# ============================================================================

def load_scene(scene):
    return {"stepType": "loadScene", "scenePath": scene}

def load_config(name):
    return {"stepType": "loadConfig", "configName": name}

def display_text(text, duration):
    return {"stepType": "displayText", "text": text, "duration": float(duration)}

def wait(duration):
    return {"stepType": "wait", "duration": float(duration)}

def display_image(path, scale, duration):
    return {"stepType": "displayImage", "imagePath": path,
            "scale": scale, "duration": float(duration)}

def play_sound(sound_path=None):
    step = {"stepType": "playSound"}
    if sound_path:
        step["soundPath"] = sound_path
    return step

def display_video(name, looping=False, mute=False, duration=None):
    step = {"stepType": "displayVideo", "videoName": name,
            "looping": looping, "muteAudio": mute}
    if duration is not None:
        step["duration"] = float(duration)
    return step

def camera(cam_id, delay=0.0, config=None, interpolation=None, dissolution=None):
    entry = {"id": str(cam_id), "delay": float(delay)}
    if config:
        entry["configName"] = config
    if interpolation:
        entry["interpolation"] = interpolation
    if dissolution:
        entry["dissolution"] = dissolution
    return entry

def interp(duration, delay=0.0, ease="InOutSine", start_config=None):
    d = {"duration": float(duration), "delay": float(delay), "ease": ease}
    if start_config:
        d["startConfigName"] = start_config
    return d

def dissolve(duration, delay=0.0):
    return {"duration": float(duration), "delay": float(delay)}

def display_cameras(duration, *cameras):
    return {"stepType": "displayCameras", "duration": float(duration),
            "cameraDatas": list(cameras)}

def likert(question, left, right, min, max, randomCursorPosition):
    return {"stepType": "displayLikertScale", "blocking": True,
            "question": question, "leftLabel": left, "rightLabel": right, "min": min, "max": max, "randomCursorPosition": randomCursorPosition}

def choice(question, options):
    return {"stepType": "displayQuestion", "blocking": True,
            "question": question, "options": list(options)}

def break_step(text, duration):
    return {"stepType": "break", "text": text, "duration": duration}


# ============================================================================
# Experiment content: define every repeated thing ONCE
# ============================================================================

# ---- Config / camera names (lab convention) --------------------------------
CFG_1PP         = "1PP"
CFG_1PP_FLIP    = "1PP - Flip"
CFG_3PP         = "3PP"
CFG_MIRROR_FLIP = "Mirror - Flip"

CAM_FRONT = "1"   # 1PP / 3PP FRONT view
CAM_BACK  = "2"   # 3PP BACK view

FIXATION = dict(path="plus", scale=0.3, duration=3.0)

# ---- FBI 2x2 questionnaire (asked after every FBI trial) -------------------
FBI_QUESTIONS = [
    ("I felt that the body I saw was my body.",
     "Strongly disagree", "Strongly agree", 0, 100, True),
    ("I felt that the touch I felt was located where I saw the stroking.",
     "Strongly disagree", "Strongly agree", 0, 100, True),
    ("I felt as if I have three bodies.",
     "Strongly disagree", "Strongly agree", 0, 100, True),
]

# ---- Meditation-phase core questions (asked after every phase) --------------
MEDITATION_CORE = [
    ("How much do you feel that the body that you saw is your body?",
     "Not at all", "Completely", 0, 5, True),
    ("How much do you experience the sensations of your body right now?",
     "Not at all", "Completely", 0, 5, True),
    ("How much do you feel your center of awareness to be located where you feel your body to be?",
     "Not at all", "Completely", 0, 5, True),
    ("How many times have you been experiencing the nature of mind during this last phase of the experiment?",
     "Never", "Continuously", 0, 5, True),
]

# ---- FBI conditions ----------------------------------------------------------
SYNC_DELAY  = 0.0
ASYNC_DELAY = 0.5
FBI_STIM_DURATION = 90.0

FBI_CONDITIONS = {
    "front-sync":  dict(cam=CAM_FRONT, config=CFG_MIRROR_FLIP, delay=SYNC_DELAY),
    "front-async": dict(cam=CAM_FRONT, config=CFG_MIRROR_FLIP, delay=ASYNC_DELAY),
    "back-sync":   dict(cam=CAM_BACK,  config=CFG_3PP,         delay=SYNC_DELAY),
    "back-async":  dict(cam=CAM_BACK,  config=CFG_3PP,         delay=ASYNC_DELAY),
}
DEFAULT_ORDER = ["front-sync", "front-async", "back-sync", "back-async"]

# Balanced Latin square for 4 conditions (rows cycle by participant ID)
LATIN_SQUARE = [
    ["front-sync", "front-async", "back-async", "back-sync"],
    ["front-async", "back-sync", "front-sync", "back-async"],
    ["back-sync", "back-async", "front-async", "front-sync"],
    ["back-async", "front-sync", "back-sync", "front-async"],
]


# ============================================================================
# Reusable building blocks
# ============================================================================

def likert_battery(tl, questions):
    for q, left, right, min, max, randomCursorPosition, in questions:
        tl.add(likert(q, left, right, min, max, randomCursorPosition))


def fbi_trial(tl, name, cam, config, delay, *, current_config, first):
    """One FBI 2x2 trial: [wait] [loadConfig] fixation [sound] cameras wait Q's.

    Returns the config that is loaded after this trial (for change tracking).
    """
    sync = "SYNC" if delay == SYNC_DELAY else "ASYNC"
    tl.subsection(f"{name} / {sync} stroking (delay {delay:g} s)")

    if not first:
        tl.add(wait(2.0))
    if config != current_config:
        tl.marker(f"config_loaded_{slug(config)}")
        tl.add(load_config(config))
    tl.add(display_image(FIXATION["path"], FIXATION["scale"], FIXATION["duration"]))
    if not first:
        tl.marker(f"fbi_{slug(name)}_cue_audio_start".lower())
        tl.concurrent(play_sound())          # stroking-instruction cue
    tl.marker(f"fbi_{slug(name)}_avatar_on".lower())
    start = tl.add(display_cameras(FBI_STIM_DURATION, camera(cam, delay=delay, config=config)))
    tl.marker(f"fbi_{slug(name)}_avatar_off".lower(), at=start + FBI_STIM_DURATION)
    tl.add(wait(2.0))
    likert_battery(tl, FBI_QUESTIONS)
    return config


def fbi_block(tl, order, *, current_config):
    """Full 2x2 FBI block in the given condition order."""
    for i, cond_name in enumerate(order):
        cond = FBI_CONDITIONS[cond_name]
        current_config = fbi_trial(
            tl, cond_name.replace("-", " ").title(),
            cond["cam"], cond["config"], cond["delay"],
            current_config=current_config, first=(i == 0),
        )
    return current_config


def settle_then_display(tl, *, label, hold_cam, hold_config, main_cam, main_config,
                        main_duration, interp_from,
                        sound_path=None, sound_at="main"):
    """Meditation-phase camera pattern:

    1. 5 s 'settle' holding the previous phase's view
    2. main display that interpolates from the previous view into the new one
    3. guidance audio, either at the settle start ("settle") or when the
       interpolation into the new view begins ("main")

    Emits LSL markers: audio start/end, avatar change (interpolation start
    and completion), avatar off (display end).
    """
    sound_t = None
    if sound_at == "settle":
        sound_t = tl.t
        tl.marker(f"{label}_audio_{sound_path}_start")
        tl.concurrent(play_sound(sound_path))
    tl.note("settle: hold previous-phase config before interpolation")
    tl.add(display_cameras(5.0, camera(hold_cam, config=hold_config)))
    if sound_at == "main":
        sound_t = tl.t
        tl.marker(f"{label}_audio_{sound_path}_start")
        tl.concurrent(play_sound(sound_path))   # audio starts as the new view morphs in
    tl.marker(f"{label}_avatar_change_to_{slug(main_config)}_start")
    main_start = tl.add(display_cameras(
        main_duration,
        camera(main_cam, config=main_config,
               interpolation=interp(5.0, 0.0, "InOutSine", start_config=interp_from)),
    ))
    tl.marker(f"{label}_avatar_change_to_{slug(main_config)}_done", at=main_start + 5.0)
    if sound_path in AUDIO_DURATIONS:
        tl.marker(f"{label}_audio_{sound_path}_end", at=sound_t + AUDIO_DURATIONS[sound_path])
    tl.marker(f"{label}_avatar_off", at=main_start + main_duration)


def meditation_questions(tl, extra):
    """Post-phase battery: 2 s wait, 4 core questions, then phase-specific ones."""
    tl.add(wait(2.0))
    likert_battery(tl, MEDITATION_CORE)
    for item in extra:
        tl.add(item)


# ============================================================================
# The full sequence
# ============================================================================

def build_sequence(baseline_order, post_order):
    tl = Timeline()

    # --- SETUP -------------------------------------------------------------
    tl.section("INTRO")
    tl.add(load_scene("BlackScene"))
    tl.add(display_video("NoM_Intro"), advance=77)   

    # --- A. BASELINE FBI ----------------------------------------------------
    tl.section("A. BASELINE FBI MEASUREMENT")
    tl.add(wait(1.0))   # single short pause after the intro video
    current_config = fbi_block(tl, baseline_order, current_config=CFG_1PP)

    tl.add(break_step("Baseline measurement complete. Please take a short break.", 30))

    # --- 1. 1PP EMBODIMENT ----------------------------------------------------
    tl.section("1. 1PP EMBODIMENT - camera 1, 1PP config")
    tl.add(load_scene("EmptyRoom"))
    settle_then_display(tl, label="phase1",
                        hold_cam=CAM_BACK, hold_config=CFG_3PP,
                        main_cam=CAM_FRONT, main_config=CFG_1PP,
                        main_duration=PHASE1_MAIN_DURATION, interp_from=CFG_3PP,
                        sound_path="NoM_Phase1", sound_at="settle")  # display timed to the 77 s audio
    meditation_questions(tl, extra=[
        likert("How stable is your attention right now?",
               "Not stable", "Very stable", 0, 5, True),
        likert("How clearly do you feel your body?",
               "Not clearly", "Very clearly", 0, 5, True),
    ])
    tl.add(wait(2.0))

    # --- 2. 3PP BACK (OBX) ------------------------------------------------------
    tl.section("2. 3PP BACK (OBX) - camera 2, 3PP back config")
    settle_then_display(tl, label="phase2",
                        hold_cam=CAM_FRONT, hold_config=CFG_1PP,
                        main_cam=CAM_BACK, main_config=CFG_3PP,
                        main_duration=119.0, interp_from=CFG_1PP,
                        sound_path="NoM_Phase2", sound_at="main")  # 64 s: 'body seen from a distance' as 3PP arrives
    meditation_questions(tl, extra=[
        choice("Where do you feel yourself to be located?",
               ["Inside the body I see", "Outside the body I see",
                "Somewhere else / unclear"]),
        likert("Does this body feel like yours?",
               "Not at all", "Completely", 0, 5, True),
    ])
    tl.add(wait(2.0))

    # --- 3. 1PP INVERTED ---------------------------------------------------------
    tl.section("3. 1PP INVERTED - camera 1, inverted config")
    settle_then_display(tl, label="phase3",
                        hold_cam=CAM_BACK, hold_config=CFG_3PP,
                        main_cam=CAM_FRONT, main_config=CFG_1PP_FLIP,
                        main_duration=178.0, interp_from=CFG_3PP,
                        sound_path="NoM_Phase3", sound_at="main")  # 73 s: 'back in your original position' as 1PP-Flip arrives
    meditation_questions(tl, extra=[
        choice("Are you the visual self or the moving self?",
               ["The visual self", "The moving self", "Both / neither"]),
        likert("How clear is your sense of where your body begins?",
               "Not clear", "Very clear", 0, 5, True),
    ])
    tl.add(wait(2.0))

    # --- 4. 3PP FRONT (two simultaneous point clouds) ----------------------------
    tl.section("4. 3PP FRONT - Facing avatar  (TWO point clouds shown SIMULTANEOUSLY)")
    tl.note("Avatar 1: own moving body (1PP - Flip). Runs the full 180 s.")
    tl.note("settle: hold previous-phase config before interpolation")
    tl.add(display_cameras(5.0, camera(CAM_FRONT, config=CFG_1PP_FLIP)))
    tl.marker("phase4_avatar1_on")
    main_start = tl.add(display_cameras(180.0, camera(CAM_FRONT, config=CFG_1PP_FLIP)))
    tl.note("Avatar 2: facing 'other self' (Mirror - Flip), starts +10 s, ends together with avatar 1.")
    tl.note("playSound aligned with avatar 2: 'Now there are two bodies' as the second body emerges. 73 s audio.")
    tl.marker("phase4_audio_NoM_Phase4_start", at=main_start + 10)
    tl.add(play_sound("NoM_Phase4"), at=main_start + 10)
    tl.marker("phase4_avatar2_on", at=main_start + 10)
    tl.add(display_cameras(
        170.0,
        camera(CAM_FRONT, config=CFG_MIRROR_FLIP,
               interpolation=interp(5.0, 0.0, "InOutSine", start_config=CFG_1PP_FLIP)),
    ), at=main_start + 10)
    tl.marker("phase4_audio_NoM_Phase4_end",
              at=main_start + 10 + AUDIO_DURATIONS["NoM_Phase4"])
    tl.marker("phase4_avatars_off", at=main_start + 180.0)
    meditation_questions(tl, extra=[
        choice("Which body felt most like you?",
               ["The moving body", "The mirror body", "Neither"]),
        likert("How fixed was your sense of self-location?",
               "Not fixed", "Very fixed", 0, 5, True),
    ])
    tl.add(wait(2.0))

    # --- 5. EGO DISSOLUTION -----------------------------------------------------
    tl.section("5. EGO DISSOLUTION - No avatar")
    tl.add(load_scene("EmptyRoom"))
    tl.marker("phase5_dissolution_start")
    diss_start = tl.add(display_cameras(
        8.0,
        camera(CAM_FRONT, config=CFG_1PP_FLIP, dissolution=dissolve(6.0, delay=1.0)),
    ))
    tl.marker("phase5_avatar_dissolved", at=diss_start + 7.0)  # dissolve: 1 s delay + 6 s
    tl.marker("phase5_audio_NoM_Phase5_start")
    audio5_t = tl.t
    tl.concurrent(play_sound("NoM_Phase5"))   # 47 s: 'Now the body is gone' right after dissolution
    tl.note("rest in open awareness")
    tl.add(wait(169.0))
    tl.marker("phase5_audio_NoM_Phase5_end", at=audio5_t + AUDIO_DURATIONS["NoM_Phase5"])
    meditation_questions(tl, extra=[
        likert("How wide or boundless did your awareness feel?",
               "Not wide", "Very wide", 0, 5, True),
        choice("Was there still a sense of a 'self' present?",
               ["0 - Not at all", "2.5 - Somewhat", "5 - Completely"]),
    ])
    tl.add(break_step(
        "This concludes the meditative phase. The following is the final measurement.", 60))

    # --- B. POST FBI ---------------------------------------------------------------
    tl.section("B. POST-SEQUENCE FBI MEASUREMENT")
    tl.add(load_scene("BlackScene"))
    tl.add(display_text("Welcome to Full Body Illusion experiment", 4.0))
    tl.add(wait(1.0))
    fbi_block(tl, post_order, current_config=None)   # scene reloaded -> always load config

    # --- END --------------------------------------------------------------------
    tl.add(wait(1.0))
    tl.add(display_text("Thank you for participating in the experiment!", 3.0))

    return tl


# ============================================================================
# CLI
# ============================================================================

def parse_order(text):
    order = [c.strip() for c in text.split(",")]
    unknown = [c for c in order if c not in FBI_CONDITIONS]
    if unknown:
        sys.exit(f"Unknown condition(s): {unknown}. "
                 f"Valid: {', '.join(FBI_CONDITIONS)}")
    return order


def main():
    p = argparse.ArgumentParser(description="Generate an FBI Standalone sequence YAML.")
    default_out = Path(__file__).resolve().parent / "FBI_sequence_generated.yaml"
    p.add_argument("-o", "--output", default=str(default_out),
                   help=f"Output YAML path (default: {default_out.name} next to this script)")
    p.add_argument("--order", help="Comma-separated FBI condition order "
                                   "(applies to baseline, and to post unless --post-order given)")
    p.add_argument("--post-order", help="Condition order for the POST block only")
    p.add_argument("--participant", type=int,
                   help="Participant ID: picks baseline/post orders from a balanced Latin square")
    args = p.parse_args()

    if args.participant is not None:
        baseline = LATIN_SQUARE[args.participant % 4]
        post = LATIN_SQUARE[(args.participant + 1) % 4]
    else:
        baseline = parse_order(args.order) if args.order else list(DEFAULT_ORDER)
        post = parse_order(args.post_order) if args.post_order else list(baseline)

    tl = build_sequence(baseline, post)

    header = (
        "# " + "=" * 73 + "\n"
        "# FBI Standalone - Demo Sequence (GENERATED - do not edit by hand)\n"
        "# Full-Body Illusion (Baseline + Post) x Meditative Body-Perspective Manipulation\n"
        "#\n"
        f"# Baseline FBI condition order: {', '.join(baseline)}\n"
        f"# Post FBI condition order:     {', '.join(post)}\n"
        "#\n"
        "# Camera mapping (per lab convention):\n"
        '#   id "1" = 1PP / 3PP FRONT view\n'
        '#   id "2" = 3PP BACK view\n'
        "# " + "=" * 73 + "\n"
    )
    text = tl.to_yaml(header=header)

    with open(args.output, "w", encoding="utf-8") as f:
        f.write(text)
    print(f"Wrote {args.output}  (total duration ~{int(tl.t)} s = {tl.t/60:.1f} min)")


if __name__ == "__main__":
    main()
