using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;
using Google.Protobuf;

namespace Akka.Cluster.Serialization
{
    public class ClusterMessageSerializer : Serializer
    {
        public ClusterMessageSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override int Identifier { get; } = 23;

        public override bool IncludeManifest => true;

        public override byte[] ToBinary(object obj)
        {
            if (obj is GossipEnvelope)
            {
                return GossipEnvelopeMessageBuilder((GossipEnvelope) obj).ToByteArray();
            }
            else if (obj is GossipStatus)
            {
                return GossipStatusMessageBuilder((GossipStatus) obj).ToByteArray();
            }
            else if (obj is InternalClusterAction.Welcome)
            {
                return WelcomeMessageBuilder((InternalClusterAction.Welcome)obj).ToByteArray();
            }

            throw new ArgumentException($"Can't serialize object of type {obj.GetType()}");
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == typeof(GossipEnvelope))
            {
                return GossipEnvelopeFrom(Protobuf.Msg.GossipEnvelope.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(GossipStatus))
            {
                return GossipStatusFrom(Protobuf.Msg.GossipStatus.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(InternalClusterAction.Welcome))
            {
                return WelcomeFrom(Protobuf.Msg.Welcome.Parser.ParseFrom(bytes));
            }

            throw new ArgumentException(typeof(ProtoSerializer) + " cannot deserialize object of type " + type);
        }

        //
        // GossipEnvelope
        //
        private static Protobuf.Msg.GossipEnvelope GossipEnvelopeMessageBuilder(GossipEnvelope gossipEnvelope)
        {
            var message = new Protobuf.Msg.GossipEnvelope();

            message.From = UniqueAddressMessageBuilder(gossipEnvelope.From);
            message.To = UniqueAddressMessageBuilder(gossipEnvelope.To);
            message.SerializedGossip = ByteString.CopyFrom(GossipMessageBuilder(gossipEnvelope.Gossip).ToByteArray());

            return message;
        }

        private static GossipEnvelope GossipEnvelopeFrom(Protobuf.Msg.GossipEnvelope gossipEnvelopeProto)
        {
            return new GossipEnvelope(
                UniqueAddressFrom(gossipEnvelopeProto.From),
                UniqueAddressFrom(gossipEnvelopeProto.To),
                GossipFrom(Protobuf.Msg.Gossip.Parser.ParseFrom(gossipEnvelopeProto.SerializedGossip)));
        }

        //
        // GossipStatus
        //
        private static Protobuf.Msg.GossipStatus GossipStatusMessageBuilder(GossipStatus gossipStatus)
        {
            var message = new Protobuf.Msg.GossipStatus();
            message.From = UniqueAddressMessageBuilder(gossipStatus.From);
            message.Version = VectorClockMessageBuilder(gossipStatus.Version);
            return message;
        }

        private static GossipStatus GossipStatusFrom(Protobuf.Msg.GossipStatus gossipStatusProto)
        {
            return new GossipStatus(UniqueAddressFrom(gossipStatusProto.From), VectorClockFrom(gossipStatusProto.Version));
        }

        //
        // Gossip (TODO: implement)
        //
        private static Protobuf.Msg.Gossip GossipMessageBuilder(Gossip gossip)
        {
            var message = new Protobuf.Msg.Gossip();

            foreach (var member in gossip.Members)
            {
                var protoMember = new Protobuf.Msg.Member();
                protoMember.UniqueAddress = UniqueAddressMessageBuilder(member.UniqueAddress);
                protoMember.UpNumber = member.UpNumber;
                protoMember.Status = (Protobuf.Msg.Member.Types.MemberStatus)member.Status;

                foreach (var role in member.Roles)
                {
                    protoMember.Roles.Add(role);
                }
                
                message.Members.Add(protoMember);
            }

            message.Overview = new Protobuf.Msg.GossipOverview();
            foreach (var seen in gossip.Overview.Seen)
            {
                message.Overview.Seen.Add(UniqueAddressMessageBuilder(seen));
            }

            message.Overview.Reachability = new Protobuf.Msg.Reachability();
            foreach (var record in gossip.Overview.Reachability.Records)
            {
                var protoRecord = new Protobuf.Msg.Record();
                protoRecord.Observer = UniqueAddressMessageBuilder(record.Observer);
                protoRecord.Subject = UniqueAddressMessageBuilder(record.Subject);
                protoRecord.Status = (Protobuf.Msg.Record.Types.ReachabilityStatus)record.Status;
                protoRecord.Version = record.Version;
                message.Overview.Reachability.Records.Add(protoRecord);
            }

            foreach (var version in gossip.Overview.Reachability.Versions)
            {
                var reachabilityVersion = new Protobuf.Msg.Reachability.Types.ReachabilityVersion();
                reachabilityVersion.UniqueAddress = UniqueAddressMessageBuilder(version.Key);
                reachabilityVersion.Version = version.Value;
                message.Overview.Reachability.Versions.Add(reachabilityVersion);
            }

            message.Version = VectorClockMessageBuilder(gossip.Version);

            return message;
        }

        private static Gossip GossipFrom(Protobuf.Msg.Gossip gossipProto)
        {
            var members = new SortedSet<Member>();
            foreach (var protoMember in gossipProto.Members)
            {
                var roles = new HashSet<string>();

                foreach (var role in protoMember.Roles)
                {
                    roles.Add(role);
                }

                var member = new Member(
                    UniqueAddressFrom(protoMember.UniqueAddress),
                    protoMember.UpNumber,
                    (MemberStatus)protoMember.Status,
                    roles.ToImmutableHashSet());
                members.Add(member);
            }

            var seens = new HashSet<UniqueAddress>();
            foreach (var protoSeen in gossipProto.Overview.Seen)
            {
                seens.Add(UniqueAddressFrom(protoSeen));
            }

            var records = new List<Reachability.Record>();
            foreach (var protoRecord in gossipProto.Overview.Reachability.Records)
            {
                var record = new Reachability.Record(
                    UniqueAddressFrom(protoRecord.Observer),
                    UniqueAddressFrom(protoRecord.Subject),
                    (Reachability.ReachabilityStatus) protoRecord.Status,
                    protoRecord.Version);

                records.Add(record);
            }

            var versions = new Dictionary<UniqueAddress, long>();
            foreach (var protoVersion in gossipProto.Overview.Reachability.Versions)
            {
                versions.Add(UniqueAddressFrom(protoVersion.UniqueAddress), protoVersion.Version);
            }

            var reachability = new Reachability(records.ToImmutableList(), versions.ToImmutableDictionary());
            
            var gossipOverview = new GossipOverview(seens.ToImmutableHashSet(), reachability);
            var version = VectorClockFrom(gossipProto.Version);
            return new Gossip(members.ToImmutableSortedSet(), gossipOverview, version);
        }

        //
        // VectorClock
        //
        private static Protobuf.Msg.VectorClock VectorClockMessageBuilder(VectorClock vectorClock)
        {
            var message = new Protobuf.Msg.VectorClock();

            foreach (var version in vectorClock.Versions)
            {
                var versionProto = new Protobuf.Msg.VectorClock.Types.Version();
                versionProto.Node = version.Key.ToString();
                versionProto.Timestamp = version.Value;
                message.Versions.Add(versionProto);
            }

            return message;
        }

        private static VectorClock VectorClockFrom(Protobuf.Msg.VectorClock vectorClockProto)
        {
            var versions = new SortedDictionary<VectorClock.Node, long>();
            foreach (var versionProto in vectorClockProto.Versions)
            {
                versions.Add(new VectorClock.Node(versionProto.Node), versionProto.Timestamp);
            }

            return VectorClock.Create(versions.ToImmutableSortedDictionary());
        }

        //
        // Welcome
        //

        private Protobuf.Msg.Welcome WelcomeMessageBuilder(InternalClusterAction.Welcome welcome)
        {
            var welcomeProto = new Protobuf.Msg.Welcome();
            welcomeProto.From = UniqueAddressMessageBuilder(welcome.From);
            welcomeProto.Gossip = GossipMessageBuilder(welcome.Gossip);
            return welcomeProto;
        }

        private static InternalClusterAction.Welcome WelcomeFrom(Protobuf.Msg.Welcome welcomeProto)
        {
            return new InternalClusterAction.Welcome(
                UniqueAddressFrom(welcomeProto.From),
                GossipFrom(welcomeProto.Gossip));
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

        private static Protobuf.Msg.UniqueAddress UniqueAddressMessageBuilder(UniqueAddress uniqueAddress)
        {
            var message = new Protobuf.Msg.UniqueAddress();
            message.Address = AddressMessageBuilder(uniqueAddress.Address);
            message.Uid = (uint)uniqueAddress.Uid;
            return message;
        }

        private static UniqueAddress UniqueAddressFrom(Protobuf.Msg.UniqueAddress uniqueAddressProto)
        {
            return new UniqueAddress(AddressFrom(uniqueAddressProto.Address), (int)uniqueAddressProto.Uid);
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
