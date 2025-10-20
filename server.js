const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);

app.use(express.static('public'));

let messages = []; // Store all chat messages
let users = [];

io.on('connection', socket => {
  let username = '';

  // Set username
  socket.on('set username', name => {
    username = name;
    users.push(username);
    io.emit('update users', users);
    socket.emit('load messages', messages);
  });

  // New message
  socket.on('chat message', text => {
    const msg = {
      id: Date.now().toString(), // unique ID
      user: username,
      text,
      time: new Date().toLocaleTimeString(),
      reactions: {} // for live emoji reactions
    };
    messages.push(msg);
    io.emit('chat message', msg);
  });

  // Edit message
  socket.on('edit message', ({ id, newText }) => {
    const msg = messages.find(m => m.id === id);
    if(msg && msg.user === username) { // only allow sender to edit
      msg.text = newText;
      io.emit('edit message', { id, newText });
    }
  });

  // Delete message
  socket.on('delete message', id => {
    const msg = messages.find(m => m.id === id);
    if(msg && msg.user === username) { // only allow sender to delete
      messages = messages.filter(m => m.id !== id);
      io.emit('delete message', id);
    }
  });

  // Live emoji reactions
  socket.on('add reaction', ({ messageId, emoji }) => {
    const msg = messages.find(m => m.id === messageId);
    if(msg) {
      msg.reactions[emoji] = (msg.reactions[emoji] || 0) + 1;
      io.emit('update reactions', { messageId, reactions: msg.reactions });
    }
  });

  // Disconnect
  socket.on('disconnect', () => {
    users = users.filter(u => u !== username);
    io.emit('update users', users);
  });
});

// Start server
const PORT = process.env.PORT || 3000;
http.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
