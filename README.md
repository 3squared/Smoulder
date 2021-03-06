![Forge Smoulder](ForgeSmoulder.png "Forge Smoulder")

# Smoulder

## Introduction
Smoulder meets the need for low-profile, slow burn processing of data in a non-time critical environment. Intended uses include aggregating statistics over a constant data stream and creating arbitrarily complex reports on large data volumes with the ability to take snapshots of the current resultant states without waiting for completion or interrupting the data processing. This is achieved by separating data preparation, processing and aggregation of results into separate loosely-coupled processes, linked together by defined data packets through internally accessible message queues.

The entire system can be implemented by creating new concrete implementations of the base abstract interfaces, allowing for real flexibility in the applications the system can be used for. Each of the three parts of the system is built to be run in a separate thread, allowing performance to scale up or down depending on the hardware the system is hosted on and decoupling each process from the other. They will communicate with each other over two concurrent queues, a thread-safe feature of C#.

Smoulder is part of the [Forge Framework](#Forge-Framework).
## System Description
### Loader
This component is responsible for retrieving data and converting it into usable data packets. Data can then be bundled into a data packet containing multiple data points, or simply applied as a stream of single data points using a customisable data object. This stream of sanitised data is then made available to the Processor by means of an inter-thread message queue called the ProcessorQueue.
### Processor
Responsible for computing the results from the provided data packet, retains necessary information about previous data packets if required. This could be keeping track of a cumulative number (e.g. number of data objects meeting a certain criterion) or calculated statistics (e.g. number of peaks in a continuous data stream).
These produced results are bundled into a results object that is then made available to the Distributor by means of a message queue.
### Distributor
Responsible for calculating the up-to-date status of the tracked statistics and providing them to external components that are polling for the results. This decouples the reporting and processing elements, ensuring that the polling for results doesn’t affect processor performance and large data volumes won’t slow down returning of results. The distributor could also be configured to publish data to an external service or database while still allowing the processor to remain agnostic. The action the distributor takes is deliberately open-ended, allowing developers to tailor the result to each individual need.

## Setup
### Data Objects
Decide the form of the data objects. One will become `TProcessData`, the other will become `TDistributeData` when passed to the generic `Build<TProcessData,TDistributeData>()` method. This will produce a `Smoulder<TProcessData,TDistributeData>`, giving type safety to the `Enqueue`/`Dequeue` calls.

### Worker units
Create `Loader`, `Processor` and `Distributor` classes that implement `ILoader`, `IProcessor` and `IDistributor`. The `Loader` has access to the `ProcessorQueue`, the `Processor` has access to the `ProcessorQueue` and `DistributorQueue`, and the `Distributor` has access to the `DistributorQueue`. The queues will be hooked up in the `Smoulder.Start()` method, so they will be successfully hooked up by the time the `Action()` method is called for the first time.

The implementation of the worker units should:
- implement relevant interface
- Extend the relevant workerUnitBase (i.e. `public class MyProcessor: ProcessorBase<MyProcessData,MyDistributeData>, IProcessor`)
- Override the `Action()` method

Optionally, the implementation can override:
- the `Startup()` method, which is called when the Smoulder.Start() method is called. This allows you to initialise any variables that can't be done in the constructor, or that you want to initialise every time the Smoulder object is started, not just at object creation.
- the `Finalise()` which will be called when the Smoulder.Stop() method is called. This could be used to ensure all the remaining data is processed before the smoulder shuts down, close any open connections etc.
- the `OnEmptyQueue()` method, which is called if there was nothing on the queue for $Timeout number of milliseconds
- the `OnError()` method, which is called if there is an uncaught error in Action(), OnEmptyQueue() or the method inside of smoulder that contains the dequeuing logic.

#### Action method
#### Action(TData item)
The `Action(TData item)` method on a workerUnit is the main payload and is the only one that must be implemented for the Smoulder object to be valid. This is what will be called continuously until the `Smoulder.Stop()` method is called. An example format for the action method for a processor would be:

    public override TDistributeData Action(TProcessData item, CancellationToken cancellationToken)
    {
        //Archive the incoming data
		_someRepository.Save(item);
		
		//Transform the data into another form
        TDistributeData outgoingData = Do.Something(item);
		
		//Pass to the distributor
        return outgoingData;
    }

The returned `TDistributeData` object will be enqueued onto the distributor queue ready for the distributor to process. Returning null means nothing is enqueued.

The signatures for the Action methods are as follows:
##### Loader
`public TProcessData Action(CancellationToken cancellationToken)`
##### Processor
`public TDistributeData Action(TProcessData item, CancellationToken cancellationToken)`
##### Distributor
`public void Action(TDistributeData item, CancellationToken cancellationToken)`

#### Available methods
##### Loader
- `Enqueue(TData itemToEnqueue)` - enqueues the item onto the `ProcessorQueue` - Not recommended to do this directly in most cases, use the return value.
- `int GetProcessorQueueCount()` - Returns the number of items on the `ProcessorQueue`

##### Processor
- `Enqueue(TDistributeData itemToEnqueue)` - enqueues the item onto the `DistributorQueue`- Not recommended to do this directly in most cases, use the return value.
- `bool Dequeue(out TProcessData item)` - does a TryTake(item, Timeout) on the `ProcessorQueue`. You usually don't have to call this as it is called for you and the result passed to Action(), but it's available for completeness. You could use:

        public override void Finalise()
        {
            while (Dequeue(out var incomingData))
            {
                var processedData = ProcessData(incomingData);
				Enqueue(processedData);
            }
        }

to cycle through all the remaining items on the queue before finally closing down for example.

- `bool Peek(out TProcessData item)` - Allows for peeking the `ProcessorQueue`. This is implemented here as BlockingCollections don't allow it normally due to multi-threading concerns. There is only one producer and consumer for each queue, so Peek is assumed to be safe.
- `int GetDistributorQueueCount()`
- `int GetProcessorQueueCount()`

##### Distributor
- `bool Dequeue(out TDistributeData item)` - enqueues the item onto the `DistributorQueue` - See processor
- `bool Peek(out TDistributeData item)`- Allows for peeking the `DistributorQueue` - See processor
- `int GetDistributorQueueCount()`

#### Startup()
Startup is deliberately distinct to the constructor so that a Smoulder can be restarted after being stopped. The constructor will only run when the workerUnits are instantiated, but the startup method is called every time the Smoulder.Start() method is called. An example for a use for this is opening a connection to a message queue in the `Startup()` method and disconnecting in the `Finalise()` method.

### Instantiation
Once classes for the workerUnits have been created, a Smoulder object can be instantiated. This is achieved with following two lines:

    var smoulderFactory = new SmoulderFactory();
    var smoulder = smoulderFactory.Build(new Loader(), new Processor(), new Distributor());
    
Any way of creating concrete instances of the workerUnits can be done, such as through an IOC container.
After instantiating the Smoulder object, starting it is as simple as:

    smoulder.Start();
    
While stopping can be achieved with:

    smoulder.Stop();
    
When the `Start()` and `Stop()` methods are called is left entirely up to the implementing developer. For example, If the smoulder is being used inside a windows service the `OnStart()` and `OnStop()` methods are obvious candidates.

# Advanced Features

## QueueBounding
The maximum number of items allowed in the queues can be set when the `SmoulderFactory.Build()` is called.
The following line sets the maximum number of ProcessData objects to 50 and the number of DistributeData objects to 100.

    var smoulder = smoulderFactory.Build(_movementLoader, _movementProcessor, _movementDistributor,50,100);
    
The `Enqueue()` method on `Loader`s and `Processor`s will block until an item is removed.

## Dequeue timeout
By default smoulder will wait `1000` milliseconds before giving up waiting for an item on the queue and calling `OnEmptyQueue()` instead. This can be changed by setting the `Timeout` attribute on the `Processor` and `Distributor` objects.
If the timeout is set to `-1`, it will wait forever or until an item arrives on the queue.

## Multiple Smoulders with IOC
Say you want two smoulder objects in the same application and you're using an IOC container. You have two different implementations of `ILoader`. The best way to split these up so your IOC container knows which is which is to create two interfaces, say `IProcessorA` and `IProcessorB` that both implement the  `Smoulder.Interfaces.IProcessor` interface. Then you can hook your IOC up using these two interfaces and everything is peachy.

# Functional/Compositional methodology

There are methods on the Smoulder object that allow delegates to be passed that overwrite the default functionality. This allows a basic Smoulder to be set up without having to directly extend, override, instantiate and pass in worker unit instances. At it's most basic, a Smoulder can be created with:

    var secondSmoulder = smoulderFactory.Build<ProcessDataObject, DistributeDataObject>()
    .SetLoaderAction(token =>
    {
        var data = GetData();
        return data;
    })
    .SetProcessorAction((incomingData, token) =>
    {
        fakeRepository.SaveData(incomingData);
        var processedData = ProcessData(incomingData);

        return processedData;
    })
    .SetDistributorAction((incomingData, token) =>
    {
        SendData(incomingData);
    });

However, due to this more functional style there are restrictions on the way methods have side effects and they are unable to call the ancillary methods on the Smoulder object like `Enqueue` and `Peek`. This could be improve on if a copy of the Smoulder object was passed into each of the methods, but that is only being considered for future work at this time.

# Worked Example - Random Number Pipe
This simply generates random numbers and passes them through, doing some nominal work on them to show that data can get from one end to the other. In doing so it attempts to show off some of the different configurations worker units can be created with and acts as a quick check that everything works nicely together. A successful build should be able to run this console application without any errors.

In the worked example, a console app creates a Smoulder object using the SmoulderFactory and sets it running.  Regular reports are printed while it is running to show progress to the user. This could be left indefinitely, but is instead stopped. The object is then started again to showcase the ability to stop and start the Smoulder object.

Reading through the worked example will be a good introduction to the different ways Smoulder can be used, it is commented to guide a user through each of the worker units. The Processor is using the most features, if speed is valued over complete comprehension then start there.

Imagine while reading this that the Loader is hooked up to some external data source, the processor is saving the incoming messages to archive and the distributor is building up some aggregate data for report.

# Forge Framework

The Forge Framework is a collection of open-source frameworks created by the team at [3Squared](https://github.com/3squared).
