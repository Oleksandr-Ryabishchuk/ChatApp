using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.HubConfig
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CommonHub: Hub
    {
        private readonly ApplicationDbContext _context;

        public CommonHub(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task SendMessage(string receiver, string message)
        {
            var senderUsername = Context?.User?.Claims.FirstOrDefault().Value;
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

        
        public override async Task OnConnectedAsync()
        {
            var username = Context?.User?.Identity?.Name;

            if (username is null)
            {
                throw new HubException("Unauthorized");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User.Identity.Name;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
