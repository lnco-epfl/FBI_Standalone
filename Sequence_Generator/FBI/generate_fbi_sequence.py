#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
generate_fbi_sequence.py
========================

Generates Full-Body Illusion (FBI) sequence files (YAML) for FBI Standalone.

Protocol
--------
- 3PP only (one camera)
- sync / async trials in balanced pairs: each repetition = 1 sync + 1 async
  in random order (2 repetitions -> 4 trials)
- the standard FBI questionnaire (3 Likert items) after each trial
- optional counterbalancing across participants:
  odd participant -> sync first, even participant -> async first
- a different random order is drawn at every run (no fixed seed);
  the seed used is written in each file's header for traceability

Usage
-----
    python generate_fbi_sequence.py                   # uses fbi_config.ini next to the script
    python generate_fbi_sequence.py -c my_config.ini  # uses another config file
    python generate_fbi_sequence.py --init            # (re)creates a default fbi_config.ini

The script can be run from any directory: relative paths in the config
(output_dir) are resolved from the config file's location, not from the
current directory. Only the Python standard library is used (Python >= 3.6).
"""

import argparse
import configparser
import csv
import datetime
import random
import secrets
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG_PATH = SCRIPT_DIR / "fbi_config.ini"

KNOWN_SCENES = ["BlackScene", "EmptyGrayRoom", "GrayInfinityScene", "EmptyRoom"]

# ---------------------------------------------------------------------------
# Participant-facing texts
# ---------------------------------------------------------------------------
TEXTS = {
    "en": {
        "welcome": "Welcome to Full Body Illusion experiment",
        "instructions": (
            "You will now see a body in front of you and feel a touch in the back "
            "and on your shoulders. Then we will ask you some questions and all "
            "this will be repeated {n_trials} times."
        ),
        "thanks": "Thank you for participating in the experiment!",
        "left_label": "Strongly disagree",
        "right_label": "Strongly agree",
        "questions": [
            "I felt that the body I saw was my body.",
            "I felt that the touch I felt was located where I saw the stroking.",
            "I felt as if I have three bodies.",
        ],
    },
    "fr": {
        "welcome": "Bienvenue dans l'expérience Full Body Illusion",
        "instructions": (
            "Vous allez voir un corps devant vous et sentir un toucher dans le dos "
            "et sur les épaules. Ensuite, nous vous poserons quelques questions et "
            "tout cela sera répété {n_trials} fois."
        ),
        "thanks": "Merci d'avoir participé à l'expérience !",
        "left_label": "Pas du tout d'accord",
        "right_label": "Tout à fait d'accord",
        "questions": [
            "J'ai eu l'impression que le corps que je voyais était mon corps.",
            "J'ai eu l'impression que le toucher que je ressentais se situait là où je voyais le corps être touché.",
            "J'ai eu l'impression d'avoir trois corps.",
        ],
    },
}

# ---------------------------------------------------------------------------
# Default config (written by --init)
# ---------------------------------------------------------------------------
DEFAULT_CONFIG_TEXT = """\
# =========================================================================
# FBI sequence generator - configuration
# Used by generate_fbi_sequence.py
# Relative paths are resolved from the location of THIS file.
# =========================================================================

[protocol]
# Number of repetitions of the (sync + async) pair. 2 -> 4 trials, 3 -> 6 trials
repetitions = 2
# Unity scene: BlackScene | EmptyGrayRoom | GrayInfinityScene | EmptyRoom
scene = BlackScene
# Language of participant-facing texts: en | fr
language = en
# Counterbalancing: odd participant -> sync first, even -> async first
# (true / false). The rest of the order stays random in balanced pairs.
counterbalance = true

[timing]
# All durations are in seconds
# Camera delay in the async condition
async_delay = 0.5
# Camera delay in the sync condition
sync_delay = 0.0
# Duration of the camera display (stroking) per trial
stimulation_duration = 90.0
# Duration of the fixation cross before each trial
fixation_duration = 3.0
# Display scale of the fixation cross
fixation_scale = 0.3
# Duration of the welcome text
welcome_duration = 5.0
# Duration of the instructions text
instructions_duration = 5.0
# Short pauses (black screen) between blocks
pause_duration = 1.0
# Duration of the final thank-you text
thanks_duration = 3.0

[files]
# Names as used by FBI Standalone (without extension)
camera_config = 3PP
camera_id = 2
display_config = Main - Display
fixation_image = plus
sound = bell-sfx

