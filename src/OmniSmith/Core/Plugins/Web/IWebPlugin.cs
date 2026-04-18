using OmniSmith.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniSmith.Core.Plugins.Web;

public interface IWebPlugin
{
    string GetPluginName();
    Task<List<SearchResult>> SearchAsync(string query);
    Task<string> DownloadAsync(string id, string destinationFolder);
}
