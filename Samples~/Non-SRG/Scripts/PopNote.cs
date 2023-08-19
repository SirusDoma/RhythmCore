using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RhythmCore;

public class PopNote : VsrgNote
{
    public ChartRenderer ChartRenderer { get; set; }

    public BoxCollider2D BoxCollider2D { get; set; }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        BoxCollider2D = GetComponent<BoxCollider2D>();
        transform.localScale = Vector2.zero;
    }

    public override void UpdatePosition(RenderState renderState)
    {
        if (EventState.Completed)
            return;

        // Should fully appear 2 beat ahead of perfect position.
        float end = EventState.Event.Position - 0.5f;

        // Avoid using division because event offset could be 0
        float travel = (end - renderState.Position);

        // Flip a and b because 0 == perfect point
        transform.localScale = Vector2.Lerp(Vector2.one, Vector2.zero, travel);
    }
}
