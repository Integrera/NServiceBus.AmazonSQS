﻿namespace NServiceBus.AcceptanceTests.Sending
{
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_compatibility_mode_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(session => session.Send(new Message())))
                .WithEndpoint<Receiver>()
                .Done(c => c.Received)
                .Run();

            // ReplyToAddress and TimeToBeReceived should not to be propagated to the transport message as properties
            Assert.That(context.MessageContent, Does.Not.ContainKey("ReplyToAddress"));
            Assert.That(context.MessageContent, Does.Not.ContainKey("TimeToBeReceived"));
        }

        public class Context : ScenarioContext
        {
            public JsonNode MessageContent { get; set; }
            public bool Received { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender() =>
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.ConfigureRouting().RouteToEndpoint(typeof(Message), typeof(Receiver));
#pragma warning disable CS0618 // Type or member is obsolete
                    builder.ConfigureSqsTransport().EnableV1CompatibilityMode = false;
#pragma warning restore CS0618 // Type or member is obsolete
                });

            public class Handler : IHandleMessages<Reply>
            {
                public Handler(Context testContext)
                    => this.testContext = testContext;

                public Task Handle(Reply message, IMessageHandlerContext context)
                {
                    testContext.Received = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver() => EndpointSetup<DefaultServer>();

            public class MyMessageHandler : IHandleMessages<Message>
            {
                public MyMessageHandler(Context testContext)
                    => this.testContext = testContext;

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.MessageContent = JsonNode.Parse(context.Extensions.Get<Amazon.SQS.Model.Message>().Body);
                    return context.Reply(new Reply());
                }

                readonly Context testContext;
            }

        }

        public class Message : ICommand
        {
        }

        public class Reply : IMessage
        {
        }
    }
}