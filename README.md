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
        ml[MutexLock]:::MutexLock ~~~ rl[RecieveLock]:::RecieveLock ~~~ sl[SendLock]:::SendLock ~~~ dre[Data Recieve Event]:::DRE ~~~ todo[TODO]:::TODO
    end
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
            IsConnected{IsConnected} -- Yes ---> SendBufferEmpty{SendBuffer Empty} -- Yes --> HostSendSleep>Sleep deltaTime] --> IsConnected
            SendBufferEmpty -- No --> CopyBuffer[Dequeue SendBuffer]:::MutexLock --> RecipientLoop[For each recipient]
            RecipientLoop --> SendData[/Send Data/]:::SendLock --> SendBufferEmpty
            IsConnected -- No --> HostQuit
            subgraph HostQuit [Quit]
                direction TB
                HostDisposeThreads[Displose of threads] --> HostDisposeSockets[Dispose of Sockets]
            end
        end
        subgraph HostingAcceptConnectionsThread [AcceptConnections Thread]
            direction TB
            WaitConnection>Wait for incoming connection] --> HostAuthenticate[/Authentication/]:::RecieveLock -- Success --> AddConnection[Add to RemoteConnections]:::MutexLock --> WaitConnection
            HostAuthenticate -- Fail --> WaitConnection
        end
        subgraph HostingReceivingThread [Receiving Thread]
            direction TB
            CheckConnectionsUpdate{RemoteConnections Updated}
            CheckConnectionsUpdate -- Yes --> CopyConnections[Copy RemoteConnections]:::MutexLock --> ConnectionLoop
            CheckConnectionsUpdate -- No --> ConnectionLoop
            h1:::hidden
            subgraph h1
                ConnectionLoop[For each connection in copy] --> IsWaiting{Marked Waiting} -- No --> DataWait>Wait for data or timeout]:::RecieveLock --> IsTimeout{IsTimeout} -- No --> HostUnmark[Unmark]:::MutexLock --> HostIsSyncBuffer{Is SyncBuffer} -- No --> PassData[/Raise data event/]:::DRE
                HostIsSyncBuffer -- Yes --> HostUpdateSyncBuffer[Update SyncBuffer]:::MutexLock
                IsWaiting -- Yes --- h2
                IsTimeout -- Yes --> IsMarked{IsMarked} -- Yes --> RemoveConnection[Remove from RemoteConnections]:::MutexLock
                IsMarked -- No --> Mark[Mark]:::MutexLock
            end
            HostUpdateSyncBuffer & PassData & RemoveConnection & Mark ---- h2:::hidden --> CheckConnectionsUpdate
        end
    end
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
            style ClientAuthSuccess display:none;
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
