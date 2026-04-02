using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AudioApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using Tomlyn.Extensions.Configuration;

namespace OstoraPlayerMention;

[PluginMetadata(Id = "OstoraPlayerMention", Version = "1.0.0", Name = "Ostora Player Mention", Author = "Zenjibad", Description = "Player mention system with sound notifications. Inspired by SwiftlyS2-SankySounds by T3Marius.", Website = "https://ostora.xyz")]
public partial class OstoraPlayerMention : BasePlugin
{
    private ServiceProvider? _provider;
    private static IAudioApi? _audio;
    private static PluginConfig Config { get; set; } = new();
    private ConcurrentDictionary<ulong, bool> _hasMentionEnabled { get; set; } = new();
    private ConcurrentDictionary<ulong, DateTime> _lastMentionTime { get; set; } = new();
    private readonly TimeSpan _playerCooldown = TimeSpan.FromSeconds(Config.PlayerCooldown);
    
    public OstoraPlayerMention(ISwiftlyCore core) : base(core) { }
    
    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        _audio = interfaceManager.GetSharedInterface<IAudioApi>("audio");
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeTomlWithModel<PluginConfig>("config.toml", "OstoraPlayerMention")
            .Configure(builder =>
            {
                builder.AddTomlFile("config.toml", optional: false, reloadOnChange: true);
            });

        ServiceCollection services = new();
        services
            .AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<PluginConfig>()
            .BindConfiguration("OstoraPlayerMention");

        _provider = services.BuildServiceProvider();
        Config = _provider.GetRequiredService<IOptions<PluginConfig>>().Value;

        // Register toggle commands
        foreach (var cmd in Config.ToggleCommands)
        {
            Core.Command.RegisterCommand(cmd, (context) =>
            {
                if (context.Sender is not IPlayer player)
                    return;

                bool currentlyEnabled = _hasMentionEnabled.TryGetValue(player.SteamID, out bool enabled) && enabled;
                bool newState = !currentlyEnabled;

                _hasMentionEnabled[player.SteamID] = newState;
                string state = newState ? "enabled" : "disabled";

                player.SendMessage(MessageType.Chat, Core.Translation.GetPlayerLocalizer(player)["prefix"] + Core.Translation.GetPlayerLocalizer(player)["mention_toggle", state]);
            });
        }
    }

    [ClientChatHookHandler]
    public HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        IPlayer player = Core.PlayerManager.GetPlayer(playerId);
        
        if (!HasUsePermission(player))
        {
            return HookResult.Continue;
        }

        // Check cooldown
        if (IsPlayerOnCooldown(player))
        {
            double remaining = (_playerCooldown - (DateTime.UtcNow - _lastMentionTime[player.SteamID])).TotalSeconds;
            player.SendMessage(
                MessageType.Chat,
                Core.Translation.GetPlayerLocalizer(player)["prefix"] +
                Core.Translation.GetPlayerLocalizer(player)["mention_cooldown", Math.Ceiling(remaining).ToString()]
            );
            return HookResult.Continue;
        }

        // Process mentions and modify chat message
        string processedText = ProcessMentions(text, player);
        
        if (processedText != text)
        {
            // Send the modified message to all players
            foreach (var targetPlayer in Core.PlayerManager.GetAllPlayers())
            {
                targetPlayer.SendMessage(MessageType.Chat, processedText);
            }
            
            return HookResult.Stop; // Prevent original message from showing
        }

        return HookResult.Continue;
    }

    private string ProcessMentions(string text, IPlayer sender)
    {
        string pattern = "@(\\w+)";
        var matches = Regex.Matches(text, pattern);
        string processedText = text;

        foreach (Match match in matches)
        {
            string mentionedName = match.Groups[1].Value;
            IPlayer? mentionedPlayer = FindPlayerByName(mentionedName);

            if (mentionedPlayer != null)
            {
                _lastMentionTime[sender.SteamID] = DateTime.UtcNow;
                
                // Play sound for mentioned player
                PlayMentionSound(mentionedPlayer);
                
                // Replace @playername with actual player name in green
                processedText = processedText.Replace(match.Value, $"[green]{mentionedPlayer.Controller.PlayerName}[default]");
            }
        }

        return processedText;
    }

    private IPlayer? FindPlayerByName(string name)
    {
        var comparison = Config.CaseSensitiveMentions ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        
        foreach (var player in Core.PlayerManager.GetAllPlayers())
        {
            if (player.Controller.PlayerName.Equals(name, comparison))
            {
                return player;
            }
        }
        return null;
    }

    private void PlayMentionSound(IPlayer player)
    {
        if (_audio == null || !_hasMentionEnabled.TryGetValue(player.SteamID, out bool enabled) || !enabled)
            return;

        try
        {
            IAudioChannelController controller = _audio.UseChannel("ostora_mention");
            IAudioSource source = _audio.DecodeFromFile(Path.Combine(Core.PluginDataDirectory, Config.MentionSound));
            controller.SetSource(source);
            controller.SetVolume(player.PlayerID, Config.SoundVolume);
            controller.Play(player.PlayerID);
        }
        catch (Exception ex)
        {
            Core.Logger.LogWarning($"Failed to play mention sound for player {player.Controller.PlayerName}: {ex.Message}");
        }
    }

    private bool IsPlayerOnCooldown(IPlayer player)
    {
        if (!_lastMentionTime.TryGetValue(player.SteamID, out DateTime lastTime))
            return false;

        return DateTime.UtcNow - lastTime < _playerCooldown;
    }

    private bool HasUsePermission(IPlayer player)
    {
        return HasAnyPermission(player, Config.UsePermissions);
    }


    private bool HasAnyPermission(IPlayer player, List<string> permissions)
    {
        foreach (string permission in permissions)
        {
            if (ulong.TryParse(permission, out ulong steamId))
            {
                if (player.SteamID == steamId)
                    return true;
            }
            else
            {
                if (Core.Permission.PlayerHasPermission(player.SteamID, permission))
                    return true;
            }
        }
        return false;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event)
    {
        if (@event.UserIdPlayer is not IPlayer player)
            return HookResult.Continue;

        _hasMentionEnabled.TryAdd(player.SteamID, true);
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (@event.UserIdPlayer is not IPlayer player)
            return HookResult.Continue;

        _hasMentionEnabled.TryRemove(player.SteamID, out _);
        _lastMentionTime.TryRemove(player.SteamID, out _);
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult EventMapTransition(EventMapTransition @event)
    {
        _hasMentionEnabled.Clear();
        _lastMentionTime.Clear();
        return HookResult.Continue;
    }

    public override void Unload()
    {
        _provider?.Dispose();
    }
}
