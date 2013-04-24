# Hydrospanner

By [SmartyStreets, LLC](http://smartystreets.com)


> "Horizontal boosters. Alluvial dampers? Ow! That's not it, bring me the 
> Hydrospanner. I don't know how we're going to get out of this one." [_citation needed_]


## Introduction and Design Philosophy

This project is the result of our experiences designing systems over the course of several years. At their core, many of our applications have been implemented using the following paradigms:

- [DDD](http://en.wikipedia.org/wiki/Domain-driven_design) (Domain-Driven Design), and
- [event sourcing](http://martinfowler.com/eaaDev/EventSourcing.html)
- [CQRS](http://martinfowler.com/bliki/CQRS.html) (Command Query Responsibility Segregation)

As these concepts became more and more prevelant we began to engineer infrastructure code to support our efforts. As our understanding has grown (along with our infrastructure code base and it's complexity) we decided to encapsulate our learning into a single tool that facilitated a unified, focused approach. Here's a brief overview of how we use these concepts to compose a functioning system with a web interface.


1. User interacts with UI which ends up in a request that ends up at a web controller. 
2. Basic validation accepts or rejects the request, returning semantic HTTP status codes to the client.
3. Valid requests are then published as a well-formed _command_ to a message queue where another process can receive and process the command.
4. Commands are handled by one or more _aggregates_, which encapsulate important business logic.
5. Commands that are not rejected by an aggregate result in one or more well-formed _events_ being published.
6. Both commands and events are persisted such that a complete history of actions is stored durably.
7. As a convenience the events can be used to maintain any number of eventually-consistent _projections_, which are just denormalized data structures that the website can use for queries.
8. Events that are known to be public to other systems are made available to those systems via the messaging system. Event and their schemas represent the only coupling between disparate systems.
9. Events can also be processed within the system by _sagas_, which coordinate long-running workflows with other worker processes within the system. These sagas are programmed to publish commands back to the aggregates (which may generate more events) when certain states are reached.


#### Problems related to the above that we've tried to solve with the Hydrospanner:


- Facilitate event-sourced messaging-based architecture
- Minimal boilerplate in application code (business logic is the focus)
- Persistence is handled automatically and all operations are batched
- Snapshots handled regulary and automatically (including loading from latest snapshot at startup)
- fast rebuild of read-model from journaled events (**replay**)
- Allow for scheduled messages ("send me this message in X minutes/hours/days")



#### Here are some conventions that we try to observe:

- Commands are generally private to a single system
- Events may be private to a single system or public to any system that subscribes.
- The state of aggregate, sagas, and projections can be rebuilt by _replaying_ all stored events in order. This is one of the primary benefits of event sourcing.
- In general, aggregates receive commands and publish events
- In general, sagas receive events and publish commands
- In general, projections only receive events, from which their state is derived.
- For aggregates, the acts of publishing the event and tranforming their internal state are split into separate functions/methods
- For sagas, the acts of publishing commands and transitioning to another 'state' in the workflow are split into separate functions/methods



## How does it work?

The Hydrospanner hosts your application code, which provides important API 'hooks' (described later). The hydrospanner takes care of the following infrastructure-level concerns:

- receiving external messages from a message queue (RabbitMQ is the supported option right now)
- persisting that message to durable storage (MySQL is the supported option right now)
- passing the message to your application where your important, tested business/domain logic lives (aggregates, sagas)
- persisting snapshots of your application's internal state as a public read-model (**projections**) and as a system-wide binary snapshot for fast loading during process restarts (really nice when you've processed millions of messages...).
- publishing resulting messages to an outbound message queue.

So, the good news about the Hydrospanner is that all you have to worry about is getting your business logic and your messaging workflows right.  No more worrying about persistence of messages or projections, sending/receiving messages, restoring from snapshots, etc...

## Standing on the shoulders of giants

At the foundation of the Hydrospanner is the [LMAX Disruptor](http://lmax-exchange.github.io/disruptor/), a "High Performance Inter-Thread Messinging Library", which allows data to be passed between threads efficiently. Using this pattern allows various processing steps to be batched efficiently around a few intelligiently provisioned ring buffers.

The Hydrospanner also makes use of a few other ready-made tools (but could be extended to support others):

- [MySQL](http://www.mysql.com/) (message and projection persistence)
- [RabbitMQ](http://www.rabbitmq.com/) (messaging system)
- [log4net](http://logging.apache.org/log4net/) (logging)

Development of the Hydrospanner was facilitated by:

- [Jetbrains ReSharper](http://www.jetbrains.com/resharper/)
- [Machine.Specifications](https://github.com/machine/machine.specifications)
- [NSubstitute](http://nsubstitute.github.io/)


## How is it used? (Application Hooks via the `IHydratable` interface)


There are a few interfaces meant to be implemented by the hosted application by one or more types which, when implemented, usually wrap over aggregates, sagas, or projections:

1. `IHydratable`
2. `IHydratable<T>`
3. `ITimeoutHydratable`

### `IHydratable`:

`string Key { get; }`

This method provides a string-based location for this `IHydratable`. It should be unique and constant for a given instance.

`bool IsComplete { get; }`

Some types that implement `IHydratable` will have an end to their life-cycle, after which they should not handle any additional messages. This field serves to inform the Hydrospanner when this occurs.

`bool IsPublicSnapshot { get; }`

All instances of types which implement `IHydratable` will be persisted to system-wide, private snapshots to facilitate fast reloading of system state at startup. However, some objects provide a snapshot for a public read-model. If that is the case for your `IHydratable`, make sure this returns `true`;

`ICollection<object> PendingMessages`

This field provides messages generated by your application as a result of handling incoming messages (see `IHydratable<T>`). Messages gathered from this method will be routed back into the application for additional handling, journaled, and then published to the outbound message queue. This collection is cleared by the Hydrospanner after the items have been collected.

`object Memento { get; }`

Returns an object that represents the current internal state of the `IHydratable` to be serialized as a snapshot (whether system-wide or public). If this object implements `ICloneable` it's `.Clone()` method will be called just after retreival. This is essential…blah blah blah


### `IHydratable<T>`

`Hydrate(Delivery<T> delivery)`

Where `<T>` represents the type of the message to be handled. Depending on the purpose of your `IHydratable` this method serves to mutate internal (projection) state and/or generate messages (saga/aggregate) to be handled by other handlers.

`Delivery<T>` provides the following fields:

- `T Message { get; }`: the actual message
- `Dictionary<string, string> Headers { get; }`: message-level headers transported by the messaging infrastructure
- `long Sequence { get; }`: The incrementing id of the message as it will be persisted to durable storage
- `bool Live { get; }`: Indicates whether or not this message is being replayed from storage to rebuild application state (after a software deployment, `== false`) or whether this message is being handled for the first time (ie, `== true`)


### `ITimeoutHydratable`

`ICollection<DateTime> Timeouts { get; }`

This special-case interface allows your application to request a 'wake-up' call. Simply add a DateTime to this collection to receive a `TimeoutMessage` at that time. That `TimeoutMessage` message will be delivered to the following method:


`void Hydrate(Delivery<TimeoutMessage> delivery)`

This is where your application is 'woken up' for a particular reason pertinent to the application.


`TimeoutMessage` provides the following fields:

- `string Key { get; }`: Corresponds to `IHydratable.Key`.- `DateTime Instant { get; }`: The requested 'wake-up' time.- `DateTime UtcNow { get; }`: When the message is actually delivered (there can be some delay or anticipation depending on the load of the system).


## Sample Application

**TODO**


## Application Configuration


The following parameters can be supplied as `<appSettings>`:

- `hydrospanner-node-id`: numeric value not larger than a `short` that uniquely identifies this hydrospanner instance.
- `hydrospanner-broker-address`: URI for the RabbitMQ server (example: `"amqp://guest:guest@localhost:5672"`)
- `hydrospanner-source-queue`: The name of the queue to which inbound messages will arrive.
- `hydrospanner-system-snapshot-location`: Directory in which to store the system-wide snapshots (defaults to 50,000).
- `hydrospanner-system-snapshot-frequency`: Numeric value which determines how many messages will be processed in between system-wide snapshots.
- `hydrospanner-journal-batch-size`: Numeric value which governs the number of messages to be inserted into MySQL as a transactional batch (defaults to 4,096).
- `hydrospanner-journal`: The name of the connection string (found in the `connectionStrings` section of your app.config) to be used to journal messages.
- `hydrospanner-public-snapshots`: The name of the connection string (found in the `connectionStrings` section of your app.config) to be used for storing public snapshots (**projections**).
- `hydrospanner-duplicate-window`: The number of most recent message id's to load in order to filter duplicate message receipt (defaults to 1024 * 128)

Alternatively, you may provide an instance of a class that inherits from `ConventionWireupParameters` to an overload of the  `Wireup.Initialize(…)` method.