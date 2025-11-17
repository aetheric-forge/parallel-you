
using ParallelYou.Repo.Abstractions;
using ParallelYou.Repo.Backends;
using ParallelYou.Bus.Abstractions;
using ParallelYou.Bus.Transports;
using Spectre.Console;

namespace ParallelYou.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create fundamental services
        IRepo repo = CreateRepo();
        var (broker, transport) = CreateBus();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        PrintStartupSummary(repo, transport);

        try
        {
            await transport.Start();

            var tui = new ParallelYouTui(repo, broker);
            await tui.RunAsync(cts.Token);

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
        finally
        {
            try { await transport.Stop(); } catch { /* best effort */ }
        }
    }

    // -----------------------------------------------------------------------
    // CreateRepo: Mongo if MONGO_URI exists, else InMemory
    // -----------------------------------------------------------------------
    private static IRepo CreateRepo()
    {
        var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");

        if (!string.IsNullOrWhiteSpace(mongoUri))
        {
            try
            {
                // Create MongoRepo via reflection to avoid direct compile dependency in environments without Mongo backend
                var type = Type.GetType("ParallelYou.Repo.Backends.MongoRepo, ParallelYou.Repo");
                if (type != null)
                {
                    var ctor = type.GetConstructor(new[] { typeof(string), typeof(string), typeof(string) });
                    if (ctor != null)
                    {
                        return (IRepo)ctor.Invoke(new object[] { mongoUri, "parallel_you", "threads" });
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                   $"[red]MongoRepo initialization failed:[/] {Markup.Escape(ex.Message)}");
                AnsiConsole.MarkupLine("[yellow]Falling back to InMemoryRepo[/]");
            }
        }

        return new InMemoryRepo();
    }

    // -----------------------------------------------------------------------
    // CreateBus: RabbitMQ if RABBITMQ_URL exists, else InMemory
    // -----------------------------------------------------------------------
    private static (IBroker broker, ITransport transport) CreateBus()
    {
        var rabbitUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");

        if (!string.IsNullOrWhiteSpace(rabbitUrl))
        {
            try
            {
                var transport = new RabbitMqTransport(rabbitUrl);
                var broker = new InMemoryBroker(transport);

                return (broker, transport);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]RabbitMQ transport init failed:[/] {Markup.Escape(ex.Message)}");
                AnsiConsole.MarkupLine("[yellow]Falling back to InMemory transport[/]");
            }
        }

        // Default: Everything in-memory
        var memTransport = new InMemoryTransport();
        IBroker memBroker = new InMemoryBroker(memTransport);

        return (memBroker, memTransport);
    }

    // -----------------------------------------------------------------------
    // Human-friendly startup output
    // -----------------------------------------------------------------------
    private static void PrintStartupSummary(IRepo repo, ITransport transport)
    {
        var repoType = repo.GetType().Name;
        var transportType = transport.GetType().Name;

        AnsiConsole.Write(new Panel(
            $"[green]Parallel You v1.0.0[/]\n" +
            $"Repo: [yellow]{repoType}[/]\n" +
            $"Transport: [yellow]{transportType}[/]"
        )
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1)
        });
    }
}
