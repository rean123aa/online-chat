const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static('public'));

let messages = [];
let users = [];

io.on('connection', socket => {
    let userName = '';

    socket.on('set username', name => {
        userName = name;
        users.push(name);
        io.emit('update users', users);
        socket.emit('load messages', messages);
        messages.push({ system: true, text: `${name} joined the chat.`, time: new Date().toLocaleTimeString('en-US', { timeZone: 'America/New_York', hour12: true }) });
        io.emit('chat message', messages[messages.length-1]);
    });

    socket.on('chat message', msg => {
        const messageObj = { user: userName, text: msg, id: Date.now(), time: new Date().toLocaleTimeString('en-US', { timeZone: 'America/New_York', hour12: true }) };
        messages.push(messageObj);
        io.emit('chat message', messageObj);
    });

    socket.on('edit message', ({id,newText})=>{
        const msg = messages.find(m=>m.id===id);
        if(msg && msg.user===userName){ msg.text=newText; io.emit('edit message', {id,newText}); }
    });

    socket.on('delete message', id=>{
        messages = messages.filter(m=>m.id!==id);
        io.emit('delete message', id);
    });

    socket.on('typing', () => {
        socket.broadcast.emit('typing', userName);
    });

    socket.on('disconnect', () => {
        users = users.filter(u=>u!==userName);
        io.emit('update users', users);
        messages.push({ system: true, text: `${userName} left the chat.`, time: new Date().toLocaleTimeString('en-US', { timeZone: 'America/New_York', hour12: true }) });
        io.emit('chat message', messages[messages.length-1]);
    });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, ()=>console.log(`Server running on port ${PORT}`));
