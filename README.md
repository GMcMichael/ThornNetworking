<div id="doc-title"></div>
<h1 align="center"> ThornNetworking </h1>
<p align="center"><a href="#doc-about">About</a> <a href="#doc-installation">Installation</a> <a href="#doc-documentation">Documentation</a> <a href="#doc-diagrams">Diagrams</a> </p>


<div id="doc-about"></div>
<h2>About</h2>
<p>This repository is a personal project with the goal of becoming an multithreaded and easy-to-use C# Class Library networking solution with a plug-and-play style system where users can easily host or join, with a variety of connection methods, using only a few simple function calls after being added as a project depenency.
</br>
</br>
If the project meets the usecase of whoever happens to stubmle upon it, it is free to use as is in projects or to dissect as a learning resource.
</p>


<div id="doc-installation"></div>
<h2>Installation</h2>
<a href="https://www.nuget.org/packages/ThornNetworking/"><img alt="NuGet Version" src="https://img.shields.io/nuget/v/ThornNetworking"></a>

<p>To install with the Nuget Package Manager, simply run:</p>

```bash
Install-Package ThornNetworking [-Version x.x.x]
```
<p> Omit the -Version to automatically install the latest version or specify the x.x.x for a certain version.
</br>
</br>
Once the package has been installed, to start using it in your code just add:</p>

```C#
using ThornNetworking;
```


<div id="doc-documentation"></div>
<h2>Documentation</h2>
<p>The following is a simple class diagram of everything nessecary to set up hosting and connecting in a project:</p>

```mermaid
classDiagram
class NetworkManager{
    +Instance NetworkManager
    +IsConnected bool$
    -SendBuffer List~byte[]~$
    +StartHost()
}
namespace ThreadParameters {
    class BaseParameters {
        +token CancellationToken
        +BaseParameters(CancellationToken) BaseParameters
        +BaseParameters(BaseParameters) BaseParameters
    }
    class SentryParameters {
        +cancellationTokenSource CancellationTokenSource
        +SentryParameters() SentryParameters
        +SentryParameters(CancellationTokenSource) SentryParameters
        +SentryParameters(SentryParameters) SentryParameters
    }
}
BaseParameters <|-- SentryParameters
```

<p>The NetworkManager is a singleton style class initialized on project startup that acts as the interface between the internal networking system and external projects.</p>

```C#
NetworkManager manager = NetworkManager.Instance;
```

<p>The project works on an event based framework so that means you set a function to run each time data is received which will be called with the format:</p>

```C#
ReceiveFunction(data: byte[])
```

<p>The receive function can be set with the following call:</p>

```C#
NetworkManager.Instance.SetRecieveFunction(function: Action<byte[]> );
```

<p>To start a host call:</p>

```C#
NetworkManager.Instance.StartHost(ip: System.Net.IPAddress, port: Integer);
```

<p>To join as a client call:</p>

```C#
NetworkManager.Instance.StartClient(ip: System.Net.IPAddress, port: Integer);
```

<p>To send data call:</p>

```C#
NetworkManager.Instance.SendData(data: byte[], destinations: [System.Net.IPAddress]);
```

<p>To send data to every client as the host, or the host as any client, call the same function without any destinations:</p>

```C#
NetworkManager.Instance.SendData(data: byte[]);
```

<div id="doc-diagrams"></div>
<h2>Diagrams</h2>
<h3>Legend</h3>
<p>The following legend can be used for the two following flowchart diagrams which depict the code flow for hosts and clients:</p>

```mermaid
flowchart
    classDef hidden display: none;
    classDef MutexLock stroke:#ff0000, stroke-dasharray: 5 5;
    classDef RecieveLock stroke:#00ff00, stroke-dasharray: 5 5;
    classDef SendLock stroke:#0000ff, stroke-dasharray: 5 5;
    classDef DRE stroke:#ff9d00, stroke-dasharray: 5 5;
    classDef TODO stroke:#6bf0ff, stroke-dasharray: 5 5;
    %% unused #d4ff00
    subgraph Legend
        direction TB
        ml[MutexLock]:::MutexLock ~~~ rl[RecieveMutexLock]:::RecieveLock ~~~ sl[SendMutexLock]:::SendLock ~~~ dre[Data Recieve Event]:::DRE ~~~ todo[TODO]:::TODO
    end
```

<h3>Host</h3>

