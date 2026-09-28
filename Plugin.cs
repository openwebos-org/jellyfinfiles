using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.WebFileManager.Configuration;

namespace Jellyfin.Plugin.WebFileManager;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Web File Manager";

    public override Guid Id =>
        Guid.Parse("4a5d7d7e-1b2e-4c64-a3c1-8b5f2a9e71d4");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "WebFileManager",
            EmbeddedResourcePath =
                "Jellyfin.Plugin.WebFileManager.Configuration.fileManager.html"
        };
    }
}
