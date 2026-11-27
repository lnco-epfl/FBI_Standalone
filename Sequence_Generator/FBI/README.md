# FBI Sequence Generator

Python script that generates YAML sequence files for **FBI Standalone**, following the Full-Body Illusion protocol:

- third-person view (3PP) only, camera `2`
- **sync** and **async** trials in balanced pairs: each repetition contains 1 sync and 1 async trial, in random order (2 repetitions = 4 trials)
- the 3 standard FBI questions after each trial (0–100 scale, cursor placed at random)
- one file per participant, with a **different order every time you generate**

## Folder contents

| File | Purpose |
|------|---------|
| `generate_fbi_sequence.py` | the script |
| `fbi_config.ini` | the parameters (edit before each generation) |
| `FBI_P01_example.yaml` | example of a generated sequence |

## Requirements

- **Python 3.6 or later.** Nothing to install: the script only uses the standard library.

To check the installed version:
```
python --version
```
> On Windows, if `python` is not recognized, try `py`.

## Quick start

1. Put `generate_fbi_sequence.py` and `fbi_config.ini` in the same folder.
2. Edit `fbi_config.ini` if needed (for example the number of participants).
3. Run:
   ```
   python generate_fbi_sequence.py
   ```
4. The files are created in the `sequences/` subfolder, next to the config:
   ```
   sequences/
   ├── FBI_P01.yaml
   ├── FBI_P02.yaml
   ├── ...
   └── FBI_orders_20261126_143012.csv   ← summary of the trial orders
   ```
5. Copy the `.yaml` files into the `Input/Sequences/` folder of FBI Standalone. Then pick the participant's file in the interface when you start the experiment.

The script can be run **from any folder** by giving its full path:
```
python "C:\Tools\fbi_generator\generate_fbi_sequence.py"
```
It then reads the `fbi_config.ini` located next to it. The output folder is resolved from the config file's location, not from the current folder.

## Command-line options

| Command | Effect |
|---------|--------|
| `python generate_fbi_sequence.py` | generates using the `fbi_config.ini` next to the script |
| `python generate_fbi_sequence.py -c my_config.ini` | generates using another config file |
| `python generate_fbi_sequence.py --init` | creates a default `fbi_config.ini` (never overwrites an existing file) |
| `python generate_fbi_sequence.py -c variant.ini --init` | creates a default config under another name |
| `python generate_fbi_sequence.py -h` | shows the help |

You can keep several configs, one per protocol variant (for example `config_3reps.ini`, `config_grayroom.ini`), and choose which one to use with `-c`.

## Parameters (`fbi_config.ini`)

Lines starting with `#` are comments. All durations are in seconds.

### `[protocol]`

| Parameter | Default | Description |
|-----------|---------|-------------|
| `repetitions` | `2` | Number of sync + async pairs. 2 = 4 trials, 3 = 6 trials |
| `scene` | `BlackScene` | Room: `BlackScene`, `EmptyGrayRoom`, `GrayInfinityScene`, `EmptyRoom` |
| `language` | `en` | Language of the texts shown to the participant: `en` or `fr` |
| `counterbalance` | `true` | If `true`, odd-numbered participants start with sync and even-numbered ones with async |

### `[timing]`

| Parameter | Default | Description |
|-----------|---------|-------------|
| `async_delay` | `0.5` | Camera delay in the async condition |
| `sync_delay` | `0.0` | Camera delay in the sync condition |
| `stimulation_duration` | `90.0` | Camera display (stroking) duration per trial |
| `fixation_duration` | `3.0` | Duration of the fixation cross before each trial |
| `fixation_scale` | `0.3` | Size of the fixation cross |
| `welcome_duration` | `5.0` | Duration of the welcome text |
| `instructions_duration` | `5.0` | Duration of the instructions text |
| `pause_duration` | `1.0` | Pauses (black screen) between blocks |
| `thanks_duration` | `3.0` | Duration of the final thank-you text |

### `[files]`

File names as known by FBI Standalone, **without extension**.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `camera_config` | `3PP` | Camera config (`Input/Config/Camera/`) |
| `camera_id` | `2` | ID of the 3PP camera |
| `display_config` | `Main - Display` | Display config (`Input/Config/Display/`) |
| `fixation_image` | `plus` | Fixation cross image (`Input/Images/`) |
| `sound` | `bell-sfx` | Sound played at the start of each stimulation (`Input/Audios/`) |

