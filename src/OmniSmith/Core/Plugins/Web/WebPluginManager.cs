using OmniSmith.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniSmith.Core.Plugins.Web;

public static class WebPluginManager
{
    private static readonly List<IWebPlugin> _plugins = new();
    public static bool IsSearching { get; private set; }

    public static void RegisterPlugin(IWebPlugin plugin)
    {
        if (!_plugins.Any(p => p.GetPluginName() == plugin.GetPluginName()))
        {
            _plugins.Add(plugin);
            Logger.Info($"WebPluginManager: Registered plugin '{plugin.GetPluginName()}'");
        }
    }

    public static List<string> GetAvailablePluginNames()
    {
        return _plugins.Select(p => p.GetPluginName()).ToList();
    }

    public static async Task<List<SearchResult>> SearchAllAsync(string query)
    {
        IsSearching = true;
        var allResults = new List<SearchResult>();
        
        try
        {
            var tasks = _plugins.Select(async p =>
            {
                try
                {
                    return await p.SearchAsync(query);
                }
                catch (Exception ex)
                {
                    Logger.Error($"WebPluginManager: Plugin '{p.GetPluginName()}' search failed", ex);
                    return new List<SearchResult>();
                }
            });

            var results = await Task.WhenAll(tasks);
            foreach (var resultList in results)
            {
                allResults.AddRange(resultList);
            }
        }
        finally
        {
            IsSearching = false;
        }

        return allResults;
    }

    public static async Task<List<SearchResult>> SearchSingleAsync(string pluginName, string query)
    {
        var plugin = _plugins.FirstOrDefault(p => p.GetPluginName() == pluginName);
        if (plugin == null) return new List<SearchResult>();

        IsSearching = true;
        try
        {
            return await plugin.SearchAsync(query);
        }
        catch (Exception ex)
        {
            Logger.Error($"WebPluginManager: Plugin '{pluginName}' search failed", ex);
            return new List<SearchResult>();
        }
        finally
        {
            IsSearching = false;
        }
    }

    public static async Task<string> DownloadSongAsync(string pluginName, string id, string destinationFolder)
    {
        var plugin = _plugins.FirstOrDefault(p => p.GetPluginName() == pluginName);
        if (plugin == null) throw new Exception($"Plugin '{pluginName}' not found.");

        return await plugin.DownloadAsync(id, destinationFolder);
    }
}
