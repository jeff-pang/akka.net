//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Serialization;
using Google.Protobuf;

namespace Akka.Persistence.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public interface IMessage { }

    /// <summary>
    /// TBD
    /// </summary>
    public class MessageSerializer : Serializer
    {
        private Information _transportInformation;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public MessageSerializer(ExtendedActorSystem system)
            : base(system)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Information TransportInformation
        {
            get
            {
                return _transportInformation ?? (_transportInformation = GetTransportInformation());
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override bool IncludeManifest
        {
            get { return true; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override byte[] ToBinary(object obj)
        {
            if (obj is IPersistentRepresentation) return PersistentToProto(obj as IPersistentRepresentation).ToByteArray();
            if (obj is AtomicWrite) return AtomicWriteToProto(obj as AtomicWrite).ToByteArray();
            if (obj is AtLeastOnceDeliverySnapshot) return SnapshotToProto(obj as AtLeastOnceDeliverySnapshot).ToByteArray();
            // TODO StateChangeEvent

            throw new ArgumentException(typeof(MessageSerializer) + " cannot serialize object of type " + obj.GetType());
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="bytes">TBD</param>
        /// <param name="type">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == null || type == typeof(Persistent) || type == typeof(IPersistentRepresentation)) return PersistentMessageFrom(bytes);
            if (type == typeof(AtomicWrite)) return AtomicWriteFrom(bytes);
            if (type == typeof(AtLeastOnceDeliverySnapshot)) return SnapshotFrom(bytes);
            // TODO StateChangeEvent
            // TODO PersistentStateChangeEvent

            throw new ArgumentException(typeof(MessageSerializer) + " cannot deserialize object of type " + type);
        }

        private AtLeastOnceDeliverySnapshot SnapshotFrom(byte[] bytes)
        {
            var snap = global::AtLeastOnceDeliverySnapshot.Parser.ParseFrom(bytes);
            var unconfirmedDeliveries = new UnconfirmedDelivery[snap.UnconfirmedDeliveries.Count];

            for (int i = 0; i < snap.UnconfirmedDeliveries.Count; i++)
            {
                var unconfirmed = snap.UnconfirmedDeliveries[i];
                var unconfirmedDelivery = new UnconfirmedDelivery(
                    deliveryId: unconfirmed.DeliveryId,
                    destination: ActorPath.Parse(unconfirmed.Destination),
                    message: PayloadFromProto(unconfirmed.Payload));
                unconfirmedDeliveries[i] = unconfirmedDelivery;
            }

            return new AtLeastOnceDeliverySnapshot(snap.CurrentDeliveryId, unconfirmedDeliveries);
        }

        private IPersistentRepresentation PersistentMessageFrom(byte[] bytes)
        {
            var persistentMessage = PersistentMessage.Parser.ParseFrom(bytes);

            return PersistentMessageFrom(persistentMessage);
        }

        private IPersistentRepresentation PersistentMessageFrom(PersistentMessage persistentMessage)
        {
            return new Persistent(
                payload: PayloadFromProto(persistentMessage.Payload),
                sequenceNr: persistentMessage.SequenceNr,
                persistenceId:persistentMessage.PersistenceId,
                manifest: persistentMessage.Manifest,
                // isDeleted is not used in new records from 1.5
                sender: !string.IsNullOrEmpty(persistentMessage.Sender) ? system.Provider.ResolveActorRef(persistentMessage.Sender) : null,
                writerGuid: persistentMessage.WriterUuid);
        }

        private object PayloadFromProto(PersistentPayload persistentPayload)
        {
            var manifest = persistentPayload.PayloadManifest?.ToStringUtf8() ?? string.Empty; 
            return system.Serialization.Deserialize(persistentPayload.Payload.ToByteArray(), persistentPayload.SerializerId, manifest);
        }

        private AtomicWrite AtomicWriteFrom(byte[] bytes)
        {
            var atomicWrite = global::AtomicWrite.Parser.ParseFrom(bytes);

            return new AtomicWrite(atomicWrite.Payload.Select(PersistentMessageFrom).ToImmutableList());
        }

        private global::AtLeastOnceDeliverySnapshot SnapshotToProto(AtLeastOnceDeliverySnapshot snap)
        {
            var builder = new global::AtLeastOnceDeliverySnapshot
            {
                CurrentDeliveryId = snap.CurrentDeliveryId
            };

            foreach (var unconfirmed in snap.UnconfirmedDeliveries)
            {
                var unconfirmedBuilder = new global::AtLeastOnceDeliverySnapshot.Types.UnconfirmedDelivery { 
                    DeliveryId=unconfirmed.DeliveryId,
                    Destination=unconfirmed.Destination.ToString(),
                    Payload=PersistentPayloadToProto(unconfirmed.Message) };

                builder.UnconfirmedDeliveries.Add(unconfirmedBuilder);
            }

            return builder;
        }

        private PersistentMessage PersistentToProto(IPersistentRepresentation p)
        {
            var builder = new PersistentMessage();

            if (p.PersistenceId != null) builder.PersistenceId = p.PersistenceId;
            if (p.Sender != null) builder.Sender=Akka.Serialization.Serialization.SerializedActorPath(p.Sender);
            if (p.Manifest != null) builder.Manifest=p.Manifest;

            builder.Payload = PersistentPayloadToProto(p.Payload);
            builder.SequenceNr=p.SequenceNr;
            // deleted is not used in new records

            if (p.WriterGuid != null) builder.WriterUuid = p.WriterGuid;

            return builder;
        }

        private PersistentPayload PersistentPayloadToProto(object payload)
        {
            return TransportInformation != null
                ? Akka.Serialization.Serialization.SerializeWithTransport(TransportInformation.System,
                    TransportInformation.Address, () => PayloadBuilder(payload))
                : PayloadBuilder(payload);
        }

        private PersistentPayload PayloadBuilder(object payload)
        {
            var serializer = system.Serialization.FindSerializerFor(payload);
            var builder = new PersistentPayload();

            if (serializer is SerializerWithStringManifest)
            {
                var manifest = ((SerializerWithStringManifest)serializer).Manifest(payload);
                if (manifest != null)
                    builder.PayloadManifest = ByteString.CopyFromUtf8(manifest);
            }
            else if (serializer.IncludeManifest)
                builder.PayloadManifest = ByteString.CopyFromUtf8(TypeQualifiedNameForManifest(payload.GetType()));

            var bytes = serializer.ToBinary(payload);

            builder.Payload = ByteString.CopyFrom(bytes);
            builder.SerializerId = serializer.Identifier;

            return builder;
        }

        private global::AtomicWrite AtomicWriteToProto(AtomicWrite aw)
        {
            var builder = new global::AtomicWrite();

            foreach (var p in (IEnumerable<IPersistentRepresentation>)aw.Payload)
            {
                builder.Payload.Add(PersistentToProto(p));
            }
            
            return builder;
        }

        private Information GetTransportInformation()
        {
            var address = system.Provider.DefaultAddress;
            return !string.IsNullOrEmpty(address.Host)
                ? new Information { Address = address, System = system }
                : new Information { System = system };
        }
    }
}

