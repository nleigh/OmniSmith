using System;

namespace OmniSmith.Domains.Guitar.Models;

[Flags]
public enum NoteTechnique
{
    None = 0,
    Bend = 1,
    Slide = 2,
    UnpitchedSlide = 4,
    HammerOn = 8,
    PullOff = 16,
    Vibrato = 32,
    Harmonic = 64,
    PinchHarmonic = 128,
    Mute = 256,
    PalmMute = 512,
    Tremolo = 1024,
    Ignore = 2048
}
