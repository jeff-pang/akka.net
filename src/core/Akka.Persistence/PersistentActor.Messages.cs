//-----------------------------------------------------------------------
// <copyright file="PersistentActor.Messages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;

namespace Akka.Persistence
{
    /// <summary>
    /// Reply message to a successful <see cref="Eventsourced.DeleteMessages"/> request.
    /// </summary>
    [Serializable]
    public sealed class DeleteMessagesSuccess : IJournalResponse, IEquatable<DeleteMessagesSuccess>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="toSequenceNr">Inclusive upper sequence number bound where a replay should end.</param>
        public DeleteMessagesSuccess(long toSequenceNr)
        {
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        public long ToSequenceNr { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(DeleteMessagesSuccess other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return ToSequenceNr == other.ToSequenceNr;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as DeleteMessagesSuccess);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            return ToSequenceNr.GetHashCode();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"DeleteMessagesSuccess<toSequenceNr: {ToSequenceNr}>";
        }
    }

    /// <summary>
    /// Reply message to failed <see cref="Eventsourced.DeleteMessages"/> request.
    /// </summary>
    [Serializable]
    public sealed class DeleteMessagesFailure : IJournalResponse, IEquatable<DeleteMessagesFailure>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cause">Failure cause.</param>
        /// <param name="toSequenceNr">Inclusive upper sequence number bound where a replay should end.</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public DeleteMessagesFailure(Exception cause, long toSequenceNr)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "DeleteMessagesFailure cause exception cannot be null");

            Cause = cause;
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Failure cause.
        /// </summary>
        public Exception Cause { get; }

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        public long ToSequenceNr { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(DeleteMessagesFailure other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause) && ToSequenceNr == other.ToSequenceNr;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as DeleteMessagesFailure);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Cause != null ? Cause.GetHashCode() : 0) * 397) ^ ToSequenceNr.GetHashCode();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"DeleteMessagesFailure<cause: {Cause}, toSequenceNr: {ToSequenceNr}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ReadHighestSequenceNr : IJournalRequest, IEquatable<ReadHighestSequenceNr>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="fromSequenceNr">TBD</param>
        /// <param name="persistenceId">TBD</param>
        /// <param name="persistentActor">TBD</param>
        public ReadHighestSequenceNr(long fromSequenceNr, string persistenceId, IActorRef persistentActor)
        {
            FromSequenceNr = fromSequenceNr;
            PersistenceId = persistenceId;
            PersistentActor = persistentActor;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public long FromSequenceNr { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public string PersistenceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef PersistentActor { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReadHighestSequenceNr other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(PersistenceId, other.PersistenceId)
                   && Equals(FromSequenceNr, other.FromSequenceNr)
                   && Equals(PersistentActor, other.PersistentActor);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadHighestSequenceNr);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FromSequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PersistentActor != null ? PersistentActor.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"ReadHighestSequenceNr<pid: {PersistenceId}, fromSeqNr: {FromSequenceNr}, actor: {PersistentActor}>";
        }
    }

    //TODO: what is that?
    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ReadHighestSequenceNrSuccess : IEquatable<ReadHighestSequenceNrSuccess>, IComparable<ReadHighestSequenceNrSuccess>
    {

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="highestSequenceNr">TBD</param>
        public ReadHighestSequenceNrSuccess(long highestSequenceNr)
        {
            HighestSequenceNr = highestSequenceNr;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public long HighestSequenceNr { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReadHighestSequenceNrSuccess other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return HighestSequenceNr == other.HighestSequenceNr;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public int CompareTo(ReadHighestSequenceNrSuccess other)
        {
            if (other == null) return 1;
            return other.HighestSequenceNr.CompareTo(HighestSequenceNr);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadHighestSequenceNrSuccess);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            return HighestSequenceNr.GetHashCode();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"ReadHighestSequenceNrSuccess<nr: {HighestSequenceNr}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ReadHighestSequenceNrFailure : IEquatable<ReadHighestSequenceNrFailure>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cause">Failure cause.</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public ReadHighestSequenceNrFailure(Exception cause)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "ReadHighestSequenceNrFailure cause exception cannot be null");

            Cause = cause;
        }

        /// <summary>
        /// Failure cause.
        /// </summary>
        public Exception Cause { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReadHighestSequenceNrFailure other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadHighestSequenceNrFailure);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            return Cause.GetHashCode();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"ReadHighestSequenceNrFailure<cause: {Cause}>";
        }
    }
}
