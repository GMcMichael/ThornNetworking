```mermaid
flowchart LR
  subgraph mt [Main Thread]
    direction TB
    A([Host Start]) --> B[Open Socket on port]
    B-->C{IsConnected}
    C-- No -->D([Quit])
    C-- Yes -->E[/Send data to remote clients/]
  end
  subgraph Act [AcceptConnections Thread]
    direction TB
    B-->F>Wait for incoming connection]
  end
```
