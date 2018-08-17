#load "Message.csx"

using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Queue; 
using Newtonsoft.Json;

// For more information about this template visit http://aka.ms/azurebots-csharp-proactive
[Serializable]
public class BasicProactiveEchoDialog : IDialog<object>
{
    protected int count = 1;
    protected bool askedAboutPancackes;

    public Task StartAsync(IDialogContext context)
    {
        context.Wait(MessageReceivedAsync);
        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        var message = await argument;
        switch (message.Text)
        {
            case "pancakes":
                if (this.askedAboutPancackes)
                {
                    // Create new queue Message
                    var queueNewMessage = new Message
                    {
                        RelatesTo = context.Activity.ToConversationReference(),
                        Text = message.Text,
                        IsTrustedServiceUrl = MicrosoftAppCredentials.IsTrustedServiceUrl(message.ServiceUrl)
                    };

                    // write the queue Message to the queue
                    await AddMessageToQueueAsync(JsonConvert.SerializeObject(queueNewMessage));

                    await context.PostAsync($"{this.count++}: I remeber..");
                    context.Wait(MessageReceivedAsync);
                }
                else
                {
                    PromptDialog.Confirm(
                    context,
                    AfterPancakesAsync,
                    "Do you like pancakes?",
                    "O'Really?!",
                    promptStyle: PromptStyle.Auto);
                }
                break;
            case "reset":
                PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "Are you sure you want to reset the count?",
                "Didn't get that!",
                promptStyle: PromptStyle.Auto);
                break;
            default:
                // Create a queue Message
                var queueMessage = new Message
                {
                    RelatesTo = context.Activity.ToConversationReference(),
                    Text = message.Text,
                    IsTrustedServiceUrl = MicrosoftAppCredentials.IsTrustedServiceUrl(message.ServiceUrl)
                };

                // write the queue Message to the queue
                await AddMessageToQueueAsync(JsonConvert.SerializeObject(queueMessage));

                await context.PostAsync($"{this.count++}: You said {queueMessage.Text}. Your message has been added to a queue, and it will be sent back to you via a Function shortly.");
                context.Wait(MessageReceivedAsync);
                break;
        }
    }

    public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
    {
        var confirm = await argument;
        if (confirm)
        {
            this.count = 1;
            await context.PostAsync("Reset count.");
        }
        else
        {
            await context.PostAsync("Did not reset count.");
        }
        context.Wait(MessageReceivedAsync);
    }

    public async Task AfterPancakesAsync(IDialogContext context, IAwaitable<bool> argument)
    {
        this.askedAboutPancackes = true;
        var confirm = await argument;
        if (confirm)
        {
            this.count = 1;
            await context.PostAsync("Me too.");
        }
        else
        {
            await context.PostAsync("Diabeties.");
        }
        context.Wait(MessageReceivedAsync);
    }
    
    public static async Task AddMessageToQueueAsync(string message)
    {
        // Retrieve storage account from connection string.
        var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));

        // Create the queue client.
        var queueClient = storageAccount.CreateCloudQueueClient();

        // Retrieve a reference to a queue.
        var queue = queueClient.GetQueueReference("bot-queue");

        // Create the queue if it doesn't already exist.
        await queue.CreateIfNotExistsAsync();
        
        // Create a message and add it to the queue.
        var queuemessage = new CloudQueueMessage(message);
        await queue.AddMessageAsync(queuemessage);
    }
}

