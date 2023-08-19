using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

using RhythmCore;

public class VsrgChartRenderer : ChartRenderer
{
    public bool autoplay;
    public GameObject note;
    public Text stats;

    public override bool Ready => SoundBank.Ready;

    protected virtual void Start()
    {
        SoundBank.Load("Music/tick");
        Render(ChartGenerator.Simple(), Difficulty.Easy, new RenderConfig()
        {
            Speed       = 3.0f, // Be aware that the ChartRenderer.Offset doesn't account Config.Speed
            RenderDelay = 2f    // Add 2 Measure / Bar before starting the chart
        });
    }

    protected override void Update()
    {
        base.Update();

        HandleInput();
    }

    public override Object Instantiate(RhythmEvent ev)
    {
        // Instantiate the note
        var target    = Instantiate(note);
        var component = target.GetComponent<VsrgNote>();

        // Initial position should be outside camera
        target.transform.position = new Vector2(0, (ev.Position - Position) * Config.Speed);

        // Configure the note script
        component.EventState        = GetEventState(ev);
        component.NoteSpeed         = Config.Speed;      // Speed need to be applied to the note by the note and not renderer
        component.QueryRenderState += () => RenderState; // The captured RenderState at the beginning of this frame

        // Assign the sound to the notes if it's key-sound (or background) note
        if (ev is RhythmEvent.Note sample && !string.IsNullOrEmpty(sample.SampleID))
            component.AudioClip = SoundBank.Get(sample.SampleID);

        return target;
    }

    protected override void OnEventJudged<T>(RhythmEvent ev, Judgment<T>.Result result)
    {
        base.OnEventJudged(ev, result);

        var score = GetScoreState<Accuracy>();
        stats.text =
            $"Perfect: {score.GetStatsFor(Accuracy.Perfect)}\nGreat: {score.GetStatsFor(Accuracy.Great)}\n" +
            $"Bad: {score.GetStatsFor(Accuracy.Bad)}\nMiss:{score.GetStatsFor(Accuracy.Miss)}";
    }

    protected override void OnEventProximityExceeded(RhythmEvent ev)
    {
        var state = GetEventState(ev);
        if (!ev.Playable || state.Completed)
            return;

        Judge(ev, Accuracy.Miss);
    }

    protected virtual void HandleInput()
    {
        // In real scenario, you need to assign the keyboard configuration into Channel
        if (!autoplay && !Input.anyKeyDown)
            return;

        // Get configured Judgment and front buffer
        var judgment = GetJudgment<Accuracy>();
        var front     = GetFrontEventList<Channel>();

        // Get the front event (the closest event that available for Judgment)
        // Alternatively, for rhythm game with "lane", we can `front.GetFrontEventFor` to get event by the channel
        foreach (var ev in front.GetFrontEvents<RhythmEvent.Note>())
        {
            // Check the distance between Judgment and note
            float latency = ev.Position - Position;
            var result    = judgment.Evaluate(ev);
            if ((autoplay && latency < 0) || (!autoplay && result.Accurracy != Accuracy.None))
            {
                Judge<Accuracy>(ev, result);

                // Only handle one event per single user input
                // In real-world scenario, you have to prevent the tap for this particular channel and continue to process other events in other channels
                break;
            }
        }
    }
}
