//-----------------------------------------------------------------------
// <copyright file="MsgEncoder.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Remote.TestKit.Proto;
using Akka.Remote.Transport;
using Helios.Buffers;
using Helios.Channels;
using Helios.Codecs;
using Helios.Logging;
using Helios.Net;
using TCP;
using Address = Akka.Actor.Address;

namespace Akka.Remote.TestKit
{
    internal class MsgEncoder : MessageToMessageEncoder<object>
    {
        private readonly ILogger _logger = LoggingFactory.GetLogger<MsgEncoder>();

        public static TCP.Address Address2Proto(Address addr)
        {
            return new TCP.Address {
                Protocol = addr.Protocol,
                System = addr.System,
                Host = addr.Host,
                Port = addr.Port.Value
            };
        }

        public static TCP.Direction Direction2Proto(ThrottleTransportAdapter.Direction dir)
        {
            switch (dir)
            {
                case ThrottleTransportAdapter.Direction.Send: return Direction.Send;
                case ThrottleTransportAdapter.Direction.Receive: return Direction.Receive;
                case ThrottleTransportAdapter.Direction.Both:
                default:
                    return Direction.Both;
            }
        }

        protected override void Encode(IChannelHandlerContext context, object message, List<object> output)
        {
            _logger.Debug("Encoding {0}", message);
            var w = new Wrapper();

            message.Match()
                .With<Hello>(
                    hello =>
                        w.Hello = new TCP.Hello
                        {
                            Name = hello.Name,
                            Address = Address2Proto(hello.Address)
                        })
                .With<EnterBarrier>(barrier =>
                {
                    var protoBarrier = new TCP.EnterBarrier
                    {
                        Name = barrier.Name
                    };

                    if (barrier.Timeout.HasValue)
                        protoBarrier.Timeout = barrier.Timeout.Value.Ticks;

                    protoBarrier.Op = BarrierOp.Enter;
                    w.Barrier = protoBarrier;
                })
                .With<BarrierResult>(result =>
                {
                    var res = result.Success ? BarrierOp.Succeeded : BarrierOp.Failed;
                    w.Barrier = new TCP.EnterBarrier { Name = result.Name, Op = res };
                })
                .With<FailBarrier>(
                    barrier =>
                        w.Barrier = new TCP.EnterBarrier
                        {
                            Name = barrier.Name,
                            Op = BarrierOp.Fail
                        })
                .With<ThrottleMsg>(
                    throttle =>
                    {
                        w.Failure = new InjectFailure
                        {
                            Failure = TCP.FailType.Throttle,
                            Address = Address2Proto(throttle.Target),
                            Direction = Direction2Proto(throttle.Direction),
                            RateMBit = throttle.RateMBit
                        };
                    })
                .With<DisconnectMsg>(
                    disconnect =>
                        w.Failure = new InjectFailure
                        {
                            Address = Address2Proto(disconnect.Target),
                            Failure = disconnect.Abort ? TCP.FailType.Abort : TCP.FailType.Disconnect
                        })
                .With<TerminateMsg>(terminate =>
                {
                    if (terminate.ShutdownOrExit.IsRight)
                    {
                        w.Failure = new InjectFailure
                        {
                            Failure = TCP.FailType.Exit,
                            ExitValue = terminate.ShutdownOrExit.ToRight().Value
                        };
                    }
                    else if (terminate.ShutdownOrExit.IsLeft && !terminate.ShutdownOrExit.ToLeft().Value)
                    {
                        w.Failure = new InjectFailure { Failure = TCP.FailType.Shutdown };
                    }
                    else
                    {
                        w.Failure = new InjectFailure { Failure = TCP.FailType.ShutdownAbrupt };
                    }
                })
                .With<GetAddress>(
                    address => w.Addr = new AddressRequest { Node = address.Node.Name })
                .With<AddressReply>(
                    reply =>
                        w.Addr = new AddressRequest
                        {
                            Node = reply.Node.Name,
                            Addr = Address2Proto(reply.Addr)
                        })
                .With<Done>(done => w.Done = string.Empty)
                .Default(obj => w.Done = string.Empty);

            output.Add(w);
        }
    }
}

