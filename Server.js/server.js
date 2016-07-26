var net = require('net');

var clients = [];

var server = net.createServer();

server.on('connection', (socket) => {
    
    clients.push(socket);

    console.log(`Connection accepted from ${getRemoteSocketIP(socket)}`);

    server.getConnections((err, count) => {
        console.log(`connections count: ${count}`);
    });    

    handleSocket(socket);

});

console.log(server.listening);

server.on('listening', () => {
    address = server.address();    
    console.log(`Server listining at ${address.address}:${address.port}`);
});

server.on('error', (err) => {
    console.log('Server error occured');
});


server.listen({ port: 8000, host: "192.168.40.33" });

var handleSocket = function(socket){

    socket.on('close', (err) => {
        console.log('connection lost');
    });

    socket.on('error', (err) => {
        console.log('An error occured');
    });

    socket.on('data', (data) => {
        console.log(`${getRemoteSocketIP(socket)}: ${data.toString()}`);
        writeToAll(clients, data);
    });
}

var getRemoteSocketIP = function(socket){
    return `${socket.remoteAddress}:${socket.remotePort}`;
}

var writeToAll = function(clients, data){
    clients.forEach(function(client) {
        client.write(data);
    });
}