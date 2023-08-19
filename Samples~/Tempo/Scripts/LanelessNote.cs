using RhythmCore;

/// <summary>
/// Represents Note for Lane-less rhythm game.
/// </summary>
public class LanelessNote : RhythmEvent.Note<float>
{
    /// <summary>
    /// Gets a value indicates whether the note is playable.
    /// </summary>
    /// <remarks>
    /// This implementation assume that every note with a sample is simply a background note.
    /// As such, Key-sounded note is not possible with this class.
    /// </remarks>
    public override bool Playable => string.IsNullOrEmpty(SampleID);
}