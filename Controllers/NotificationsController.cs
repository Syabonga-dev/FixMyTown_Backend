using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Common;
using FixMyTownApi.Models.Dtos.Notifications;
using FixMyTownApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Controllers
{

    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db) => _db = db;

      
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<NotificationReadDto>>> GetMine()
        {
            var userId = User.CurrentUserId();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new NotificationReadDto
                {
                    NotificationID = n.NotificationId,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        
        [HttpGet("unread-count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            var userId = User.CurrentUserId();
            var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(new { count });
        }

        
        [HttpPost("{id:int}/read")]
        public async Task<ActionResult<ApiMessageDto>> MarkAsRead(int id)
        {
            var userId = User.CurrentUserId();
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
                return NotFound(new ApiMessageDto("Notification not found."));

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new ApiMessageDto("Notification marked as read."));
        }

       
        [HttpPost("read-all")]
        public async Task<ActionResult<ApiMessageDto>> MarkAllAsRead()
        {
            var userId = User.CurrentUserId();
            var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new ApiMessageDto("All notifications marked as read."));
        }
    }
}
