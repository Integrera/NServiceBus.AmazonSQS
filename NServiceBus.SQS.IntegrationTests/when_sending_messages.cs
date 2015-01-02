﻿using System;
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json;

namespace NServiceBus.SQS.IntegrationTests
{
	[TestFixture]
	public class when_sending_messages
	{
        private SqsTestContext _context;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _context = new SqsTestContext();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _context.Dispose();
        }

        [Test]
        public void body_should_transmit_correctly()
        {
            var transportMessage = new TransportMessage();
            transportMessage.Body = Encoding.Default.GetBytes("This is a test");

            var received = _context.SendAndReceiveMessage(transportMessage);

            Assert.AreEqual("This is a test", Encoding.Default.GetString( received.Body, 0, received.Body.Length));

        }
        [Test]
        public void should_set_message_id()
        {
            var transportMessage = new TransportMessage();

            var received = _context.SendAndReceiveMessage(transportMessage);

            Assert.AreEqual(transportMessage.Id, received.Id);
        }

        [Test]
        public void Should_set_the_reply_to_address()
        {
            var address = Address.Parse("myAddress");

            var transportMessage = new TransportMessage();

            transportMessage.Headers[NServiceBus.Headers.ReplyToAddress] = address.ToSqsQueueName();
          
            var received = _context.SendAndReceiveMessage(transportMessage);

            Assert.AreEqual(transportMessage.ReplyToAddress, received.ReplyToAddress);
        }

        [Test]
        public void Should_transmit_all_transportMessage_headers()
        {
            var transportMessage = new TransportMessage();

            transportMessage.Headers["h1"] = "v1";
            transportMessage.Headers["h2"] = "v2";

            var received = _context.SendAndReceiveMessage(transportMessage);

            Assert.AreEqual(transportMessage.Headers["h1"], received.Headers["h1"]);
            Assert.AreEqual(transportMessage.Headers["h2"], received.Headers["h2"]);
        }


        [Test]
        public void Should_set_the_time_to_be_received()
        {
            var timeToBeReceived = TimeSpan.FromDays(1);

            var transportMessage = new TransportMessage();
            transportMessage.TimeToBeReceived = timeToBeReceived;

            var received = _context.SendAndReceiveMessage(transportMessage);

            Assert.AreEqual(received.TimeToBeReceived, transportMessage.TimeToBeReceived);
        }

        [Test]
        public void malformed_message_is_handled_gracefully()
        {
            Assert.Throws<JsonReaderException>(() =>
                _context.SendRawAndReceiveMessage("this is not valid json and so cant be deserialized by the receiver.")
            );
        }
	}
}
