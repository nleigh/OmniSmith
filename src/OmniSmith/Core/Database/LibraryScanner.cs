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
        private readonly SemaphoreSlim _scanLock = new(1, 1);

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
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Library Scanner Error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ScanLibraryAsync(CancellationToken token)
        {
            if (!await _scanLock.WaitAsync(0, token)) return;

            try
            {
                foreach (var path in MidiPathsManager.MidiPaths)
                {
                    if (token.IsCancellationRequested) break;
                    if (!Directory.Exists(path)) continue;

                    var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".psarc", StringComparison.OrdinalIgnoreCase) ||
                                    s.EndsWith(".mid", StringComparison.OrdinalIgnoreCase));
                                    
                    foreach (var file in files)
                    {
                        if (token.IsCancellationRequested) break;

                        var info = new FileInfo(file);
                        double mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
                        long size = info.Length;

                        var existing = _db.GetMtimeSize(file);
                        if (existing != null && existing.Value.Mtime == mtime && existing.Value.Size == size)
                        {
                            continue;
                        }
                        
                        SongMeta meta;
                        if (file.EndsWith(".psarc", StringComparison.OrdinalIgnoreCase))
                        {
                            meta = RocksmithParser.GetMetadata(file);
                        }
                        else
                        {
                            meta = new SongMeta(
                                Title: Path.GetFileNameWithoutExtension(file),
                                Artist: "", Album: "", Year: "", Duration: 0, Tuning: "", Arrangements: "", HasLyrics: false
                            );
                        }
                        
                        _db.Put(file, mtime, size, meta);
                    }
                }
            }
            finally
            {
                _scanLock.Release();
            }
        }
    }
}
