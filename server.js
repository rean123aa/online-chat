const express = require('express');
const app = express();
const http = require('http').createServer(app);
const io = require('socket.io')(http);

app.use(express.static('public'));

let users = {};
let messages = [];

function getESTTime(){
    return new Date().toLocaleString("en-US", { hour:'numeric', minute:'numeric', hour12:true, timeZone:"America/New_York" });
}

io.on('connection', socket => {
  socket.on('set username', name => {
    socket.username = name;
    users[socket.id] = name;
    io.emit('update users', Object.values(users));
    socket.emit('load messages', messages);
  });

  socket.on('chat message', text => {
    const msg = { id: Date.now(), user: socket.username, text, time: getESTTime(), reactions:{} };
    messages.push(msg);
    io.emit('chat message', msg);
  });

  socket.on('typing', isTyping => {
    io.emit('typing', isTyping ? socket.username : '');
  });

  socket.on('edit message', ({id,newText}) => {
    const msg = messages.find(m => m.id === id);
    if(msg){ msg.text=newText; io.emit('edit message',{id,newText}); }
  });

  socket.on('delete message', id => {
    messages = messages.filter(m => m.id !== id);
    io.emit('delete message', id);
  });

  socket.on('add reaction', ({messageId,emoji}) => {
    const msg = messages.find(m => m.id===messageId);
    if(msg){
      msg.reactions[emoji] = (msg.reactions[emoji]||0)+1;
      io.emit('update reactions',{messageId,reactions:msg.reactions});
    }
  });

  socket.on('disconnect', ()=>{
    delete users[socket.id];
    io.emit('update users', Object.values(users));
  });
});

const PORT = process.env.PORT || 3000;
http.listen(PORT, ()=>console.log(`Server running on port ${PORT}`));
