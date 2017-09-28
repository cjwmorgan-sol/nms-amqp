# NMS-AMQP
### Overview
The goal of this project is to combine the [.NET Message Service API](http://activemq.apache.org/nms/) (NMS) with the [Advanced Message Queuing Protocol (AMQP)](https://www.amqp.org/) 1.0 standard wireline protocol. Historically, the Apache community created the NMS API which provided a vendor agnostic .NET interface to a variety of messaging systems. The NMS API gives the flexibility to write .NET applications in C#, VB or any other .NET language, all while using a single API to connect to any number of messaging providers. The Advanced Message Queuing Protocol (AMQP) is an open and standardized internet protocol for reliably passing messages between applications or organizations. Before AMQP became a standard, organizations used proprietary wireline protocols to connect their systems which lead to vendor lock-in and integration problems when integrating with external organizations.

The key to enabling vendor independence and mass adoption of technology is to combine open source APIs and standard wireline protocols which is precisely what this project is all about. Here’s how AMQP 1.0 support within NMS helps the .NET community:
 - __More Choice:__ As more message brokers and services implement the AMQP 1.0 standard wireline, .NET developers and architects will have more options for messaging technology.
 - __No Migration Risk:__ Since AMQP 1.0 is a wireline standard, you won’t run into the problems that used to happen when switching between implementations.
 - __Innovation:__ Competition is a key component of technology innovation. Directly competitive messaging implementations, with seamless pluggability, forces vendors to innovate and differentiate.
 
If you are a .NET developer that doesn’t want to be locked into a messaging implementation then get engaged with this project. Here you will find the open source code base and please provide comments and make your own enhancements. The project will be folded into the Apache community once fully mature.


### AMQP1.0 Protocol Engine AmqpNetLite
NMS-AMQP uses [AmqpNetLite](https://github.com/Azure/amqpnetlite) as the underlying AMQP 1.0 transport Protocol engine. 

### Overall Architecture
NMS-AMQP should bridge the familiar NMS concepts to AMQP protocol concepts as described in the document [amqp-bindmap-jms-v1.0-wd07.pdf](https://www.oasis-open.org/committees/download.php/59981/amqp-bindmap-jms-v1.0-wd07.pdf).
So in general most of the top level classes that implement the Apache.NMS interface _Connection, Session, MessageProducer,_ etc  create, manage, and destroy the amqpnetlite equivalent object _Connection, Session, Link,_ etc.

### Building
There are two projects: NMS-AMQP, and HelloWorld. Both use the new csproj format available in Visual Studio 2017.
NMS-AMQP is the library which implements The Apache.NMS Interface using AmqpNetLite.
HelloWorld is a sample application using the NMS library which can send messages to an AMQP Message Broker.

To build launch Visual Studio 2017 with the nms-amqp.sln file and build the solution.
All build artifacts will be under <root_folder>\build\\$(Configuration)\\$(TargetFramework).

### Testing
_TODO_ pick unit test framework.
