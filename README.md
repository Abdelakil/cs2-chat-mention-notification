<div align="center">
  <h2><strong>Ostora Player Mention</strong></h2>
  <h3>A powerful plugin for SwiftlyS2 that enables player mentions with sound notifications.</h3>
</div>

## 🎯 Features

- **Player Mention Detection**: Detect "@playername" in chat messages
- **Sound Notifications**: Play custom sounds when players are mentioned
- **Permission System**: Simple control for who can use mentions
- **Cooldown Management**: Prevent spam with configurable cooldowns
- **Private Notifications**: Only mentioned players receive notifications (messages hidden from chat)
- **Multi-language Support**: Easy-to-extend translation structure

## 📋 Requirements

- **[SwiftlyS2](https://github.com/swiftly-solution/swiftlys2)**
- **[AudioApi](https://github.com/SwiftlyS2-Plugins/Audio)**

## 🚀 Installation

1. Download the latest release
2. Extract to your server's `addons/swiftlys2/plugins/` directory
3. Restart your server or reload plugins
4. Configure the plugin as needed

## ⚙️ Configuration

Configuration file: `addons/swiftlys2/configs/plugins/OstoraPlayerMention/config.toml`

```toml
[OstoraPlayerMention]
ToggleCommands = ["mention", "om", "ostoramention"]
PlayerCooldown = 5
UsePermissions = ["ostora.mention.use"]
# Everyone can receive mentions - no permission required
MentionSound = "mention.mp3"
SoundVolume = 0.6
CaseSensitiveMentions = false
```

## 🎵 Sound Setup

Place sound files in: `addons/swiftlys2/data/OstoraPlayerMention/`

Default sound: `mention.mp3`

## 🔧 Permissions

- `ostora.mention.use` - Allow players to use mentions
- **Everyone** can receive mention notifications (no permission required)
- **No admin system** - simplified for community use

## 💬 Usage

### Mention Players
Type `@playername` in chat to mention a player:
```
@PlayerName Hey there!
```

### Toggle Notifications
- `!mention` - Toggle mention sounds on/off
- `!om` - Alternative toggle command
- `!ostoramention` - Full command name

## 🌍 Translation

Add custom translations in: `resources/translations/`

Supported languages:
- English (en.jsonc)

## 🔧 Advanced Features

- **Case Sensitivity**: Configure whether mentions are case-sensitive
- **Private Mentions**: Mention messages are hidden from public chat
- **Sound Volume**: Adjustable notification volume
- **Cooldown System**: Prevent spam with per-player cooldowns

## 🐛 Troubleshooting

- Ensure AudioApi plugin is installed and loaded
- Check sound files are in the correct directory
- Verify permissions are properly configured
- Check server console for error messages

## 📄 License

This plugin is released under the MIT License.

## 🙏 Acknowledgments

- **[T3Marius/SwiftlyS2-SankySounds](https://github.com/T3Marius/SwiftlyS2-SankySounds)** - Inspiration and reference for audio integration and plugin structure
- **[SwiftlyS2](https://github.com/swiftly-solution/swiftlys2)** - The framework that makes this plugin possible
- **[AudioApi](https://github.com/SwiftlyS2-Plugins/Audio)** - Audio system integration
