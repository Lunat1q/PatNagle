# 🎣 PatNagle

**PatNagle** is an external, purely visual fishing bot for **World of Warcraft**. It uses advanced pixel scanning to locate your bobber and detect the specific "splash" animation (bobber dive) without reading game memory.

> *"I could wait and fish all day!"*

![Language](https://img.shields.io/badge/language-C%23-blue.svg) ![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg) ![Status](https://img.shields.io/badge/status-active-success.svg)

## Coffee

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/lunatiq)

## Screenshot
<img width="737" height="579" alt="image" src="https://github.com/user-attachments/assets/e41421cd-db26-4fae-a225-82e6f91562d3" />

## Contact me:
[Discord](https://discord.gg/mHrbVSuKRG)


## ✨ Features

* **👁️ Visual Splash Detection:** Uses computer vision to detect the specific moment the bobber "dives," ensuring accurate catches without relying on sound.
* **🎯 Adjustable Precision:**
  * **Zone Range:** Resize the scanning area to focus on the bobber and ignore the rest of the screen.
  * **Dive Threshold:** Fine-tune the sensitivity of the splash detection to avoid false positives (e.g., water ripples).
  * **Color Tolerance:** Adjust `Color max distance` to ensure the bot tracks the bobber regardless of lighting or water color.
* **🖱️ Click Calibration:** Includes X/Y offset sliders to fine-tune exactly where the mouse clicks relative to the detected bobber.
* **⌨️ Advanced Hotkeys:** Supports complex key combinations (e.g., `Ctrl + Alt + F`) for starting/stopping and toggling the overlay.
* **📌 Always on Top:** Built-in "Topmost" toggle to keep the control panel visible over the game window.

## 🛠️ Configuration Guide

The **Settings Menu** gives you full control over the detection logic. Here is what every slider does:

### 🔍 Detection Settings
| Setting | Description |
| :--- | :--- |
| **Bobber zone range** | Expands or shrinks the search area around the mouse cursor where the bot looks for the bobber. |
| **Bobber dive threshold** | **Sensitivity.** Determines how much the bobber needs to move/splash to trigger a click. Lower this if the bot misses subtle catches; raise it if waves are triggering false clicks. |
| **Color max distance** | **Tolerance.** Adjusts how strict the color matching is. Increase this if the bot loses track of the bobber in dark/night waters. |
| **Thread sleep time** | Controls the scanning frequency. Lower values = faster reaction but higher CPU usage. |

### 🖱️ Mouse Calibration
| Setting | Description |
| :--- | :--- |
| **Mouse hook x offset** | Adjusts the click position horizontally relative to the detected object. |
| **Mouse hook y offset** | Adjusts the click position vertically. |

### ⌨️ Controls & Hotkeys
You can bind keys using the dropdowns and modifier checkboxes (`Alt`, `Control`).

* **Cast Key:** The key bound to your fishing skill in WoW (e.g., `F`).
* **Start/Stop:** Global hotkey to toggle the bot on/off.
* **Topmost:** Global hotkey to force the settings window to stay on top of the game.

## 🚀 Usage

1. **Launch WoW** and equip your fishing pole.
2. **Run `PatNagle.exe`** as Administrator.
3. **Position Camera:** Zoom in slightly and angle the camera so water fills most of the screen (removes background distractions).
4. **Calibrate:**
   * Cast your line manually once.
   * Adjust **Bobber zone range** until the bot "sees" the bobber.
   * Adjust **Color max distance** until detection is stable.
5. **Start:** Press your configured **Start/Stop** combination (Default is often `Ctrl + Alt + F`).

## ⚠️ Disclaimer
> [!WARNING]
> **Use at your own risk.**
> This software automates gameplay via visual analysis and input simulation. While it does not read game memory, automated gameplay is against Blizzard's Terms of Service. The developer is not responsible for actions taken against your account.

---

## 📄 License
Distributed under the MIT License.
