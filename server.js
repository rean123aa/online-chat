const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);
const port = process.env.PORT || 3000;

// Serve static files
app.use(express.static('public'));

// In-memory storage
let messages = [];
let users = [];

// Utility function to get current Eastern Time
function getESTTime() {
    const date = new Date();
    const utc = date.getTime() + date.getTimezoneOffset() * 60000;
    const estOffset = -5; // EST is UTC-5
    return new Date(utc + 3600000 * estOffset).toLocaleTimeString();
}

io.on('connection', (socket) => {
    let username = '';

    // Set username
    socket.on('set username', (name) => {
        username = name;
        if (!users.includes(username)) users.push(username);
        io.emit('update users', users);
        // Load previous messages
        socket.emit('load messages', messages);
    });

    // Chat message
    socket.on('chat message', (text) => {
        const msg = {
            id: Date.now() + Math.random(), // unique ID
            user: username,
            text,
            time: getESTTime()
        };
        messages.push(msg);
        io.emit('chat message', msg);
    });

    // Edit message
    socket.on('edit message', ({ id, newText }) => {
        const msg = messages.find(m => m.id == id);
        if (msg && msg.user === username) {
            msg.text = newText;
            io.emit('edit message', { id, newText });
        }
    });

    // Delete message
    socket.on('delete message', (id) => {
        const msgIndex = messages.findIndex(m => m.id == id);
        if (msgIndex !== -1 && messages[msgIndex].user === username) {
            messages.splice(msgIndex, 1);
            io.emit('delete message', id);
        }
    });

    // Typing notification
    socket.on('typing', (isTyping) => {
        socket.broadcast.emit('typing', { user: username, typing: isTyping });
    });

    // Disconnect
    socket.on('disconnect', () => {
        if (username) {
            users = users.filter(u => u !== username);
            io.emit('update users', users);
        }
    });
});

http.listen(port, () => {
    console.log(`Server running on port ${port}`);
});