```mermaid
flowchart
    classDef hidden display: none;
    classDef MutexLock stroke:#ff0000, stroke-dasharray: 5 5;
    classDef RecieveLock stroke:#00ff00, stroke-dasharray: 5 5;
    classDef SendLock stroke:#0000ff, stroke-dasharray: 5 5;
    classDef DRE stroke:#ff9d00, stroke-dasharray: 5 5;
    classDef TODO stroke:#6bf0ff, stroke-dasharray: 5 5;
    subgraph Host
        direction LR
        HostEntryPoint ~~~ HostingSendingThread
        HostingAcceptConnectionsThread ~~~ HostingReceivingThread
        subgraph HostEntryPoint [Entry Point]
            direction TB
            HostStart([Host Start]) --> OpenSocket[Open Socket on port] --> HostStartThreads
            subgraph HostStartThreads [Start Threads]
                direction TB
                act([AcceptConnections Thread]) ~~~ rt([Receiving Thread]) ~~~ st([Sending Thread])
            end
        end
        subgraph HostingSendingThread [Sending Thread]
            direction TB
            HostSendIsCancelled{IsCancelled} -- Yes --> HostSendQuit([Quit])
            HostSendIsCancelled -- No --> SendBufferEmpty{SendBuffer Empty}
            SendBufferEmpty -- Yes --> HostSendSleep>Sleep deltaTime] --- HostSendHidden:::hidden
            SendBufferEmpty -- No --> CopyBuffer[Dequeue SendBuffer]:::MutexLock --> RecipientLoop[For each recipient]
            RecipientLoop --> SendData[/Send Data/]:::SendLock --- HostSendHidden --> HostSendIsCancelled
        end
        subgraph HostingAcceptConnectionsThread [AcceptConnections Thread]
            direction TB
            WaitConnection>Wait for incoming connection] --> HostAuthenticate[/Authentication/]:::RecieveLock -- Success --> AddConnection[Add to RemoteConnections]:::MutexLock --> WaitConnection
            HostAuthenticate -- Fail --> WaitConnection
        end
        subgraph HostingReceivingThread [Receiving Thread]
            direction TB
            IsConnectedOrCancelled{Disconnected or Cancelled} -- Yes --> HostQuit
            IsConnectedOrCancelled -- No ---> CheckConnectionsUpdate{RemoteConnections Updated}
            CheckConnectionsUpdate -- Yes --> CopyConnections[Copy RemoteConnections]:::MutexLock --> HostReceivingNew[For each new connection] --> HostReceivingStartWait([Start Async Waiting]) --- HostReceivingHidden:::hidden
            CheckConnectionsUpdate -- No --> HostReceivingCheckTimeout([Dispose of connection if past timeout]) --> HostReceivingSleep>Sleep DeltaTime] --- HostReceivingHidden --> IsConnectedOrCancelled
            subgraph HostQuit [Quit]
                direction TB
                HostDisposeThreads[Displose of threads] --> HostDisposeSockets[Dispose of RemoteConnections]
            end
        end
    end
```

<h3>Client</h3>

```mermaid
flowchart
    classDef hidden display: none;
    classDef MutexLock stroke:#ff0000, stroke-dasharray: 5 5;
    classDef RecieveLock stroke:#00ff00, stroke-dasharray: 5 5;
    classDef SendLock stroke:#0000ff, stroke-dasharray: 5 5;
    classDef DRE stroke:#ff9d00, stroke-dasharray: 5 5;
    classDef TODO stroke:#6bf0ff, stroke-dasharray: 5 5;
    subgraph Client
        direction TB
        ClientSendingThread & ClientEntryPoint ~~~ ClientReceivingThread
        subgraph ClientEntryPoint [Entry Point]
            direction TB
            ClientStart[(Client Start)] --> AttemptConnection[Attempt Connection] --> ClientAuthenticate[/Authentication/]:::SendLock --> ClientStartThreads
            subgraph ClientStartThreads [Start Threads]
                direction TB
                crt[Recieving Thread] ~~~ cst[Sending Thread]
            end
        end
        subgraph ClientSendingThread [Sending Thread]
            direction TB
            ClientConnected{Connected} -- No ---> ClientQuit([Quit])
            ClientConnected -- Yes --> ClientSendBufferEmpty{SendBuffer Empty} -- No --> ClientCopyBuffer[Copy and Clear SendBuffer]:::MutexLock --> ClientSendData[/Send Data to host/]:::SendLock  --> ClientSendBufferEmpty
            ClientSendBufferEmpty -- Yes ---> ClientPing[/Send Ping for timeout/]:::SendLock --> ClientSendSleep>Sleep deltaTime] --> ClientConnected
            subgraph ClientQuit [Quit]
                direction TB
                ClientDisposeThreads[Dispose Threads] --> ClientDisposeSocket[Dispose Socket]
            end
        end
        subgraph ClientReceivingThread [Receiving Thread]
            direction TB
            ClientRecieveWait>Wait for data]:::RecieveLock
            ClientRecieveWait --> ClientIsSync{IsSyncBuffer} -- No --> ClientHandleData[/Raise data recieve event/]:::DRE
            ClientIsSync -- Yes --> ClientUpdateSync[Update SyncBuffer]:::MutexLock
            ClientHandleData & ClientUpdateSync --- h4:::hidden --> ClientRecieveWait
        end
    end
```
