using Dalamud.Configuration;

namespace Divination.TwitterIntegration;

public class PluginConfig : IPluginConfiguration
{
    public bool ShowListTimeline = false;
    public string ListId = string.Empty;
    public int UpdateIntervalInMs = 3000;
    public long? SinceId = null;

    public int Version { get; set; } = 1;
}
