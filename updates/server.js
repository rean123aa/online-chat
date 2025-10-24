const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const app = express();
const server = http.createServer(app);
const io = new Server(server);
const PORT = process.env.PORT || 3000;

app.use(express.static('public'));

let users = {};
let messages = [];

// Helper: get EST time string
function getESTTime() {
    return new Date().toLocaleTimeString('en-US', { timeZone: 'America/New_York', hour12:true, hour:'2-digit', minute:'2-digit' });
}

io.on('connection', (socket) => {
    let currentUser = '';

    // User sets username
    socket.on('set username', (name) => {
        currentUser = name;
        users[socket.id] = name;
        io.emit('update users', Object.values(users));
        io.emit('system message', `⭐ ${name} joined the chat`);
        socket.emit('load messages', messages);
    });

    // Chat messages
    socket.on('chat message', (text) => {
        const msg = {
            id: Date.now() + Math.random().toString(36).substring(2,7),
            user: currentUser,
            text,
            time: getESTTime()
        };
        messages.push(msg);
        io.emit('chat message', msg);
    });

    // Typing indicator
    socket.on('typing', (isTyping) => {
        if(isTyping) {
            io.emit('typing', currentUser);
        } else {
            io.emit('typing', '');
        }
    });

    // Disconnect
    socket.on('disconnect', () => {
        if(currentUser) {
            io.emit('system message', `👋 ${currentUser} left the chat`);
            delete users[socket.id];
            io.emit('update users', Object.values(users));
        }
    });

    // Edit message
    socket.on('edit message', ({id, newText}) => {
        const msg = messages.find(m => m.id === id);
        if(msg && msg.user === currentUser) {
            msg.text = newText;
            io.emit('edit message', {id, newText});
        }
    });

    // Delete message
    socket.on('delete message', (id) => {
        const index = messages.findIndex(m => m.id === id);
        if(index !== -1 && messages[index].user === currentUser) {
            messages.splice(index, 1);
            io.emit('delete message', id);
        }
    });
});

server.listen(PORT, () => console.log(`Server running on port ${PORT}`));
