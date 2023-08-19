using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/// <summary>
/// Represents a collection of <see cref="AudioClip"/>.
/// </summary>
/// <remarks>
/// DO NOT USE! This is only sample code. Please implement Sound Bank properly by yourself (if needed).
/// </remarks>
public static class SoundBank
{
    private static Dictionary<string, AudioClip> _clips = new();

    /// <summary>
    /// Gets a value indicates whether the registered <see cref="AudioClip"/>'s are loaded and ready to play.
    /// </summary>
    public static bool Ready => _clips?.Values.All(c => c?.loadState == AudioDataLoadState.Loaded) ?? false;

    /// <summary>
    /// Load <see cref="AudioClip"/>'s by the specified clip paths.
    /// </summary>
    /// <param name="clips">The <see cref="Resources"/> filename of the <see cref="AudioClip"/>.</param>
    public static void Load(params string[] clips)
    {
        _clips = new Dictionary<string, AudioClip>();
        foreach (string name in clips)
            _clips[name] = Resources.Load<AudioClip>(name);
    }

    /// <summary>
    /// Gets the <see cref="AudioClip"/> associated with the specified key.
    /// </summary>
    /// <param name="clip">The name of the <see cref="AudioClip"/> to get.</param>
    /// <returns><see cref="AudioClip"/> if the <see cref="SoundBank"/> contains an element with the specified name; otherwise, <see langword="null"/>.</returns>
    public static AudioClip Get(string clip)
    {
        return _clips.TryGetValue(clip, out var result) ? result : null;
    }
}