using System;
using System.Collections.Generic;

using UnityEngine;

namespace RhythmCore
{
    /// <summary>
    /// Represents configuration of the <see cref="ChartRenderer"/> class.
    /// </summary>
    [Serializable]
    public class RenderConfig
    {
        /// <summary>
        /// Gets or sets the rendering speed.
        /// </summary>
        /// <remarks>
        /// This will not affect <see cref="ChartRenderer{TChart}.Position"/> calculation.
        /// You need to apply this value to the instantiated <see cref="RhythmEvent"/> by yourself.
        /// </remarks>
        [field: SerializeField]
        public float Speed                  { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the threshold range of the <see cref="RhythmEvent"/> instantiation.
        /// </summary>
        [field: SerializeField]
        public float InstantiationProximity { get; set; } = 5f;

        /// <summary>
        /// Gets or sets the render delay at the beginning of the music.
        /// </summary>
        [field: SerializeField]
        public float RenderDelay            { get; set; } = 1f;
    }

    /// <summary>
    /// Provides a class for rendering a <see cref="Chart"/>.
    /// </summary>
    public abstract class ChartRenderer : ChartRenderer<Chart>
    {
    }

    /// <summary>
    /// Provides a class for rendering a <typeparamref name="TChart"/>.
    /// </summary>
    /// <typeparam name="TChart">The type of <see cref="RhythmCore.IChart"/> to render.</typeparam>
    public abstract class ChartRenderer<TChart> : MonoBehaviour
        where TChart : IChart
    {
        /// <summary>
        /// Gets the measure interval multiplier in seconds.
        /// </summary>
        protected float TickSignature => 60f * 4;

        private bool _rendering;
        private bool _paused;
        private Enum _difficulty;

        private RhythmEvent[] _events;
        private Dictionary<int, EventState> _states;
        private RenderState _latestState = new(0, new RhythmEvent.Tempo());
        private IJudgment _judgment;
        private IFrontEventStateList _frontEventStateList;
        private IScoreState _score;

        private RhythmEvent.Tempo _tempo = new() { ID = int.MinValue };
        private float _refPosition;
        private float _pinpoint;

        /// <summary>
        /// Gets the time at the beginning of this frame.
        /// </summary>
        protected virtual float CurrentTime => Time.time;

        /// <summary>
        /// Gets a value indicates whether the <see cref="TChart"/> is being rendered by the current instance of the <see cref="ChartRenderer"/>.
        /// </summary>
        public bool Rendering => _rendering && !_paused && Ready;

        /// <summary>
        /// Gets a value indicates whether the rendering process is paused.
        /// </summary>
        public bool Paused => _paused;

        /// <summary>
        /// Gets a value indicates whether the current instance of the <see cref="ChartRenderer"/> is ready for rendering.
        /// </summary>
        public virtual bool Ready { get; protected set; } = true;

        /// <summary>
        /// Gets the <typeparamref name="TChart"/> that being rendered by <see cref="ChartRenderer"/>.
        /// </summary>
        public TChart Chart { get; private set; }

        /// <summary>
        /// Gets the rendering configuration.
        /// </summary>
        public RenderConfig Config { get; private set; } = new();

        /// <summary>
        /// Gets the reference time of the last executed timing events in seconds.
        /// </summary>
        /// <remarks>
        /// The initial value will set to <see cref="Time.time"/> when <see cref="Render{T}"/> is invoked.<br/><br/>
        /// When a <see cref="RhythmEvent.Tempo"/> event is executed, adjustment will be made to this value depending on the event types.
        /// <list type="bullet">
        ///   <item><see cref="RhythmEvent.Tempo.Bpm"/>: The value will be increased based on <see cref="RhythmEvent.Tempo"/> position.</item>
        ///   <item><see cref="RhythmEvent.Tempo.Skip"/>: The value will be decreased based on <see cref="RhythmEvent.Tempo.Skip"/> value.</item>
        ///   <item><see cref="RhythmEvent.Tempo.Stop"/>: The value will be increased based on <see cref="RhythmEvent.Tempo.Stop"/> value at the end of the stop.</item>
        /// </list>
        /// Be advised that the value is described in <b>seconds</b>, not in <b>position</b> format.
        /// </remarks>
        public float ReferenceTime { get; protected set; }

        /// <summary>
        /// Gets the current tempo.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public RhythmEvent.Tempo Tempo
        {
            get => _tempo;
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Tempo cannot be null");

                // BPM Event: The ReferenceTime and BPM position need to be updated.
                // Otherwise the position calculation will assume that the BPM is being used since the beginning of the music
                if (value.Bpm > 0 && Math.Abs(value.Bpm - _tempo.Bpm) > 0f)
                {
                    ReferenceTime += ((value.Position - _refPosition) / Tempo.Bpm * TickSignature);
                    _refPosition   = value.Position;
                }
                else
                {
                    // Fallback to current BPM
                    value.Bpm = _tempo.Bpm;
                }

                // Recalculate StartTime to apply Skip Event
                if (value.Skip > 0)
                    ReferenceTime -= value.Skip / value.Bpm * TickSignature;

                // Set the tempo and check the state
                _tempo = value;
                var state = GetEventState(Tempo);

                // Mark processed state as true when the event doesn't contain Stop
                // Otherwise, it will be marked when Stop is fully applied
                if (_tempo.Stop == 0)
                    state.Complete();
            }
        }

