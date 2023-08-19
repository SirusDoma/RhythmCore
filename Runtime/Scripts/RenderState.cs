using System;

namespace RhythmCore
{
    /// <summary>
    /// Represents a captured state of <see cref="ChartRenderer"/> at a specific point of time.
    /// </summary>
    public class RenderState
    {
        /// <summary>
        /// Gets the <see cref="ChartRenderer"/> position when the <see cref="RenderState"/> is captured.
        /// </summary>
        public float Position { get; private set; }

        /// <summary>
        /// Gets the <see cref="ChartRenderer"/> tempo when this instance of <see cref="RenderState"/> is captured.
        /// </summary>
        public RhythmEvent.Tempo Tempo { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RenderState"/>.
        /// </summary>
        /// <param name="position">The position to capture.</param>
        /// <param name="tempo">The tempo to capture.</param>
        public RenderState(float position, RhythmEvent.Tempo tempo)
        {
            Position = position;
            Tempo    = tempo;
        }

        /// <summary>
        /// Capture the specified <see cref="ChartRenderer"/> state.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Chart"/>.</typeparam>
        /// <param name="renderer">The <see cref="ChartRenderer"/> state that will be captured.</param>
        /// <returns>The <see cref="RenderState"/> of specified <see cref="ChartRenderer"/>.</returns>
        public static RenderState Capture<T>(ChartRenderer<T> renderer)
            where T : IChart
        {
            return new RenderState(renderer.Position, renderer.Tempo);
        }
    }
}