[output]
# Number of files (= participants) to generate
n_participants = 1
# Number of the first participant (files P01, P02, ...)
first_participant = 1
# File name prefix -> FBI_P01.yaml
prefix = FBI
# Output folder (relative to this file, or absolute)
output_dir = sequences
# Overwrite existing files (true / false)
overwrite = false
"""


# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------
class ConfigError(Exception):
    pass


def load_config(path):
    parser = configparser.ConfigParser(inline_comment_prefixes=None)
    parser.read_string(DEFAULT_CONFIG_TEXT)  # defaults
    with open(path, encoding="utf-8") as f:
        parser.read_file(f)

    def get_float(sec, key, minimum=0.0):
        try:
            v = parser.getfloat(sec, key)
        except ValueError:
            raise ConfigError("[{}] {}: expected a number".format(sec, key))
        if v < minimum:
            raise ConfigError("[{}] {}: must be >= {}".format(sec, key, minimum))
        return v

    def get_int(sec, key, minimum):
        try:
            v = parser.getint(sec, key)
        except ValueError:
            raise ConfigError("[{}] {}: expected an integer".format(sec, key))
        if v < minimum:
            raise ConfigError("[{}] {}: must be >= {}".format(sec, key, minimum))
        return v

    def get_bool(sec, key):
        try:
            return parser.getboolean(sec, key)
        except ValueError:
            raise ConfigError("[{}] {}: expected true or false".format(sec, key))

    def get_str(sec, key):
        v = parser.get(sec, key).strip()
        if not v:
            raise ConfigError("[{}] {}: must not be empty".format(sec, key))
        return v

    cfg = {
        "repetitions": get_int("protocol", "repetitions", 1),
        "scene": get_str("protocol", "scene"),
        "language": get_str("protocol", "language").lower(),
        "counterbalance": get_bool("protocol", "counterbalance"),
        "async_delay": get_float("timing", "async_delay"),
        "sync_delay": get_float("timing", "sync_delay"),
        "stimulation_duration": get_float("timing", "stimulation_duration"),
        "fixation_duration": get_float("timing", "fixation_duration"),
        "fixation_scale": get_float("timing", "fixation_scale"),
        "welcome_duration": get_float("timing", "welcome_duration"),
        "instructions_duration": get_float("timing", "instructions_duration"),
        "pause_duration": get_float("timing", "pause_duration"),
        "thanks_duration": get_float("timing", "thanks_duration"),
        "camera_config": get_str("files", "camera_config"),
        "camera_id": get_str("files", "camera_id"),
        "display_config": get_str("files", "display_config"),
        "fixation_image": get_str("files", "fixation_image"),
        "sound": get_str("files", "sound"),
        "n_participants": get_int("output", "n_participants", 1),
        "first_participant": get_int("output", "first_participant", 1),
        "prefix": get_str("output", "prefix"),
        "output_dir": get_str("output", "output_dir"),
        "overwrite": get_bool("output", "overwrite"),
    }

    if cfg["language"] not in TEXTS:
        raise ConfigError("[protocol] language: '{}' unknown, choose among {}".format(
            cfg["language"], ", ".join(TEXTS)))
    if cfg["scene"] not in KNOWN_SCENES:
        print("WARNING: scene '{}' is not among the known scenes ({}). "
              "Check that it exists in FBI Standalone.".format(
                  cfg["scene"], ", ".join(KNOWN_SCENES)), file=sys.stderr)
    if cfg["async_delay"] == cfg["sync_delay"]:
        print("WARNING: async_delay = sync_delay, the two conditions are identical.",
              file=sys.stderr)

    out = Path(cfg["output_dir"]).expanduser()
    if not out.is_absolute():
        out = Path(path).resolve().parent / out
    cfg["output_dir"] = out
    return cfg


# ---------------------------------------------------------------------------
# Trial order
# ---------------------------------------------------------------------------
def make_order(rng, repetitions, participant, counterbalance):
    """Balanced pairs: each pair holds one sync and one async, in random order.
    With counterbalancing, the first pair is oriented according to the
    participant's parity (odd -> sync first, even -> async first)."""
    order = []
    for _ in range(repetitions):
        pair = ["sync", "async"]
        rng.shuffle(pair)
        order.extend(pair)
    if counterbalance:
        first = "sync" if participant % 2 == 1 else "async"
        if order[0] != first:
            order[0], order[1] = order[1], order[0]
    return order


# ---------------------------------------------------------------------------
# Timeline construction
# ---------------------------------------------------------------------------
class Timeline:
    def __init__(self):
        self.t = 0.0
        self.items = []  # dict (step) or str (comment)

    def comment(self, text):
        self.items.append(text)

    # Non-blocking steps (sound, LSL markers) are placed slightly BEFORE the
    # blocking step they accompany, so they always fire first even if the
    # player's sort by startTime is not stable. The offset is negligible.
    LEAD = 0.01

    def add(self, step_type, advance=0.0, blocking=True, **params):
        t = self.t if blocking else max(0.0, self.t - self.LEAD)
        step = {"stepType": step_type, "startTime": round(t, 3), "blocking": blocking}
        step.update(params)
        self.items.append(step)
        self.t += advance