        /// <summary>
        /// Gets the captured <see cref="RenderState"/> of the beginning of this frame.
        /// </summary>
        /// <remarks>
        /// The <see cref="RenderState"/> class is an intermediate class.
        /// Allowing the other components to get the current position and tempo without having direct dependency with this class.
        /// </remarks>
        public RenderState RenderState => _latestState;

        /// <summary>
        /// Gets the elapsed render playback position since the beginning of the music.
        /// </summary>
        public virtual float Position
        {
            get
            {
                // Calculate the position
                float position = ((CurrentTime - ReferenceTime) / TickSignature * Tempo.Bpm) + _refPosition;

                // Check for paused state
                if (_paused)
                    return _pinpoint;

                // Check for tempo state
                var state = GetEventState(Tempo);
                if (state.Completed)
                    return position;

                // Check for the Stop Event
                if (Tempo.Stop > 0f)
                {
                    // Stop Event: Stop the current position from advancing when its within Stop range;
                    if (position < Tempo.Position + Tempo.Skip + Tempo.Stop)
                        return Tempo.Position + Tempo.Skip;

                    // Recalculate start point to resume rendering after stopping
                    ReferenceTime += Tempo.Stop / Tempo.Bpm * TickSignature;
                    position = ((CurrentTime - ReferenceTime) / TickSignature * Tempo.Bpm) + _refPosition;
                }

                // And finally, mark the tempo event as processed
                state.Complete();

                return position;
            }
        }

        protected virtual void Awake()
        {
            ConfigureChannel<Channel>();
            ConfigureJudgment<Accuracy>(judgment =>
            {
                judgment.Register(Accuracy.Perfect, 0.25f);
                judgment.Register(Accuracy.Great, 0.50f);
                judgment.Register(Accuracy.Bad, 0.65f);

                return judgment.Build(Accuracy.Perfect, Accuracy.Miss);
            });

            ConfigureScoreStats<Accuracy>(score =>
            {
                score.Register(Accuracy.Perfect, 100);
                score.Register(Accuracy.Great, 50);
                score.Register(Accuracy.Bad, 25);

                score.SetComboBreakers(Accuracy.Bad, Accuracy.Miss);
            });
        }

        protected virtual void Update()
        {
            if (!Rendering)
                return;

            // Update dependencies
            _latestState = RenderState.Capture(this);
            _judgment?.Update(_latestState);

            // Check the events
            bool playing = false;
            foreach (var ev in _events)
            {
                // Check whether the event state has been processed
                var state = GetEventState(ev);
                if (state.Completed)
                    continue;

                // There's event to be processed, update the flag
                playing = true;

                // Try to update the front event
                _frontEventStateList.Update(state);

                // Calculate the latency of the event
                double latency = ev.Position - Position;

                // Instantiate the event within proximity
                if (ev.Instantiable && !state.Instantiated && (Config.InstantiationProximity <= 0 || latency < Config.InstantiationProximity))
                    state.Update(Instantiate(ev));
                else if (!ev.Playable && !state.Completed && latency <= 0)
                    Execute(ev);

                if (ev.Playable && (_judgment?.CheckProximityExceeded(ev) ?? false))
                    OnEventProximityExceeded(ev);
            }

            // If there's no more event to be processed, turn off the rendering flag
            if (_rendering && !playing)
            {
                _rendering = false;
                OnRenderCompleted();
            }
        }

