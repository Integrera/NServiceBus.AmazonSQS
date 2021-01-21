namespace NServiceBus.Transport.SQS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static class Batcher
    {
        public static IReadOnlyList<BatchEntry<TMessage>> Batch<TMessage>(IEnumerable<TMessage> preparedMessages)
            where TMessage : SqsPreparedMessage
        {
            var allBatches = new List<BatchEntry<TMessage>>();
            var currentDestinationBatches = new Dictionary<string, TMessage>();

            var groupByDestination = preparedMessages.GroupBy(m => m.QueueUrl, StringComparer.Ordinal);
            foreach (var group in groupByDestination)
            {
                TMessage firstMessage = null;
                var payloadSize = 0L;
                foreach (var message in group)
                {
                    firstMessage = firstMessage ?? message;

                    // Assumes the size was already calculated by the dispatcher
                    var size = message.Size;
                    payloadSize += size;

                    if (payloadSize > TransportConstraints.MaximumMessageSize)
                    {
                        allBatches.Add(message.ToBatchRequest(currentDestinationBatches));
                        currentDestinationBatches.Clear();
                        payloadSize = size;
                    }

                    // we don't have to recheck payload size here because the support layer checks that a request can always fit 256 KB size limit
                    // we can't take MessageId because batch request ID can only contain alphanumeric characters, hyphen and underscores, message id could be overloaded
                    currentDestinationBatches.Add(Guid.NewGuid().ToString(), message);

                    var currentCount = currentDestinationBatches.Count;
                    if (currentCount != 0 && currentCount % TransportConstraints.MaximumItemsInBatch == 0)
                    {
                        allBatches.Add(message.ToBatchRequest(currentDestinationBatches));
                        currentDestinationBatches.Clear();
                        payloadSize = 0;
                    }
                }

                if (currentDestinationBatches.Count > 0)
                {
                    allBatches.Add(firstMessage.ToBatchRequest(currentDestinationBatches));
                    currentDestinationBatches.Clear();
                }
            }

            return allBatches;
        }
    }
}