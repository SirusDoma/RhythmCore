using System;
using System.Collections.Generic;

namespace RhythmCore
{
    /// <summary>
    /// Provides a class for determine the timing accuracy of user input.
    /// </summary>
    public interface IJudgment
    {
        void Update(RenderState state);

        /// <summary>
        /// Check whether the specified event is no longer judge-able.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to be checked.</param>
        /// <returns><c>true</c> if the event is considered as <i>missed</i>; otherwise, <c>false</c></returns>
        bool CheckProximityExceeded(RhythmEvent ev);

        /// <summary>
        /// Check whether the specified event is within judge-able range.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to be checked.</param>
        /// <returns><c>true</c> if the event is can be judged; otherwise, <c>false</c></returns>
        bool CheckProximity(RhythmEvent ev);
    }

    public sealed class Judgment : Judgment<Accuracy>
    {
        public Judgment()
            : base(Accuracy.Perfect, Accuracy.Miss)
        {
            Register(Accuracy.Perfect, 0.25f);
            Register(Accuracy.Great, 0.5f);
            Register(Accuracy.Bad, 0.75f);
        }
    }

    /// <summary>
    /// Represents the timing accuracy of user input.
    /// </summary>
    public interface IJudgmentResult
    {
        public float Latency { get; }
    }

