const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);
const PORT = process.env.PORT || 3000;

app.use(express.static('public'));

let users = {};
let messages = [];

// Handle socket connections
io.on('connection', (socket) => {
    console.log(`User connected: ${socket.id}`);

    // Set username when user joins
    socket.on('set username', (username) => {
        users[socket.id] = username;
        socket.emit('load messages', messages); // send previous messages
        io.emit('update users', Object.values(users));
        io.emit('chat message', { text: `${username} joined the chat`, system: true, time: new Date().toLocaleTimeString('en-US', { hour12: false, timeZone: 'America/New_York' }) });
    });

    // Handle chat messages
    socket.on('chat message', (msg) => {
        const messageObj = {
            id: Date.now() + Math.random().toString(36).substring(2, 7),
            user: users[socket.id] || 'Unknown',
            text: msg,
            time: new Date().toLocaleTimeString('en-US', { hour12: false, timeZone: 'America/New_York' })
        };
        messages.push(messageObj);
        io.emit('chat message', messageObj);
    });

    // Edit messages
    socket.on('edit message', ({ id, newText }) => {
        const msg = messages.find(m => m.id === id);
        if(msg && users[socket.id] === msg.user){
            msg.text = newText;
            io.emit('edit message', { id, newText });
        }
    });

    // Delete messages
    socket.on('delete message', (id) => {
        const index = messages.findIndex(m => m.id === id);
        if(index !== -1 && users[socket.id] === messages[index].user){
            messages.splice(index, 1);
            io.emit('delete message', id);
        }
    });

    // Remove user on disconnect
    socket.on('disconnect', () => {
        const username = users[socket.id];
        delete users[socket.id];
        io.emit('update users', Object.values(users));
        if(username) io.emit('chat message', { text: `${username} left the chat`, system: true, time: new Date().toLocaleTimeString('en-US', { hour12: false, timeZone: 'America/New_York' }) });
        console.log(`User disconnected: ${socket.id}`);
    });
});

http.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});
