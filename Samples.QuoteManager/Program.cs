using System;
using System.IO;
using System.Text.Json;
using Samples.QuoteManager.Recommended;

namespace Samples.QuoteManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                PrintUsage();

                return;
            }

            var quoteManager = args[0] switch
            {
                "simple" => new SimpleQuoteManager(),
                "recommended" => BuildRecommendedQuoteManager<QuoteExtended>(),
                _ => (IQuoteManager)NullQuoteManager.Instance
            };

            if (ReferenceEquals(quoteManager, NullQuoteManager.Instance))
            {
                Console.WriteLine($"Unrecognized manager type string [{args[0]}] specified, using NullQuoteManager implementation. For real execution, specify a valid QuoteManager type string, see usage for details (as follows):");
                PrintUsage();
            }

            Console.WriteLine("Press CTRL+C to exit anytime....");

            do
            {
                Console.WriteLine("Enter a valid command and hit enter to process:");

                var currentRequest = Console.ReadLine();

                if (string.IsNullOrEmpty(currentRequest))
                {
                    continue;
                }

                var commandIndex = currentRequest.IndexOf(' ');

                if (commandIndex < 0)
                {
                    continue;
                }

                var currentCommand = currentRequest.Substring(0, commandIndex).Trim();

                switch (currentCommand)
                {
                    case "usage":
                        PrintUsage();

                        break;

                    case "getq":
                        var getSymbol = currentRequest.Substring(commandIndex).Trim();

                        if (string.IsNullOrEmpty(getSymbol))
                        {
                            break;
                        }

                        var bestQuote = quoteManager.GetBestQuoteWithAvailableVolume(getSymbol);

                        Console.WriteLine($"  Best available quote: [{JsonSerializer.Serialize(bestQuote)}]");

                        break;

                    case "file":
                        var file = currentRequest.Substring(commandIndex).Trim();

                        if (string.IsNullOrEmpty(file))
                        {
                            break;
                        }

                        if (!File.Exists(file))
                        {
                            Console.WriteLine("File does not exist");
                            break;
                        }

                        var quoteLines = File.ReadAllLines(file);

                        Console.WriteLine($"Got [{quoteLines.Length}] quote lines from file");

                        foreach (var quoteLine in quoteLines)
                        {
                            var fileQuote = GetQuote(quoteLine, 0);

                            quoteManager.AddOrUpdateQuote(fileQuote);
                        }

                        Console.WriteLine($"Added [{quoteLines.Length}] quotes from file");

                        break;

                    case "dump":
                        if (!(quoteManager is IQuoteManagerExtended<QuoteExtended> quoteManagerExtended))
                        {
                            Console.WriteLine("Can only dump extended implementations");

                            break;
                        }

                        Console.WriteLine("ALL quotes dump:");

                        foreach (var quoteDump in quoteManagerExtended.StorageService.GetAllQuotes())
                        {
                            Console.WriteLine($"  Quote: [{JsonSerializer.Serialize(quoteDump)}]");
                        }

                        break;

                    case "rmq":
                        var quoteId = currentRequest.Substring(commandIndex).Trim();

                        if (string.IsNullOrEmpty(quoteId))
                        {
                            Console.WriteLine("Invalid quote id, please try again");

                            break;
                        }

                        quoteManager.RemoveQuote(new Guid(quoteId));

                        Console.WriteLine("  Quote removed");

                        break;

                    case "rms":
                        var removeSymbol = currentRequest.Substring(commandIndex).Trim();

                        if (string.IsNullOrEmpty(removeSymbol))
                        {
                            Console.WriteLine("Invalid symbol, please try again");

                            break;
                        }

                        quoteManager.RemoveAllQuotes(removeSymbol);

                        Console.WriteLine("  All quotes removed");

                        break;

                    case "quote":
                        var quote = GetQuote(currentRequest, commandIndex);

                        quoteManager.AddOrUpdateQuote(quote);

                        Console.WriteLine("  Quote added/updated");

                        break;

                    case "trade":
                        var (symbol, volume) = GetTradeRequest(currentRequest, commandIndex);

                        var tradeResult = quoteManager.ExecuteTrade(symbol, volume);

                        Console.WriteLine($"  Trade request for [{tradeResult.VolumeRequested}] shares of [{tradeResult.Symbol}] resulted in executed [{tradeResult.VolumeExecuted}] at [{Math.Round(tradeResult.VolumeWeightedAveragePrice, 5)}] average price");

                        break;
                }
            } while (true);
        }

        private static IQuoteManagerExtended<T> BuildRecommendedQuoteManager<T>()
            where T : class, IQuoteExtended
        {
            // Builds a RecommendedQuoteManager quoteManager object that is observed by one or more observers (logging, cleanup, archival, etc.)
            var indexService = new InMemoryQuoteIndexService<T>();
            var storageService = new CompositeQuoteStorageService<T>(new InMemorySortedQuoteStorageService<T>(), indexService);

            var (observer, quoteManager) = ObservedQuoteManager<T>.Create(o => new RecommendedQuoteManager<T>(o, storageService,
                                                                                                              new ReaderWriterLockManager(),
                                                                                                              new CourseLockedTradeExecuter<T>()));

            observer.Subscribe(new ConsoleLoggingQuoteObserver<T>());
            observer.Subscribe(new SimpleBackgroundIndexStorageCleanupQuoteObserver<T>(storageService, indexService));

            return quoteManager;
        }

        private static QuoteExtended GetQuote(string command, int startIndex)
        {
            var values = command.Substring(startIndex).Trim().Split('|');

            if (values.Length < 5)
            {
                Console.WriteLine("Invalid quote line, please try again");

                return null;
            }

            try
            {
                var timestamp = long.Parse(values[4]);

                var quote = new QuoteExtended
                            {
                                Id = string.IsNullOrEmpty(values[0])
                                         ? Guid.NewGuid()
                                         : new Guid(values[0]),
                                Symbol = values[1],
                                Price = double.Parse(values[2]),
                                AvailableVolume = uint.Parse(values[3]),
                                ExpirationDate = DateTime.UnixEpoch.AddSeconds(timestamp),
                                ExpirationTimestamp = timestamp
                            };

                return quote;
            }
            catch(Exception x)
            {
                Console.WriteLine($"Could not parse quote line, exception [{x.Message}], please try again");

                return null;
            }
        }

        private static (string Symbol, uint Volume) GetTradeRequest(string command, int startIndex)
        {
            var values = command.Substring(startIndex).Trim().Split('|');

            if (values.Length < 2)
            {
                Console.WriteLine("Invalid trade line, please try again");

                return (null, 0);
            }

            try
            {
                var symbol = values[0];
                var volume = uint.Parse(values[1]);

                return (symbol, volume);
            }
            catch(Exception x)
            {
                Console.WriteLine($"Could not parse quote line, exception [{x.Message}], please try again");

                return (null, 0);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("USAGE:");
            Console.WriteLine("dotnet qm.dll <simple|recommended>");
            Console.WriteLine("");
            Console.WriteLine("PARAMS:");

            Console.WriteLine(@"- simple|recommended
Required.
The specification of the IQuoteManager implementation to use for this execution.
simple = A simple, naive, functional quote manager with minimal guards, thread safefty concerns, etc.
recommended = A reasonable (in my opinion obviously) approach to solving the problem in the given time with no external dependencies
 
");

            Console.WriteLine("Valid commands include:");
            Console.WriteLine("");

            Console.WriteLine(@"** Add quotes to the system by typing quote lines:
    quote <id>|<symbol>|<price>|<available volume>|<expiration date as unixtimestamp>

Leaving the ID out will simply generate a quote with a new ID.

For example:
    quote 15c064c7-c585-4ee8-aba4-87461597affd|AMZN|1.23|4321|1591750923
    quote |AMZN|1.23|4321|1591750923

** Submit a buy trade by typing trade lines:
    trade <symbol>|<volume requested>

For example:
    trade AMZN|12

** Get a quote:
    getq <symbol>

** Remove a quote with:
    rmq <quote id>

** Remove all quotes for a symbol with:
    rms <symbol>
 
** DUMP all quotes (if using recommended manager):
    dump <space>

** Import quotes from a file in the above quote format without the leading 'quote' command:
    file <file location>

** Print this usage information:
    usage <space>

");
        }
    }
}
