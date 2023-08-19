using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RhythmCore
{
    /// <summary>
    /// Represents <see cref="RhythmEvent"/> state during rendering.
    /// </summary>
    public class EventState
    {
        private IJudgmentResult _result;

        /// <summary>
        /// Gets the ID of referenced <see cref="RhythmEvent"/>.
        /// </summary>
        public int ID  => Event.ID;

        /// <summary>
        /// Gets a value indicates whether the <see cref="RhythmEvent"/> with <see cref="RhythmEvent.Instantiable"/> flag has been instantiated to the scene.
        /// </summary>
        public bool Instantiated => !ReferenceEquals(Object, null);

        /// <summary>
        /// Gets the source of <see cref="RhythmEvent"/> of this instance of <see cref="EventState"/>.
        /// </summary>
        public RhythmEvent Event { get; private set; }

        /// <summary>
        /// Gets a value indicates whether the current instance of <see cref="EventState"/> has been completed.
        /// </summary>
        /// <remarks>
        /// No further action could be performed when the <see cref="EventState"/> is completed.
        /// </remarks>
        public bool Completed { get; private set; }

        /// <summary>
        /// Gets the instantiated object.
        /// </summary>
        public Object Object { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="EventState"/> class.
        /// </summary>
        /// <param name="ev">The <see cref="RhythmEvent"/> that will be tracked.</param>
        public EventState(RhythmEvent ev)
        {
            Event = ev;
        }

        /// <summary>
        /// Gets the <see cref="Judgment{T}.Result"/> of the referenced <see cref="RhythmEvent"/>.
        /// </summary>
        /// <typeparam name="T">Type of the accuracy.</typeparam>
        /// <returns>The <see cref="Judgment{T}.Result"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified <typeparamref name="T"/> is mismatched with the actual <see cref="Judgment{T}.Result"/>.</exception>
        public Judgment<T>.Result GetJudgeResult<T>()
            where T : Enum
        {
            if (_result == null)
                return null;

            if (_result is not Judgment<T>.Result judgment)
                throw new ArgumentOutOfRangeException(nameof(T), "Accuracy type is mismatch");

            return judgment;
        }

        /// <summary>
        /// Update the state with instantiated <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="obj">The instantiated <see cref="UnityEngine.Object"/> to track.</param>
        public void Update(Object obj)
        {
            Object = obj;
        }

        /// <summary>
        /// Update the state to completed.
        /// </summary>
        /// <param name="accuracy">The Judgment accuracy.</param>
        /// <param name="position">The <see cref="ChartRenderer"/> position.</param>
        /// <param name="latency">The user input latency.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        public void Complete<T>(T accuracy, float position, float latency = 0f)
            where T : Enum
        {
            Complete(new Judgment<T>.Result(accuracy, position, latency));
        }

        /// <summary>
        /// Update the state to completed.
        /// </summary>
        /// <param name="result">The Judgment result.</param>
        /// <typeparam name="T">The type of accuracy.</typeparam>
        public void Complete<T>(Judgment<T>.Result result)
            where T : Enum
        {
            _result = result;
            Complete();
        }

        /// <summary>
        /// Update the state to completed without specifying <see cref="Judgment{T}.Result"/>.
        /// </summary>
        public void Complete()
        {
            Completed = true;
        }
    }
}