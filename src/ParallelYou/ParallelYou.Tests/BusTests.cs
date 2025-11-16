using ParallelYou.Bus.Abstractions;
using ParallelYou.Bus.Transports;
using ParallelYou.Model.Messages;

namespace ParallelYou.Tests;

public class BusTests
{
    private static (InMemoryTransport transport, IBroker broker) NewBus()
    {
        var transport = new InMemoryTransport();
        var broker = new InMemoryBroker(transport);
        return (transport, broker);
    }

    private sealed class TestMessage(string? type = null) : Message(id: null, type: type)
    {
    }

    [Fact]
    public async Task Publish_To_Exact_Route_Invokes_Handler()
    {
        var (transport, broker) = NewBus();

        int count = 0;
        broker.Route("alpha.beta", _ => { count++; return Task.CompletedTask; });

        await transport.Start();
        await broker.Publish(new TestMessage(type: "alpha.beta"));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Publish_With_Wildcards_Dispatches_Correctly()
    {
        var (transport, broker) = NewBus();
        int star = 0, hash = 0, exact = 0;

        broker.Route("alpha.*.gamma", _ => { star++; return Task.CompletedTask; });
        broker.Route("alpha.#", _ => { hash++; return Task.CompletedTask; });
        broker.Route("alpha.beta.gamma", _ => { exact++; return Task.CompletedTask; });

        await transport.Start();
        await broker.Publish(new TestMessage(type: "alpha.beta.gamma"));

        Assert.Equal(1, exact);
        Assert.Equal(1, star);
        Assert.Equal(1, hash);
    }

    [Fact]
    public async Task Multiple_Handlers_All_Invoked()
    {
        var (transport, broker) = NewBus();
        int a = 0, b = 0;
        broker.Route("x.y", _ => { a++; return Task.CompletedTask; });
        broker.Route("x.y", _ => { b++; return Task.CompletedTask; });

        await transport.Start();
        await broker.Publish(new TestMessage(type: "x.y"));

        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }

    [Fact]
    public async Task Publish_Before_Start_Throws()
    {
        var (transport, broker) = NewBus();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await broker.Publish(new TestMessage(type: "a.b"));
        });
    }
    
    [Fact]
    public async Task Publish_With_No_Matching_Route_Does_Not_Throw()
    {
        var (transport, broker) = NewBus();
        await transport.Start();
        var ex = await Record.ExceptionAsync(() =>
            broker.Publish(new TestMessage(type: "no.handlers.here")));
        Assert.Null(ex);
    }
    
    [Fact]
    public async Task Star_Wildcard_Does_Not_Match_Extra_Segments()
    {
        var (transport, broker) = NewBus();
        int count = 0;

        broker.Route("alpha.*", _ => { count++; return Task.CompletedTask; });

        await transport.Start();
        await broker.Publish(new TestMessage(type: "alpha.beta.gamma"));

        Assert.Equal(0, count);
    }
    
    [Fact]
    public async Task Hash_Wildcard_As_Single_Segment_Matches_All()
    {
        var (transport, broker) = NewBus();
        int count = 0;

        broker.Route("#", _ => { count++; return Task.CompletedTask; });

        await transport.Start();
        await broker.Publish(new TestMessage(type: "alpha.beta.gamma"));

        Assert.Equal(1, count);
    }
    
    [Fact]
    public async Task Route_After_Start_Still_Receives_Messages()
    {
        var (transport, broker) = NewBus();
        int count = 0;

        await transport.Start();
        broker.Route("alpha.beta", _ => { count++; return Task.CompletedTask; });

        await broker.Publish(new TestMessage(type: "alpha.beta"));

        Assert.Equal(1, count);
    }
}
