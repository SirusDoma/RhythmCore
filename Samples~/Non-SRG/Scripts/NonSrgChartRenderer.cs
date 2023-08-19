using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using RhythmCore;

public class NonSrgChartRenderer : VsrgChartRenderer
{
    public GameObject JudgmentLine;

    public static readonly float VerticalViewport = 2.4f;

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
        Render(ChartGenerator.Laneless(), Difficulty.Easy, new RenderConfig()
        {
            RenderDelay            = 2f, // Add 2 Measure / Bar before starting the chart
            InstantiationProximity = 1f  // Instantiate note game object when it is one measure away
        });
    }

    protected override void Update()
    {
        base.Update();

        // Move the judgment line ping-pong per 1 measure
        float offset = Mathf.PingPong(Position, 1f);
        JudgmentLine.transform.position = Vector2.Lerp(new Vector2(0, -VerticalViewport), new Vector2(0, VerticalViewport), offset / 1f);
    }

    public override Object Instantiate(RhythmEvent ev)
    {
        if (ev is not RhythmEvent.Note<float> playableEv)
            return null;

        // Instantiate the note
        var target    = Instantiate(note);
        var component = target.GetComponent<PopNote>();

        // Determine note position
        float position = Mathf.PingPong(ev.Position, 1f);
        target.transform.position = new Vector3(playableEv.Channel, Mathf.Lerp(-VerticalViewport, VerticalViewport, position / 1f));
        target.transform.localScale = Vector2.zero;

        // Configure the note script
        component.ChartRenderer     = this;
        component.EventState        = GetEventState(ev);
        component.NoteSpeed         = Config.Speed;      // Speed need to be applied to the note by the note and not renderer
        component.QueryRenderState += () => RenderState; // The captured RenderState at the beginning of this frame

        // Assign the sound to the notes if it's key-sound (or background) note
        if (!string.IsNullOrEmpty(playableEv.SampleID))
            component.AudioClip = SoundBank.Get(playableEv.SampleID);

        // We return the PopNote component this time
        return component;
    }

    protected override void HandleInput()
    {
        if (!autoplay && !Input.GetMouseButtonDown(0))
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
            if (autoplay && latency >= 0)
                continue;

            // Since we're returning PopNote component during instantiation, we can use it right away without doing query from GameObject
            var state = GetEventState(ev);
            var pop   = state.Object as PopNote;
            if (pop == null || pop.BoxCollider2D == null)
                continue;

            // Check whether the user has skill issue when pointing the mouse pointer into the note object
            if (!autoplay && !pop.BoxCollider2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)))
                continue;

            // Judge the note when it is within Judgment proximity
            var result = judgment.Evaluate(ev);
            Judge<Accuracy>(ev, result);

            // Only judge one note per mouse click
            return;
        }
    }
}
