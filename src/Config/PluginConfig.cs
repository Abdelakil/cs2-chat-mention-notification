namespace OstoraPlayerMention;

public class PluginConfig
{
    public List<string> ToggleCommands { get; set; } = ["mention", "om", "ostoramention"];
    public int PlayerCooldown { get; set; } = 5; // in seconds
    public List<string> UsePermissions { get; set; } = ["ostora.mention.use"];
    // Everyone can receive mentions - no permission required
    public string MentionSound { get; set; } = "mention.mp3";
    public float SoundVolume { get; set; } = 0.6f;
    public bool CaseSensitiveMentions { get; set; } = false;
}
