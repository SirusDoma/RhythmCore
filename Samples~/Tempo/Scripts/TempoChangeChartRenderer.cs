using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RhythmCore;

public class TempoChangeChartRenderer : LanelessChartRenderer
{
    protected override void Start()
    {
        base.Start();

        SoundBank.Load("Music/tick");
        Render(ChartGenerator.LanelessTempoChange(1.5f), Difficulty.Easy, new RenderConfig()
        {
            Speed       = 4.0f, // Be aware that the ChartRenderer.Offset doesn't account Config.Speed
            RenderDelay = 2f    // Add 2 Measure / Bar before starting the chart
        });
    }
}
