using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SohbetServis.Hubs
{
    public class ChatHub : Hub
    {
        private readonly string _botUser;
        private readonly IDictionary<string,UserConnection> _connections;
        public ChatHub(IDictionary<string, UserConnection> connections)
        {
            _botUser = "Bot";
            _connections = connections;
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            if(_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                _connections.Remove(Context.ConnectionId);
                Clients.Group(userConnection.Room)
                    .SendAsync("ReceiveMessage", _botUser, $"{userConnection.User} odayı terketti");

                SendConnectedUsers(userConnection.Room);
            }
            return base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(string message)
        {
            if(_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                if(IsPasswordTrue(userConnection.Room))
                { await Clients.Group(userConnection.Room)
                    .SendAsync("ReceiveMessage", userConnection.User, message);
                } else {
                    await Clients.Group(userConnection.Room)
                    .SendAsync("ReceiveMessage", userConnection.User, ComputeSha256Hash(message));
                }
            }
        }
        public async Task JoinRoom(UserConnection userConnection)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room);

            _connections[Context.ConnectionId] = userConnection;

            await Clients.Group(userConnection.Room).SendAsync("ReceiveMessage", _botUser,
                $"{userConnection.User}, {userConnection.Password}, {userConnection.Room}'ya girdi.");

            await SendConnectedUsers(userConnection.Room);
            await SendIsPasswordTrue(userConnection.Room);
        }

        public Task SendConnectedUsers(string room)
        {
            var users = _connections.Values
                .Where(c => c.Room == room)
                .Select(c => c.User);

                return Clients.Group(room).SendAsync("UsersInRoom",users);
        }
        public Task SendIsPasswordTrue(string room)
        {

            var passwords = _connections.Values
                .Where(c => c.Room == room)
                .Select(c => c.Password);
            foreach(var password in passwords)
            {
                var ifExists = _connections.Values.Any(c => c.Room == room && password != c.Password );
                if(ifExists)
                    return Clients.Group(room).SendAsync("IsPasswordTrue", false);
            }
                return Clients.Group(room).SendAsync("IsPasswordTrue", true);
        }

        public Boolean IsPasswordTrue(string room)
        {
            //işlemler yap sonra dönen değeri hashle
            var passwords = _connections.Values
                .Where(c => c.Room == room)
                .Select(c => c.Password);
            foreach(var password in passwords)
            {
                var ifExists = _connections.Values.Any(c => c.Room == room && password != c.Password );
                if(ifExists)
                    return false;
            }
            return true;
        }
        static string ComputeSha256Hash(string rawData)  
        {  
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())  
            {  
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();  
                for (int i = 0; i < bytes.Length; i++)  
                {  
                    builder.Append(bytes[i].ToString("x2"));  
                }  
                return builder.ToString();  
            }  
        }  
    }
}
