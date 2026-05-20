# FBI Standalone

# Summary
  * [Description](#description)
  * [Requirements](#requirements)
  * [How to use](#how-to-use)
  * [Input Files](#input-files)
  * [Sequence Files](#sequence-files)
  * [Config Editor](#config-editor)
  * [Config Files](#config-files)
  * [Output Files](#output-files)
  * [GUI](#gui)
  * [Authors](#authors)

# Description

This application has been created with Unity (version 6000.3.11f1). The project uses Femto Bolt cameras to capture and display real-time point clouds of participants as Point cloud.

The application is designed for research experiments involving point cloud visualizations. It allows researchers to display live or delayed point cloud feeds from multiple cameras as part of a configurable experiment sequence. The experiment flow is defined through YAML sequence files and supports a variety of step types including camera display, text, images, questions, and Likert scales.

# Requirements

## Hardware

* **Laptop:** High-end GPU gaming laptop, 32 GB RAM minimum recommended
* **VR Headset:** [Meta Quest 3 / 3S](https://www.meta.com/quest/quest-3s/), connected to the laptop via USB-C cable
* **RGBD Cameras:** [ORBBEC Femto Bolt](https://www.orbbec.com/products/tof-camera/femto-bolt/), one or more, each connected via USB-C data cable + external power supply
* **Tripod(s):** One per camera

## Software

The following software must be installed on the laptop before running the application.

### Meta Horizon Link

[Meta Horizon Link](https://www.oculus.com/download_app/?id=1582076955407037) is required to use the Meta Quest 3 / 3S as a PCVR headset. It must be installed and running on the laptop before connecting the headset (with your Meta account). In Quest Link settings, allow unknown sources and enable OpenXR. When the Quest 3 / 3S is connected via USB-C cable and turned on, it automatically launches Quest Link; this behaviour is configured directly on the headset in the Quest settings. 

### Orbbec SDK & Drivers (for Femto Bolt cameras)

The Femto Bolt cameras are the recommended replacement for Azure Kinect cameras. They are developed by Orbbec in partnership with Microsoft and are compatible with the Azure Kinect SDK via an Orbbec wrapper.

> ℹ️ The Unity project uses the [Azure Kinect and Femto Bolt Examples for Unity](https://assetstore.unity.com/packages/tools/integration/azure-kinect-and-femto-bolt-examples-for-unity-149700) asset with the OrbbecFemtoWrapper already imported, no additional Unity-side camera setup is required.
>
> For the full setup guide, refer to the [official plugin documentation](https://rfilkov.com/2019/08/26/azure-kinect-tips-tricks/#t19).


Before first use of an Orbbec Camera, follow these steps:
 
1. **Connect** a Femto Bolt camera to its power supply and to the laptop via USB-C.
2. **Download and install [Azure Kinect Sensor SDK](https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/docs/usage.md)** (v1.4.1). This is required as a base dependency even when using Femto Bolt cameras.
3. **Download and install [Azure Kinect Body Tracking SDK](https://learn.microsoft.com/en-us/previous-versions/azure/kinect-dk/body-sdk-download)** (v1.1.2). It is required even when using Femto Bolt cameras, as the plugin relies on it for body tracking.
4. Follow the [official installation instructions](https://learn.microsoft.com/en-us/azure/kinect-dk/body-sdk-setup). The SDK must be installed in its default location: `C:\Program Files\Azure Kinect Body Tracking SDK`. NB: this procedure requires running command lines from Windows Power Shell launched as administrator on your computer.
3. **Download and unzip Orbbec Viewer from the [Orbbec SDK for Windows](https://www.orbbec.com/developers/orbbec-sdk/)** (v1.8.1 or later, not in V2). Launch Orbbec Viewer, select the connected camera, and verify that the color, depth, IR and IMU streams are visible and that device timestamps are rolling. Then close Orbbec Viewer.
4. **Check the firmware version** of the device against [Orbbec's firmware repository](https://github.com/orbbec/OrbbecFirmware). Upgrade if needed via Orbbec Viewer.
5. **Download and unzip [Orbbec's K4A-Wrapper](https://github.com/orbbec/OrbbecSDK-K4A-Wrapper)** (v1.8.1 or later). Run the `k4aviewer` app from its `bin` folder, open the device, start the cameras, and verify all streams are working. Then close the app.
6. **On Windows**, go to the `script` subfolder of the K4A-Wrapper folder and follow the instructions in `obsensor_metadata_win10.md` to enable device timestamps over the UVC protocol.

The step 4 (installation instructions) should be performed for each Orbbec camera plugged individually.

# How to use

To run the application:

* Connect all Femto Bolt cameras to the PC (ensure they are recognized by the system).
* Connect the Quest 3 / 3S to the laptop with the usb-C cable, and start Meta Horizon link (Quest Link running on the laptop, validate connection in the headset). 
* Run `FBI Standalone.exe`
* Enter the participant data (age, gender) and select a sequence file.
* Create or edit a config file (which defines camera positions, depth values, etc.)
* Press the **Start** button to begin the experiment.

# Input Files

All input files are located in the `Input/` folder at the root of the executable directory.

```
Input/
├── Sequences/       ← YAML sequence files
├── Configs/         ← YAML camera configuration files
├── Images/          ← Image assets (PNG, JPG, BMP, TGA)
├── Videos/          ← Video assets (MP4)
└── Audio/           ← Audio assets (WAV, OGG, MP3)

```

Images and audio files are loaded automatically at startup. They are referenced in sequence files by their filename (without extension).

# Sequence Files

Sequence files are YAML files located in `Input/Sequences/`. Each file defines the ordered list of steps that will be executed during an experiment session.

Output files are named after the sequence file used: `date_hours_sequenceFileName_Output.csv`

## Step Types

### LoadScene
Loads a Unity scene by name.

| Parameter | Type | Description |
|-----------|------|-------------|
| `scenePath` | string | Name of the scene to load |

List of the avialable scene : 
* `BlackScene`
* `EmptyGrayRoom`
* `GrayInfinityScene`
* `EmptyRoom`

```yaml
- stepType: LoadScene
  scenePath: "EmptyRoom"
```

---

### LoadConfig
Loads a camera configuration file by name.

| Parameter | Type | Description |
|-----------|------|-------------|
| `fileName` | string | Name of the config file to load (without extension) |

```yaml
- stepType: LoadConfig
  fileName: Bruno
```

---

### DisplayText
Displays a text message on screen for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `text` | string | Text to display |
| `duration` | float | Display duration in seconds |

```yaml
- stepType: DisplayText
  text: "Welcome"
  duration: 3.0
```

---

### Wait
Pauses the sequence for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `duration` | float | Wait duration in seconds |

```yaml
- stepType: Wait
  duration: 2.0
```


### DisplayCamera
Displays the point cloud from a specific Femto Bolt camera, with an optional temporal delay. Optionally, a different config file can be loaded for this step, and a smooth interpolation can be applied to transition the point cloud from its previous transform to the new one.
 
| Parameter | Type | Description |
|-----------|------|-------------|
| `duration` | float | Display duration in seconds |
| `cameraID` | string | ID of the camera to display (`"1"`, `"2"`, etc.) |
| `delay` | float | Temporal delay in seconds. `0.0` = real-time display |
| `fileName` | string | *(Optional)* Name of a config file to load for this step (without extension). If omitted, the currently loaded config is used |
| `interpolation` | object | *(Optional)* If defined, smoothly animates the point cloud transform from the previous config position to the new one |
| `interpolation.duration` | float | Duration of the interpolation animation in seconds |
| `interpolation.delay` | float | Delay before the interpolation starts, in seconds |
| `interpolation.ease` | string | Easing function from [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween#support). Recommended values: `Default`, `Linear`, `InOutSine`, `InOutQuad`, `InOutCubic`, `InOutQuart`, `InOutExpo`. For a full list of available values, see the PrimeTween documentation |
 
```yaml
# Basic usage - real-time display
- stepType: DisplayCamera
  duration: 20.0
  cameraID: "1"
  delay: 0.0
 
# With temporal delay
- stepType: DisplayCamera
  duration: 20.0
  cameraID: "1"
  delay: 1.5
 
# With config switch and interpolation
- stepType: DisplayCamera
  duration: 20.0
  cameraID: "1"
  delay: 0.0
  fileName: AnotherConfig
  interpolation:
    duration: 3.0
    delay: 1.0
    ease: InOutQuad
```
 
> ⚠️ When using `interpolation`, the `fileName` parameter must also be set. The interpolation animates the point cloud transform from the position defined in the previously loaded config to the position defined in the new config file.
 
 
---


### DisplayImage
Displays an image from the `Input/Images/` folder.

| Parameter | Type | Description |
|-----------|------|-------------|
| `imagePath` | string | Image filename without extension |
| `scale` | float | Display scale of the image |
| `duration` | float | Display duration in seconds |

```yaml
- stepType: DisplayImage
  imagePath: "plus"
  scale: 0.3
  duration: 5.0
```

---

### PlaySound
Plays an audio file from the `Input/Audio/` folder.

| Parameter | Type | Description |
|-----------|------|-------------|
| `soundPath` | string | Audio filename without extension |

```yaml
- stepType: PlaySound
  soundPath: "bell-sfx"
```

---

### DisplayVideo
Plays a video file from the `Input/Videos/` folder. Supported formats: MP4, WEBM, MOV. The step ends automatically when the video finishes, or after `duration` seconds as a fallback timeout if the video duration cannot be determined. If `looping` is enabled, the step runs until the `duration` timeout is reached.

| Parameter | Type | Description |
|-----------|------|-------------|
| `videoName` | string | Video filename without extension |
| `looping` | bool | If `true`, the video loops until the duration timeout. Default: `false` |
| `muteAudio` | bool | If `true`, the video plays without audio. Default: `false` |
| `duration` | float | Fallback timeout in seconds, used when looping or if the video duration cannot be read |

```yaml
# Play a video once (ends automatically when finished)
- stepType: DisplayVideo
  videoName: "intro"
  looping: false
  muteAudio: false
  duration: 60.0

# Loop a video for 30 seconds
- stepType: DisplayVideo
  videoName: "background"
  looping: true
  muteAudio: true
  duration: 30.0
```

---

### DisplayQuestion
Displays a multiple-choice question and waits for a response.

| Parameter | Type | Description |
|-----------|------|-------------|
| `question` | string | Question text |
| `options` | list of strings | List of response options |

```yaml
- stepType: DisplayQuestion
  question: "How do you feel?"
  options:
    - "Option 1"
    - "Option 2"
    - "Option 3"
```

---

### DisplayLikertScale
Displays a Likert scale question and waits for a response.

| Parameter | Type | Description |
|-----------|------|-------------|
| `question` | string | Question text |
| `leftLabel` | string | Label for the left (low) end of the scale |
| `rightLabel` | string | Label for the right (high) end of the scale |

```yaml
- stepType: DisplayLikertScale
  question: "How satisfied are you?"
  leftLabel: "Not satisfied"
  rightLabel: "Very satisfied"
```

---

### Break
Displays a break screen with instructions for a given duration.

| Parameter | Type | Description |
|-----------|------|-------------|
| `text` | string | Instructions to display during the break |
| `duration` | float | Break duration in seconds |

```yaml
- stepType: Break
  text: "Take a break."
  duration: 90
```

---

## Full Sequence Example

```yaml
steps:
  - stepType: LoadScene
    scenePath: "EmptyRoom"

  - stepType: LoadConfig
    fileName: DefaultConfig

  - stepType: DisplayText
    text: "Welcome to the experiment"
    duration: 3.0

  - stepType: Wait
    duration: 2.0

  - stepType: DisplayCamera
    duration: 20.0
    cameraID: "1"
    delay: 0.0

  - stepType: DisplayLikertScale
    question: "How natural did the movement feel?"
    leftLabel: "Not natural"
    rightLabel: "Very natural"

  - stepType: Break
    text: "Please take a short break."
    duration: 90

  - stepType: DisplayText
    text: "Thank you."
    duration: 4.0
```

# Config Editor
 
The Config Editor is a dedicated interface for creating and editing camera configuration files. It can be opened from the main GUI and provides a real-time preview of the scene from the participant's perspective.

 <img width="1932" height="1098" alt="Capture d&#39;écran 2026-05-20 080347" src="https://github.com/user-attachments/assets/fa9f5c5f-4c59-4a78-8a3d-d2e0d53dd35c" />

 
## Toolbar
 
| Button | Description |
|--------|-------------|
| **New** | Creates a new empty configuration |
| **Open** | Opens an existing config file from disk via a file browser |
| **Save** | Saves the current configuration to disk |
| **Save as...** | Saves the current configuration under a new name and location |
| **≡** (menu) | Opens a menu with clipboard options: **Copy** to copy the current configuration as YAML to the clipboard, and **Paste** to load a configuration from the clipboard |
| **Scene** | Dropdown to select which Unity scene to load in the preview (e.g. `EmptyRoom`) |
 
On the left of the status indicator, the name of the currently loaded config file is displayed. The status indicator shows a message reflecting the current state of the editor:
 
| Message | Color | Description |
|---------|-------|-------------|
| `No config loaded.` | Grey | Initial state at startup, no config is active |
| `Ready.` | Grey | Config is loaded and no pending changes |
| `Unsaved changes.` | Yellow | The config has been modified but not yet saved — displayed continuously until saved |
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

 
## Camera Panels (Camera 1, Camera 2, ...)
 
One panel is displayed per connected camera. Each panel contains the following controls.
 
**Display toggle** — enables or disables the real-time point cloud preview for that camera. Only one camera can be displayed at a time.
 
### Transform
Controls the position and rotation of the point cloud in the 3D scene.
 
| Control | Description |
|---------|-------------|
| **Position X / Y / Z** | Position of the point cloud in world space |
| **Rotation X / Y / Z** | Euler rotation of the point cloud |
 
### Depth
Controls the depth range captured by the camera. Points outside this range are discarded.
 
| Control | Description |
|---------|-------------|
| **Max** | Maximum capture depth in meters (0–10 m) |
| **Min** | Minimum capture depth in meters (0–10 m) |
 
### Clamp
Crops the visible area of the point cloud along X and Y axes. Values are normalized between `0` and `1`, where `0` is the left/bottom edge and `1` is the right/top edge of the camera frame.
 
| Control | Description |
|---------|-------------|
| **X Min / X Max** | Left and right crop boundaries |
| **Y Min / Y Max** | Bottom and top crop boundaries |
 
### Mirror
Flips the point cloud along a given axis.
 
| Button | Description |
|--------|-------------|
| **↔ Horizontal** | Flips the point cloud horizontally (X axis) |
| **↕ Vertical** | Flips the point cloud vertically (Y axis) |
 
## Stimulus Display
 
The **Stimulus Display** section controls the position and appearance of the in-world UI canvas — the panel used to display text, images, questions, and other stimuli to the participant.
 
| Control | Description |
|---------|-------------|
| **Display toggle** | Shows or hides the stimulus display canvas in the preview |
| **Position X / Y / Z** | Position of the canvas in world space |
| **Rotation Y** | Vertical rotation of the canvas |
| **Background color** | Background color of the canvas (click to open a color picker) |
 
## Preview Panel (right side)
 
The right side of the Config Editor shows a live preview of the scene split into two viewports.
 
| Viewport | Description |
|----------|-------------|
| **Participant view** | First-person view from the VR headset's perspective. Reflects the actual experience seen by the participant |
| **Static view** | Fixed camera view of the scene, useful for monitoring the overall layout |
 
Additional controls in the preview panel:
 
| Control | Description |
|---------|-------------|
| **VR edition** toggle | Enables editing mode inside the VR headset, allowing the researcher to adjust the config while wearing the headset |
| **Reset Headset Orientation** | Resets the headset's forward direction to the current facing direction |
| **Display Avatar** toggle | Shows or hides a body avatar in the scene |
| **Camera Position / Rotation** | Displays the current position and rotation of the static preview camera (read-only) |
 

# Config Files

Config files are YAML files located in `Input/Configs/`. They define the spatial configuration and depth settings for each Azure Kinect camera's point cloud.

```yaml
configName: DefaultConfig
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
```

## Stimulus Display

The `stimulusDisplay` block defines the position, orientation and background color of the in-world UI panel displayed during the experiment.

| Field | Type | Description |
|-------|------|-------------|
| `position` | Vector3 | Position of the display in the scene |
| `rotation` | Vector3 | Euler rotation of the display |
| `scale` | Vector3 | Scale of the display |
| `backgroundColor` | Color (r,g,b,a) | Background color of the display. Values are normalized floats between `0` and `1` |

## Point Clouds

The `pointClouds` list defines the spatial configuration, depth settings and spatial clipping for each camera.

| Field | Type | Description |
|-------|------|-------------|
| `iD` | int | Camera ID, must match the camera index |
| `position` | Vector3 | Position of the point cloud in the scene |
| `rotation` | Vector3 | Euler rotation of the point cloud |
| `scale` | Vector3 | Scale of the point cloud. Use `-1` on X or Y to flip the axis |
| `depthMax` | float | Maximum depth distance captured by the camera (in meters) |
| `depthMin` | float | Minimum depth distance captured by the camera (in meters) |
| `clampXMin` | float | Left boundary of the visible area, as a normalized value between `0` and `1` |
| `clampXMax` | float | Right boundary of the visible area, as a normalized value between `0` and `1` |
| `clampYMin` | float | Bottom boundary of the visible area, as a normalized value between `0` and `1` |
| `clampYMax` | float | Top boundary of the visible area, as a normalized value between `0` and `1` |

Config files can be created and edited directly through the application GUI, which saves changes automatically.

# Output Files

All output files are saved in the `Output/` folder at the root of the executable directory. They follow the naming format: `yyyyMMdd_HHmmss_sequenceFileName_filetype`.

There are two types of output files:

- **Event** (`_Events.txt`): A log file that records all events during the experiment, including errors, warnings, and state transitions. Useful for monitoring experiment progress and debugging.
- **Output** (`_Output.csv`): A CSV file containing all data collected during the experiment. Each row corresponds to a `DisplayCamera` step.

## Output file parameters

| Name | Type | Description |
|------|------|-------------|
| `Gender` | string | Participant's gender |
| `Age` | int | Participant's age |
| `Language` | string | Language used during the experiment |
| `SequenceFile` | string | Name of the sequence file used |
| `ConfigFile` | string | Name of the config file loaded |
| `TimeSinceStart` | double | Time elapsed since experiment start (seconds) |
| `StepType` | string | Type of the current step (e.g. `DisplayCamera`) |
| `StepCount` | int | Current step index |
| `CameraID` | string | ID of the camera displayed |
| `CameraDelay` | float | Temporal delay applied to the camera feed (seconds) |
| `CameraDisplayDuration` | float | Duration the camera was displayed (seconds) |
| `LikertResponse` | int | Response given to a Likert scale question |
| `LikertResponseTime` | double | Response time for the Likert scale (seconds) |
| `QuestionResponse` | string | Response given to a multiple-choice question |
| `QuestionResponseTime` | double | Response time for the question (seconds) |

# GUI

<img width="1607" height="912" alt="1" src="https://github.com/user-attachments/assets/df034183-59e6-41f2-a367-58aca8f5fc43" />

### Participant form
- **Age:** Enter the participant's age.
- **Gender:** Select the participant's gender (Male, Female, Other).

### Sequence form
- **Sequence File:** Select which sequence file to use for the experiment.

### Config form
- **Config File:** Select which camera configuration file to load.

### Experiment Status
- **Start Time:** Displays the time the experiment started.
- **Time since start:** Displays elapsed time since start.
- **Task steps:** Displays the current step index over the total number of steps.
- **Task progression:** A progress bar showing overall experiment progress.
- **Previous / Next step buttons:** Navigate between steps manually.

### Experiment Controls
- **Start button:** Starts the experiment. Requires a sequence file to be selected.
- **Play/Pause button:** Pauses or resumes the experiment. The current step restarts when resuming.
- **Stop button:** Stops the experiment and resets the state.

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Q | Start the experiment |
| W | Stop the experiment |
| Space | Play / Pause |
| ← / → | Previous / Next step |


<img width="1607" height="912" alt="1294" src="https://github.com/user-attachments/assets/a671e382-7cb6-4912-83de-cecffbcbfcc7" />

# Authors

Developed by Arnaud Droxler, Haotian Yao.  
With the help and advice of Bruno Herbelin.
