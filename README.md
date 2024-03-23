```mermaid
flowchart TB
  mt ~~~ act ~~~ rt
  subgraph mt [Main Thread]
    A([Host Start])-->B[Open Socket on port]
    B-->[Test]
    B-->D{IsConnected}
    D-- No -->E([Quit])
    D-- Yes -->F[/Send data to remote clients/]
  end
  subgraph act [AcceptConnections Thread]
    W-->X
  end
  subgraph rt [Receiving Thread]
    Y-->Z
  end
```