        /// <summary>
        /// Starts rendering the specified <paramref name="chart"/>.
        /// </summary>
        /// <param name="chart">The <see cref="TChart"/> to render.</param>
        /// <param name="difficulty">The difficulty of chart to render.</param>
        /// <param name="config">The rendering configuration.</param>
        /// <typeparam name="T">The type of difficulty.</typeparam>
        public void Render<T>(TChart chart, T difficulty, RenderConfig config = default)
            where T : Enum
        {
            _difficulty   = difficulty;
            _events       = chart.GetEvents(_difficulty);
            _states       = new Dictionary<int, EventState>();

            Chart         = chart;
            Config        = config ?? new RenderConfig();
            Tempo         = new RhythmEvent.Tempo { ID = int.MinValue, Bpm = chart.Bpm };
            ReferenceTime = CurrentTime + Config.RenderDelay / Tempo.Bpm * TickSignature;

            _rendering    = true;
            OnRender();
        }

        /// <summary>
        /// Gets the difficulty of <see cref="Chart"/> that being rendered.
        /// </summary>
        /// <typeparam name="T">The type of difficulty.</typeparam>
        /// <returns>The difficulty that in use for rendering.</returns>
        public T GetDifficulty<T>()
            where T : Enum
        {
            return (T)_difficulty;
        }

        /// <summary>
        /// Pauses the rendering process.
        /// </summary>
        public virtual void Pause()
        {
            if (!_rendering || _paused)
                return;

            _paused   = true;
            _pinpoint = Position;
        }

        /// <summary>
        /// Resumes the paused rendering process.
        /// </summary>
        public virtual void Resume()
        {
            if (!_rendering || !_paused)
                return;

            _paused   = false;
            _pinpoint = 0f;
        }

        /// <summary>
        /// Instantiate the specified <see cref="RhythmEvent"/> to an Unity <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to instantiate.</param>
        /// <returns>The instantiated Unity <see cref="UnityEngine.Object"/>.</returns>
        public abstract UnityEngine.Object Instantiate(RhythmEvent ev);

        /// <summary>
        /// Execute a non-playable <see cref="RhythmEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to execute.</param>
        public virtual void Execute(RhythmEvent ev)
        {
            var state = GetEventState(ev);
            if (state.Completed || ev is not RhythmEvent.Tempo tempo || tempo == Tempo || (Tempo.Stop > 0f && !GetEventState(Tempo).Completed))
                return;

            // Apply the Tempo changes
            Tempo = tempo;
        }

        /// <summary>
        /// Trigger Judgment for the specified <see cref="RhythmEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to judge.</param>
        /// <param name="hits">The number of registered hits for the associated event.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        /// <returns><c>true</c> if the Judgment is passed successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Judgment{T}"/> or <see cref="ScoreState{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// Do nothing when the specified <see cref="RhythmEvent"/> is out of Judgment proximity or already judged.
        /// Successful Judgment will update the <see cref="ScoreState{T}"/> as well.
        /// </remarks>
        public bool Judge<T>(RhythmEvent ev, int hits = 1)
            where T : Enum
        {
            var state = GetEventState(ev);
            if (state.Completed)
                return false;

            var judgment  = GetJudgment<T>();
            var result    = judgment.Evaluate(ev);
            var score     = _score as ScoreState<T>;
            if (judgment.Default.Equals(result.Accurracy))
                return false;

            state.Complete(result);
            score?.Update(result.Accurracy, hits);

            OnEventJudged(ev, result);
            return true;
        }

