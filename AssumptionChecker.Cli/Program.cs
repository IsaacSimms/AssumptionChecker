///// CLI tool to use, test, and debug the AssumptionChecker library and functionalities /////
// == To use the CLI in debugger, you need to make sure that the startup project is set to multiple projects with the Engine starting up first == // 

// == namespaces == //
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssumptionChecker.Contracts;

var baseUrl      = args.Length > 0 ? args[0] : "http://localhost:5046"; // get the base URL from command-line arguments or use default
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };   // create an HttpClient with the specified base URL

// == initialize JSON options == //
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,                                  // use camelCase for JSON property names
    Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }, // ensure enums are serialized in camelCase
    WriteIndented        = true                                                         // format JSON output with indentation for readability
};

// == headering of CLI output == //
Console.WriteLine("//=== AssumptionChecker CLI ===//");
Console.WriteLine($"Base URL: {baseUrl}");
Console.WriteLine("Type in a prompt to analyze for assumptions (or 'exit' to quit)");

// == main CLI loop == //
while (true)
{
    // prompt and read user input
    Console.Write("> ");
    var input = Console.ReadLine();

    // handle null
    if (string.IsNullOrWhiteSpace(input)) // validate that the input is not empty
    {
        Console.WriteLine("Please enter a valid prompt.");
        continue;
    }

    // handle exit command
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) // check if the user wants to exit the CLI
    {
        Console.WriteLine("Exiting AssumptionChecker CLI. Goodbye!");
        break;
    }

    var request = new AnalyzeRequest { Prompt = input }; // create object of AnalyzeRequest with the user input as prompt

    // attempt to develop and print assumptions
    try
    {
        var response = await client.PostAsJsonAsync("/analyze", request, jsonOptions); // send a POST request to the /analyze endpoint with the request object serialized as JSON
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(jsonOptions);

        if (result is null ) { Console.WriteLine("empty response"); continue; } // still print if no response is provided

        Console.WriteLine($"\nFound {result.Assumptions.Count} assumptions " +
                          $"({result.Metadata.ModelUsed}, {result.Metadata.LatencyMs}ms)\n");

        // == print each assumption
        foreach (var assumption in result.Assumptions)
        {
            // color code based on risk level
            var riskColor = assumption.RiskLevel switch
            {
                RiskLevel.High   => ConsoleColor.Red,    // high risk make red
                RiskLevel.Medium => ConsoleColor.Yellow, // medium risk make yellow 
                _ => ConsoleColor.Blue                   // everything else is blue
            };
            // print the assumption
            Console.ForegroundColor = riskColor;
            Console.Write($"     [{assumption.RiskLevel.ToString().ToUpper()}] ");
            Console.ResetColor();
            Console.WriteLine(assumption.AssumptionText);
            Console.WriteLine($"     Category:  {assumption.Category}");
            
            // Only print clarifying question if it exists
            if (!string.IsNullOrWhiteSpace(assumption.ClarifyingQuestion))
            {
                Console.WriteLine($"     Ask:       {assumption.ClarifyingQuestion}");
            }
            
            Console.WriteLine($"     Rationale: {assumption.Rationale}");
            Console.WriteLine();
        }

        // compile clarifying questions for medium risk assumptions
        var questions = string.Join("\n", result.Assumptions
            .Where(assumption => assumption.RiskLevel == RiskLevel.Medium && !string.IsNullOrWhiteSpace(assumption.ClarifyingQuestion))
            .Select(assumption => $"- {assumption.ClarifyingQuestion}"));

        // print suggested clarifying questions for medium risk assumptions
        if (!string.IsNullOrEmpty(questions))
        {
            Console.WriteLine("// == Suggested Clarifying Questions == //");
            Console.WriteLine(questions);
            Console.WriteLine();
        }

        // handle suggested prompts
        if (result.SuggestedPrompts.Any())
        {
            Console.WriteLine("// == Suggested Improved Prompts == //");
            Console.ForegroundColor = ConsoleColor.Green;
            for (int i = 0; i < result.SuggestedPrompts.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {result.SuggestedPrompts[i]}");
            }
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    // handle HTTP request errors
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Request error: {ex.Message}");
        Console.ResetColor();
    }
}