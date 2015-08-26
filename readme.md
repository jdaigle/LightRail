# LightRail

**LightRail** is an opinionated message broker and enterprise service bus (ESB) framework
designed for .NET server applications. It is based on the same architecture principals
popularized by the successful [NServiceBus](https://github.com/Particular/NServiceBus) and
[MassTransit](http://masstransit-project.com/) projects.

# Requirements

LightRail requires the .NET 4.5.

# Roadmap

**Messaging Patterns**
* Message Broker *- in progress*
* Publish/Subscribe *- not started*

**Transports**
* SQL Server Service Broker *- in progress*
* SSSB Timer Support *- not started*
* SQL Server Tables *- not started*
* MSMQ *- not started*
* RabbitMQ *- not started*
* Azure Service Bus *- maybe*

**API, Runtime Client, and Configuration**
* Message type conventions *- completed*
* Message mapper & concrete interface type builder *- completed*
* Logging framework abstraction *- completed*
* Message handler pipelining *- in progress*

# Acknowledgements

This project utilizes the following open source projects:
* https://github.com/kevin-montrose/Jil
* https://github.com/psake/psake
* http://logging.apache.org/log4net/

Some core code is is derived from [NServiceBus 2.0](https://github.com/NServiceBus/NServiceBus/blob/2.0/src/impl/messageInterfaces/NServiceBus.MessageInterfaces.MessageMapper.Reflection/MessageMapper.cs)
which is licensed under [Apache Licence, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0). The relevant code files have headers to indicate the attribution.