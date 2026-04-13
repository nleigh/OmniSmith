using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSmith.Core.Database;
using OmniSmith.Domains.Guitar.Services;
using OmniSmith.Settings;

namespace OmniSmith.Core.Database
{
    public class LibraryScanner
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly LibraryDatabase _db;
        private bool _isScanning = false;

        public LibraryScanner()
        {
            _db = LibraryDatabase.Instance;
        }

        public void Start()
        {
            Task.Run(async () => await ScanLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task ScanLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ScanLibraryAsync(token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Library Scanner Error: {ex.Message}");
                }

                // Scan every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), token);
            }
        }

        private async Task ScanLibraryAsync(CancellationToken token)
        {
            if (_isScanning) return;
            _isScanning = true;

            try
            {
                foreach (var path in MidiPathsManager.MidiPaths)
                {
                    if (token.IsCancellationRequested) break;
                    if (!Directory.Exists(path)) continue;

                    var files = Directory.GetFiles(path, "*.psarc", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (token.IsCancellationRequested) break;

                        var info = new FileInfo(file);
                        double mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
                        long size = info.Length;

                        // We don't have a 'Get' method in LibraryDatabase yet, 
                        // but Put handles INSERT OR REPLACE.
                        // For performance, in a real app we'd check if it needs updating first.
                        
                        var meta = RocksmithParser.GetMetadata(file);
                        _db.Put(file, mtime, size, meta);
                    }
                }
            }
            finally
            {
                _isScanning = false;
            }
        }
    }
}
