# FBI Standalone

# Summary

* [Description](#description)
* [Requirements](#requirements)
* [How to use](#how-to-use)
* [Input Files](#input-files)
  * [Sequence Files](#sequence-files)
  * [Camera Config Files](#camera-config-files)
  * [Display Config Files](#display-config-files)
  * [Images](#images)
  * [Videos](#videos)
  * [Audios](#audios)
* [Output Files](#output-files)
* [GUI](#gui)
  * [Main GUI](#main-gui)
  * [Config Editor](#config-editor)
* [Screenshot](#screenshot)
* [Authors](#authors)

# Description

The application is designed for behavioral research experiments based on the full-body illusion (FBI) by [Lenggenhager et al. (2007)](https://www.science.org/doi/10.1126/science.1143439). In a typical setup, participants immersed in VR will see a 3D representation of their own body under different conditions, e.g., from a first-person or a third-person perspective, or with or without delay. The software allows researchers to connect and setup multiple depth cameras and to develop a configurable experiment sequence. The experiment flow is defined through YAML sequence files and supports a variety of step types including camera display, text, images, videos, sounds, questionnaires, and Likert scales for participants feedback.

This application has been created with Unity (version 6000.3.11f1). The project uses Femto Bolt cameras to capture image and depth and to display in real-time the subjects as point clouds. It is provided 'as is', without waranty of any kind, by the [EPFL Blanke Lab](https://www.epfl.ch/labs/lnco/) under GPL3+ licence.

# Requirements

## Hardware

The system has been designed for the following hardware:

* **Laptop:** High-end GPU gaming PC/laptop, 32 GB RAM minimum recommended. Windows 11.
* **VR Headset:** [Meta Quest 3 / 3S](https://www.meta.com/quest/quest-3s/), connected to the PC/laptop via USB-C cable
* **RGBD Cameras:** [ORBBEC Femto Bolt](https://www.orbbec.com/products/tof-camera/femto-bolt/), one or more, each connected via USB-C data cable + external power supply

## Software

The following software must be installed on the computer before running the application.

### Meta Horizon Link

[Meta Horizon Link](https://www.oculus.com/download_app/?id=1582076955407037) is required to use the Meta Quest 3 / 3S as a PCVR headset. It must be installed and running on the laptop before connecting the headset (with your Meta account). In Quest Link settings, allow unknown sources and enable OpenXR. When the Quest 3 / 3S is connected via USB-C cable and turned on, it automatically launches Quest Link; this behaviour is configured directly on the headset in the Quest settings.

### Orbbec SDK & Drivers (for Femto Bolt cameras)

The Femto Bolt cameras are developed by [ORBBEC](https://www.orbbec.com) in partnership with Microsoft as a direct replacement for Azure Kinect cameras. 

> ℹ️ The Unity project uses the [Azure Kinect and Femto Bolt Examples for Unity](https://assetstore.unity.com/packages/tools/integration/azure-kinect-and-femto-bolt-examples-for-unity-149700) asset by [RF Solutions](http://rfilkov.com/) with the OrbbecFemtoWrapper already imported, no additional Unity-side camera setup is required. For the full setup guide, refer to the [official plugin documentation](https://rfilkov.com/2019/08/26/azure-kinect-tips-tricks/#t19).

Before first use, follow these steps:

1. **Connect** a Femto Bolt camera to its power supply and to the laptop via USB-C.
2. **Download and install [Azure Kinect Sensor SDK](https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/docs/usage.md)** (v1.4.1). This is required as a base dependency even when using Femto Bolt cameras.
3. **Download and install [Azure Kinect Body Tracking SDK](https://learn.microsoft.com/en-us/previous-versions/azure/kinect-dk/body-sdk-download)** (v1.1.2). It is required even when using Femto Bolt cameras, as the plugin relies on it for body tracking. Follow the [official installation instructions](https://learn.microsoft.com/en-us/azure/kinect-dk/body-sdk-setup). The SDK must be installed in its default location: `C:\Program Files\Azure Kinect Body Tracking SDK`. Note: this procedure requires running commands from Windows PowerShell launched as administrator.
4. **Download and unzip Orbbec Viewer from the [Orbbec SDK for Windows](https://www.orbbec.com/developers/orbbec-sdk/)** (v1.8.1 or later, not V2). Launch Orbbec Viewer, select the connected camera, and verify that the color, depth, IR and IMU streams are visible and that device timestamps are rolling. Then close Orbbec Viewer.
5. **Check the firmware version** of the device against [Orbbec's firmware repository](https://github.com/orbbec/OrbbecFirmware). Upgrade if needed via Orbbec Viewer.
6. **Download and unzip [Orbbec's K4A-Wrapper](https://github.com/orbbec/OrbbecSDK-K4A-Wrapper)** (v1.8.1 or later). Run the `k4aviewer` app from its `bin` folder, open the device, start the cameras, and verify all streams are working. Then close the app.
7. **On Windows**, go to the `script` subfolder of the K4A-Wrapper folder and follow the instructions in `obsensor_metadata_win10.md` to enable device timestamps over the UVC protocol.

> ⚠️ Step 7 must be performed for each Orbbec camera plugged in individually.

# How to use

To run the application:

* Connect all Femto Bolt cameras to the PC and ensure they are recognized by the system.
* Connect the Quest 3 / 3S to the laptop with the USB-C cable and start Meta Horizon Link. Validate the Quest Link connection from inside the headset.
* Run `FBI Standalone.exe`
* Enter the participant data (age, gender) and select a sequence file.
* Create or edit a config file (which defines camera positions, depth values, etc.) and a display config file (which defines the stimulus canvas position and appearance)
* Press the **Start** button to begin the experiment.

# Input Files

All input files are located in the `Input/` folder at the root of the executable directory. This folder is created automatically on first launch if it does not exist. Files are loaded at startup and referenced in sequence files by their filename without extension.

```
Input/
├── Sequences/       ← YAML sequence files
├── Config/
│   ├── Camera/      ← YAML camera configuration files
│   └── Display/     ← YAML stimulus display configuration files
├── Images/          ← Image assets (PNG, JPG, BMP, TGA)
├── Videos/          ← Video assets (MP4 + SRT)
└── Audios/          ← Audio assets (WAV, OGG, MP3)
```

## Sequence Files

Sequence files are YAML files located in `Input/Sequences/`. Each file defines a timeline of steps that will be executed during an experiment session. The sequence file name is used to name the output files.

### Timeline model

The sequence is **time-based**, not purely sequential: every step has a `startTime` (in seconds, relative to the start of the experiment) at which it is triggered. Steps with different start times can overlap and run concurrently — for example, a sound can play while a question is displayed, or two `DisplayCameras` steps can run side by side.

Each step also has a `blocking` flag:

| Parameter | Type | Description |
|-----------|------|-------------|
| `startTime` | float | Time in seconds, from the start of the experiment, at which the step is triggered. Default: `0` |
| `blocking` | bool | If `true`, the sequence timeline pauses until this step finishes, then jumps directly to the `startTime` of the next step. If `false` (default), the timeline keeps advancing normally while the step runs in the background |

> ℹ️ Steps are automatically sorted by `startTime` when the sequence is loaded, they do not need to be written in chronological order in the YAML file.

> ⚠️ If several `blocking` steps would overlap in time, only the first one encountered blocks the timeline; subsequent steps wait their turn.

These two parameters (`startTime`, `blocking`) are available on every step type, in addition to the parameters described below.

### Step Types

#### LoadScene

Loads a Unity scene by name.

| Parameter | Type | Description |
|-----------|------|-------------|
| `scenePath` | string | Name of the scene to load |

Available scenes:
 
<table>
<tr>
<td align="center"><b>BlackScene</b><br/><img width="800" height="449" alt="BlackScene" src="https://github.com/user-attachments/assets/49a8d5e9-78b8-4344-889a-c7c9b131c16e" /></td>
<td align="center"><b>EmptyGrayRoom</b><br/><img width="800" height="450" alt="EmptyGrayRoom" src="https://github.com/user-attachments/assets/3aef4415-213c-4dc9-9606-4b5f4e0be8fd" /></td>
</tr>
<tr>
<td align="center"><b>GrayInfinityScene</b><br/><img width="800" height="450" alt="GrayInfinityScene" src="https://github.com/user-attachments/assets/332ffd76-3ae7-4388-9515-19eec551d1e0" /></td>
<td align="center"><b>EmptyRoom</b><br/><img width="800" height="450" alt="EmptyRoom" src="https://github.com/user-attachments/assets/1d56b02e-2dc5-49e8-a8b8-a1a6dd0fd6f0" /></td>
</tr>
</table>


```yaml
- stepType: loadScene
  scenePath: "EmptyRoom"
```

---

#### LoadCameraConfig

Loads a camera configuration file by name.

| Parameter | Type | Description |
|-----------|------|-------------|
| `configName` | string | Name of the camera config file to load (without extension) |

```yaml
- stepType: loadCameraConfig
  configName: Bruno
```

---

#### LoadDisplayConfig

Loads a display configuration file by name, and/or applies one-off overrides to the stimulus display canvas (position, rotation, scale, background color). Useful for switching the canvas layout mid-experiment, or for tweaking a single value without maintaining a dedicated file.

| Parameter | Type | Description |
|-----------|------|-------------|
| `configName` | string | *(Optional)* Name of a display config file to load (without extension). If omitted, the currently active stimulus display settings are used as the base |
| `positionOverride` | Vector3 | *(Optional)* Overrides the canvas position. If omitted, keeps the loaded/current value |
| `rotationOverride` | Vector3 | *(Optional)* Overrides the canvas Euler rotation. If omitted, keeps the loaded/current value |
| `scaleOverride` | Vector3 | *(Optional)* Overrides the canvas scale. If omitted, keeps the loaded/current value |
| `backgroundColorOverride` | Color (r,g,b,a) | *(Optional)* Overrides the canvas background color. If omitted, keeps the loaded/current value |

```yaml
# Load a display config file as-is
- stepType: loadDisplayConfig
  configName: MainDisplay

# Load a display config file, but override the background color for this step only
- stepType: loadDisplayConfig
  configName: MainDisplay
  backgroundColorOverride:
    r: 1
    g: 1
    b: 1
    a: 1

# No config file — just nudge the currently active canvas position
- stepType: loadDisplayConfig
  positionOverride:
    x: 0.0
    y: 1.2
    z: 2.0
```

> ⚠️ If neither `configName` nor any override field is set, the step has no effect.

---

#### DisplayText

Displays a text message on screen for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `text` | string | Text to display |
| `duration` | float | Display duration in seconds |

```yaml
- stepType: displayText
  text: "Welcome"
  duration: 3.0
```

---

#### Wait

Pauses the sequence for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `duration` | float | Wait duration in seconds |

```yaml
- stepType: wait
  duration: 2.0
```

---

#### DisplayCameras
 
Displays one or more point clouds simultaneously, each with its own settings. Each camera entry is defined under a `cameraDatas` list and can independently have a temporal delay, a config file, an interpolation animation, a dissolution effect, or a fade effect.
 
| Parameter | Type | Description |
|-----------|------|-------------|
| `duration` | float | Total display duration in seconds for the step |
| `cameraDatas` | list | List of camera entries to display simultaneously |
 
Each entry in `cameraDatas` supports the following fields:
 
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | ID of the camera to display (`"1"`, `"2"`, etc.) |
| `delay` | float | Temporal delay in seconds. `0.0` = real-time display |
| `configName` | string | *(Optional)* Name of a camera config file to load for this camera (without extension). If omitted, the currently loaded config is used |
| | | |
| `interpolation` | object | *(Optional)* Smoothly animates the point cloud particules from a start config to the target config |
| `interpolation.duration` | float | Duration of the interpolation animation in seconds |
| `interpolation.delay` | float | Delay before the interpolation starts, in seconds |
| `interpolation.ease` | string | Easing function available : `Default`, `Linear`, `InOutSine`, `InOutQuad`, `InOutCubic`, `InOutQuart`, `InOutExpo`. For more detail on easing function, here is a [cheat sheet](https://easings.net/)|
| `interpolation.startConfigName` | string | *(Optional)* Name of the config to use as the start position of the interpolation. If omitted, the current transform is used as the start |
| | | |
| `dissolution` | object | *(Optional)* Progressively dissolves the point cloud using a particle/noise effect |
| `dissolution.duration` | float | Duration of the dissolution effect in seconds |
| `dissolution.delay` | float | Delay before the dissolution starts, in seconds |
| | | |
| `fade` | object | *(Optional)* Fades out the point cloud by reducing its opacity to zero. Alternative to `dissolution` — use one or the other per camera entry |
| `fade.duration` | float | Duration of the fade-out in seconds |
| `fade.delay` | float | Delay before the fade starts, in seconds |

> ℹ️ The dissolution effect's sphere center is not set per-step — it comes from the `referencePoint` field of the camera config file currently loaded for that camera (see [Point Clouds](#point-clouds)).
 
The step also supports an optional `rigInterpolation` block (at the step level, not per camera) that smoothly translates the VR rig — moving the participant's point of view in the scene. This is used to transition between a first-person perspective (1PP) and a third-person perspective (3PP) during a single step, while the point clouds are displayed.
 
| Field | Type | Description |
|-------|------|-------------|
| `rigInterpolation` | object | *(Optional)* Smoothly moves the VR rig (player origin) during the step |
| `rigInterpolation.startPosition` | Vector3 | World position of the rig at the start of the transition |
| `rigInterpolation.endPosition` | Vector3 | World position of the rig at the end of the transition |
| `rigInterpolation.startYaw` | float | Y-axis rotation of the rig at the start of the transition, in degrees. Default: `0` |
| `rigInterpolation.endYaw` | float | Y-axis rotation of the rig at the end of the transition, in degrees. Default: `0` (set to `180` to rotate to face backwards, i.e. a full 3PP back view) |
| `rigInterpolation.duration` | float | Duration of the rig movement in seconds |
| `rigInterpolation.delay` | float | Delay before the rig starts moving, in seconds |
| `rigInterpolation.ease` | string | Easing function from [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween#support). Recommended: `InOutSine` for VR comfort |
 
> ℹ️ `rigInterpolation` runs in the same timeline as point cloud interpolation and dissolution — all `delay` values are relative to the start of the step and can overlap freely.
 
> ℹ️ To dissolve the 1PP point cloud as the rig recedes, set `dissolution.delay` on that camera entry to match the moment in the rig movement at which the 1PP body is far enough away (e.g. `rigInterpolation.delay + rigInterpolation.duration * 0.6`).
 
> ⚠️ Yaw rotation is applied around the Y axis only. Pitch and roll are not supported to avoid VR disorientation.
 
> ⚠️ If the step is interrupted before the rig movement starts, the rig remains at `startPosition`. If interrupted mid-movement, the rig snaps immediately to `endPosition`.
 
```yaml
# Single camera, real-time display
- stepType: displayCameras
  duration: 20.0
  cameraDatas:
    - id: "1"
      delay: 0.0
 
# Two cameras simultaneously, one with delay
- stepType: displayCameras
  duration: 20.0
  cameraDatas:
    - id: "1"
      delay: 0.0
    - id: "2"
      delay: 1.5
 
# With config switch and interpolation
- stepType: displayCameras
  duration: 20.0
  cameraDatas:
    - id: "1"
      delay: 0.0
      configName: TargetConfig
      interpolation:
        duration: 3.0
        delay: 1.0
        ease: InOutQuad
        startConfigName: StartConfig
 
# With dissolution effect
- stepType: displayCameras
  duration: 20.0
  cameraDatas:
    - id: "1"
      delay: 0.0
      configName: MyConfig
      dissolution:
        duration: 3.0
        delay: 5.0
 
# With fade effect
- stepType: displayCameras
  duration: 20.0
  cameraDatas:
    - id: "1"
      delay: 0.0
      configName: MyConfig
      fade:
        duration: 3.0
        delay: 5.0
 
# 1PP to 3PP transition: the rig recedes while the 1PP point cloud dissolves,
# leaving only the 3PP (back camera) visible at the end.
# dissolution.delay = rigInterpolation.delay + rigInterpolation.duration * 0.6 = 1.0 + 3.0 * 0.6 = 2.8s
- stepType: displayCameras
  duration: 6.0
  cameraDatas:
    - id: "1"
      delay: 0.0
      configName: config_1pp
      dissolution:
        duration: 1.5
        delay: 2.8
    - id: "2"
      delay: 0.0
      configName: config_3pp
  rigInterpolation:
    startPosition:
      x: 0.0
      y: 0.0
      z: 0.0
    endPosition:
      x: 0.0
      y: 0.0
      z: -2.0
    startYaw: 0.0
    endYaw: 0.0
    delay: 1.0
    duration: 3.0
    ease: InOutSine
```
 
> ⚠️ When using `interpolation`, `configName` must also be set — it defines the end position of the animation.
 
> ⚠️ Only one delay value can be set per camera; if multiple values are provided, only the last one will be used.
 
---


#### DisplayImage

Displays an image from the `Input/Images/` folder.

| Parameter | Type | Description |
|-----------|------|-------------|
| `imagePath` | string | Image filename without extension |
| `scale` | float | Display scale of the image |
| `duration` | float | Display duration in seconds |

```yaml
- stepType: displayImage
  imagePath: "plus"
  scale: 0.3
  duration: 5.0
```

---

#### PlaySound

Plays an audio file from the `Input/Audio/` folder.

| Parameter | Type | Description |
|-----------|------|-------------|
| `soundPath` | string | Audio filename without extension |
| `subtitle` | bool |  If `true`, displays the corresponding `.srt` subtitle file (if present in `Input/Audios/`) while the sound plays. Default: `false` |

```yaml
- stepType: playSound
  soundPath: "bell-sfx"
  subtitle: false
```

---

#### DisplayVideo

Plays a video file from the `Input/Videos/` folder. The step ends automatically when the video finishes, or after `duration` seconds as a fallback timeout if the video duration cannot be determined. If `looping` is enabled, the step runs until the `duration` timeout is reached.

| Parameter | Type | Description |
|-----------|------|-------------|
| `videoName` | string | Video filename without extension |
| `looping` | bool | If `true`, the video loops until the duration timeout. Default: `false` |
| `muteAudio` | bool | If `true`, the video plays without audio. Default: `false` |
| `duration` | float | Fallback timeout in seconds, used when looping or if the video duration cannot be read |
| `subtitle` | bool | If `true`, displays the corresponding `.srt` subtitle file (if present in `Input/Videos/`) during playback. Default: `true` |

```yaml
# Play a video once (ends automatically when finished)
- stepType: displayVideo
  videoName: "intro"
  looping: false
  muteAudio: false
  duration: 60.0
  subtitle: true

# Loop a video for 30 seconds, without subtitles
- stepType: displayVideo
  videoName: "background"
  looping: true
  muteAudio: true
  duration: 30.0
  subtitle: false
```

---

#### DisplayQuestion

Displays a multiple-choice question and waits for a response.

| Parameter | Type | Description |
|-----------|------|-------------|
| `question` | string | Question text |
| `options` | list of strings | List of response options |

```yaml
- stepType: displayQuestion
  question: "How do you feel?"
  options:
    - "Option 1"
    - "Option 2"
    - "Option 3"
```

---

#### DisplayLikertScale

Displays a Likert scale question and waits for a response.

| Parameter | Type | Description |
|-----------|------|-------------|
| `question` | string | Question text |
| `leftLabel` | string | Label for the left (low) end of the scale |
| `rightLabel` | string | Label for the right (high) end of the scale |
| `min` | int | Min value of the scale |
| `max` | int | Max value of the scale |
| `randomCursorPosition` | bool | Place the cursor at a random position, otherwise, center it in the middle |

```yaml
- stepType: displayLikertScale
  question: "How satisfied are you?"
  leftLabel: "Not satisfied"
  rightLabel: "Very satisfied"
  min: 1
  max: 5
  randomCursorPosition: true
```

---

#### Break

Displays a break screen with instructions for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `text` | string | Instructions to display during the break |
| `duration` | float | Break duration in seconds |

```yaml
- stepType: break
  text: "Take a break."
  duration: 90
```

---

#### SendLSLEvent

Sends an LSL ([Lab Streaming Layer](https://labstreaminglayer.org)) event marker, used to synchronize the experiment timeline with external recording systems (e.g. EEG, physiological sensors).

| Parameter | Type | Description |
|-----------|------|-------------|
| `eventName` | string | Name/label of the event marker to send |

```yaml
- stepType: sendLSLEvent
  eventName: "test"
```

### Full Sequence Example

This example uses `startTime` and `blocking` to mix sequential and overlapping steps: a sound (`PlaySound`) and an LSL marker fire in parallel with the camera display, while the rest of the sequence remains strictly sequential thanks to `blocking: true`.

```yaml
steps:
  - stepType: loadScene
    scenePath: "EmptyRoom"
    startTime: 0
    blocking: true

  - stepType: loadCameraConfig
    configName: DefaultConfig
    startTime: 1
    blocking: true

  - stepType: loadDisplayConfig
    configName: MainDisplay
    startTime: 1
    blocking: true

  - stepType: displayText
    text: "Welcome to the experiment"
    duration: 3.0
    startTime: 2
    blocking: true

  - stepType: wait
    duration: 2.0
    startTime: 5
    blocking: true

  # 1PP display
  - stepType: displayCameras
    duration: 5.0
    startTime: 7
    blocking: true
    cameraDatas:
      - id: "1"
        delay: 0.0
        configName: config_1pp

  # 1PP to 3PP transition: rig recedes while the 1PP point cloud dissolves
  - stepType: displayCameras
    duration: 6.0
    startTime: 12
    blocking: true
    cameraDatas:
      - id: "1"
        delay: 0.0
        configName: config_1pp
        dissolution:
          duration: 1.5
          delay: 2.8
      - id: "2"
        delay: 0.0
        configName: config_3pp
    rigInterpolation:
      startPosition:
        x: 0.0
        y: 0.0
        z: 0.0
      endPosition:
        x: 0.0
        y: 0.0
        z: -2.0
      startYaw: 0.0
      endYaw: 0.0
      delay: 1.0
      duration: 3.0
      ease: InOutSine

  # 3PP display, after the transition
  - stepType: displayCameras
    duration: 9.0
    startTime: 18
    blocking: true
    cameraDatas:
      - id: "2"
        delay: 0.0
        configName: config_3pp

  # Runs in parallel with the 1PP display step, does not block the timeline
  - stepType: playSound
    soundPath: "bell-sfx"
    startTime: 7
    blocking: false

  - stepType: sendLSLEvent
    eventName: "camera_display_start"
    startTime: 7
    blocking: false

  - stepType: displayLikertScale
    question: "How natural did the movement feel?"
    leftLabel: "Not natural"
    rightLabel: "Very natural"
    startTime: 27
    blocking: true

  - stepType: break
    text: "Please take a short break."
    duration: 90
    startTime: 37
    blocking: true

  - stepType: displayText
    text: "Thank you."
    duration: 4.0
    startTime: 127
    blocking: true
```

In addition, the build and the repository contain two sequence files: a ‘Demo’ file, which demonstrates in detail all the features available in a sequence, and an ‘FBI’ file, which replicates the sequence of events in a typical full-body illusion experience. 

## Camera Config Files

Camera config files are YAML files located in `Input/Config/Camera/`. They define the spatial configuration, depth settings, and clipping boundaries for each camera's point cloud. Camera config files can be created and edited through the **Cameras** tab of the Config Editor.

```yaml
configName: DefaultConfig
createdAt: 2026-04-02 15:20:45
lastModified: 2026-04-10 12:26:32
pointClouds:
  - iD: 1
    position:
      x: 0
      y: 1
      z: 0.36
    rotation:
      x: 0
      y: 0
      z: 0
    scale:
      x: -1
      y: 1
      z: 1
    depthMax: 3.47
    depthMin: 0
    clampXMin: 0.0
    clampXMax: 1.0
    clampYMin: 0.0
    clampYMax: 1.0
    referencePoint:
      x: 0.0
      y: -0.148
      z: 1.658
```

### Point Clouds

The `pointClouds` list defines the spatial configuration, depth settings and spatial clipping for each camera.

| Field | Type | Description |
|-------|------|-------------|
| `iD` | int | Camera ID, must match the camera index |
| `position` | Vector3 | Position of the point cloud in the scene |
| `rotation` | Vector3 | Euler rotation of the point cloud |
| `scale` | Vector3 | Scale of the point cloud. Use `-1` on X or Y to flip the axis |
| `depthMax` | float | Maximum depth distance captured by the camera (in meters) |
| `depthMin` | float | Minimum depth distance captured by the camera (in meters) |
| `clampXMin` | float | Left boundary of the visible area, normalized between `0` and `1` |
| `clampXMax` | float | Right boundary of the visible area, normalized between `0` and `1` |
| `clampYMin` | float | Bottom boundary of the visible area, normalized between `0` and `1` |
| `clampYMax` | float | Top boundary of the visible area, normalized between `0` and `1` |
| `referencePoint` | Vector3 | A general-purpose local-space position, relative to the point cloud (not world space) — **stored value is always local**, even though it's edited in world space in the UI. Currently drives the [dissolution effect's](#displaycameras) sphere center. Editable via the [Reference Point](#reference-point) panel |

## Display Config Files

Display config files are YAML files located in `Input/Config/Display/`. They define the position and appearance of the stimulus display canvas, the in-world UI panel used to display text, images, questions, and other stimuli to the participant. Display config files can be created and edited through the **Stimulus Display** tab of the Config Editor, and loaded during an experiment with the [`LoadDisplayConfig`](#loaddisplayconfig) step.

```yaml
configName: MainDisplay
createdAt: 2026-04-02 15:20:45
lastModified: 2026-04-10 12:26:32
stimulusDisplay:
  position:
    x: 0.11
    y: 0.68
    z: 2.47
  rotation:
    x: 0
    y: 23.2
    z: 0
  scale:
    x: 1
    y: 1
    z: 1
  backgroundColor:
    r: 0
    g: 0
    b: 0
    a: 1
```

### Stimulus Display

The `stimulusDisplay` block defines the position, orientation and background color of the in-world UI panel displayed during the experiment.

| Field | Type | Description |
|-------|------|-------------|
| `position` | Vector3 | Position of the display in the scene |
| `rotation` | Vector3 | Euler rotation of the display |
| `scale` | Vector3 | Scale of the display |
| `backgroundColor` | Color (r,g,b,a) | Background color of the display. Values are normalized floats between `0` and `1` |

## Images

Image assets are located in `Input/Images/`. Supported formats: **PNG, JPG/JPEG, BMP, TGA**.

All images are loaded at startup (and can be reloaded on demand). The folder is scanned recursively (subfolders are supported), and each image is referenced by its filename without extension - e.g. a file `Input/Images/plus.png` is referenced as `"plus"` in a [`DisplayImage`](#displayimage) step.

> ⚠️ Since only the filename (without extension) is used as the key, two images with the same name in different subfolders will collide, the last one loaded overwrites the previous one.

## Videos

Video assets are located in `Input/Videos/`. Supported formats: **MP4, WEBM, MOV**.

Videos are registered at startup and referenced by filename without extension in a [`DisplayVideo`](#displayvideo) step - e.g. `Input/Videos/intro.mp4` is referenced as `"intro"`. Unlike images and audios, video files are not loaded into memory at startup; only their file path is kept and the video is streamed when played.

An optional `.srt` subtitle file with the same filename (e.g. `intro.srt`) can be placed alongside the video to enable subtitles via the `subtitle` parameter of the `DisplayVideo` step.

## Audios

Audio assets are located in `Input/Audios/`. Supported formats: **WAV, OGG, MP3**.

All audio files are loaded asynchronously at startup and referenced by filename without extension in a [`PlaySound`](#playsound) step - 
e.g. `Input/Audios/bell-sfx.wav` is referenced as `"bell-sfx"`. As with images, the folder is scanned recursively and the key is the filename only, so identically named files in different subfolders will collide.

An optional `.srt` subtitle file with the same filename can be placed alongside the audio file to enable subtitles via the `subtitle` parameter of the `PlaySound` step.

# Output Files

All output files are saved in the `Output/` folder at the root of the executable directory. They follow the naming format: `yyyyMMdd_HHmmss_sequenceFileName_filetype`.

There are two types of output files:

- **Event log** (`_Events.txt`): Records all events during the experiment, including errors, warnings, and state transitions. Useful for monitoring experiment progress and debugging.
- **Data output** (`_Output.csv`): A CSV file containing all data collected during the experiment. Each row corresponds to a `DisplayCameras` step.

### Output file parameters

| Name | Type | Description |
|------|------|-------------|
| `Gender` | string | Participant's gender |
| `Age` | int | Participant's age |
| `Language` | string | Language used during the experiment |
| `SequenceFile` | string | Name of the sequence file used |
| `CameraConfigFile` | string | Name of the camera config file loaded |
| `DisplayConfigFile` | string | Name of the display config file loaded |
| `Scene` | string | Name of the Unity scene currently loaded |
| `TimeSinceStart` | double | Time elapsed since experiment start (seconds) |
| `StepType` | string | Type of the current step (e.g. `DisplayCameras`) |
| `StepCount` | int | Current step index |
| `CameraIDs` | string | IDs of the cameras displayed, as a comma-separated list (e.g. `[1,2,]`) |
| `CameraDelays` | string | Temporal delays applied to each camera feed, as a comma-separated list matching `CameraIDs` order |
| `CameraDisplayDuration` | float | Duration the cameras were displayed (seconds) |
| `AsInterpolation` | bool | Whether point cloud interpolation was enabled for this step |
| `AsDissolution` | bool | Whether point cloud dissolution was enabled for this step |
| `LikertResponse` | int | Response given to a Likert scale question |
| `LikertResponseTime` | double | Response time for the Likert scale (seconds) |
| `QuestionResponse` | string | Response given to a multiple-choice question |
| `QuestionResponseIndex` | string | Index of the selected option in the `options` list of the question |
| `QuestionResponseTime` | double | Response time for the question (seconds) |

# GUI

## Main GUI
<img width="1920" height="1080" alt="main-gui" src="https://github.com/user-attachments/assets/8f4d2d0f-65d4-408d-ab53-1426f704fb26" />

### Participant form

- **Age:** Enter the participant's age.
- **Gender:** Select the participant's gender (Male, Female, Other).
- **Sequence File:** Select which sequence file to use for the experiment.

### Config editor

- **Config editor:** Open the config editor window.

### Experiment Status

- **Start Time:** Displays the time the experiment started.
- **Elapsed:** Displays elapsed time since start.
- **Sequence time:** Current sequence time value.
- **Jump to Time:** Allow to jump to any sequence time.
- **Progress:** A progress bar showing overall experiment progress.

### Experiment Controls

- **Start button:** Starts the experiment. Requires a sequence file to be selected.
- **Play/Pause button:** Pauses or resumes the experiment. The current step restarts when resuming.
- **Stop button:** Stops the experiment and resets the state.
- **Reset head orientation:** Realigns the participant's view to the current facing direction.
- **Audio volume controls:** Adjust the volume of audio and video during the experiment.
- **Fullscreen view:** Sets the participant view to fullscreen.

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Q | Start the experiment |
| W | Stop the experiment |
| Space | Play / Pause |
| R | Reset head orientation |
| M | Mute sound |
| ← / → | Previous / Next step |


## Config Editor

The Config Editor is a dedicated interface for creating and editing camera and display configuration files. It can be opened from the main GUI and provides a real-time preview of the scene from the participant's perspective.

<img width="1932" height="1098" alt="Config Editor" src="https://github.com/user-attachments/assets/fa9f5c5f-4c59-4a78-8a3d-d2e0d53dd35c" />

### Tabs

The editor is split into two independent tabs, each with its own toolbar, its own currently-loaded file, and its own status indicator, since camera configs and display configs are separate files:

| Tab | Edits | Saved to |
|-----|-------|----------|
| **Cameras** | [Camera Panels](#camera-panels-camera-1-camera-2-) — point cloud position, depth, clamp and mirror settings per camera | `Input/Config/Camera/` |
| **Stimulus Display** | [Stimulus Display](#stimulus-display-1) panel — position, rotation and background color of the in-world UI canvas | `Input/Config/Display/` |

Switching tabs only changes which panel and toolbar are shown — it does not close or discard the file open in the other tab.

### Toolbar

Each tab has its own toolbar with the same set of controls, operating on that tab's file only:

| Button | Description |
|--------|-------------|
| **New** | Creates a new empty configuration |
| **Open** | Opens an existing config file from disk via a file browser |
| **Save** | Saves the current configuration to disk |
| **Save as...** | Saves the current configuration under a new name and location |
| **≡** (menu) | Opens a menu with clipboard options: **Copy** to copy the current configuration as YAML to the clipboard, and **Paste** to load a configuration from the clipboard |
| **Scene** | Dropdown to select which Unity scene to load in the preview (e.g. `EmptyRoom`) |

On the left of the status indicator, the name of the currently loaded file (for that tab) is displayed. The status indicator shows a message reflecting the current state of the active tab's editor:

| Message | Color | Description |
|---------|-------|-------------|
| `No config loaded.` | Grey | Initial state at startup, no config is active |
| `Ready.` | Grey | Config is loaded and no pending changes |
| `Unsaved changes.` | Yellow | The config has been modified but not yet saveddis, played continuously until saved |
| `Opened {filename}` | Green | A config file was successfully opened from disk |
| `Saved as {filename}` | Green | The config was successfully saved under a new name |
| `Config copied to clipboard.` | Green | The config was successfully copied to the clipboard |
| `Config pasted from clipboard.` | Green | A config was successfully loaded from the clipboard |
| `Open cancelled.` | Grey | The file browser was closed without selecting a file |
| `Save cancelled.` | Grey | The save dialog was closed without saving |
| `Clipboard is empty.` | Red | Paste was attempted but the clipboard contains nothing |
| `Clipboard content is invalid.` | Red | Paste was attempted but the clipboard content is not a valid config |
| `Failed to save file.` | Red | An error occurred while saving to disk |
| `Failed to read file.` | Red | An error occurred while reading a file from disk |
| `Invalid file format.` | Red | The opened file is not a valid config YAML |

### Preview Panel (right side)

The right side of the Config Editor shows a live preview of the scene split into two viewports.

| Viewport | Description |
|----------|-------------|
| **Participant view** | First-person view from the VR headset's perspective. Reflects the actual experience seen by the participant |
| **Static view** | Fixed camera view of the scene, useful for monitoring the overall setup |

Additional controls in the preview panel:

| Control | Description |
|---------|-------------|
| **VR edition** toggle | Enables editing mode inside the VR headset, allowing the researcher to adjust the config while wearing the headset |
| **Reset Headset Orientation** | Resets the headset's forward direction to the current facing direction |
| **Display Avatar** toggle | Shows or hides a body avatar in the scene (The avatar is only displayed in the config editor ) |
| **Camera Position / Rotation** | Displays the current position and rotation of the static preview camera (read-only) |


### Camera Tab

One panel is displayed per connected camera. Each panel contains the following controls.

**Display toggle:** enables or disables the real-time point cloud preview for that camera. Only one camera can be displayed at a time.

#### Transform

Controls the position and rotation of the point cloud in the 3D scene.

| Control | Description |
|---------|-------------|
| **Position X / Y / Z** | Position of the point cloud in world space |
| **Rotation X / Y / Z** | Euler rotation of the point cloud |

#### Depth

Controls the depth range captured by the camera. Points outside this range are discarded.

| Control | Description |
|---------|-------------|
| **Max** | Maximum capture depth in meters (0–10 m) |
| **Min** | Minimum capture depth in meters (0–10 m) |

#### Clamp

Crops the visible area of the point cloud along X and Y axes. Values are normalized between `0` and `1`, where `0` is the left/bottom edge and `1` is the right/top edge of the camera frame.

| Control | Description |
|---------|-------------|
| **X Min / X Max** | Left and right crop boundaries |
| **Y Min / Y Max** | Bottom and top crop boundaries |

#### Mirror

Flips the point cloud along a given axis.

| Button | Description |
|--------|-------------|
| **↔ Horizontal** | Flips the point cloud horizontally (X axis) |
| **↕ Vertical** | Flips the point cloud vertically (Y axis) |

#### Reference Point

A general-purpose reference point relative to the point cloud; it currently drives the center of the sphere used by the [dissolution effect](#displaycameras). It is stored **relative to the point cloud** (local space) in the config file, so it stays correctly placed regardless of the point cloud's own position/rotation.

For ease of use, the **X / Y / Z fields are an offset from the point cloud's own position, aligned to the scene's global axes** (like Unity's "Global" tool handle mode) rather than the point cloud's own (possibly rotated) local axes — `0, 0, 0` places it exactly on the point cloud, and moving "X" by 1 always moves it 1 unit along the scene's global X axis, regardless of how the point cloud is rotated. The conversion to/from local space happens automatically; only the local value ever gets written to the config file.

| Control | Description |
|---------|-------------|
| **Position X / Y / Z** | Offset from the point cloud's own position, aligned to the scene's global axes |
| **Show gizmo** toggle | Displays a sphere gizmo in the preview at the current position, to help placing it visually. This is a visual aid only for the editor — it is not saved to the config file and always starts hidden when a config is loaded |

### Display Tab

Shown under the **Stimulus Display** tab. This section controls the position and appearance of the in-world UI canvas — the panel used to display text, images, questions, and other stimuli to the participant. Changes here are saved to a [Display Config File](#display-config-files), independent from the camera config open in the **Cameras** tab.

| Control | Description |
|---------|-------------|
| **Display toggle** | Shows or hides the stimulus display canvas in the preview |
| **Position X / Y / Z** | Position of the canvas in world space |
| **Rotation Y** | Vertical rotation of the canvas |
| **Background color** | Background color of the canvas (click to open a color picker) |

### Keyboard Shortcuts

| Key | VR Controller | Action |
|-----|--------|--------|
| R | Left Controller thumbstick click | Reset head orientation |
| K | Left Controller primary button (A) | Start Dissolution effect |

# Screenshot

### Mirror view
<img width="1920" height="1080" alt="Mirror view" src="https://github.com/user-attachments/assets/11933bce-0d12-4c67-af16-1fbbbd573cfc" />

### 1PP view
<img width="1920" height="1080" alt="1PP view" src="https://github.com/user-attachments/assets/e0115e12-dedd-4c00-8907-0d4da5daa898" />

### 3PP view
<img width="1920" height="1078" alt="3PP view" src="https://github.com/user-attachments/assets/07f5fcce-f090-4133-8701-1926ba2b4728" />

### Full body illusion
<img width="1920" height="1080" alt="Full body illusion" src="https://github.com/user-attachments/assets/36ab1ab1-0b46-41e7-8183-a50634a27b7a" />

# Authors

Development Arnaud Droxler, Haotian Yao.

Supervision Bruno Herbelin, Idil Sezer, Olaf Blanke.
