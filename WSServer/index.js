'use strict';

const WebSocketServer = require('websocket').server;
const http = require('http');

const defaultPort = 1337;
const port = process.env.PORT||defaultPort;

// Create HTTP server for receiving WebSocket connections
var server = http.createServer((request, response) => {
    response.writeHead(200, {"Content-Type": "text/html"});
    response.end(landingPage);
});
server.listen(port, a => {
    console.log('### Ready for connections on port ' + port);
});

// Create the websocket server
var wsServer = new WebSocketServer({
    httpServer: server
});

// On WebSocket request
wsServer.on('request', function(request, response) {
    setTimeout(() => {
        console.log('### Got websocket request');
        var connection = request.accept('zap-test', request.origin);

        connection.on('message', function(request) {
            console.log('Echoing: ' + request.utf8Data);
            setTimeout(() => {
                connection.sendUTF(request.utf8Data)
            });
        });
    }, 10);
});
