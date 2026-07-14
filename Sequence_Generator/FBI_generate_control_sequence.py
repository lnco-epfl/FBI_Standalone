#!/usr/bin/env python3
"""
FBI Standalone - CONTROL Sequence Generator (random-order FBI)
==============================================================
Generates `FBI_sequence_control_random_lsl.yaml` in the same folder as this script.

Structure:
  Instructions (text + voice-over)
  -> PRE  FBI measurement, 4 conditions in RANDOM order
  -> break
  -> LSL "start_meditation"  ...  MEDITATION_DURATION (20 min)  ...  LSL "end_meditation"
  -> break
  -> POST FBI measurement, 4 conditions in RANDOM order (independent shuffle)
  -> thank-you text

Usage:
    python FBI_generate_control_sequence.py            # new random order each run
    python FBI_generate_control_sequence.py 42         # reproducible order (seed 42)

The chosen orders are printed to the console and written into the header
comment of the YAML file. The LSL stream also identifies each condition
(fbi_front_sync_avatar_on, ...), so the order is always recoverable.

Requires: PyYAML  (pip install pyyaml)
"""

import os
import sys
import random
import yaml

OUTPUT_NAME = "FBI_sequence_control_random_lsl.yaml"

# ---------------------------------------------------------------------------
# Timing constants (all in seconds)
# ---------------------------------------------------------------------------

FBI_INSTRUCTIONS_DURATION = 17.904   # exact duration of FBI_instructions_audio

# Meditation duration (for reference: VR sequence phases 1-5 = 940 s = 15 min 40 s)
MEDITATION_DURATION = 1200.0         # 20 min

GAP = 1.0                            # standard 1 s gap between chained blocks

# The four FBI conditions
CONDITIONS = ["front_sync", "front_async", "back_sync", "back_async"]


def ts(x):
    """Round to ms and keep whole numbers as ints (cleaner YAML)."""
    x = round(x, 3)
    return int(x) if x == int(x) else x


# ---------------------------------------------------------------------------
# Reusable text / questionnaires
# ---------------------------------------------------------------------------

FBI_INSTRUCTIONS = (
    "You will now see a body in front of you and feel a touch in the back and "
    "on your shoulders. Then we will ask you some questions and all this will "
    "be repeated four times."
)

FBI_QUESTIONS = [
    "I felt that the body I saw was my body.",
    "I felt that the touch I felt was located where I saw the stroking.",
    "I felt as if I have three bodies.",
]


def make_header(pre_order, post_order, seed):
    seed_note = f"seed {seed}" if seed is not None else "no seed (fresh random order each run)"
    return f"""\
# =========================================================================
# FBI Standalone - CONTROL Sequence (GENERATED - do not edit by hand)
# Random-order FBI (pre + post) with a meditation gap of {ts(MEDITATION_DURATION)} s ({ts(MEDITATION_DURATION/60)} min)
#
# Randomization: {seed_note}
# Pre  FBI condition order: {', '.join(c.replace('_', '-') for c in pre_order)}
# Post FBI condition order: {', '.join(c.replace('_', '-') for c in post_order)}
#
# Camera mapping (per lab convention):
#   id "1" = 1PP / 3PP FRONT view
#   id "2" = 3PP BACK view
# ========================================================================="""


def section(title):
    bar = "  # " + "=" * 73
    return f"{bar}\n  # {title}\n{bar}"


# ---------------------------------------------------------------------------
# Step factories (dict key order matters: it is preserved in the YAML output)
# ---------------------------------------------------------------------------

def load_scene(t, scene):
    return {"stepType": "loadScene", "startTime": ts(t), "scenePath": scene}


def load_config(t, name):
    return {"stepType": "loadConfig", "startTime": ts(t), "configName": name}


def display_text(t, text, duration):
    return {"stepType": "displayText", "startTime": ts(t), "text": text,
            "duration": ts(duration)}


def wait(t, duration=1.0):
    return {"stepType": "wait", "startTime": ts(t), "duration": ts(duration)}


def lsl(t, event):
    return {"stepType": "sendLSLEvent", "startTime": ts(t), "eventName": event}


def play_sound(t, sound=None):
    step = {"stepType": "playSound", "startTime": ts(t)}
    if sound is not None:
        step["soundPath"] = sound
    return step


def display_image(t, image="plus", scale=0.3, duration=3.0):
    return {"stepType": "displayImage", "startTime": ts(t), "imagePath": image,
            "scale": scale, "duration": duration}


def camera(cam_id, delay, config):
    return {"id": cam_id, "delay": delay, "configName": config}


def display_cameras(t, duration, cameras):
    return {"stepType": "displayCameras", "startTime": ts(t), "duration": ts(duration),
            "cameraDatas": cameras}


def likert(t, question, left, right, lo=0, hi=100):
    return {"stepType": "displayLikertScale", "startTime": ts(t), "blocking": True,
            "question": question, "leftLabel": left, "rightLabel": right,
            "min": lo, "max": hi, "randomCursorPosition": True}


def fbi_likert_battery(t):
    """Three FBI items on a 0-100 scale, one second apart."""
    return [likert(t + i, q, "Strongly disagree", "Strongly agree")
            for i, q in enumerate(FBI_QUESTIONS)]


# ---------------------------------------------------------------------------
# One FBI condition block (99 s slot), usable in any order
# ---------------------------------------------------------------------------

