using OmniSmith.Core.Midi;
using OmniSmith.Ui.Windows;
using ImGuiNET;
using OmniSmith.Core.Interfaces;
using OmniSmith.Domains.Piano;

namespace OmniSmith.Core;

public class Application
{
    public static Application AppInstance;
    public static bool IsLoading { get; set; } = false;
    public static IPlayableSong? CurrentSong { get; set; }
    protected bool _isRunning = true;
    protected List<ImGuiWindow> _imguiWindows = new();

    public Application()
    {
        AppInstance = this;
        Init();
    }

    private void Init()
    {
        CreateWindows();
    }

    private void CreateWindows()
    {
        HomeWindow homeWindow = new();
        MidiBrowserWindow midiBrowserWindow = new();
        ModeSelectionWindow modeSelectionWindow = new();
        MidiPlaybackWindow midiPlaybackWindow = new();
        PlayModeWindow playModeWindow = new();
        SettingsWindow settingsWindow = new();
        _imguiWindows.Add(homeWindow);
        _imguiWindows.Add(midiBrowserWindow);
        _imguiWindows.Add(modeSelectionWindow);
        _imguiWindows.Add(midiPlaybackWindow);
        _imguiWindows.Add(playModeWindow);
        _imguiWindows.Add(settingsWindow);
    }

    public List<ImGuiWindow> GetWindows()
    {
        return _imguiWindows;
    }

    public void OnUpdate()
    {
        // Update active song logic (audio sync, highway time, etc)
        Application.CurrentSong?.Update(MidiPlayer.Timer);

        if (MidiPlayer.ShouldAdvanceQueue)
        {
            MidiPlayer.ShouldAdvanceQueue = false;
            string nextFile = SongQueueManager.GetNext();
            if (nextFile != null)
            {
                Application.IsLoading = true;
                _ = Task.Run(async () => {
                    try {
                        var song = await SongFactory.LoadSongAsync(nextFile);
                        
                        // Dispose previous song first to release audio devices
                        Application.CurrentSong?.Dispose();
                        Application.CurrentSong = null;

                        // Ensure MIDI playback is cleared so it doesn't bleed into new Guitar bindings
                        if (MidiPlayer.Playback != null)
                        {
                            try { MidiPlayer.Playback.Stop(); } catch { }
                            MidiPlayer.Playback.Dispose();
                            MidiPlayer.Playback = null!;
                            MidiPlayer.StopTimer();
                        }

                        if (song is PianoSong pianoSong)
                        {
                            MidiFileHandler.LoadMidiFile(pianoSong.SongFile);
                        }
                        else if (song is OmniSmith.Domains.Guitar.GuitarSong guitarSong)
                        {
                            guitarSong.InitAudio();
                        }

                        Application.CurrentSong = song;
                        
                        // Start audio/timer logic
                        MidiPlayer.Timer = 0;
                        MidiPlayer.Seconds = 0;
                        if (MidiPlayer.Playback != null)
                        {
                            MidiPlayer.Playback.Start();
                            MidiPlayer.StartTimer();
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error loading song: {ex.Message}");
                    } finally {
                        Application.IsLoading = false;
                    }
                });
            }
        }

        foreach (ImGuiWindow window in GetWindows())
        {
            if (window.Active())
                window.RenderWindow();
        }

        ImGuiController.UpdateMouseCursor();
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    public void Quit()
    {
        MidiPlayer.SoundFontEngine?.Dispose();
        _isRunning = false;
    }
}
