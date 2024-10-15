using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid.SystemEvents;
using MyNet8IsolatedFunction.Model;
using Azure;

namespace MyNet8IsolatedFunction
{
    public class Function
    {
        private readonly ILogger<Function> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public Function(ILogger<Function> logger, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        [Function(nameof(Function))]
        public async Task Run(
            [ServiceBusTrigger("blobcreated", Connection = "SBConnectionString")]
            ServiceBusReceivedMessage message,
            //EventGridEvent message,
            ServiceBusMessageActions messageActions)
        {
            var bodyString = message.Body?.ToString() ?? string.Empty;
            var eventGridEvent = System.Text.Json.JsonSerializer.Deserialize<EventGridEvent>(bodyString);

            if (eventGridEvent?.EventType == "Microsoft.Storage.BlobCreated")
            {
                var blobCreatedData = System.Text.Json.JsonSerializer.Deserialize<BlobCreatedEventData>(eventGridEvent.Data.ToString());
                var blobUrl = blobCreatedData!.Url;

                _logger.LogInformation($"---------------Blob is created URL: {blobUrl}");
                Uri uri = new Uri(blobUrl);
                string[] segments = uri.Segments;
                string containerName = segments[1].Trim('/');
                string blobName = string.Join("", segments[2..]);

                BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(containerName)
                                        .GetBlobClient(blobName);

                var tags = await blobClient.GetTagsAsync();
                foreach (var tag in tags.Value.Tags)
                {
                    _logger.LogInformation($"---------------Blob tags: {tag.Key} : {tag.Value}--------{blobName}");
                }
                if (tags.Value.Tags.ContainsKey("Malware Scanning scan result"))
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    // Use Task.Delay for asynchronous waiting
                    await Task.Delay(2000);
                    _logger.LogInformation($"---------------Sleep 2000 ms, Abandon message----------{blobName}");
                    await messageActions.AbandonMessageAsync(message);
                }
            }
            else
            {
                _logger.LogWarning($"---------------Unexpected event type: {eventGridEvent?.EventType}");
            }

            // Complete the message

        }
    }
}