        /// <summary>
        /// Trigger immediate Judgment for the specified <see cref="RhythmEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to judge.</param>
        /// <param name="accuracy">The judged accuracy for the associated event.</param>
        /// <param name="hits">The number of registered hits for the associated event.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        /// <returns><c>true</c> if the Judgment is passed successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Judgment{T}"/> or <see cref="ScoreState{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// Do nothing when the specified <see cref="RhythmEvent"/> is out of Judgment proximity or already judged.
        /// Successful Judgment will update the <see cref="ScoreState{T}"/> as well.
        /// </remarks>
        protected bool Judge<T>(RhythmEvent ev, T accuracy, int hits = 1)
            where T : Enum
        {
            var state = GetEventState(ev);
            if (state.Completed)
                return false;

            var judgment = GetJudgment<T>();
            var score    = _score as ScoreState<T>;
            if (judgment.Default.Equals(accuracy))
                return false;

            float position = Position;
            var result   = new Judgment<T>.Result(accuracy, position, 0f);

            state.Complete(result);
            score?.Update(accuracy, hits);

            OnEventJudged(ev, result);
            return true;
        }

        /// <summary>
        /// Occurs when the <see cref="Render{T}"/> method is called.
        /// </summary>
        protected virtual void OnRender() {}

        /// <summary>
        /// Occurs when a <see cref="RhythmEvent"/> timing has passed beyond the <see cref="Judgment{T}"/> proximity range and no longer possible to judge.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> that has been out of range.</param>
        /// <remarks>
        /// The given <see cref="RhythmEvent"/> is considered as <i>missed</i>.
        /// You should call <see cref="Judge{T}(RhythmCore.RhythmEvent,T,int)"/> with the given <see cref="RhythmEvent"/> and lowest accuracy grade (e.g <see cref="Accuracy.Miss"/>).
        /// </remarks>
        protected abstract void OnEventProximityExceeded(RhythmEvent ev);

        /// <summary>
        /// Occurs when Judgment for provided <see cref="RhythmEvent"/> has been passed successfully.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> that has been judged successfully.</param>
        /// <param name="result">The Judgment result.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        protected virtual void OnEventJudged<T>(RhythmEvent ev, Judgment<T>.Result result)
            where T : Enum {}

        /// <summary>
        /// Occurs when the rendering process has been completed.
        /// </summary>
        protected virtual void OnRenderCompleted() {}

