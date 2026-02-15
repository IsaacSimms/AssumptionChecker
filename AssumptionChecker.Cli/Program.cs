///// CLI tool to use, test, and debug the AssumptionChecker library and functionalities /////
// == To use the CLI in debugger, you need to make sure that the startup project is set to multiple projects with the Engine starting up first == // 

// == namespaces == //
using System;
using Microsoft.Extensions.DependencyInjection;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5046"; // get the base URL from command-line arguments or use default

// == set up dependency injection using the shared Core registration == //
var services = new ServiceCollection();
services.AddAssumptionChecker(baseUrl);
using var provider = services.BuildServiceProvider();

var service = provider.GetRequiredService<IAssumptionCheckerService>();

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

    // attempt to develop and print assumptions
    try
    {
        var result = await service.AnalyzeAsync(input, maxAssumptions: 10);

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