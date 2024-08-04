using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.HubConfig
{
    public class CommonHub: Hub
    {
        private readonly ApplicationDbContext _context;

        public CommonHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiver, string message)
        {
            var senderUsername = Context?.User?.Identity?.Name;
            var sender = _context.Users.SingleOrDefault(u => u.Username == senderUsername);
            var receiverUser = _context.Users.SingleOrDefault(u => u.Username == receiver);

            if (sender != null && receiverUser != null)
            {
                var newMessage = new Message
                {
                    Content = message,
                    Timestamp = DateTime.Now,
                    SenderId = sender.Id,
                    ReceiverId = receiverUser.Id
                };

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();

                await Clients.User(receiver).SendAsync("ReceiveMessage", sender.Username, message);
            }
        }

        public override Task OnConnectedAsync()
        {
            var username = Context?.User?.Identity?.Name;
            Groups.AddToGroupAsync(Context.ConnectionId, username);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User.Identity.Name;
            Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
