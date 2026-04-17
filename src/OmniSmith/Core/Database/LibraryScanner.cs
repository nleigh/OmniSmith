using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSmith.Core;
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

        public string CurrentStatus { get; private set; } = "Idle";
        public int TotalSongsFound { get; private set; } = 0;

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
                    CurrentStatus = $"Error: {ex.Message}";
                    Logger.Error($"Library Scanner Loop Error", ex);
                }

                CurrentStatus = "Idle (Waiting 30s)";
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
                // Reorder paths to prioritize Rocksmith DLC
                var paths = MidiPathsManager.MidiPaths.ToList();
                var dlcPath = paths.FirstOrDefault(p => p.Contains("Rocksmith2014", StringComparison.OrdinalIgnoreCase) && p.EndsWith("dlc", StringComparison.OrdinalIgnoreCase));
                if (dlcPath != null)
                {
                    paths.Remove(dlcPath);
                    paths.Insert(0, dlcPath);
                }

                TotalSongsFound = 0;

                foreach (var path in paths)
                {
                    if (token.IsCancellationRequested) break;
                    if (!Directory.Exists(path)) continue;

                    CurrentStatus = $"Scanning {Path.GetFileName(path)}...";
                    
                    var files = MidiPathsManager.SafeEnumerateFiles(path, token)
                        .Where(s => s.EndsWith(".psarc", StringComparison.OrdinalIgnoreCase) ||
                                    s.EndsWith(".mid", StringComparison.OrdinalIgnoreCase));
                                    
                    foreach (var file in files)
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            var info = new FileInfo(file);
                            double mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
                            long size = info.Length;

                            var existing = _db.GetMtimeSize(file);
                            if (existing != null && existing.Value.Mtime == mtime && existing.Value.Size == size)
                            {
                                TotalSongsFound++;
                                continue;
                            }
                            
                            CurrentStatus = $"Parsing {Path.GetFileName(file)}...";
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
                            TotalSongsFound++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error scanning file {file}", ex);
                        }
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
