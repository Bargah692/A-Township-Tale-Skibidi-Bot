using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Channels;
using Townsharp.Infrastructure.Configuration;
using Townsharp.Infrastructure.Consoles;
using Townsharp.Infrastructure.Subscriptions;
using Townsharp.Infrastructure.WebApi;
using Newtonsoft.Json; 
using System;
using System.Collections.Generic;
using Skibidi_Bot.Command_Classes;


var recognizer = new SpeechRecognitionEngine();
var synthesizer = new SpeechSynthesizer();

// Define the choices for the grammar
Choices commands = new Choices();
commands.Add(new string[] {
    "skibidi speed",
    "skibidi feed",
    "skibidi list toilet heads"
});


// Setting up Townsharp and connecting to the server
var botCreds = BotCredential.FromEnvironmentVariables();
var webApiClient = new WebApiBotClient(botCreds); // Api Client
var consoleClientFactory = new ConsoleClientFactory(); // Used to connect to consoles
var subscriptionMultiplexerFactory = new SubscriptionMultiplexerFactory(botCreds); // Used to create a subscription multiplexer, this mechanism may change in the future.
var subscriptionMultiplexer = subscriptionMultiplexerFactory.Create(2); // how many concurrent connections do you need?  Rule of thumb is 200 servers per connection
var accessRequestResult = await webApiClient.RequestConsoleAccessAsync(430116864); 

if (!accessRequestResult.IsSuccess)
{
    throw new InvalidOperationException("Unable to connect to the server.  It is either offline or access was denied.");
}
else
{
    Console.Beep();
}

var accessToken = accessRequestResult.Content.token!;
var endpointUri = accessRequestResult.Content.BuildConsoleUri();
Channel<ConsoleEvent> eventChannel = Channel.CreateUnbounded<ConsoleEvent>(); 
var consoleClient = consoleClientFactory.CreateClient(endpointUri, accessToken, eventChannel.Writer);
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(); // used to end the session.
await consoleClient.ConnectAsync(cancellationTokenSource.Token); // Connect the client to the console endpoint.




string userName = "'Elder Toad'"; // Username for commands



// Setting up and initizalizing Speech Recognition
GrammarBuilder grammarBuilder = new GrammarBuilder();
grammarBuilder.Append(commands);
Grammar grammar = new Grammar(grammarBuilder);
recognizer.LoadGrammar(grammar);
recognizer.SpeechRecognized += async (sender, e) =>
{
    if (e.Result.Confidence >= 0.94)
    {
        switch (e.Result.Text)
        {


            case "skibidi speed":
                string speedResponse = "speed given.";
                Console.WriteLine(speedResponse);
                synthesizer.Speak(speedResponse);

                await consoleClient.RunCommandAsync($"player setstat {userName} speed 3");
                await consoleClient.RunCommandAsync($"player modify-stat {userName} luminosity -9 3");
                await consoleClient.RunCommandAsync($"player message {userName} 'Speed given' 2");
                break;

            case "skibidi feed":
                string feedResponse = "feeding";
                Console.WriteLine(feedResponse);
                synthesizer.Speak(feedResponse);

                await consoleClient.RunCommandAsync($"player setstat {userName} hunger 3");
                await consoleClient.RunCommandAsync($"player modify-stat {userName}' lumonosity -9 3");
                await consoleClient.RunCommandAsync($"player message {userName} 'You're skibidi tummy has been skibidi filled' 2");
                break;

                

            case "skibidi list toilet heads":
                string listResponse = "Listing the toilet heads";
                Console.WriteLine(listResponse);
                synthesizer.Speak(listResponse);

                var rawResponsePlayerList = await consoleClient.RunCommandAsync("player list");

                var playerJSON = rawResponsePlayerList.Result;

                List<PlayerList> users = JsonConvert.DeserializeObject<List<PlayerList>>(playerJSON);

               
                Dictionary<int, string> userDict = new Dictionary<int, string>();
                int counter = 1;

                foreach (var user in users)
                {
                    userDict[counter] = user.Username;
                    counter++;
                    if (counter > 12)
                    {
                        counter = 1; 
                    }
                }

                string formatted = "";
                
                foreach (var kvp in userDict)
                {
                    formatted += $"{kvp.Key}: {kvp.Value}\n";
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
                await consoleClient.RunCommandAsync($"player message * '{formatted}'");
                break;

                
        }
    }
};


recognizer.SetInputToDefaultAudioDevice();


recognizer.RecognizeAsync(RecognizeMode.Multiple);

Console.WriteLine("Bot is running. Press any key to exit...");
Console.ReadKey();