```mermaid
flowchart LR
  mt ~~~ act ~~~ rt
  subgraph mt [Main Thread]
    direction TB
    A([Host Start])-->B[Open Socket on port]
    B-->[Test]
    B-->D{IsConnected}
    D-- No -->E([Quit])
    D-- Yes -->F[/Send data to remote clients/]
  end
  subgraph act [AcceptConnections Thread]
    direction TB
    W-->X
  end
  subgraph rt [Receiving Thread]
    direction TB
    Y-->Z
  end
```