def build_sequence(cfg, order):
    txt = TEXTS[cfg["language"]]
    delays = {"sync": cfg["sync_delay"], "async": cfg["async_delay"]}
    pause = cfg["pause_duration"]
    tl = Timeline()

    tl.comment("--- Setup ---")
    tl.add("loadScene", advance=0.0, scenePath=cfg["scene"])
    tl.add("loadDisplayConfig", advance=0.0, configName=cfg["display_config"])
    tl.add("loadCameraConfig", advance=1.0, configName=cfg["camera_config"])

    tl.comment("--- Introduction ---")
    tl.add("sendLSLEvent", blocking=False, eventName="experiment_start")
    tl.add("displayText", advance=cfg["welcome_duration"],
           text=txt["welcome"], duration=cfg["welcome_duration"])
    tl.add("displayText", advance=cfg["instructions_duration"],
           text=txt["instructions"].format(n_trials=len(order)),
           duration=cfg["instructions_duration"])

    for i, cond in enumerate(order, start=1):
        label = "trial{:02d}_{}".format(i, cond)
        tl.comment("=" * 69)
        tl.comment("--- Trial {}/{}: {} (delay {} s) ---".format(
            i, len(order), cond.upper(), fmt_num(delays[cond])))
        tl.comment("=" * 69)

        tl.add("wait", advance=pause, duration=pause)
        tl.add("displayImage", advance=cfg["fixation_duration"],
               imagePath=cfg["fixation_image"], scale=cfg["fixation_scale"],
               duration=cfg["fixation_duration"])
        # Start of stroking: bell + LSL marker, then the camera display
        tl.add("playSound", blocking=False, soundPath=cfg["sound"], subtitle=False)
        tl.add("sendLSLEvent", blocking=False, eventName=label + "_start")
        tl.add("displayCameras", advance=cfg["stimulation_duration"],
               duration=cfg["stimulation_duration"],
               cameraDatas=[{
                   "id": cfg["camera_id"],
                   "delay": delays[cond],
                   "configName": cfg["camera_config"],
               }])
        tl.add("sendLSLEvent", blocking=False, eventName=label + "_end")
        tl.add("wait", advance=pause, duration=pause)

        for question in txt["questions"]:
            tl.add("displayLikertScale", advance=1.0,
                   question=question,
                   leftLabel=txt["left_label"], rightLabel=txt["right_label"],
                   min=0, max=100, randomCursorPosition=True)

    tl.comment("--- End of experiment ---")
    tl.add("wait", advance=pause, duration=pause)
    tl.add("sendLSLEvent", blocking=False, eventName="experiment_end")
    tl.add("displayText", advance=cfg["thanks_duration"],
           text=txt["thanks"], duration=cfg["thanks_duration"])

    return tl


# ---------------------------------------------------------------------------
# YAML writer (no external dependency)
# ---------------------------------------------------------------------------
def fmt_num(x):
    s = "{:.3f}".format(float(x)).rstrip("0")
    return s + "0" if s.endswith(".") else s


def yaml_value(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, int):
        return str(v)
    if isinstance(v, float):
        return fmt_num(v)
    s = str(v).replace("\\", "\\\\").replace('"', '\\"')
    return '"' + s + '"'


def emit_step(step, lines):
    first = True
    for key, value in step.items():
        prefix = "  - " if first else "    "
        first = False
        if isinstance(value, list):
            lines.append("{}{}:".format(prefix, key))
            for item in value:
                first_sub = True
                for k2, v2 in item.items():
                    p2 = "    - " if first_sub else "      "
                    first_sub = False
                    lines.append("{}{}: {}".format(p2, k2, yaml_value(v2)))
        elif key == "stepType":
            lines.append("{}{}: {}".format(prefix, key, value))
        else:
            lines.append("{}{}: {}".format(prefix, key, yaml_value(value)))


