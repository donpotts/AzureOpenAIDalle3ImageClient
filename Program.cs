using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

// Check if the required arguments are provided
if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run <prompt> <outputFileName>");
    return;
}

var prompt = args[0];
var outputFileName = args[1];

// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Get OpenAI and OpenAIClient options from configuration
var openAIOptions = config.GetSection("OpenAI").Get<ImageGenerationOptions>() ?? throw new InvalidOperationException("OpenAI configuration section not found.");
var openAIClientOptions = config.GetSection("OpenAIClient").Get<OpenAIClientOptions>() ?? throw new InvalidOperationException("OpenAIClient configuration section not found.");

// Initialize cancellation token
var cancellationToken = new CancellationToken();

// Set OpenAI options
openAIOptions.Prompt = prompt;
openAIOptions.ImageCount = 1;
openAIOptions.Style = ImageGenerationStyle.Vivid;
openAIOptions.Size = ImageSize.Size1024x1024;
openAIOptions.Quality = ImageGenerationQuality.Hd;

// Initialize OpenAI client
var client = new OpenAIClient(new Uri(openAIClientOptions.Endpoint), new AzureKeyCredential(openAIClientOptions.ApiKey));

// Check if endpoint and API key are provided
if (string.IsNullOrEmpty(openAIClientOptions.Endpoint) || string.IsNullOrEmpty(openAIClientOptions.ApiKey))
{
    Console.WriteLine("Endpoint and/or ApiKey is missing.");
    return;
}

// Call OpenAI API to generate images
var response = await client.GetImageGenerationsAsync(openAIOptions, cancellationToken);

// Access the Value property to get the ImageGenerations object
var generations = response.Value.Data;

foreach (var generation in generations)
{
    // Download and save each generated image
    using (var httpClient = new HttpClient())
    {
        var imageBytes = await httpClient.GetByteArrayAsync(generation.Url);
        await File.WriteAllBytesAsync($"{outputFileName}.png", imageBytes);
    }

    Console.WriteLine($"Image saved as {outputFileName}.png");
}
