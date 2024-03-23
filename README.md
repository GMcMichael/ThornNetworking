```mermaid
flowchart LR
  subgraph mt [Main Thread]
    A([Host Start]) --> B[Open Socket on port]
    B-->C{IsConnected}
    C-- No -->D([Quit])
    C-- Yes -->E[/Send data to remote clients/]
  end
  subgraph act [AcceptConnections Thread]
    B-->F>Wait for incoming connection]
  end
```