def render_yaml(cfg, order, participant, seed, tl, generated_at):
    delays = {"sync": cfg["sync_delay"], "async": cfg["async_delay"]}
    order_str = ", ".join("{}({}s)".format(c, fmt_num(delays[c])) for c in order)
    est = tl.t
    lines = [
        "# " + "=" * 73,
        "# FBI Standalone - Full-Body Illusion sequence (3PP)",
        "# Generated by generate_fbi_sequence.py on {}".format(generated_at),
        "#",
        "# Participant      : P{:02d}".format(participant),
        "# Trial order      : {}".format(order_str),
        "# Random seed      : {}".format(seed),
        "# Counterbalancing : {}".format("on" if cfg["counterbalance"] else "off"),
        "# Scene            : {}".format(cfg["scene"]),
        "# Language         : {}".format(cfg["language"]),
        "# Repetitions      : {} ({} trials)".format(cfg["repetitions"], len(order)),
        "# Stimulation      : {} s / trial".format(fmt_num(cfg["stimulation_duration"])),
        "#",
        "# Camera mapping   : id \"{}\" = 3PP view (config {})".format(
            cfg["camera_id"], cfg["camera_config"]),
        "# Display config   : {}".format(cfg["display_config"]),
        "#",
        "# Nominal duration : ~{} s (~{:.1f} min), excluding questionnaire response time".format(
            int(round(est)), est / 60.0),
        "# " + "=" * 73,
        "",
        "steps:",
    ]
    prev_comment = False
    for item in tl.items:
        if isinstance(item, str):
            if not prev_comment:
                lines.append("")
            lines.append("  # " + item)
            prev_comment = True
            continue
        prev_comment = False
        lines.append("")
        emit_step(item, lines)
    lines.append("")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def write_default_config(path):
    path = Path(path)
    if path.exists():
        print("The file {} already exists, it was not modified.".format(path))
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(DEFAULT_CONFIG_TEXT, encoding="utf-8")
    print("Default config created: {}".format(path))


def main(argv=None):
    ap = argparse.ArgumentParser(
        description="Generates Full-Body Illusion (3PP, sync/async) sequences for FBI Standalone.")
    ap.add_argument("-c", "--config", default=str(DEFAULT_CONFIG_PATH),
                    help="config file (.ini). Default: fbi_config.ini next to the script")
    ap.add_argument("--init", action="store_true",
                    help="create a default config file at the path given by --config")
    args = ap.parse_args(argv)

    config_path = Path(args.config).expanduser().resolve()

    if args.init:
        write_default_config(config_path)
        return 0

    if not config_path.exists():
        print("Config file not found: {}".format(config_path), file=sys.stderr)
        print("Create one with: python \"{}\" --init".format(Path(__file__).resolve()),
              file=sys.stderr)
        return 1

    try:
        cfg = load_config(config_path)
    except (ConfigError, configparser.Error) as e:
        print("Config error: {}".format(e), file=sys.stderr)
        return 1

    out_dir = cfg["output_dir"]
    participants = list(range(cfg["first_participant"],
                              cfg["first_participant"] + cfg["n_participants"]))
    targets = [(p, out_dir / "{}_P{:02d}.yaml".format(cfg["prefix"], p)) for p in participants]

    existing = [t for _, t in targets if t.exists()]
    if existing and not cfg["overwrite"]:
        print("These files already exist (overwrite = false), nothing was generated:",
              file=sys.stderr)
        for t in existing:
            print("  " + str(t), file=sys.stderr)
        print("Change first_participant, or set overwrite = true.", file=sys.stderr)
        return 1

    out_dir.mkdir(parents=True, exist_ok=True)
    now = datetime.datetime.now()
    generated_at = now.strftime("%Y-%m-%d %H:%M:%S")
    delays = {"sync": cfg["sync_delay"], "async": cfg["async_delay"]}
    summary_rows = []

    for participant, target in targets:
        seed = secrets.randbits(32)  # different at every run, recorded in the file
        rng = random.Random(seed)
        order = make_order(rng, cfg["repetitions"], participant, cfg["counterbalance"])
        tl = build_sequence(cfg, order)
        target.write_text(render_yaml(cfg, order, participant, seed, tl, generated_at),
                          encoding="utf-8", newline="\n")
        summary_rows.append([participant, target.name, seed] +
                            ["{}({}s)".format(c, fmt_num(delays[c])) for c in order])
        print("P{:02d} -> {}   [{}]".format(participant, target.name, ", ".join(order)))

    summary = out_dir / "{}_orders_{}.csv".format(cfg["prefix"], now.strftime("%Y%m%d_%H%M%S"))
    with open(summary, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["participant", "file", "seed"] +
                   ["trial{}".format(i) for i in range(1, 2 * cfg["repetitions"] + 1)])
        w.writerows(summary_rows)

    print("\n{} file(s) generated in {}".format(len(targets), out_dir))
    print("Summary of orders: {}".format(summary.name))
    return 0


if __name__ == "__main__":
    sys.exit(main())
