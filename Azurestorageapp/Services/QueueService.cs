using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Azurestorageapp.Services
{
    /// <summary>
    /// Handles Azure Queue Storage for order processing and inventory management messages.
    /// Queue name: "order-processing"
    /// </summary>
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]!;
            var queueName = configuration["AzureStorage:QueueName"]!;

            _queueClient = new QueueClient(connectionString, queueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

            _queueClient.CreateIfNotExists();
        }

        /// <summary>Sends a message to the Azure Queue.</summary>
        public async Task SendMessageAsync(string message)
            => await _queueClient.SendMessageAsync(message);

        // ── Convenience helpers for order/inventory messages ──────────────

        public Task SendOrderProcessingMessageAsync(string orderId, string customerName, string productName, int qty)
            => SendMessageAsync($"[ORDER] OrderId: {orderId} | Customer: {customerName} | Product: {productName} | Qty: {qty} | Status: Processing");

        public Task SendInventoryUpdateMessageAsync(string productName, int newQty)
            => SendMessageAsync($"[INVENTORY] Product: {productName} | NewQty: {newQty} | Status: Updated");

        public Task SendImageUploadMessageAsync(string imageName)
            => SendMessageAsync($"[UPLOAD] Image uploaded: {imageName}");

        // ── Queue read operations ─────────────────────────────────────────

        /// <summary>Peeks at up to 32 messages without removing them.</summary>
        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 32)
        {
            var messages = new List<string>();
            PeekedMessage[] peeked = await _queueClient.PeekMessagesAsync(maxMessages: maxMessages);
            foreach (var msg in peeked)
                messages.Add(msg.MessageText);
            return messages;
        }

        /// <summary>Dequeues (receives and deletes) a single message.</summary>
        public async Task<string?> DequeueMessageAsync()
        {
            QueueMessage[] msgs = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
            if (msgs.Length == 0) return null;
            await _queueClient.DeleteMessageAsync(msgs[0].MessageId, msgs[0].PopReceipt);
            return msgs[0].MessageText;
        }

        /// <summary>Returns approximate number of messages in the queue.</summary>
        public async Task<int> GetMessageCountAsync()
        {
            var props = await _queueClient.GetPropertiesAsync();
            return props.Value.ApproximateMessagesCount;
        }
    }
}
