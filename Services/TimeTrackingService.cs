using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class TimeTrackingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TimeTrackingService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimeTrackingService(
            AppDbContext context,
            ILogger<TimeTrackingService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<TimeRecord> CheckInAsync(string userId, string method, string location)
        {
            // Check if user already has an open time record
            var openRecord = await _context.TimeRecords
                .Where(tr => tr.UserId == userId && !tr.CheckOutTime.HasValue)
                .FirstOrDefaultAsync();

            if (openRecord != null)
            {
                _logger.LogWarning("User {UserId} already has an open time record", userId);
                return openRecord;
            }

            // Retrieve the user from the database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            // Create new time record
            var timeRecord = new TimeRecord
            {
                UserId = userId,
                User = user,
                CheckInTime = DateTime.Now,
                CheckInMethod = method,
                CheckInLocation = location
            };

            _context.TimeRecords.Add(timeRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} checked in at {Time}", userId, timeRecord.CheckInTime);
            return timeRecord;
        }

        public async Task<TimeRecord> CheckOutAsync(string userId, string method, string location)
        {
            // Find the user's open time record
            var openRecord = await _context.TimeRecords
                .Where(tr => tr.UserId == userId && !tr.CheckOutTime.HasValue)
                .FirstOrDefaultAsync();

            if (openRecord == null)
            {
                _logger.LogWarning("User {UserId} has no open time record to check out from", userId);
                throw new InvalidOperationException("No open time record found for check-out");
            }

            // Update with check-out info
            openRecord.CheckOutTime = DateTime.Now;
            openRecord.CheckOutMethod = method;
            openRecord.CheckOutLocation = location;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} checked out at {Time}", userId, openRecord.CheckOutTime);
            return openRecord;
        }

        // Get total work time for a pay period
        public async Task<decimal> GetTotalHoursAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var records = await _context.TimeRecords
                .Where(tr => tr.UserId == userId &&
                            tr.CheckInTime >= startDate &&
                            tr.CheckInTime < endDate &&
                            tr.CheckOutTime.HasValue)
                .ToListAsync();

            return (decimal)records.Sum(r => r.Duration);
        }

        // Get days worked in a period
        public async Task<int> GetDaysWorkedAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeRecords
                .Where(tr => tr.UserId == userId &&
                            tr.CheckInTime >= startDate &&
                            tr.CheckInTime < endDate)
                .Select(tr => tr.CheckInTime.Date)
                .Distinct()
                .CountAsync();
        }
    }
}