    /// <summary>
    /// Provides a class for determine the timing accuracy of user input.
    /// </summary>
    public class Judgment<T> : IJudgment
        where T : Enum
    {
        /// <summary>
        /// Provides a class to build the <see cref="Judgment{T}"/> instance.
        /// </summary>
        public class Builder
        {
            private readonly Judgment<T> _judgment = new();

            /// <summary>
            /// Initializes a new instance of <see cref="Builder"/> class.
            /// </summary>
            public Builder()
            {
            }

            /// <summary>
            /// Register the specified accuracy and its timing to the current instance of this class.
            /// </summary>
            /// <param name="accuracy">The accuracy to register.</param>
            /// <param name="seconds">The timing threshold for the specified <paramref name="accuracy"/> in seconds.</param>
            /// <returns>The current instance of this class.</returns>
            public Builder Register(T accuracy, float seconds)
            {
                _judgment.Register(accuracy, seconds);
                return this;
            }

            /// <summary>
            ///  Register the specified accuracy and its timing evaluation function to the current instance of this class.
            /// </summary>
            /// <param name="accuracy">The accuracy to register.</param>
            /// <param name="evaluator">The function to evaluate whether the given <see cref="RhythmEvent"/> and <see cref="RenderState"/> associated with the specified <paramref name="accuracy"/>.</param>
            /// <returns>The current instance of this class.</returns>
            public Builder Register(T accuracy, Func<RhythmEvent, RenderState, Evaluation> evaluator)
            {
                _judgment.Register(accuracy, evaluator);
                return this;
            }

            /// <summary>
            /// Build the <see cref="Judgment{T}"/>.
            /// </summary>
            /// <param name="highest">The highest accuracy.</param>
            /// <param name="lowest">The lowest accuracy.</param>
            /// <param name="none">The accuracy that represents an <see cref="RhythmEvent"/> is not ready to judge.</param>
            /// <returns>The built <see cref="Judgment{T}"/>.</returns>
            public Judgment<T> Build(T highest, T lowest, T none = default)
            {
                _judgment.Highest = highest;
                _judgment.Lowest  = lowest;
                _judgment.Default = none;

                return _judgment;
            }
        }

        /// <summary>
        /// Represents a Judgment Evaluation.
        /// </summary>
        public class Evaluation
        {
            /// <summary>
            /// Gets the value indicates whether the input is registered.
            /// </summary>
            public bool Registered { get; private set; }

            /// <summary>
            /// Gets the latency between input position and renderer position.
            /// </summary>
            public float Latency { get; private set; }

            /// <summary>
            /// Initializes a new instance of <see cref="Evaluation"/> class.
            /// </summary>
            /// <param name="registered">The registered value.</param>
            /// <param name="latency">The latency value.</param>
            public Evaluation(bool registered, float latency = 0f)
            {
                Registered = registered;
                Latency    = latency;
            }

            /// <summary>
            /// Defines an implicit conversion of a <see cref="bool"/> to a <see cref="Evaluation"/>.
            /// </summary>
            /// <param name="registered">The value indicates whether the input is registered.</param>
            /// <returns>A <see cref="Evaluation"/> object.</returns>
            public static implicit operator Evaluation(bool registered)
            {
                return new Evaluation(registered);
            }

            /// <summary>
            /// Defines an implicit conversion of an <see cref="Evaluation"/> to a <see cref="Tuple{Bool,Single}"/>.
            /// </summary>
            /// <param name="eval">The <see cref="Evaluation"/> to convert.</param>
            /// <returns>A <see cref="Tuple{Bool,Single}"/> with <see cref="Registered"/> and <see cref="Latency"/> value in respective order.</returns>
            public static implicit operator (bool, float)(Evaluation eval)
            {
                return (eval.Registered, eval.Latency);
            }

            /// <summary>
            /// Defines an implicit conversion of a <see cref="Tuple{Bool,Single}"/> to a <see cref="Evaluation"/>.
            /// </summary>
            /// <param name="tuple">The <see cref="Tuple{Bool,Single}"/> to convert.</param>
            /// <returns>A <see cref="Evaluation"/> object.</returns>
            public static implicit operator Evaluation((bool, float) tuple)
            {
                return new Evaluation(tuple.Item1, tuple.Item2);
            }
        }

        /// <summary>
        /// Represents a result of the Judgment process.
        /// </summary>
        public class Result : IJudgmentResult
        {
            /// <summary>
            /// Gets the accuracy of the input.
            /// </summary>
            public T Accurracy { get; private set; }

            /// <summary>
            /// Gets the captured renderer position when the judgment happens.
            /// </summary>
            public float Position { get; private set; }

            /// <summary>
            /// Gets the latency between input position and renderer position.
            /// </summary>
            public float Latency { get; private set; }

            /// <summary>
            /// Initializes a new instance of <see cref="Result"/> class.
            /// </summary>
            /// <param name="accurracy">The accuracy value.</param>
            /// <param name="position">The position value.</param>
            /// <param name="latency">The latency value.</param>
            public Result(T accurracy, float position, float latency)
            {
                Accurracy = accurracy;
                Position  = position;
                Latency   = latency;
            }


            /// <summary>
            /// Defines an implicit conversion of an <see cref="Evaluation"/> to a <typeparamref name="T"/>.
            /// </summary>
            /// <param name="result">The <see cref="Result"/> to convert.</param>
            /// <returns>An object with type of <typeparamref name="T"/> that represents the accuracy.</returns>
            public static implicit operator T(Result result)
            {
                return result == null ? default : result.Accurracy;
            }
        }

        private RenderState _latestState = new(0, new RhythmEvent.Tempo());
        private readonly Dictionary<T, Func<RhythmEvent, RenderState, Evaluation>> _evaluators = new();

        /// <summary>
        /// Gets the accuracy that represents an <see cref="RhythmEvent"/> is not ready to judge.
        /// </summary>
        public T Default { get; set; }

        /// <summary>
        /// Gets the highest accuracy.
        /// </summary>
        public T Highest { get; set; }

        /// <summary>
        /// Gets the lowest accuracy.
        /// </summary>
        public T Lowest  { get; set; }

        private Judgment()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Judgment{T}"/> class.
        /// </summary>
        protected Judgment(T highest, T lowest, T none = default)
        {
            Highest = highest;
            Lowest  = lowest;
            Default = none ?? default;
        }

        /// <summary>
        /// Register the specified accuracy and its timing to the current instance of this class.
        /// </summary>
        /// <param name="accuracy">The accuracy to register.</param>
        /// <param name="seconds">The timing threshold for the specified <paramref name="accuracy"/> in seconds.</param>
        /// <returns>The current instance of this class.</returns>
        public Judgment<T> Register(T accuracy, float seconds)
        {
            return Register(accuracy, (ev, context) =>
            {
                float distance = ev.Position - context.Position;
                float latency  = TimingUtility.PositionToSeconds(distance, context.Tempo.Bpm);

                return (Math.Abs(latency) <= seconds, latency);
            });
        }

        /// <summary>
        ///  Register the specified accuracy and its timing evaluation function to the current instance of this class.
        /// </summary>
        /// <param name="accuracy">The accuracy to register.</param>
        /// <param name="evaluator">The function to evaluate whether the given <see cref="RhythmEvent"/> and <see cref="RenderState"/> associated with the specified <paramref name="accuracy"/>.</param>
        /// <returns>The current instance of this class.</returns>
        public Judgment<T> Register(T accuracy, Func<RhythmEvent, RenderState, Evaluation> evaluator)
        {
            if (accuracy.Equals(Default))
                throw new ArgumentOutOfRangeException(nameof(accuracy), accuracy, "Default accuracy cannot have Judgment definition");

            _evaluators[accuracy] = evaluator ?? throw new ArgumentNullException(nameof(evaluator), "Evaluator cannot be null");
            return this;
        }

        /// <summary>
        /// Evaluate the Judgment of the specified <see cref="RhythmEvent"/> with the last updated <see cref="RenderState"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to evaluate.</param>
        /// <returns>The Judgment <see cref="Result"/>.</returns>
        public Result Evaluate(RhythmEvent ev)
        {
            return Evaluate(ev, _latestState);
        }

        /// <summary>
        /// Evaluate the Judgment of the specified <see cref="RhythmEvent"/> with the specified <see cref="RenderState"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to evaluate.</param>
        /// <param name="state">The <see cref="RenderState"/> to use for the position comparison.</param>
        /// <returns>>The Judgment <see cref="Result"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
        public Result Evaluate(RhythmEvent ev, RenderState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state), "RenderState cannot be null");

