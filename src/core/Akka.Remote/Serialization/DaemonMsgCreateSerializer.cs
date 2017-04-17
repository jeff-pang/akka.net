﻿//-----------------------------------------------------------------------
// <copyright file="DaemonMsgCreateSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Proto;
using Akka.Routing;
using Akka.Serialization;
using Google.Protobuf;

namespace Akka.Remote.Serialization
{
    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes <see cref="DaemonMsgCreate"/> only.
    /// Serialization of contained <see cref="RouterConfig"/>, <see cref="Config"/>, and <see cref="Scope"/> is done with the
    /// configured serializer for those classes.
    /// </summary>
    public class DaemonMsgCreateSerializer : Serializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DaemonMsgCreateSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public DaemonMsgCreateSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        /// <summary>
        /// Completely unique value to identify this implementation of Serializer, used to optimize network traffic
        /// Values from 0 to 16 is reserved for Akka internal usage
        /// </summary>
        public override int Identifier
        {
            get { return 3; }
        }

        /// <summary>
        /// Returns whether this serializer needs a manifest in the fromBinary method
        /// </summary>
        public override bool IncludeManifest
        {
            get { return false; }
        }

        private ActorRefData SerializeActorRef(IActorRef @ref)
        {
            return new ActorRefData
            {
                Path = Akka.Serialization.Serialization.SerializedActorPath(@ref)
            };
        }

        private ByteString Serialize(object obj)
        {
            var serializer = system.Serialization.FindSerializerFor(obj);
            var bytes = serializer.ToBinary(obj);
            return ByteString.CopyFrom(bytes);
        }

        private object Deserialize(ByteString bytes, Type type)
        {
            var serializer = system.Serialization.FindSerializerForType(type);
            var o = serializer.FromBinary(bytes.ToByteArray(), type);
            return o;
        }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A byte array containing the serialized object</returns>
        /// <exception cref="ArgumentException">Can't serialize a non-<see cref="DaemonMsgCreate"/> message using <see cref="DaemonMsgCreateSerializer"/></exception>
        public override byte[] ToBinary(object obj)
        {
            var msg = obj as DaemonMsgCreate;
            if (msg == null)
            {
                throw new ArgumentException(
                    "Can't serialize a non-DaemonMsgCreate message using DaemonMsgCreateSerializer");
            }

            DaemonMsgCreateData daemonBuilder = new DaemonMsgCreateData
            {
                Props = GetPropsData(msg.Props),
                Deploy = GetDeployData(msg.Deploy),
                Path = msg.Path,
                Supervisor = SerializeActorRef(msg.Supervisor)
            };

            return daemonBuilder.ToByteArray();
        }

        private PropsData GetPropsData(Props props)
        {
            var builder = new PropsData
            {
                Clazz = props.Type.AssemblyQualifiedName,
                Deploy = GetDeployData(props.Deploy)
            };

            foreach (object arg in props.Arguments)
            {
                if (arg == null)
                {
                    builder.Args.Add(ByteString.Empty);
                    builder.Classes.Add("");
                }
                else
                {
                    builder.Args.Add(Serialize(arg));
                    builder.Classes.Add(arg.GetType().AssemblyQualifiedName);
                }
            }

            return builder;
        }

        private DeployData GetDeployData(Deploy deploy)
        {
            var res = new DeployData
            {
                Path = deploy.Path
            };

            if (deploy.Config != ConfigurationFactory.Empty)
                res.Config = Serialize(deploy.Config);
            if (deploy.RouterConfig != NoRouter.Instance)
                res.RouterConfig = Serialize(deploy.RouterConfig);
            if (deploy.Scope != Deploy.NoScopeGiven)
                res.Scope=Serialize(deploy.Scope);
            if (deploy.Dispatcher != Deploy.NoDispatcherGiven)
                res.Dispatcher=deploy.Dispatcher;

            return res;
        }

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        /// <exception cref="TypeLoadException">
        /// Could not find type on the remote system.
        /// Ensure that the remote system has an assembly that contains the type in its assembly search path.
        /// </exception>
        public override object FromBinary(byte[] bytes, Type type)
        {
            var proto = DaemonMsgCreateData.Parser.ParseFrom(bytes);
            Type clazz; 

            try
            {
                clazz = Type.GetType(proto.Props.Clazz, true);
            }
            catch (TypeLoadException ex)
            {
                var msg = string.Format(
                       "Could not find type '{0}' on the remote system. " +
                       "Ensure that the remote system has an assembly that contains the type {0} in its assembly search path", 
                       proto.Props.Clazz);


                throw new TypeLoadException(msg, ex);
            }

            var args = GetArgs(proto);
            var props = new Props(GetDeploy(proto.Props.Deploy), clazz, args);
            return new DaemonMsgCreate(
                props,
                GetDeploy(proto.Deploy),
                proto.Path,
                DeserializeActorRef( proto.Supervisor));
        }

        private Deploy GetDeploy(DeployData protoDeploy)
        {
            Config config;
            if (!protoDeploy.Config.IsEmpty)
                config = (Config) Deserialize(protoDeploy.Config, typeof (Config));
            else
                config = ConfigurationFactory.Empty;

            RouterConfig routerConfig;
            if (!protoDeploy.RouterConfig.IsEmpty)
                routerConfig = (RouterConfig)Deserialize(protoDeploy.RouterConfig, typeof(RouterConfig));
            else
                routerConfig = NoRouter.Instance;

            Scope scope;
            if (!protoDeploy.Scope.IsEmpty)
                scope = (Scope) Deserialize(protoDeploy.Scope, typeof (Scope));
            else
                scope = Deploy.NoScopeGiven;

            string dispatcher;
            if (!string.IsNullOrEmpty(protoDeploy.Dispatcher))
                dispatcher = protoDeploy.Dispatcher;
            else 
                dispatcher = Deploy.NoDispatcherGiven;

            return new Deploy(protoDeploy.Path, config, routerConfig, scope, dispatcher);
        }

        private IEnumerable<object> GetArgs(DaemonMsgCreateData proto)
        {
            var args = new object[proto.Props.Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                var typeName = proto.Props.Classes[i];
                var arg = proto.Props.Args[i];
                if (typeName == "" && ByteString.Empty.Equals(arg))
                {
                    //HACK: no typename and empty arg gives null 
                    args[i] = null;
                }
                else
                {
                    Type t = null;
                    if (typeName != null)
                        t = Type.GetType(typeName);
                    args[i] = Deserialize(arg, t);
                }
            }
            return args;
        }

        private IActorRef DeserializeActorRef(ActorRefData actorRefData)
        {
            var path = actorRefData.Path;
            var @ref = system.Provider.ResolveActorRef(path);
            return @ref;
        }
    }
}
