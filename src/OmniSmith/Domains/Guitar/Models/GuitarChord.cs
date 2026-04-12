using System.Collections.Generic;

namespace OmniSmith.Domains.Guitar.Models;

public class GuitarChord
{
    public float Time { get; set; }
    public int ChordId { get; set; }
    public List<GuitarNote> Notes { get; set; } = new();
}
