namespace OmniSmith.Core;
using OmniSmith.Enums;

public static class WindowsManager
{
    public static Windows Window { get; private set; }

    public static void SetWindow(Windows window)
    {
        if (window != Windows.MidiPlayback && window != Windows.PlayMode)
        {
            Program._window.Title = $"OmniSmith {ProgramData.ProgramVersion}";
        }

        foreach (var win in Application.AppInstance.GetWindows())
        {
            win.SetActive(win.GetId() == window.ToString());
        }

        Window = window;
    }
}
