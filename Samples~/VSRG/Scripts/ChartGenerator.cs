using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using RhythmCore;

/// <summary>
/// Provides utility class to generate pre-defined <see cref="Chart"/> instance.
/// </summary>
public static class ChartGenerator
{
    /// <summary>
    /// Generate a <see cref="Chart"/> instance with pre-defined <see cref="Channel"/> type.
    /// </summary>
    /// <returns>The generated <see cref="Chart"/>.</returns>
    public static Chart Simple()
    {
        var events = new List<RhythmEvent>
        {
            new RhythmEvent.Note
            {
                ID       = 0, // IMPORTANT: MUST BE UNIQUE!
                SampleID = "Music/tick",
                Channel  = Channel.Background,
                Position = 0f
            },
        };

        for (int i = 0; i < 20; i++)
        {
            events.Add(new RhythmEvent.Note
            {
                ID      = 1 + i, // IMPORTANT: MUST BE UNIQUE!
                Channel = i % 2 == 0 ? Channel.K1 : Channel.K2,
                Position = 0.25f * i
            });
        }

        var chart = new Chart
        {
            Title        = "Rhythm Core Test",
            Artist       = "CXO2",
            Illustrator  = "N/A",
            Bpm          = 120,
        };

        chart.AddEvents(Difficulty.Easy, events.ToArray());
        return chart;
    }
}