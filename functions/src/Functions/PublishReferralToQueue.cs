using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using src.Models;
using System.IO;
using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

public class PublishReferralToQueue
{
    [Function("PublishReferralToQueue")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var referral =
            JsonSerializer.Deserialize<ReferralMessage>(body);

        if (referral is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var connectionString =
            Environment.GetEnvironmentVariable("ServiceBusConnection");

        var serviceBusClient =
            new ServiceBusClient(connectionString);

        var sender =
            serviceBusClient.CreateSender("emailqueue");

        var messageBody =
            JsonSerializer.Serialize(referral);

        Console.WriteLine("Publishing referral to emailqueue");

        await sender.SendMessageAsync(
            new ServiceBusMessage(messageBody));

        await sender.DisposeAsync();
        await serviceBusClient.DisposeAsync();

        return req.CreateResponse(HttpStatusCode.OK);
    }
}