### `[output]`

| Parameter | Default | Description |
|-----------|---------|-------------|
| `n_participants` | `1` | Number of files to generate (one per participant) |
| `first_participant` | `1` | Number of the first participant. Example: `5` with `n_participants = 3` gives P05, P06, P07 |
| `prefix` | `FBI` | File name prefix: `FBI_P01.yaml` |
| `output_dir` | `sequences` | Output folder, relative to the config file or absolute |
| `overwrite` | `false` | If `false`, the script refuses to overwrite existing files |

## Structure of a generated sequence

```
Load the room, the display config and the camera config
Welcome text → instructions                (LSL: experiment_start)
For each trial (random order):
    pause → fixation cross
    bell-sfx sound + 3PP camera for 90 s   (LSL: trialNN_<cond>_start / _end)
    pause → 3 FBI questions (blocking, wait for the answer)
Pause → thank-you text                     (LSL: experiment_end)
```

The 3 questions, in a fixed order:
1. *I felt that the body I saw was my body.*
2. *I felt that the touch I felt was located where I saw the stroking.*
3. *I felt as if I have three bodies.* (control question)

### LSL markers

| Marker | When |
|--------|------|
| `experiment_start` | start of the experiment |
| `trial01_sync_start` | start of the stimulation of trial 1 (sync condition) |
| `trial01_sync_end` | end of the stimulation of trial 1 |
| `trial02_async_start` … | same for the following trials |
| `experiment_end` | end of the experiment |

The marker name contains the condition, so it can be matched directly with the EEG or physiological signals.

> ℹ️ The sound and the LSL markers are placed 0.01 s before the blocking step they accompany. This way they always fire at the start of the stimulation, whatever order the software uses to sort steps that share the same `startTime`.

## Traceability

- **Header of each YAML:** participant, trial order, random seed, room, language, nominal duration.
- **`FBI_orders_<date>_<time>.csv` file:** created at each generation, one row per participant:
  ```
  participant,file,seed,trial1,trial2,trial3,trial4
  1,FBI_P01.yaml,3275853094,sync(0.0s),async(0.5s),sync(0.0s),async(0.5s)
  2,FBI_P02.yaml,127831191,async(0.5s),sync(0.0s),async(0.5s),sync(0.0s)
  ```
  Keep it with the data: it gives the order of conditions for the analysis. The FBI Standalone output CSV only shows the condition through the `CameraDelays` column.

## Variant examples

**3 repetitions (6 trials) in the gray room, texts in French:**
```ini
[protocol]
repetitions = 3
scene = EmptyGrayRoom
language = fr
```

**Generate 20 participants at once:**
```ini
[output]
n_participants = 20
first_participant = 1
```

**Add participants later (P21 to P30) without touching the existing files:**
```ini
[output]
n_participants = 10
first_participant = 21
```

**Shorter stimulation and a 1 s async delay:**
```ini
[timing]
stimulation_duration = 60.0
async_delay = 1.0
```

## Common error messages

| Message | Cause and fix |
|---------|---------------|
| `Config file not found` | No `fbi_config.ini` next to the script. Run `--init` to create one, or give the path with `-c` |
| `These files already exist (overwrite = false)` | Files for these participants already exist. Change `first_participant`, or set `overwrite = true` |
| `Config error: [timing] async_delay: expected a number` | Badly written value. Use a dot as decimal separator (`0.5`, not `0,5`) |
| `WARNING: scene '...' is not among the known scenes` | Unknown room name (check upper/lower case). Generation continues anyway |
| `WARNING: async_delay = sync_delay` | Both conditions are identical: check the delays |

## Editing the texts

The texts shown to the participant (welcome, instructions, questions, scale labels) are in the `TEXTS` dictionary at the top of `generate_fbi_sequence.py`, in English (`en`) and French (`fr`).

> ⚠️ The French version of the FBI questions is an unvalidated translation. Replace it with the lab's reference version if there is one.

To add a language, copy the `"en"` block, rename it (for example `"de"`) and translate it. Then set `language = de` in the config.
