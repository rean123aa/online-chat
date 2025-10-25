const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const moment = require('moment-timezone'); // For EST timestamps

const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static('public')); // Serve index.html & public folder

let messages = [];
let users = [];

io.on('connection', socket => {
    let username = '';

    socket.on('set username', name => {
        username = name;
        if(!users.includes(username)) users.push(username);
        io.emit('update users', users);
        socket.emit('load messages', messages);
    });

    socket.on('chat message', text => {
        const msg = {
            id: Date.now() + Math.random(),
            user: username,
            text,
            time: moment().tz("America/New_York").format('hh:mm A'),
        };
        messages.push(msg);
        io.emit('chat message', msg);
    });

    socket.on('edit message', ({id,newText}) => {
        const msg = messages.find(m => m.id === id);
        if(msg && msg.user === username){
            msg.text = newText;
            io.emit('edit message', {id, newText});
        }
    });

    socket.on('delete message', id => {
        const index = messages.findIndex(m => m.id === id);
        if(index !== -1 && messages[index].user === username){
            messages.splice(index, 1);
            io.emit('delete message', id);
        }
    });

    socket.on('disconnect', () => {
        if(username){
            users = users.filter(u => u !== username);
            io.emit('update users', users);
        }
    });
});

server.listen(process.env.PORT || 3000, () => {
    console.log('Server running...');
});
