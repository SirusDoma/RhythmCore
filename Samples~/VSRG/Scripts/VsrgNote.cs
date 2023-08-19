using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RhythmCore;

public class VsrgNote : MonoBehaviour
{
    private static readonly Vector2 PerfectPoint = new(0f, -3f);
    private static readonly float[] ColumnPosition = { -1.5f, 1.5f };
    private AudioSource _audioSource;
    private float _fadeStartAt = -1f;
    private Vector2 _fadeScaleStart;
    private Vector2 _initialPosition;
    private SpriteRenderer[] _spriteRenderers;

    [field: SerializeField]
    public EventState EventState { get; set; }

    [field: SerializeField]
    public AudioClip AudioClip { get; set; }

    [field: SerializeField]
    public float NoteSpeed { get; set; }

    public Func<RenderState> QueryRenderState { get; set; }

    protected virtual void Start()
    {
        if (EventState.Event is RhythmEvent.Note ev)
        {
            _initialPosition = transform.position;
            transform.position = new Vector2(ev.Channel == Channel.K1 ? ColumnPosition[0] : ColumnPosition[1], _initialPosition.y);
        }

        _initialPosition = transform.position;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null && AudioClip != null)
            _audioSource.clip = AudioClip;

        // Disable renderer if it's not playable
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (!EventState.Event.Playable)
        {
            foreach (var spriteRenderer in _spriteRenderers)
                spriteRenderer.enabled = false;
        }
    }

    protected virtual void Update()
    {
        // Calculate current note offset
        var renderState  = QueryRenderState(); // Get the ChartRenderer Offset at the beginning of this frame
        float noteOffset = EventState.Event.Position - renderState.Position;

        if (EventState.Completed)
        {
            if (EventState.Event.Playable)
                AnimateExit();  // Stop moving the note and fade it out
            else if (!_audioSource.isPlaying)
                Destroy(gameObject); // Background music is finished

            return;
        }

        if (EventState.Event.Playable)
        {
            UpdatePosition(renderState);
        }
        else if (noteOffset < 0 && !EventState.Completed)
        {
            // You may need to check whether your clip is fully loaded into memory
            _audioSource.Play();
            EventState.Complete();
        }
    }

    public virtual void UpdatePosition(RenderState renderState)
    {
        // Translate note position based on elapsed ChartRenderer offset
        float travel = (EventState.Event.Position - renderState.Position) * NoteSpeed;
        transform.position = new Vector2(_initialPosition.x, PerfectPoint.y + travel);
    }

    protected void AnimateExit()
    {
        if (_fadeStartAt < 0f)
        {
            _fadeStartAt = Time.time;
            _fadeScaleStart = transform.localScale;
        }

        float progress = (Time.time - _fadeStartAt) / 0.5f; // Animate span under 0.5 second
        FadeOut(progress);
        ScaleOut(progress);

        // Make sure not to destroy the object before being judged
        if (progress >= 1 && EventState.Completed)
            Destroy(gameObject);
    }

    private void FadeOut(float t)
    {
        foreach (var sprite in _spriteRenderers)
        {
            var color = sprite.color;
            sprite.color = Color.Lerp(new Color(color.r, color.g, color.b, 1f), new Color(color.r, color.g, color.b, 0), t);
        }
    }

    private void ScaleOut(float t)
    {
        transform.localScale = Vector2.Lerp(_fadeScaleStart,  _fadeScaleStart * 2f, t);
    }
}
