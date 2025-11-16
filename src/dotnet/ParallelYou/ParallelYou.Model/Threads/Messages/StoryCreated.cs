using ParallelYou.Model.Bus;

namespace ParallelYou.Model.Threads.Messages;

public sealed class StoryCreated(Story story) : Message<Story>(story) { }
