namespace RhythmCore
{
    public static class TimingUtility
    {
        /// <summary>
        /// Convert the specified position into seconds.
        /// </summary>
        /// <param name="position">The position to convert.</param>
        /// <param name="bpm">The beats per minute.</param>
        /// <returns>The seconds converted from the specified position.</returns>
        public static float PositionToSeconds(float position, float bpm)
        {
            return position * 4 * (60 / bpm);
        }

        /// <summary>
        /// Convert the specified seconds into position.
        /// </summary>
        /// <param name="seconds">The seconds to convert.</param>
        /// <param name="bpm">The beats per minute.</param>
        /// <returns>The position converted from the specified seconds.</returns>
        public static float SecondsToPosition(float seconds, float bpm)
        {
            return seconds / (4 * (60 / bpm));
        }
    }
}