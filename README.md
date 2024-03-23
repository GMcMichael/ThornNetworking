```mermaid
graph TD
  A([Host Start]) --> B[Open Socket on port]
  B-->C{IsConnected}
  C-- Yes -->D[/Send data to remote clients/]
  C-- No -->E([Quit])
```
