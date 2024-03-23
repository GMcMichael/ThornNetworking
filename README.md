```mermaid
graph TD
  A([Host Start]) --> B[Open Socket on port]
  B-->C{IsConnected}
  C-- No -->D([Quit])
  C-- Yes -->E[/Send data to remote clients/]

  B-->F>Wait for incoming connection]
```
