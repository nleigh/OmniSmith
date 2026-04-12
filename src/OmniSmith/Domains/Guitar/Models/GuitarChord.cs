using System.Collections.Generic;

namespace OmniSmith.Domains.Guitar.Models;

public class GuitarChord
{
    public float Time { get; set; }
    public int ChordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<GuitarNote> ChordNotes { get; set; } = new();
}
