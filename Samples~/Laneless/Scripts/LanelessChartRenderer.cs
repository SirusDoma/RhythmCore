using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using RhythmCore;

public class LanelessChartRenderer : VsrgChartRenderer
{
    protected override void Awake()
    {
        base.Awake();

        // Channel still need to be configured for lane-less game mode.
        // Otherwise the GetFrontEventList<T>() will always return null events.
        ConfigureChannel<float>();
    }

    protected override void Start()
    {
        SoundBank.Load("Music/tick");
        Render(ChartGenerator.Laneless(1.5f), Difficulty.Easy, new RenderConfig()
        {
            Speed       = 2.0f, // Be aware that the ChartRenderer.Offset doesn't account Config.Speed
            RenderDelay = 2f    // Add 2 Measure / Bar before starting the chart
        });
    }

    public override Object Instantiate(RhythmEvent ev)
    {
        if (ev is not RhythmEvent.Note<float> playableEv)
            return null;

        // Instantiate the note
        var target = Instantiate(note);
        var component = target.GetComponent<VsrgNote>();

        // Initial position should be outside camera
        target.transform.position = new Vector2(playableEv.Channel, (ev.Position - Position) * Config.Speed);

        // Configure the note script
        component.EventState        = GetEventState(ev);
        component.NoteSpeed         = Config.Speed;      // Speed need to be applied to the note by the note and not renderer
        component.QueryRenderState += () => RenderState; // The captured RenderState at the beginning of this frame

        // Assign the sound to the notes if it's key-sound (or background) note
        if (!string.IsNullOrEmpty(playableEv.SampleID))
            component.AudioClip = SoundBank.Get(playableEv.SampleID);

        return target;
    }

    protected override void HandleInput()
    {
        if (!autoplay && !Input.anyKeyDown)
            return;

        // Get configured Judgment and front buffer
        var judgment = GetJudgment<Accuracy>();
        var front     = GetFrontEventList<float>();

        // Get the front event (the closest event that available for Judgment)
        // For lane-less game mode, we need to iterate the front events in all "Channels"
        foreach (var ev in front.GetFrontEvents<RhythmEvent.Note<float>>())
        {
            // Check the distance between Judgment and note
            float latency = ev.Position - Position;
            var result    = judgment.Evaluate(ev);
            if ((autoplay && latency < 0) || (!autoplay && result.Accurracy != Accuracy.None))
            {
                Judge<Accuracy>(ev, result);

                // In real-world (touch screen) scenario, you have to prevent the touch to process other event within specific channel ranges
                // You may also want to track the hold state of your touch for various note types such as long notes and drag notes
                break;
            }
        }
    }
}
