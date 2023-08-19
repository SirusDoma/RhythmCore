using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmCore
{
    /// <summary>
    /// Represents state of the score.
    /// </summary>
    public interface IScoreState
    {
        /// <summary>
        /// Gets the chart that the current instance of this class attached.
        /// </summary>
        public IChart Chart    { get; }

        /// <summary>
        /// Gets the difficulty of chart.
        /// </summary>
        public Enum Difficulty { get; }

        /// <summary>
        /// Gets the total score.
        /// </summary>
        public int Score       { get; }

        /// <summary>
        /// Gets the combo.
        /// </summary>
        public int Combo       { get; }

        /// <summary>
        /// Gets the max combo.
        /// </summary>
        public int MaxCombo    { get; }
    }

    /// <summary>
    /// Represents state of the score for the specified accuracy type.
    /// </summary>
    /// <typeparam name="T">The type of accuracy.</typeparam>
    [Serializable]
    public class ScoreState<T> : IScoreState
        where T : Enum
    {
        /// <summary>
        /// Provides class to build the <see cref="ScoreState{T}"/> instance.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Build the <see cref="ScoreState{T}"/>.
            /// </summary>
            /// <param name="chart">The chart of this score attached to.</param>
            /// <param name="difficulty">The difficulty of the specified chart.</param>
            /// <param name="highest">The highest accuracy.</param>
            /// <param name="lowest">The lowest accuracy.</param>
            /// <param name="none">The accuracy that represents an <see cref="RhythmEvent"/> is not ready to judge.</param>
            /// <returns>The built <see cref="ScoreState{T}"/>.</returns>
            public static ScoreState<T> Build<TD>(IChart chart, TD difficulty, T highest, T lowest, T none = default)
                where TD : Enum
            {
                var score = new ScoreState<T>(chart, difficulty) { _default = none };
                score.SetComboBreakers(lowest);

                return score;
            }
        }

        private T _default = default(T);

        private readonly HashSet<T> _breakers = new();
        private readonly Dictionary<T, int> _stats = new();
        private readonly Dictionary<T, Func<IChart, Enum, int>> _scorings = new();

        public IChart Chart    { get; private set; }

        public Enum Difficulty { get; private set; }

        [field: SerializeField]
        public int Score       { get; private set; }

        [field: SerializeField]
        public int Combo       { get; private set; }

        [field: SerializeField]
        public int MaxCombo    { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ScoreState{T}"/> class.
        /// </summary>
        /// <param name="chart">The chart of this score attached to.</param>
        /// <param name="difficulty">The difficulty of the specified chart.</param>
        protected ScoreState(IChart chart, Enum difficulty)
        {
            Chart      = chart;
            Difficulty = difficulty;
            foreach (T acc in Enum.GetValues(typeof(T)))
            {
                if (!acc.Equals(_default))
                    _stats[acc] = 0;
            }
        }

        /// <summary>
        /// Update the score state for the specified accuracy.
        /// </summary>
        /// <param name="accuracy">The accuracy of the stats to update.</param>
        /// <param name="hits">The number of hits.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="hits"/> is zero or negative numbers.</exception>
        public virtual void Update(T accuracy, int hits = 1)
        {
            if (accuracy.Equals(_default))
                return;

            if (hits <= 0)
                throw new ArgumentOutOfRangeException(nameof(hits), "Number of hits cannot be equal or less than 0");

            _stats[accuracy] += hits;
            if (!_breakers.Contains(accuracy))
                Combo += hits;
            else
                Combo = 0;

            MaxCombo = Math.Max(Combo, MaxCombo);
            if (_scorings.TryGetValue(accuracy, out var evaluator))
                Score += evaluator.Invoke(Chart, Difficulty);
        }

        /// <summary>
        /// Register the specified accuracy and its scoring point to the current instance of this class.
        /// </summary>
        /// <param name="accuracy">The accuracy to register.</param>
        /// <param name="point">The scoring point associated to the specified <paramref name="accuracy"/>.</param>
        public void Register(T accuracy, int point)
        {
            _scorings[accuracy] = (_, _) => point;
        }

        /// <summary>
        ///  Register the specified accuracy and its scoring evaluation function to the current instance of this class.
        /// </summary>
        /// <param name="accuracy">The accuracy to register.</param>
        /// <param name="scoreEvaluation">The function to evaluate the score of the specified <paramref name="accuracy"/>.</param>
        public void Register(T accuracy, Func<IChart, Enum, int> scoreEvaluation)
        {
            _scorings[accuracy] = scoreEvaluation;
        }

        /// <summary>
        ///  Register the specified accuracy and its scoring evaluation function based on <see cref="IChart"/> and difficulty to the current instance of this class.
        /// </summary>
        /// <param name="accuracy">The accuracy to register.</param>
        /// <param name="scoreEvaluation">The function to evaluate the score of the specified <paramref name="accuracy"/>.</param>
        /// <typeparam name="TChart">The type of chart.</typeparam>
        /// <typeparam name="TDifficulty">The type of difficulty.</typeparam>
        public void Register<TChart, TDifficulty>(T accuracy, Func<TChart, TDifficulty, int> scoreEvaluation)
            where TChart      : class, IChart
            where TDifficulty : Enum
        {
            _scorings[accuracy] = (chart, diff) => scoreEvaluation.Invoke(chart as TChart, (TDifficulty)diff);
        }

        /// <summary>
        /// Gets the number of hits of the specified accuracy.
        /// </summary>
        /// <param name="accuracy">The accuracy of hits to get.</param>
        /// <returns>The number of hits of the specified accuracy.</returns>
        public int GetStatsFor(T accuracy)
        {
            return _stats.GetValueOrDefault(accuracy, 0);
        }

        /// <summary>
        /// Set the accuracies that would break the current <see cref="Combo"/> when <see cref="Update"/> is invoked with specified accuracies.
        /// </summary>
        /// <param name="accuracies">The accuracies of combo breakers.</param>
        public void SetComboBreakers(params T[] accuracies)
        {
            foreach (var acc in accuracies)
                _breakers.Add(acc);
        }

        /// <summary>
        /// Reset the stats counting for the current instance of this class.
        /// </summary>
        public virtual void Reset()
        {
            foreach (T acc in Enum.GetValues(typeof(T)))
            {
                if (!acc.Equals(_default))
                    _stats[acc] = 0;
            }

            Score    = 0;
            Combo    = 0;
            MaxCombo = 0;
        }
    }

    /// <summary>
    /// Represents an opinionated state of the score for the specified accuracy type.
    /// </summary>
    [Serializable]
    public class ScoreState : ScoreState<Accuracy>
    {
        private const float PerfectPercentageValue = 1.0f;
        private const float GreatPercentageValue   = 0.5f;
        private const float BadPercentageValue     = 0.25f;

        /// <summary>
        /// Gets the score percentage.
        /// </summary>
        [field: SerializeField]
        public float Percentage { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ScoreState"/> class.
        /// </summary>
        /// <param name="chart">The chart of this score attached to.</param>
        /// <param name="difficulty">The difficulty of the specified chart.</param>
        public ScoreState(IChart chart, Difficulty difficulty)
            : base(chart, difficulty)
        {
            Register(Accuracy.Perfect, 100);
            Register(Accuracy.Great, 50);
            Register(Accuracy.Bad, 15);

            SetComboBreakers(Accuracy.Bad, Accuracy.Miss);
        }

        public override void Update(Accuracy accuracy, int hits = 1)
        {
            base.Update(accuracy, hits);

            Percentage = (GetStatsFor(Accuracy.Perfect) * PerfectPercentageValue +
                          GetStatsFor(Accuracy.Great) * GreatPercentageValue +
                          GetStatsFor(Accuracy.Bad) * BadPercentageValue) / Chart.GetPlayableEventCount(Difficulty) * 100f;
        }

        public override void Reset()
        {
            base.Reset();
            Percentage = 0;
        }
    }
}