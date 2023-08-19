using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace RhythmCore
{
    /// <summary>
    /// Represents a list of the <see cref="ChartRenderer{T}"/> front <see cref="RhythmEvent"/> state's in a frame.
    /// </summary>
    public interface IFrontEventStateList
    {
        /// <summary>
        /// Update the event list that match with associated channel of the specified state.
        /// Do nothing if the specified state is no longer valid or older than the current state.
        /// </summary>
        /// <param name="state">The event state to store inside the list</param>
        void Update(EventState state);

        /// <summary>
        /// Gets an array of <see cref="RhythmEvent.INote"/> that represents all front events stored in the current instance of this class.
        /// </summary>
        /// <returns>An array of <see cref="RhythmEvent.INote"/>.</returns>
        T[] GetFrontEvents<T>()
            where T : RhythmEvent, RhythmEvent.INote;

        /// <summary>
        /// Invalidate the front event state of specified channel.
        /// </summary>
        /// <param name="channel">The channel of the front event to invalidate.</param>
        void Invalidate(int channel);
    }

    /// <summary>
    /// Represents a list of the <see cref="ChartRenderer{T}"/> front <see cref="RhythmEvent"/> state's in a frame.
    /// </summary>
    public class FrontEventStateList : FrontEventStateList<Channel>
    {
    }

    /// <summary>
    /// Represents a list of the <see cref="ChartRenderer{T}"/> front <see cref="RhythmEvent"/> state's in a frame.
    /// </summary>
    /// <typeparam name="T">The type of channel.</typeparam>
    public class FrontEventStateList<T> : IFrontEventStateList
        where T : struct, IConvertible
    {
        private readonly Dictionary<T, EventState> _frontStates = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="FrontEventStateList{T}"/> class.
        /// </summary>
        public FrontEventStateList()
        {
        }

        /// <summary>
        /// Update the event list that match with associated channel of the specified state.
        /// Do nothing if the specified state is no longer valid or older than the current state.
        /// </summary>
        /// <param name="state">The event state to store inside the list</param>
        public void Update(EventState state)
        {
            if (state.Completed || state.Event is not RhythmEvent.Note<T> { Playable: true } ev)
                return;

            if (_frontStates.TryGetValue(ev.Channel, out var current) && current is { Completed: false } && current.Event.Position < ev.Position)
                return;

            _frontStates[ev.Channel] = state;
        }

        /// <summary>
        /// Gets an array of <typeparamref name="TV"/> that represents all front events stored in the current instance of this class.
        /// </summary>
        /// <returns>An array of <typeparamref name="TV"/>.</returns>
        public TV[] GetFrontEvents<TV>()
            where TV : RhythmEvent, RhythmEvent.INote
        {
            var result = new List<TV>();
            foreach (var (channel, state) in _frontStates.ToList())
            {
                if (state == null || state.Completed)
                {
                    _frontStates.Remove(channel);
                    continue;
                }

                if (_frontStates[channel].Event is TV ev)
                    result.Add(ev);
            }

            result.Sort((a, b) => ((RhythmEvent.INote)a).Position.CompareTo(((RhythmEvent.INote)b).Position));
            return result.ToArray();
        }

        /// <summary>
        /// Gets the front event that associated with the specified channel.
        /// </summary>
        /// <param name="channel">The channel that associated with the front event to get.</param>
        /// <returns><see cref="RhythmEvent"/> if the front event with associated channel exists; otherwise, <c>null</c>.</returns>
        /// <remarks>The front buffer is cleared if the event is judged and will be updated on the next frame.</remarks>
        public RhythmEvent.Note<T> GetFrontEventFor(T channel)
        {
            if (!_frontStates.TryGetValue(channel, out var state))
                return null;

            if (!state.Completed)
                return state.Event as RhythmEvent.Note<T>;

            _frontStates.Remove(channel);
            return null;
        }

        /// <summary>
        /// Invalidate the front event state of specified channel.
        /// </summary>
        /// <param name="channel">The channel of the front event to invalidate.</param>
        public void Invalidate(T channel)
        {
            _frontStates[channel] = null;
        }

        /// <summary>
        /// Invalidate the front event state of specified channel.
        /// </summary>
        /// <param name="channel">The channel of the front event to invalidate.</param>
        public void Invalidate(int channel)
        {
            var key = (T)Convert.ChangeType(channel, typeof(T));
            _frontStates[key] = null;
        }
    }
}