        /// <summary>
        /// Configure the <see cref="Judgment{T}"/> for the current instance of the <see cref="ChartRenderer"/>.
        /// </summary>
        /// <param name="configurator">The configurator to build the <see cref="Judgment{T}"/>.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        /// <exception cref="ArgumentNullException">The <paramref name="configurator"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Subsequent call will override the existing <see cref="Judgment{T}"/> configuration.
        /// The <see cref="ScoreState{T}"/> will also be overriden if <typeparamref name="T"/> is different than the previous call.
        /// </remarks>
        protected void ConfigureJudgment<T>(Func<Judgment<T>.Builder, Judgment<T>> configurator)
            where T : Enum
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator), "Configurator cannot be null");

            _judgment = configurator.Invoke(new Judgment<T>.Builder());
            if (_score is not ScoreState<T>)
                ConfigureScoreStats<T>(null);
        }

        /// <summary>
        /// Configure the <see cref="Judgment"/> with default <see cref="Accuracy"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="ScoreState{T}"/> will be overriden if the accuracy type of <see cref="ScoreState{T}"/> is not <see cref="Accuracy"/>.
        /// </remarks>
        protected void ConfigureJudgment()
        {
            _judgment = new Judgment();
            if (_score is not ScoreState)
                ConfigureScoreStats();
        }

        /// <summary>
        /// Configure the <see cref="ScoreState{T}"/> for the current instance of the <see cref="ChartRenderer"/>.
        /// </summary>
        /// <param name="configurator">The provided configurator to build the <see cref="ScoreState{T}"/>.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Judgment{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// Subsequent call will override the existing <see cref="ScoreState{T}"/> configuration.
        /// </remarks>
        protected void ConfigureScoreStats<T>(Action<ScoreState<T>> configurator)
            where T : Enum
        {
            if (_judgment is not Judgment<T> judgment)
                throw new ArgumentOutOfRangeException(nameof(T), $"Judgment is not configured for {typeof(T).Name}");

            if (_score is not ScoreState<T>)
                _score = ScoreState<T>.Builder.Build(Chart, _difficulty, judgment.Highest, judgment.Lowest, judgment.Default);

            configurator?.Invoke((ScoreState<T>)_score);
        }

        /// <summary>
        /// Configure the <see cref="Judgment"/> with default <see cref="Accuracy"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Judgment{T}"/> is not configured for <see cref="Accuracy"/>.</exception>
        protected void ConfigureScoreStats()
        {
            if (_judgment is not Judgment judgment)
                throw new InvalidOperationException($"Judgment is not configured for {nameof(Accuracy)}");

            _score = new ScoreState(Chart, (Difficulty)_difficulty);
        }

        /// <summary>
        /// Configure the Channels of the rendered <see cref="Chart"/> events.
        /// </summary>
        /// <typeparam name="T">The type of channel to use.</typeparam>
        /// <remarks>
        /// <see cref="T"/> will be used to initializes <see cref="FrontEventStateList{T}"/>.
        /// </remarks>
        protected void ConfigureChannel<T>()
            where T : struct, IConvertible
        {
            _frontEventStateList = new FrontEventStateList<T>();
        }

        /// <summary>
        /// Get the <see cref="Judgment{T}"/> associated with the specified accuracy type.
        /// </summary>
        /// <typeparam name="T">The accuracy type.</typeparam>
        /// <returns>The <see cref="Judgment{T}"/> that associated with type of <see cref="T"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Judgment{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        protected Judgment<T> GetJudgment<T>()
            where T : Enum
        {
            if (_judgment is not Judgment<T> judgment)
                throw new ArgumentOutOfRangeException(nameof(T), $"Judgment is not configured for {typeof(T).Name}");

            return judgment;
        }

        /// <summary>
        /// Get the <see cref="ScoreState{T}"/> associated with the specified accuracy type.
        /// </summary>
        /// <typeparam name="T">The accuracy type.</typeparam>
        /// <returns>The <see cref="ScoreState{T}"/> that associated with type of <see cref="T"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="ScoreState{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        protected ScoreState<T> GetScoreState<T>()
            where T : Enum
        {
            if (_score is not ScoreState<T> score)
                throw new ArgumentOutOfRangeException(nameof(T), $"ScoreState is not configured {typeof(T).Name}");

            return score;
        }

        /// <summary>
        /// Get the <see cref="FrontEventStateList{T}"/> associated with the specified channel type.
        /// </summary>
        /// <typeparam name="T">The channel type.</typeparam>
        /// <returns>The <see cref="FrontEventStateList{T}"/> that associated with type of <see cref="T"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="FrontEventStateList{T}"/> is not configured for <typeparamref name="T"/>.</exception>
        protected FrontEventStateList<T> GetFrontEventList<T>()
            where T : struct, IConvertible
        {
            if (_frontEventStateList is not FrontEventStateList<T> buffer)
                throw new ArgumentOutOfRangeException(nameof(T), $"Channel is not configured {typeof(T).Name}");

            return buffer;
        }

        /// <summary>
        /// Gets an array of <see cref="RhythmEvent"/> that being rendered by the current instance of the <see cref="ChartRenderer"/>.
        /// </summary>
        /// <returns>An array of <see cref="RhythmEvent"/> that being rendered.</returns>
        protected RhythmEvent[] GetEvents()
        {
            return _events;
        }

        /// <summary>
        /// Gets the state of the specified <see cref="RhythmEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> of the state to get.</param>
        /// <returns>The state of the specified <see cref="RhythmEvent"/>.</returns>
        protected EventState GetEventState(RhythmEvent ev)
        {
            if (_states == null)
                _states = new Dictionary<int, EventState>();

            _states.TryAdd(ev.ID, new EventState(ev));
            return _states[ev.ID];
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                Pause();
            else
                Resume();
        }
    }

}


