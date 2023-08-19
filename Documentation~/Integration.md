## Integration

The micro-framework is agnostic: it allows you to use any modules of your choice, but it means the framework is unaware of anything that happens within other modules. 
For example, audio playback latency will not be accounted for when rendering the music events, resulting in desynchronization between audio playback and notes position.  

This guide will attempt to address some of these issues.

### Handling Audio Latency

To prevent desynchronization between audio and the displayed notes, you must override the  `ChartRenderer.CurrentTime` to account for the audio latency.
Here's the example implementation to account for audio latency of Unity [AudioSource](https://docs.unity3d.com/ScriptReference/AudioSource.html) into `ChartRenderer.CurrentTime`.

```csharp

    public class HyperMapRenderer : ChartRenderer<HyperMap>
    {
        // Track when the audio is started
        private float _audioStartTime;

        // Track the audio source that is used for playing the background music
        private AudioSource _audioSource;
        
        public float AudioLatency
        {
            if (!_audioSource.isPlaying)
                return 0f;

            // The latency needs to account for 3 things: Render Delay from config, the audio start time, and the current position of audio playback
            return (Time.time - (TimingUtility.PositionToSeconds(Config.RenderDelay, Chart.Bpm) + _audioStartTime)) - _audioSource.time;
        } 

        // What you will have to do is subtract the latency from the current time
        public override float CurrentTime => Time.time - AudioLatency;
    }

```

> [!Note]
> Subsequent `Time.time` calls will produce the same value when they're called within the same frame.

This will force the game to follow the latency from an `AudioSource`. However, this solution might not be fit if you're developing a full-fledged key-sounded rhythm game with dozens of audio playing in the background.  

There are multiple solutions to this problem. For example, you can calculate the average latency between the audio samples or use the latency of the longest audio sample as the reference.