            if (_latestState == null || _latestState.Position < state.Position)
                _latestState = state;

            float latency = ev.Position - state.Position;
            foreach (var (acc, fn) in _evaluators)
            {
                var eval = fn.Invoke(ev, _latestState);
                if (eval.Registered)
                    return new Result(acc, state.Position, eval.Latency);
            }

            return latency > 0 ? new Result(Default, state.Position, 0f) : new Result(Lowest, state.Position, 0f);
        }

        /// <summary>
        /// Determines whether the specified <see cref="RhythmEvent"/> is out of Judgment proximity range and impossible to judge in the future.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to check.</param>
        /// <returns><c>true</c> if the specified event is considered as <i>missed</i>; otherwise, <c>false</c>.</returns>
        public bool CheckProximityExceeded(RhythmEvent ev)
        {
            return Evaluate(ev).Accurracy.Equals(Lowest);
        }

        /// <summary>
        /// Determines whether the specified <see cref="RhythmEvent"/> is within Judgment proximity range.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> to check.</param>
        /// <returns><c>true</c> if the specified event is within the Judgment proximity range; otherwise, <c>false</c>.</returns>
        public bool CheckProximity(RhythmEvent ev)
        {
            var accurracy = Evaluate(ev).Accurracy;
            return !accurracy.Equals(Default) && !accurracy.Equals(Lowest);
        }

        /// <summary>
        /// Update the latest <see cref="RenderState"/>.
        /// </summary>
        /// <param name="state">The newly updated <see cref="RenderState"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
        public void Update(RenderState state)
        {
            _latestState = state ?? throw new ArgumentNullException(nameof(state), "RenderState cannot be null");
        }
    }
}