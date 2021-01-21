﻿namespace NServiceBus.Transport.SQS.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;
    using NUnit.Framework;
    using Performance.TimeToBeReceived;
    using SimpleJson;
    using SQS;
    using Transport;

    [TestFixture]
    public class TransportMessageTests
    {
        [Test]
        public void Defaults_TimeToBeReceived_to_TimeSpan_MaxTime_when_DiscardIfNotReceivedBefore_is_not_provided()
        {
            var outgoingMessage = new OutgoingMessage(string.Empty, new Dictionary<string, string>(), new byte[0]);

            var transportMessage = new TransportMessage(outgoingMessage, new DispatchProperties());

            Assert.AreEqual(TimeSpan.MaxValue.ToString(), transportMessage.TimeToBeReceived, "TimeToBeReceived is not TimeSpan.MaxValue");
        }

        [Test]
        public void Populates_TimeToBeReceived_when_DiscardIfNotReceivedBefore_is_provided()
        {
            var outgoingMessage = new OutgoingMessage(string.Empty, new Dictionary<string, string>(), new byte[0]);
            var dispatchProperties = new DispatchProperties
            {
                DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(_expectedTtbr)
            };

            var transportMessage = new TransportMessage(outgoingMessage, dispatchProperties);

            Assert.AreEqual(_expectedTtbr.ToString(), transportMessage.TimeToBeReceived, "TimeToBeReceived is not the expected value");
        }

        [Test]
        public void Populates_TimeToBeReceived_when_TimeToBeReceived_Header_is_present()
        {
            var transportMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>
                {
                    {TransportHeaders.TimeToBeReceived, _expectedTtbr.ToString()}
                }
            };

            Assert.AreEqual(_expectedTtbr.ToString(), transportMessage.TimeToBeReceived, "TimeToBeReceived does not match expected value.");
        }

        [Test]
        public void Adds_TimeToBeReceived_Header_when_property_value_is_provided()
        {
            var transportMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>(),
                TimeToBeReceived = _expectedTtbr.ToString()
            };

            Assert.IsTrue(transportMessage.Headers.ContainsKey(TransportHeaders.TimeToBeReceived), "TimeToBeReceived header is missing");
            Assert.AreEqual(_expectedTtbr.ToString(), transportMessage.Headers[TransportHeaders.TimeToBeReceived], "TimeToBeReceived header does not match expected value.");
        }

        [Test]
        public void Does_not_add_TimeToBeReceived_Header_when_property_is_set_to_null()
        {
            var transportMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>()
            };

            Assert.IsFalse(transportMessage.Headers.ContainsKey(TransportHeaders.TimeToBeReceived), "TimeToBeReceived header was populated");
        }

        [Test]
        public void Populates_ReplyToAddress_when_header_is_present()
        {
            var outgoingMessage = new OutgoingMessage(string.Empty, new Dictionary<string, string>
            {
                {Headers.ReplyToAddress, ExpectedReplyToAddress}
            }, new byte[0]);

            var transportMessage = new TransportMessage(outgoingMessage, new DispatchProperties());

            Assert.AreEqual(ExpectedReplyToAddress, transportMessage.ReplyToAddress.Value.Queue, "ReplyToAddress is not the expected value");
        }

        [Test]
        public void ReplyToAddress_is_null_when_no_ReplyToAddress_header_is_present()
        {
            var outgoingMessage = new OutgoingMessage(string.Empty, new Dictionary<string, string>(), new byte[0]);

            var transportMessage = new TransportMessage(outgoingMessage, new DispatchProperties());

            Assert.IsNull(transportMessage.ReplyToAddress, "ReplyToAddress is not null");
        }

        [Test]
        public void Adds_ReplyToAddress_Header_when_property_value_is_provided()
        {
            var transportMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>(),
                ReplyToAddress = new TransportMessage.Address { Queue = ExpectedReplyToAddress }
            };

            Assert.IsTrue(transportMessage.Headers.ContainsKey(Headers.ReplyToAddress), "ReplyToAddress header is missing");
            Assert.AreEqual(ExpectedReplyToAddress, transportMessage.Headers[Headers.ReplyToAddress], "ReplyToAddress header does not match expected value.");
        }

        [Test]
        public void Does_not_add_ReplyToAddress_Header_when_property_value_is_set_to_null()
        {
            var transportMessage = new TransportMessage
            {
                Headers = new Dictionary<string, string>(),
                ReplyToAddress = null
            };

            Assert.IsFalse(transportMessage.Headers.ContainsKey(Headers.ReplyToAddress), "ReplyToAddress header was created");
        }

        [Test]
        public void Can_be_built_from_serialized_v1_message()
        {
            var json = SimpleJson.SerializeObject(new TransportMessage
            {
                Headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.Empty.ToString()}
                },
                Body = "empty message",
                S3BodyKey = null,
                TimeToBeReceived = _expectedTtbr.ToString(),
                ReplyToAddress = new TransportMessage.Address
                {
                    Queue = ExpectedReplyToAddress,
                    Machine = Environment.MachineName
                }
            });

            var transportMessage = SimpleJson.DeserializeObject<TransportMessage>(json);

            Assert.IsTrue(transportMessage.Headers.ContainsKey(TransportHeaders.TimeToBeReceived), "TimeToBeReceived header is missing");
            Assert.AreEqual(_expectedTtbr.ToString(), transportMessage.Headers[TransportHeaders.TimeToBeReceived], "TimeToBeReceived header does not match expected value.");
            Assert.IsTrue(transportMessage.Headers.ContainsKey(Headers.ReplyToAddress), "ReplyToAddress header is missing");
            Assert.AreEqual(ExpectedReplyToAddress, transportMessage.Headers[Headers.ReplyToAddress], "ReplyToAddress header does not match expected value.");
        }

        [Test]
        public void Can_be_built_from_serialized_message()
        {
            var json = SimpleJson.SerializeObject(new
            {
                Headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.Empty.ToString()}
                },
                Body = "empty message",
                S3BodyKey = (string)null
            });

            var transportMessage = SimpleJson.DeserializeObject<TransportMessage>(json);

            Assert.IsFalse(transportMessage.Headers.ContainsKey(TransportHeaders.TimeToBeReceived), "TimeToBeReceived header was found");
            Assert.AreEqual(TimeSpan.MaxValue.ToString(), transportMessage.TimeToBeReceived, "TimeToBeReceived does not match expected value.");
            Assert.IsFalse(transportMessage.Headers.ContainsKey(Headers.ReplyToAddress), "ReplyToAddress header was found");
            Assert.IsNull(transportMessage.ReplyToAddress, "ReplyToAddress was not null.");
        }

        const string ExpectedReplyToAddress = "TestReplyToAddress";
        static TimeSpan _expectedTtbr = TimeSpan.MaxValue.Subtract(TimeSpan.FromHours(1));
    }
}