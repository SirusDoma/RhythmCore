using System;

namespace RhythmCore
{
    /// <summary>
    /// Represents a musical event in <see cref="Chart"/>.
    /// </summary>
    public abstract class RhythmEvent
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the timing position.
        /// </summary>
        public float Position { get; set; } = new();

        /// <summary>
        /// Gets a value indicates whether the event is instantiable into Unity <see cref="UnityEngine.Object"/>.
        /// </summary>
        public abstract bool Instantiable { get; }

        /// <summary>
        /// Gets a value indicates whether the event is playable by the player.
        /// </summary>
        public abstract bool Playable { get; }

        /// <summary>
        /// Represents Musical Note Event.
        /// </summary>
        public interface INote
        {
            /// <summary>
            /// Gets or sets the Sample ID.
            /// </summary>
            string SampleID  { get; set; }

            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            int Channel      { get; set; }

            /// <summary>
            /// Gets or sets the timing position.
            /// </summary>
            float Position   { get; set; }
        }

        /// <summary>
        /// Represents musical note event.
        /// </summary>
        public class Note : Note<Channel>, INote
        {
            public override bool Instantiable => true;

            public override bool Playable     => Channel != Channel.Background;

            int INote.Channel
            {
                get => (int)Channel;
                set => Channel = (Channel)value;
            }
        }

        /// <summary>
        /// Represents a Musical Note Event with specified channel type.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel. The specified type must be <see cref="IConvertible"/> to <see cref="Int32"/>.</typeparam>
        /// <remarks>
        /// IMPORTANT: <typeparamref name="TChannel"/> MUST BE CONVERTIBLE TO <see cref="Int32"/>;
        /// OTHERWISE, <see cref="InvalidCastException"/> MAY THROWN WHEN INTERACTING WITH THE <see cref="RhythmEvent.INote.Channel"/>.
        /// </remarks>
        public class Note<TChannel> : RhythmEvent, INote
            where TChannel : struct, IConvertible
        {
            public string SampleID { get; set; }

            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            public TChannel Channel { get; set; }

            public override bool Instantiable => true;

            public override bool Playable     => true;

            int INote.Channel
            {
                get => Channel.ToInt32(null);
                set => Channel = (TChannel)Convert.ChangeType(value, typeof(TChannel));
            }
        }

        /// <summary>
        /// Represents Tempo and Timing change Event.
        /// </summary>
        public class Tempo : RhythmEvent
        {
            /// <summary>
            /// Gets or sets the BPM.
            /// </summary>
            public float Bpm { get; set; }

            /// <summary>
            /// Gets or sets Stop duration.
            /// </summary>
            public float Stop { get; set; }

            /// <summary>
            /// Gets or sets Skip length.
            /// </summary>
            public float Skip { get; set; }

            public override bool Instantiable => false;

            public override bool Playable => false;
        }
    }
}