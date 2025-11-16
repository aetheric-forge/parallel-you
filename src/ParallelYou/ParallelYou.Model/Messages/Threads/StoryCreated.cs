using ParallelYou.Model.Threads;

namespace ParallelYou.Model.Messages.Threads;

public sealed class StoryCreated(Story story) : Message<Story>(story) { }
