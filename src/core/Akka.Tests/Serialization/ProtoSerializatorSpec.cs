//-----------------------------------------------------------------------
// <copyright file="SerializationSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Serialization;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Xunit;
using FluentAssertions;

namespace Akka.Tests.Serialization
{
    public class ProtoSerializatorSpec : AkkaSpec
    {
        [Fact]
        public void Can_serialize_ActorRef()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            Sys.Serialization.FindSerializerFor(actorRef).Should().BeOfType<ProtoSerializer>();
            AssertEqual(actorRef);
        }

        [Fact]
        public void Can_serialize_ActorPath()
        {
            var uri = "akka.tcp://sys@localhost:9000/user/actor";
            var actorPath = ActorPath.Parse(uri);
            Sys.Serialization.FindSerializerFor(actorPath).Should().BeOfType<ProtoSerializer>();
            AssertEqual(actorPath);
        }

        [Fact]
        public void Can_serialize_Identify()
        {
            var identify = new Identify("message");
            Sys.Serialization.FindSerializerFor(identify).Should().BeOfType<ProtoSerializer>();
            AssertEqual(identify);
        }

        [Fact]
        public void Can_serialize_ActorIdentity()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            var actorIdentity = new ActorIdentity("message", actorRef);
            Sys.Serialization.FindSerializerFor(actorIdentity).Should().BeOfType<ProtoSerializer>();
            AssertEqual(actorIdentity);
        }

        [Fact]
        public void Can_serialize_PoisonPill()
        {
            var poisonPill = PoisonPill.Instance;
            Sys.Serialization.FindSerializerFor(poisonPill).Should().BeOfType<ProtoSerializer>();
            AssertEqual(poisonPill);
        }

        private void AssertEqual<T>(T message)
        {
            var serializer = Sys.Serialization.FindSerializerFor(message);
            var serialized = serializer.ToBinary(message);
            var result = serializer.FromBinary(serialized, typeof(T));
            var deserialized = (T)result;
            Assert.Equal(message, deserialized);
        }
    }
}

