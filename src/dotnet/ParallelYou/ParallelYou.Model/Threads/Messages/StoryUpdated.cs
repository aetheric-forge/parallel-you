using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;

public sealed class StoryUpdated(Story story): Message<Story>(story) { }
