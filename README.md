# 🖱️ SmoothScroll

**Buttery smooth scrolling for all Windows applications**

![SmoothScroll Banner](https://img.shields.io/badge/version-1.0.0-blue) ![Windows](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Features

- 😍 **Save Your Eyes** - Fluid, natural scroll animation that's easy on the eyes
- 🤞 **Prevent RSI** - Scroll acceleration reduces repetitive strain injury risks
- ⚙️ **Highly Customizable** - Adjust smoothness, speed, friction, and animation curves
- 📱 **Per-App Settings** - Configure custom scroll behavior for specific applications
- 🎨 **Beautiful UI** - Modern, dark-themed interface with smooth animations
- 🔧 **System Tray** - Runs quietly in the background

## 🚀 Installation

1. Download `SmoothScroll.exe` from the `publish` folder
2. Run the application
3. (Optional) Enable "Start with Windows" in settings

## 🎛️ Settings

### Animation Options

| Setting | Description | Range |
|---------|-------------|-------|
| **Smoothness** | How smooth the scrolling feels | Very Smooth → Instant |
| **Scroll Speed** | Multiplier for scroll distance | 0.5x → 3.0x |
| **Momentum/Friction** | How long scroll continues after release | Low → Very High |
| **Animation Curve** | Easing function for animation | Linear, EaseOut, Elastic, etc. |

### Animation Curves

- **Linear** - Constant speed throughout
- **Ease Out Quad** - Default, natural deceleration
- **Ease Out Cubic** - Smooth, gradual stop
- **Ease Out Expo** - Quick start, slow finish
- **Ease Out Circ** - Circular motion feel
- **Ease In Out Quad** - Smooth start and end
- **Elastic** - Bouncy, playful feel
- **Back** - Slight overshoot effect

## 📱 Per-App Settings

You can exclude specific applications from smooth scrolling (useful for video players, design software, etc.):

**Default Excluded Apps:**
- VLC Media Player
- MPC-HC
- PotPlayer
- Adobe Photoshop
- Adobe Illustrator

To add more apps, click **"+ Add App"** and enter the process name (e.g., `chrome`, `notepad`, `firefox`).

## 🔧 Technical Details

- Built with **WPF (.NET 8)**
- Uses **low-level mouse hooks** for system-wide scrolling
- **60-120 FPS** animation engine
- **JSON-based settings** stored in `%AppData%\SmoothScroll\settings.json`

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Close Window | Minimize to tray |
| Double-click Tray | Open window |
| Right-click Tray | Context menu |

## 📝 Changelog

### v1.0.0
- Initial release
- Smooth scrolling engine with multiple easing functions
- Per-application settings
- Modern dark theme UI
- System tray integration
- Start with Windows option

## 🤝 Contributing

Feel free to submit issues and pull requests!

## 📄 License

MIT License - Feel free to use, modify, and distribute.

---

**Made with ❤️ for smooth scrolling enthusiasts**
