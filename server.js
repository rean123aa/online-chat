const express = require('express');
const http = require('http');
const { Server } = require('socket.io');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static('public'));

let users = {};
let messages = [];

io.on('connection', (socket) => {
  socket.on('set username', (username) => {
    users[socket.id] = username;
    io.emit('update users', Object.values(users));
    socket.emit('load messages', messages);
    socket.broadcast.emit('chat message', { system: true, text: `${username} joined the chat` });
  });

  socket.on('chat message', (msg) => {
    const timestamp = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    const messageObj = { id: Date.now(), user: users[socket.id], text: msg, time: timestamp };
    messages.push(messageObj);
    io.emit('chat message', messageObj);
  });

  socket.on('edit message', ({ id, newText }) => {
    messages = messages.map(m => m.id === id ? { ...m, text: newText } : m);
    io.emit('edit message', { id, newText });
  });

  socket.on('delete message', (id) => {
    messages = messages.filter(m => m.id !== id);
    io.emit('delete message', id);
  });

  socket.on('disconnect', () => {
    if(users[socket.id]){
      socket.broadcast.emit('chat message', { system: true, text: `${users[socket.id]} left the chat` });
      delete users[socket.id];
      io.emit('update users', Object.values(users));
    }
  });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log(`Server running on port ${PORT}`));
