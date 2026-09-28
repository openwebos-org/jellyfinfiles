using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.WebFileManager.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string RootPath { get; set; } = "/media";
}
