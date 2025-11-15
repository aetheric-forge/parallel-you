# Parallel You Architecture

## Overview

Parallel You is designed as a modular application with many reusable components. This application requires JSON document storage in
MongoDB, as well as distributed message transport via RabbitMQ.

While these are definitely fit-for-purpose application components, they are certainly not the only ones. The YAML backend that was used for
prototyping is now gone, but it could easily be added here. A Kafka one is possible, though maybe not practical. :)

The design was produced using some best practices, such as domain driven design. This will later lead to development of individual services
which can be distributed in Kubernetes. The overall application design would probably best be described as an event-driven architecture, however. A pub-sub API via the central message broker will provide the event streams and responses. The central message broker can be as simple as an in-memory implementation, though RabbitMQ is also provided. The storage API is quite simple, but easily extended both in terms of functionality and storage backends. In-memory and MongoDB have been provided.

The overall vision for this project has expanded in scope quite a bit, but the [v1.3 Roadmap](ROADMAP.md) is likely a stable version which will then have further interfaces developed, such as a VS Code plugin and web app.

## Application Components

### Storage Repository

The Storage Repository is exposed in the form of a protocol, shown below:

```python
class Repo(Protocol):
    def list(self, FilterSpec) -> list[Thread]: ...
    def upsert(self, Thread) -> str: ...
    def get(self, str) -> Thread | None: ...
    def delete(self, str) -> bool: ...
    def clear(self, str) -> None: ...
```

While this definition includes the `Thread` class that is specific to the Parallel You application, this interface can easily be extracted for use in other applications. A simple helper in `make_repo()` has been provided to determine backend and credentials from the user environment.  For MongoDB, the environment variable `MONGO_URI` should be set to the full `mongodb://` URL, including credentials; and the environment variable `PARALLEL_YOU_REPO` should be set to `mongo`. An in-memory implementation (`MemoryRepo`) has been provided, and is used by default, and the MongoDB implmentation is returned by `make_repo()` when the environment is set as described previously.

### Domain Model

The fundamental unit of this application is a `Thread`. This terminology has been chosen to align with the idea of multi-threading as an analogy for managing multiple tasks and projects. While it's been developed to also align with Agile software practices, it was also intended for simpler stand-alone tasks as well.

`Thread`, from the Agile perspective, is implemented as two subclasses: `Saga`, and `Story`. These are as you would expect, `Saga` being the parent of the hierarchy, and `Story` being the task-level specification. We will add `Task` as both a top-level and saga-level `Thread` type. Task management will be organized as `Sprints` as per Agile methodology, and features added to sprint management as the application develops.

#### Terminology

In keeping with the multi-threading analogy, Parallel You uses these terms:

* **Thread** - the basic unit of thought. Records timestamps, completed and in-progress tasks, _quantum_, and _energy band_. _Threads_ are divided into _Sagas_, the top-level of a long-running thought, and _Stories_, individual work packets.

* **Quantum** - development endurance requires avoiding overwork. _Threads_ are given _quantums_, a dedicated session length tuned to your realistic single-session output. There are many signs that a session has gone too long, usually in the form of high levels of stress, or grinding during troubleshooting. It is important to step away when these symptoms start becoming unmanageable. The _quantum_ is used to drive timers while you work. _Quantum_ should be tuned by ramping up slowly, and considering the _energy band_ of the task.

* **Energy Band** - A bit of a fuzzier "planning poker" categorization, but enough to separate difficult tasks from easy ones at a glance. A short change of pace and scenery can contribute to endurance. The terms `Deep`, `Moderate`, and `Light` are used, but easily customized.

* **Story State** - A reasonable set of states in which a story can exist. `Ready` stories are available to be started which will begin a timer and provide notification when the timer is complete. `Running` stories are currently in progress, and will notify when timer has expired. `Parked` stories are placeholders for later development. `Blocked` stories, annotated in the sprint management, represent dependencies and other roadblocks. Completed stories will be put in the `Done` state, and then they can be archived to drop them from the default view. Commands to view the archive are provided.

### Message Bus

The Message Bus is the internal communication fabric of Parallel You. All domain events, commands, and cross-component notifications flow through it. It decouples producers of events from consumers, allowing the UI, domain services, and storage layers to evolve independently.

The Message Bus has been implemented in two different ways in this release: In-Memory transport, and RabbitMQ transport. The default broker is the in-memory transport, where all application components run in the same process. A RabbitMQ transport has also been provided to allow for distribution and horizontal scaling of application components. In general, the In-Memory transport is appropriate for rapid development and testing without the need for local RabbitMQ maintenance, while the RabbitMQ transport is a production-grade transport appropriate for scaling the application and its component services.

Events and notifications are co-ordinated by topics. We use a standard topic naming scheme, a hierarchical namespace concept that is used in most programming languages today. For example, the topics `saga.created` and `app.request_refresh` would exist for components to publish to. In the example topics we have here, the application flow might go like:

1. UI submits `SagaCreated` event via message bus that is received by the parent application.
   ```python
   await broker.emit("saga.created", {...}) # entire JSON object representation of Saga object follows
   ```
2. Parent application, subscribed to `SagaCreated` event submits now-validated request to storage layer
   ```python
   def handle_saga_events(self, ev: MessageBase):
       if isinstance(ev, SagaCreated):
           # handle Saga creation
       elif isinstance(ev, SagaUpdated):
           # ...

   # ...
   await broker.subscribe("saga.*", handle_saga_events)
   ```
3. Upon confirmation of repo update, publish `RequestRefresh` event back to entire UI layer (e.g., multiple clients and interfaces)

Understanding this part of the architecture is critical to designing horizontally scalable (and hence fault-tolerant) services, and applications that can be built in a modular fashion. Let's look at a specific example from the Parallel You application to see the message flow and application response:

As an example, the `InlineInput` widget sends a `SagaCreated` event once the user input has been validated and the repository confirms the insert. The message flow follows this pattern:

1. `InlineInput` widget receives input submission
2. Widget runs validator method if provided, and proceeds if validation is successful. Otherwise, indicate error to user.
3. Widget constructs the object to be updated using the model factory and the provided input
4. Widget receives confirmation of successful repo update, or sends error back to user
5. Widget constructs object of message class type using the model object parameter in the message class initializer, and then posts it to the message bus

Any part of the application can subscribe to `saga.created` and update itself in real time. With RabbitMQ, the same pattern works across processes, machines, and interfaces.
