const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);
const PORT = process.env.PORT || 3000;

app.use(express.static('public'));

let users = {};
let typingUsers = new Set();

io.on('connection', socket => {
    socket.on('set username', name => {
        socket.username = name;
        users[socket.id] = name;
        io.emit('update users', Object.values(users));
    });

    socket.on('chat message', msg => {
        io.emit('chat message', { user: socket.username, text: msg });
    });

    socket.on('typing', isTyping => {
        if(isTyping) typingUsers.add(socket.username);
        else typingUsers.delete(socket.username);
        const typingUser = Array.from(typingUsers).find(u => u !== socket.username);
        io.emit('typing', typingUser || '');
    });

    socket.on('disconnect', () => {
        delete users[socket.id];
        typingUsers.delete(socket.username);
        io.emit('update users', Object.values(users));
    });
});

http.listen(PORT, () => console.log(`Server running on port ${PORT}`));
