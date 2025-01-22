using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using wu2.Models;
using WebGrease;
using System.Net.Http;
using System.Web.Mvc;

namespace wu2
{
    public class chatHub : Hub
    {
        wuEntities1 db=new wuEntities1();
        private static readonly HttpClient client = new HttpClient();
        public void BroadcastMessage(int groupId, int userId, string message, string timestamp, string userName)
        {
            try
            {
                Console.WriteLine($"Broadcasting message from user {userName} (ID: {userId}) in group {groupId}: {message} at {timestamp}");
                Clients.Group(groupId.ToString()).addMessage(userName, message, timestamp, groupId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting message: {ex.Message}");
                throw;
            }
        }

        public Task JoinGroup(int groupId)
        {
            return Groups.Add(Context.ConnectionId, groupId.ToString());
        }

        public Task LeaveGroup(int groupId)
        {
            return Groups.Remove(Context.ConnectionId, groupId.ToString());
        }
    }
}