const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);

const PORT = process.env.PORT || 3000;

app.use(express.static('public'));

// Messages and users storage (in-memory)
let messages = [];
let users = [];

io.on('connection', (socket) => {
    console.log('A user connected:', socket.id);

    // Set username
    socket.on('set username', username => {
        socket.username = username;
        users.push(username);
        io.emit('update users', users);
        socket.emit('load messages', messages);
    });

    // Chat message
    socket.on('chat message', text => {
        const msg = {
            id: Date.now().toString(),
            user: socket.username,
            text,
            time: new Date().toLocaleTimeString(),
            reactions: {}
        };
        messages.push(msg);
        io.emit('chat message', msg);
    });

    // Edit message
    socket.on('edit message', ({id, newText}) => {
        const msg = messages.find(m => m.id === id);
        if(msg && msg.user === socket.username){
            msg.text = newText;
            io.emit('edit message', {id, newText});
        }
    });

    // Delete message
    socket.on('delete message', id => {
        const msgIndex = messages.findIndex(m => m.id === id);
        if(msgIndex !== -1 && messages[msgIndex].user === socket.username){
            messages.splice(msgIndex,1);
            io.emit('delete message', id);
        }
    });

    // Reactions
    socket.on('add reaction', ({ messageId, emoji }) => {
        const msg = messages.find(m => m.id === messageId);
        if(!msg.reactions[emoji]) msg.reactions[emoji] = 0;
        msg.reactions[emoji]++;
        io.emit('update reactions', { messageId, reactions: msg.reactions });
    });

    socket.on('disconnect', () => {
        users = users.filter(u => u !== socket.username);
        io.emit('update users', users);
        console.log('A user disconnected:', socket.id);
    });
});

http.listen(PORT, () => console.log(`Server running on port ${PORT}`));
