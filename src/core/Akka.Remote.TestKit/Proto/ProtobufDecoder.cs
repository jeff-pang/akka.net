//-----------------------------------------------------------------------
// <copyright file="ProtobufDecoder.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Helios.Buffers;
using Helios.Channels;
using Helios.Codecs;
using Helios.Logging;
using Helios.Util;
using Google.Protobuf.Reflection;

namespace Akka.Remote.TestKit.Proto
{
    /// <summary>
    /// Decodes a message from a <see cref="IByteBuf"/> into a Google protobuff wire format
    /// </summary>
    public class ProtobufDecoder : ByteToMessageDecoder
    {
        private readonly ILogger _logger = LoggingFactory.GetLogger<ProtobufDecoder>();
        private readonly IMessage _prototype;
        
        public ProtobufDecoder(IMessage prototype)
        {
            _prototype = prototype;
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuf input, List<object> output)
        {
            _logger.Debug("Decoding {0} into Protobuf", input);

            var readable = input.ReadableBytes;
            var buf = new byte[readable];
            input.ReadBytes(buf);
            var bs = ByteString.CopyFrom(buf);
            _prototype.MergeFrom(bs);
            output.Add(_prototype);
        }
    }
}