def fbi_condition(t, cond):
    """
    One FBI condition:
      wait 1 s -> load config -> fixation cross -> 90 s stroking -> 3 Likert items.
    Because the order is random, EVERY condition (re)loads its own config.
    The per-condition structure (cue audio for all but front-sync) matches
    the fixed-order sequence.
    Returns (blocks, time_after_block).
    """
    view, sync = cond.split("_")                     # "front"/"back", "sync"/"async"
    config = "Mirror - Flip" if view == "front" else "3PP"
    config_tag = "Mirror_Flip" if view == "front" else "3PP"
    cam_id = "1" if view == "front" else "2"
    delay = 0.0 if sync == "sync" else 0.5
    name = f"fbi_{view}_{sync}"

    blocks = [f"  # --- {view.capitalize()} {sync.capitalize()} / "
              f"{sync.upper()} stroking (delay {delay} s) ---"]
    blocks.append(wait(t))
    blocks.append(lsl(t + 1, f"config_loaded_{config_tag}"))
    blocks.append(load_config(t + 1, config))
    blocks.append(display_image(t + 2))
    if cond != "front_sync":
        blocks.append(lsl(t + 5, f"{name}_cue_audio_start"))
        blocks.append(play_sound(t + 5))
    blocks.append(lsl(t + 5, f"{name}_avatar_on"))
    blocks.append(display_cameras(t + 5, 90.0, [camera(cam_id, delay, config)]))
    blocks.append(lsl(t + 95, f"{name}_avatar_off"))
    blocks.append(wait(t + 95))
    blocks.extend(fbi_likert_battery(t + 96))
    return blocks, t + 99


def fbi_measurement(t, order):
    """Full FBI measurement: the 4 conditions in the given order."""
    blocks = []
    for cond in order:
        cond_blocks, t = fbi_condition(t, cond)
        blocks.extend(cond_blocks)
    return blocks, t


# ---------------------------------------------------------------------------
# Sequence assembly
# ---------------------------------------------------------------------------

def build_blocks(pre_order, post_order):
    b = []
    t = 0

    # ================== PRE FBI MEASUREMENT (random order) ==================
    b.append(section("PRE FBI MEASUREMENT (random order: "
                     + ", ".join(c.replace("_", "-") for c in pre_order) + ")"))
    b.append(load_scene(t, "BlackScene"))
    t += GAP
    # instructions: on-screen text + voice-over, text shown exactly as long as the audio
    b.append(display_text(t, FBI_INSTRUCTIONS, FBI_INSTRUCTIONS_DURATION))
    b.append(play_sound(t, "FBI_instructions_audio"))
    t += FBI_INSTRUCTIONS_DURATION
    blocks, t = fbi_measurement(t, pre_order)
    b.extend(blocks)
    b.append({"stepType": "break", "startTime": ts(t),
              "text": "Baseline measurement complete. Please take a short break.",
              "duration": 30})
    t += 30

    # =================== MEDITATION (same duration as VR phases 1-5) ===================
    b.append(section(f"MEDITATION - {ts(MEDITATION_DURATION)} s ({ts(MEDITATION_DURATION / 60)} min)"))
    b.append(lsl(t, "start_meditation"))
    b.append(wait(t, MEDITATION_DURATION))
    t += MEDITATION_DURATION
    b.append(lsl(t, "end_meditation"))
    b.append({"stepType": "break", "startTime": ts(t),
              "text": "This concludes the meditative phase. The following is the final measurement.",
              "duration": 60})
    t += 60

    # ================== POST FBI MEASUREMENT (random order) ==================
    b.append(section("POST FBI MEASUREMENT (random order: "
                     + ", ".join(c.replace("_", "-") for c in post_order) + ")"))
    b.append(load_scene(t, "BlackScene"))
    t += GAP
    b.append(display_text(t, "Welcome to Full Body Illusion experiment", 4.0))
    t += 4.0
    b.append(display_text(t, FBI_INSTRUCTIONS, FBI_INSTRUCTIONS_DURATION))
    b.append(play_sound(t, "FBI_instructions_audio"))
    t += FBI_INSTRUCTIONS_DURATION
    blocks, t = fbi_measurement(t, post_order)
    b.extend(blocks)
    b.append(wait(t))
    t += GAP
    b.append(display_text(t, "Thank you for participating in the experiment!", 3.0))

    return b


# ---------------------------------------------------------------------------
# YAML rendering
# ---------------------------------------------------------------------------

def render_step(step):
    text = yaml.dump([step], sort_keys=False, default_flow_style=False,
                     width=10_000, allow_unicode=True)
    return "\n".join("  " + line if line else line
                     for line in text.rstrip("\n").split("\n"))


def main():
    seed = int(sys.argv[1]) if len(sys.argv) > 1 else None
    rng = random.Random(seed)

    pre_order = CONDITIONS[:]
    post_order = CONDITIONS[:]
    rng.shuffle(pre_order)
    rng.shuffle(post_order)

    blocks = build_blocks(pre_order, post_order)
    rendered = [blk if isinstance(blk, str) else render_step(blk) for blk in blocks]
    text = make_header(pre_order, post_order, seed) + "\n\nsteps:\n\n\n" + "\n\n".join(rendered) + "\n"

    out_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), OUTPUT_NAME)
    with open(out_path, "w", newline="\r\n", encoding="utf-8") as f:
        f.write(text)

    n_steps = sum(1 for blk in blocks if isinstance(blk, dict))
    print(f"Wrote {out_path} ({n_steps} steps)")
    print(f"Pre  FBI order: {', '.join(pre_order)}")
    print(f"Post FBI order: {', '.join(post_order)}")
    if seed is None:
        print("Tip: pass a seed for a reproducible order, e.g. "
              "`python FBI_generate_control_sequence.py 42`")


if __name__ == "__main__":
    main()
