//-----------------------------------------------------------------------
// <copyright file="JournalProtocol.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace Akka.Persistence
{
    /// <summary>
    /// Marker interface for internal journal messages
    /// </summary>
    public interface IJournalMessage : IPersistenceMessage { }

    /// <summary>
    /// Internal journal command
    /// </summary>
    public interface IJournalRequest : IJournalMessage { }
    
    /// <summary>
    /// Internal journal acknowledgement
    /// </summary>
    public interface IJournalResponse : IJournalMessage { }

    /// <summary>
    /// Request to delete all persistent messages with sequence numbers up to `toSequenceNr` (inclusive).  
    /// </summary>
    [Serializable]
    public sealed class DeleteMessagesTo : IJournalRequest, IEquatable<DeleteMessagesTo>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="persistenceId">Requesting persistent actor id.</param>
        /// <param name="toSequenceNr">Sequence number where replay should end (inclusive).</param>
        /// <param name="persistentActor">Requesting persistent actor.</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public DeleteMessagesTo(string persistenceId, long toSequenceNr, IActorRef persistentActor)
        {
            if (string.IsNullOrEmpty(persistenceId))
                throw new ArgumentNullException(nameof(persistenceId), "DeleteMessagesTo requires persistence id to be provided");

            PersistenceId = persistenceId;
            ToSequenceNr = toSequenceNr;
            PersistentActor = persistentActor;
        }

        /// <summary>
        /// Requesting persistent actor id.
        /// </summary>
        public string PersistenceId { get; }

        /// <summary>
        /// Sequence number where replay should end (inclusive).
        /// </summary>
        public long ToSequenceNr { get; }

        /// <summary>
        /// Requesting persistent actor.
        /// </summary>
        public IActorRef PersistentActor { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(DeleteMessagesTo other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(PersistenceId, other.PersistenceId) &&
                   ToSequenceNr == other.ToSequenceNr &&
                   Equals(PersistentActor, other.PersistentActor);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as DeleteMessagesTo);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ToSequenceNr.GetHashCode();
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
            return $"DeleteMessagesTo<pid: {PersistenceId}, seqNr: {ToSequenceNr}, persistentActor: {PersistentActor}>";
        }
    }

    /// <summary>
    /// Request to write messages.
    /// </summary>
    [Serializable]
    public sealed class WriteMessages : IJournalRequest, IEquatable<WriteMessages>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="messages">Messages to be written.</param>
        /// <param name="persistentActor">Write requestor.</param>
        /// <param name="actorInstanceId">TBD</param>
        public WriteMessages(IEnumerable<IPersistentEnvelope> messages, IActorRef persistentActor, int actorInstanceId)
        {
            Messages = messages;
            PersistentActor = persistentActor;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Messages to be written.
        /// </summary>
        public IEnumerable<IPersistentEnvelope> Messages { get; }

        /// <summary>
        /// Write requestor.
        /// </summary>
        public IActorRef PersistentActor { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public int ActorInstanceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(WriteMessages other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(PersistentActor, other.PersistentActor)
                   && Equals(Messages, other.Messages);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteMessages);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Messages != null ? Messages.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PersistentActor != null ? PersistentActor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                return hashCode;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"WriteMessages<actorInstanceId: {ActorInstanceId}, actor: {PersistentActor}>";
        }
    }

    /// <summary>
    /// Reply message to a successful <see cref="WriteMessages"/> request. This reply is sent 
    /// to the requestor before all subsequent <see cref="WriteMessageSuccess"/> replies.
    /// </summary>
    [Serializable]
    public sealed class WriteMessagesSuccessful : IJournalResponse
    {
        /// <summary>
        /// The singleton instance of WriteMessagesSuccessful.
        /// </summary>
        public static WriteMessagesSuccessful Instance { get; } = new WriteMessagesSuccessful();

        private WriteMessagesSuccessful() { }

        public override string ToString()
        {
            return "WriteMessagesSuccessful<>";
        }
    }

    /// <summary>
    /// Reply message to a failed <see cref="WriteMessages"/> request. This reply is sent 
    /// to the requestor before all subsequent <see cref="WriteMessageFailure"/> replies.
    /// </summary>
    [Serializable]
    public sealed class WriteMessagesFailed : IJournalResponse, IEquatable<WriteMessagesFailed>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cause">Failure cause.</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public WriteMessagesFailed(Exception cause)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "WriteMessagesFailed cause exception cannot be null");

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
        public bool Equals(WriteMessagesFailed other)
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
            return Equals(obj as WriteMessagesFailed);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            return (Cause != null ? Cause.GetHashCode() : 0);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"WriteMessagesFailed<cause: {Cause}>";
        }
    }

    /// <summary>
    /// Reply message to a successful <see cref="WriteMessages"/> request. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requestor.
    /// </summary>
    [Serializable]
    public sealed class WriteMessageSuccess : IJournalResponse, IEquatable<WriteMessageSuccess>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="persistent">Successfully written message.</param>
        /// <param name="actorInstanceId">TBD</param>
        public WriteMessageSuccess(IPersistentRepresentation persistent, int actorInstanceId)
        {
            Persistent = persistent;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Successfully written message.
        /// </summary>
        public IPersistentRepresentation Persistent { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public int ActorInstanceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(WriteMessageSuccess other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteMessageSuccess);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Persistent != null ? Persistent.GetHashCode() : 0) * 397) ^ ActorInstanceId;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"WriteMessageSuccess<actorInstanceId: {ActorInstanceId}, message: {Persistent}>";
        }
    }

    /// <summary>
    /// Reply message to a rejected <see cref="WriteMessages"/> request. The write of this message was rejected
    /// before it was stored, e.g. because it could not be serialized. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requestor.
    /// </summary>
    [Serializable]
    public sealed class WriteMessageRejected : IJournalResponse, IEquatable<WriteMessageRejected>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="persistent">Message rejected to be written.</param>
        /// <param name="cause">Failure cause.</param>
        /// <param name="actorInstanceId">TBD</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public WriteMessageRejected(IPersistentRepresentation persistent, Exception cause, int actorInstanceId)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "WriteMessageRejected cause exception cannot be null");

            Persistent = persistent;
            Cause = cause;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Message failed to be written.
        /// </summary>
        public IPersistentRepresentation Persistent { get; }

        /// <summary>
        /// Failure cause.
        /// </summary>
        public Exception Cause { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public int ActorInstanceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(WriteMessageRejected other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent)
                   && Equals(Cause, other.Cause);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteMessageRejected);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Cause != null ? Cause.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                hashCode = (hashCode * 397) ^ (Persistent != null ? Persistent.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"WriteMessageRejected<actorInstanceId: {ActorInstanceId}, message: {Persistent}, cause: {Cause}>";
        }
    }

    /// <summary>
    /// Reply message to a failed <see cref="WriteMessages"/> request. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requestor.
    /// </summary>
    [Serializable]
    public sealed class WriteMessageFailure : IJournalResponse, IEquatable<WriteMessageFailure>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="persistent">Message failed to be written.</param>
        /// <param name="cause">Failure cause.</param>
        /// <param name="actorInstanceId">TBD</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public WriteMessageFailure(IPersistentRepresentation persistent, Exception cause, int actorInstanceId)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "WriteMessageFailure cause exception cannot be null");

            Persistent = persistent;
            Cause = cause;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Message failed to be written.
        /// </summary>
        public IPersistentRepresentation Persistent { get; }

        /// <summary>
        /// Failure cause.
        /// </summary>
        public Exception Cause { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public int ActorInstanceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(WriteMessageFailure other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent)
                   && Equals(Cause, other.Cause);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteMessageFailure);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Cause != null ? Cause.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                hashCode = (hashCode * 397) ^ (Persistent != null ? Persistent.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"WriteMessageFailure<actorInstanceId: {ActorInstanceId}, message: {Persistent}, cause: {Cause}>";
        }
    }

    /// <summary>
    /// Reply message to a <see cref="WriteMessages"/> with a non-persistent message.
    /// </summary>
    [Serializable]
    public sealed class LoopMessageSuccess : IJournalResponse, IEquatable<LoopMessageSuccess>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">A looped message.</param>
        /// <param name="actorInstanceId">TBD</param>
        public LoopMessageSuccess(object message, int actorInstanceId)
        {
            Message = message;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// A looped message.
        /// </summary>
        public object Message { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public int ActorInstanceId { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(LoopMessageSuccess other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Message, other.Message);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as LoopMessageSuccess);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Message != null ? Message.GetHashCode() : 0) * 397) ^ ActorInstanceId;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"LoopMessageSuccess<actorInstanceId: {ActorInstanceId}, message: {Message}>";
        }
    }

    /// <summary>
    /// Request to replay messages to the <see cref="PersistentActor"/>.
    /// </summary>
    [Serializable]
    public sealed class ReplayMessages : IJournalRequest, IEquatable<ReplayMessages>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="fromSequenceNr">Sequence number where replay should start (inclusive).</param>
        /// <param name="toSequenceNr">Sequence number where replay should end (inclusive).</param>
        /// <param name="max">Maximum number of messages to be replayed.</param>
        /// <param name="persistenceId">Requesting persistent actor identifier.</param>
        /// <param name="persistentActor">Requesting persistent actor.</param>
        public ReplayMessages(long fromSequenceNr, long toSequenceNr, long max, string persistenceId,
            IActorRef persistentActor)
        {
            FromSequenceNr = fromSequenceNr;
            ToSequenceNr = toSequenceNr;
            Max = max;
            PersistenceId = persistenceId;
            PersistentActor = persistentActor;
        }

        /// <summary>
        /// Inclusive lower sequence number bound where a replay should start.
        /// </summary>
        public long FromSequenceNr { get; }

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        public long ToSequenceNr { get; }

        /// <summary>
        /// Maximum number of messages to be replayed.
        /// </summary>
        public long Max { get; }

        /// <summary>
        /// Requesting persistent actor identifier.
        /// </summary>
        public string PersistenceId { get; }

        /// <summary>
        /// Requesting persistent actor.
        /// </summary>
        public IActorRef PersistentActor { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReplayMessages other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(PersistenceId, other.PersistenceId)
                   && Equals(PersistentActor, other.PersistentActor)
                   && Equals(FromSequenceNr, other.FromSequenceNr)
                   && Equals(ToSequenceNr, other.ToSequenceNr)
                   && Equals(Max, other.Max);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReplayMessages);
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
                hashCode = (hashCode * 397) ^ ToSequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ Max.GetHashCode();
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
            return $"ReplayMessages<fromSequenceNr: {FromSequenceNr}, toSequenceNr: {ToSequenceNr}, max: {Max}, persistenceId: {PersistenceId}>";
        }
    }

    /// <summary>
    /// Reply message to a <see cref="ReplayMessages"/> request. A separate reply is sent to the requestor for each replayed message.
    /// </summary>
    [Serializable]
    public sealed class ReplayedMessage : IJournalResponse, IEquatable<ReplayedMessage>, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="persistent">Replayed message.</param>
        public ReplayedMessage(IPersistentRepresentation persistent)
        {
            Persistent = persistent;
        }

        /// <summary>
        /// Replayed message.
        /// </summary>
        public IPersistentRepresentation Persistent { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReplayedMessage other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Persistent, other.Persistent);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReplayedMessage);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            return (Persistent != null ? Persistent.GetHashCode() : 0);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return $"ReplayedMessage<message: {Persistent}>";
        }
    }

    /// <summary>
    /// Reply message to a successful <see cref="ReplayMessages"/> request. This reply is sent 
    /// to the requestor after all <see cref="ReplayedMessage"/> have been sent (if any).
    /// 
    /// It includes the highest stored sequence number of a given persistent actor.
    /// Note that the replay might have been limited to a lower sequence number.
    /// </summary>
    [Serializable]
    public sealed class RecoverySuccess : IJournalResponse, IEquatable<RecoverySuccess>, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="highestSequenceNr">Highest stored sequence number.</param>
        public RecoverySuccess(long highestSequenceNr)
        {
            HighestSequenceNr = highestSequenceNr;
        }

        /// <summary>
        /// Highest stored sequence number.
        /// </summary>
        public long HighestSequenceNr { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(RecoverySuccess other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(HighestSequenceNr, other.HighestSequenceNr);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as RecoverySuccess);
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
            return $"RecoverySuccess<highestSequenceNr: {HighestSequenceNr}>";
        }
    }

    /// <summary>
    /// Reply message to a failed <see cref="ReplayMessages"/> request. This reply is sent to the requestor
    /// if a replay could not be successfully completed.
    /// </summary>
    [Serializable]
    public sealed class ReplayMessagesFailure : IJournalResponse, IEquatable<ReplayMessagesFailure>, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cause">TBD</param>
        /// <exception cref="ArgumentNullException">TBD</exception>
        public ReplayMessagesFailure(Exception cause)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause), "ReplayMessagesFailure cause exception cannot be null");

            Cause = cause;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Exception Cause { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(ReplayMessagesFailure other)
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
            return Equals(obj as ReplayMessagesFailure);
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
            return string.Format("ReplayMessagesFailure<cause: {0}>", Cause);
        }
    }
}
