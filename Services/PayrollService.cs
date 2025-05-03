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
    public class PayrollService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PayrollService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TimeTrackingService _timeTrackingService;

        public PayrollService(
            AppDbContext context,
            ILogger<PayrollService> logger,
            UserManager<ApplicationUser> userManager,
            TimeTrackingService timeTrackingService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _timeTrackingService = timeTrackingService;
        }

        public async Task<PayPeriod> CreatePayPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var payPeriod = new PayPeriod
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = "Open"
            };

            _context.PayPeriods.Add(payPeriod);
            await _context.SaveChangesAsync();
            return payPeriod;
        }

        public async Task<EmployeePayment> ProcessEmployeePaymentAsync(string userId, int payPeriodId, decimal hourlyRate = 0, decimal dailyRate = 0)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found", nameof(userId));
            }

            var payPeriod = await _context.PayPeriods.FindAsync(payPeriodId);
            if (payPeriod == null)
            {
                throw new ArgumentException("Pay period not found", nameof(payPeriodId));
            }

            // Calculate days worked and hours
            int daysWorked = await _timeTrackingService.GetDaysWorkedAsync(
                userId, payPeriod.StartDate, payPeriod.EndDate);

            decimal totalHours = await _timeTrackingService.GetTotalHoursAsync(
                userId, payPeriod.StartDate, payPeriod.EndDate);

            // Default to 8 hours per day for regular time
            decimal regularHours = daysWorked * 8;
            decimal overtimeHours = Math.Max(0, totalHours - regularHours);

            // Calculate pay based on provided rates or stored employee rates
            decimal grossPay = 0;
            if (dailyRate > 0)
            {
                grossPay = daysWorked * dailyRate;
            }
            else if (hourlyRate > 0)
            {
                // Simplified calculation - you might have different rates for regular vs overtime
                grossPay = (regularHours * hourlyRate) + (overtimeHours * hourlyRate * 1.5m);
            }

            // Simplified - in real life you'd have tax and benefit deductions
            decimal deductions = 0;
            decimal netPay = grossPay - deductions;

            var payment = new EmployeePayment
            {
                UserId = userId,
                User = user,
                PayPeriodId = payPeriodId,
                PayPeriod = payPeriod,
                DaysWorked = daysWorked,
                TotalHours = totalHours,
                RegularHours = regularHours,
                OvertimeHours = overtimeHours,
                GrossPay = grossPay,
                Deductions = deductions,
                NetPay = netPay,
                ProcessedDate = DateTime.Now,
                Status = "Pending"
            };

            _context.EmployeePayments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task<List<EmployeePayment>> ProcessAllPaymentsAsync(int payPeriodId)
        {
            var payPeriod = await _context.PayPeriods.FindAsync(payPeriodId);
            if (payPeriod == null)
            {
                throw new ArgumentException("Pay period not found", nameof(payPeriodId));
            }

            // Get all staff users
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var payments = new List<EmployeePayment>();

            foreach (var user in staffUsers)
            {
                // You would get the employee's hourly or daily rate from their profile
                // For this example, we're using a fixed rate
                decimal dailyRate = 500.00m; // Example daily rate

                var payment = await ProcessEmployeePaymentAsync(user.Id, payPeriodId, 0, dailyRate);
                payments.Add(payment);
            }

            // Mark pay period as processing
            payPeriod.Status = "Processing";
            await _context.SaveChangesAsync();

            return payments;
        }
    }
}