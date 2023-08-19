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

    /// <summary>
    /// Generate a <see cref="Chart"/> instance using single floating value as the <see cref="RhythmEvent.Note.Channel"/> type.
    /// </summary>
    /// <param name="range">The range of generated channel.</param>
    /// <returns>The generated <see cref="Chart"/>.</returns>
    public static Chart Laneless(float range = 3.5f)
    {
        var events = new List<RhythmEvent>
        {
            new LanelessNote
            {
                ID       = 0, // IMPORTANT: MUST BE UNIQUE!
                SampleID = "Music/tick",
                Channel  = 0f,
                Position = 0f
            },
        };

        float max = 10f;
        for (int i = 0; i < 20; i++)
        {
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + i, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
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

    /// <summary>
    /// Generate a <see cref="Chart"/> instance with complex tempo changes.
    /// </summary>
    /// <param name="range">The range of generated channel.</param>
    /// <returns>The generated <see cref="Chart"/>.</returns>
    public static Chart LanelessTempoChange(float range = 3.5f)
    {
        var events = new List<RhythmEvent>
        {
            new LanelessNote
            {
                ID       = 0, // IMPORTANT: MUST BE UNIQUE!
                SampleID = "Music/tick",
                Channel  = 0f,
                Position = 0f
            },
        };

        float max  = 10f;
        float offset = 0;

        // Normal BPM
        for (int i = 0; i < 10; i++)
        {
            offset = 0.25f * i;
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + i, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
                Position = offset
            });
        }

        events.Add(new RhythmEvent.Tempo
        {
            ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
            Bpm     = 240,
            Position = offset
        });

        // BPM: 240
        for (int i = 10; i < 15; i++)
        {
            offset += 0.25f * 2;
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
                Position = offset
            });
        }

        events.Add(new RhythmEvent.Tempo
        {
            ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
            Bpm     = 60,
            Position = offset
        });

        // BPM: 60
        for (int i = 15; i < 20; i++)
        {
            offset += 0.25f / 2;
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
                Position = offset
            });
        }

        // Skip and Stop
        events.Add(new RhythmEvent.Tempo
        {
            ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
            Skip    = 2 + (0.25f * 3),  // Skip 2 Measures and 3 Beats
            Stop    = 1f,               // Stop the renderer for 1 measure
            Position = offset
        });

        offset += 1f * 3;

        // BGM again
        events.Add(new LanelessNote
        {
            ID       = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
            SampleID = "Music/tick",
            Channel  = 0f,
            Position = offset
        });

        // BPM: 60
        for (int i = 0; i < 10; i++)
        {
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
                Position = offset
            });

            offset += 0.25f / 2f;
        }

        offset -= 0.25f / 2f;
        events.Add(new RhythmEvent.Tempo
        {
            ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
            Bpm     = 120,
            Position = offset
        });

        // BPM: 120
        for (int i = 0; i < 10; i++)
        {
            offset += 0.25f;
            float pingpong = (int)Mathf.PingPong(i / 0.5f, max);
            events.Add(new LanelessNote
            {
                ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
                Channel = Mathf.Lerp(-range, range, pingpong / max),
                Position = offset
            });

            events.Add(new RhythmEvent.Tempo
            {
                ID      = 1 + events.Count, // IMPORTANT: MUST BE UNIQUE!
                Skip    = 0.25f,
                Stop    = 0.25f,
                Position = offset
            });
        }
        events.Remove(events.Last());

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