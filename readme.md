# ClientWebSocket.SendAsync hang test

A WebSocket client and server used to provoke a `System.Net.WebSockets.ClientWebSocket.SendAsync` hang issue. The test creates two connections to the test WebSocket server, using aggressive keep alive interval which triggers the issue after a little while. In production you would run with far greater keep alive interval, but this would require many more connections over a much greater time period to provoke the issue. Using this short keep alive triggers the bug in a few minutes.

## Starting WSServer

In `WSServer` folder run

```
npm install
npm start
```

## Starting WSClient

- Open `WSClient/WebSocketTest.sln` in Visual Studio.
- If `WSServer` is running on the same host start the application to start running the test
 - In case `WSServer` is running in another host, change `localhost` in `Program.cs` to the correct hostname/ip-address