namespace OmniSmith.Domains.Guitar.Models;

public class GuitarNote
{
    public float Time { get; set; }
    public int String { get; set; }
    public int Fret { get; set; }
    public float Duration { get; set; }
    public NoteTechnique Techniques { get; set; }
    public float BendValue { get; set; }
    public int SlideTo { get; set; }
    public int Finger { get; set; } = -1;
}
