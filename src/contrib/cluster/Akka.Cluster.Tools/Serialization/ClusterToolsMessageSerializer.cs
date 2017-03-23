//-----------------------------------------------------------------------
// <copyright file="ClusterClientMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;
using Google.Protobuf;

namespace Akka.Cluster.Tools.Serialization
{
    public class ClusterToolsMessageSerializer : Serializer
    {
        public ClusterToolsMessageSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override int Identifier { get; } = 22;

        public override bool IncludeManifest => true;

        public override byte[] ToBinary(object obj)
        {
            if (obj is PublishSubscribe.Internal.Status)
                return StatusMessageBuilder((PublishSubscribe.Internal.Status)obj).ToByteArray();

            if (obj is PublishSubscribe.Internal.Delta)
                return DeltaMessageBuilder((PublishSubscribe.Internal.Delta)obj).ToByteArray();

            throw new ArgumentException($"Can't serialize object of type {obj.GetType()}");
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == typeof(PublishSubscribe.Internal.Status))
            {
                return StatusFrom(Protobuf.Msg.Status.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(PublishSubscribe.Internal.Delta))
            {
                return DeltaFrom(Protobuf.Msg.Delta.Parser.ParseFrom(bytes));
            }

            throw new ArgumentException(typeof(ProtoSerializer) + " cannot deserialize object of type " + type);
        }

        //
        // PublishSubscribe.Internal.Status
        //
        private static Protobuf.Msg.Status StatusMessageBuilder(PublishSubscribe.Internal.Status status)
        {
            var message = new Protobuf.Msg.Status();
            message.ReplyToStatus = status.IsReplyToStatus;
            foreach (var version in status.Versions)
            {
                var protoVersion = new Protobuf.Msg.Status.Types.Version();
                protoVersion.Timestamp = version.Value;
                protoVersion.Address = AddressMessageBuilder(version.Key);
                message.Versions.Add(protoVersion);
            }

            return message;
        }

        private static PublishSubscribe.Internal.Status StatusFrom(Protobuf.Msg.Status statusProto)
        {
            var versions = new Dictionary<Address, long>();

            foreach (var protoVersion in statusProto.Versions)
            {
                versions.Add(AddressFrom(protoVersion.Address), protoVersion.Timestamp);
            }

            return new PublishSubscribe.Internal.Status(versions, statusProto.ReplyToStatus);
        }

        //
        // PublishSubscribe.Internal.Delta
        //
        private static Protobuf.Msg.Delta DeltaMessageBuilder(PublishSubscribe.Internal.Delta delta)
        {
            var message = new Protobuf.Msg.Delta();
            foreach (var bucket in delta.Buckets)
            {
                var protoBucket = new Protobuf.Msg.Delta.Types.Bucket();
                protoBucket.Owner = AddressMessageBuilder(bucket.Owner);
                protoBucket.Version = bucket.Version;

                foreach (var bucketContent in bucket.Content)
                {
                    var valueHolder = new Protobuf.Msg.Delta.Types.ValueHolder();
                    valueHolder.Ref = Akka.Serialization.Serialization.SerializedActorPath(bucketContent.Value.Ref); // TODO: reuse the method from the core serializer
                    valueHolder.Version = bucketContent.Value.Version;
                    protoBucket.Content.Add(bucketContent.Key, valueHolder);
                }

                message.Buckets.Add(protoBucket);
            }

            return message;
        }

        private PublishSubscribe.Internal.Delta DeltaFrom(Protobuf.Msg.Delta deltaProto)
        {
            var buckets = new List<PublishSubscribe.Internal.Bucket>();
            foreach (var protoBuckets in deltaProto.Buckets)
            {
                var content = new Dictionary<string, PublishSubscribe.Internal.ValueHolder>();

                foreach (var protoBucketContent in protoBuckets.Content)
                {
                    var valueHolder = new PublishSubscribe.Internal.ValueHolder(protoBucketContent.Value.Version, ResolveActorRef(protoBucketContent.Value.Ref));
                    content.Add(protoBucketContent.Key, valueHolder);
                }

                var bucket = new PublishSubscribe.Internal.Bucket(AddressFrom(protoBuckets.Owner), protoBuckets.Version, content.ToImmutableDictionary());
                buckets.Add(bucket);
            }

            return new PublishSubscribe.Internal.Delta(buckets.ToArray());
        }

        //
        // Private helpers
        //

        private static Protobuf.Msg.Address AddressMessageBuilder(Address address)
        {
            var message = new Protobuf.Msg.Address();
            message.System = address.System;
            message.Hostname = address.Host;
            message.Port = address.Port ?? 0;
            message.Protocol = address.Protocol;
            return message;
        }

        private static Address AddressFrom(Protobuf.Msg.Address addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Hostname,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        // TODO: reuse the method from the core serializer
        private IActorRef ResolveActorRef(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            return system.Provider.ResolveActorRef(path);
        }
    }
}