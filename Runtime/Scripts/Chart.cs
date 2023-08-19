using System;
using System.Collections.Generic;

using UnityEngine;

namespace RhythmCore
{
    /// <summary>
    /// Represents a music header and collections of <see cref="RhythmEvent"/>.
    /// </summary>
    public interface IChart
    {
        /// <summary>
        /// Gets or sets beats per minute.
        /// </summary>
        public float Bpm { get; set; }

        /// <summary>
        /// Gets the number of events of specified difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty of the event count to get.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        public int GetEventCount<T>(T difficulty)
            where T : Enum;

        /// <summary>
        /// Gets the number of playable events of specified difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty of the playable event count to get.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        public int GetPlayableEventCount<T>(T difficulty)
            where T : Enum;

        /// <summary>
        /// Gets an array of <see cref="RhythmEvent"/> that need to be played or executed throughout the music.
        /// </summary>
        /// <param name="difficulty">The difficulty of the event list to get.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        /// <returns>An array of <see cref="RhythmEvent"/> of specified <paramref name="difficulty"/>.</returns>
        RhythmEvent[] GetEvents<T>(T difficulty)
            where T : Enum;

        /// <summary>
        /// Adds the elements of the specified collection of <see cref="RhythmEvent"/> to the end of the events list.
        /// </summary>
        /// <param name="difficulty">The difficulty of the event list to add.</param>
        /// <param name="events">The collection whose elements should be added to the end of the events list.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        void AddEvents<T>(T difficulty, params RhythmEvent[] events)
            where T : Enum;

        /// <summary>
        /// Gets the Level of specified <paramref name="difficulty"/>.
        /// </summary>
        /// <param name="difficulty">The difficulty of level to get.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        /// <returns>The level of given <paramref name="difficulty"/>.</returns>
        int GetLevel<T>(T difficulty)
            where T : Enum;

        /// <summary>
        /// Sets the Level for specified <paramref name="difficulty"/>.
        /// </summary>
        /// <param name="difficulty">The difficulty of level.</param>
        /// <param name="level">The level value.</param>
        /// <typeparam name="T">Type of difficulty.</typeparam>
        void SetLevel<T>(T difficulty, int level)
            where T : Enum;
    }

    /// <summary>
    /// Represents an opinionated implementation of music header and collections of <see cref="RhythmEvent"/>.
    /// </summary>
    [Serializable]
    public class Chart : IChart
    {
        private Dictionary<Enum, List<RhythmEvent>> _events = new();
        private Dictionary<Enum, bool> _sorted = new();
        private Dictionary<Enum, int> _levels = new();
        private Dictionary<Enum, int> _eventCount = new();
        private Dictionary<Enum, int> _playableEventCount = new();

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [field: SerializeField]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        [field: SerializeField]
        public string Artist { get; set; }

        /// <summary>
        /// Gets or set the illustrator.
        /// </summary>
        [field: SerializeField]
        public string Illustrator { get; set; }

        /// <summary>
        /// Gets or sets the BPM.
        /// </summary>
        [field: SerializeField]
        public float Bpm { get; set; }

        /// <summary>
        /// Add specified events to this instance of <see cref="Chart"/>.
        /// </summary>
        /// <param name="difficulty">The difficulty that the specified events belong.</param>
        /// <param name="events"></param>
        /// <typeparam name="T"></typeparam>
        public void AddEvents<T>(T difficulty, params RhythmEvent[] events)
            where T : Enum
        {
            _events.TryAdd(difficulty, new List<RhythmEvent>());
            _eventCount.TryAdd(difficulty, 0);
            _playableEventCount.TryAdd(difficulty, 0);

            foreach (var ev in events)
            {
                _events[difficulty].Add(ev);
                if (ev.Playable)
                    _playableEventCount[difficulty] += 1;

                _eventCount[difficulty] += 1;
            }

            _sorted[difficulty] = false;
        }

        /// <summary>
        /// Gets the number of events of specified difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty of event count.</param>
        /// <typeparam name="T">The type of difficulty.</typeparam>
        /// <returns>The number of events for specified difficulty.</returns>
        public int GetEventCount<T>(T difficulty) where T : Enum
        {
            return _eventCount.GetValueOrDefault(difficulty, 0);
        }

        /// <summary>
        /// Gets the number of <see cref="RhythmEvent.Playable"/> events of specified difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty of playable event count.</param>
        /// <typeparam name="T">The type of difficulty.</typeparam>
        /// <returns>The number of playable events for specified difficulty.</returns>
        public int GetPlayableEventCount<T>(T difficulty) where T : Enum
        {
            return _playableEventCount.GetValueOrDefault(difficulty, 0);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="difficulty"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public RhythmEvent[] GetEvents<T>(T difficulty)
            where T : Enum
        {
            _events.TryAdd(difficulty, new List<RhythmEvent>());
            _sorted.TryAdd(difficulty, false);

            if (!_sorted[difficulty])
            {
                _events[difficulty].Sort((a, b) =>
                    a.Position.CompareTo(b.Position)
                );
            }

            return _events[difficulty].ToArray();
        }

        public void SetLevel<T>(T difficulty, int level)
            where T : Enum
        {
            _levels[difficulty] = level;
        }

        public int GetLevel<T>(T difficulty)
            where T : Enum
        {
            _levels.TryAdd(difficulty, 0);
            return _levels[difficulty];
        }
    }
}