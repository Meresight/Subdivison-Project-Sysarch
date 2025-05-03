// Services/FaceRecognitionService.cs
using Microsoft.EntityFrameworkCore; // Add this using directive
using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

public class FaceRecognitionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FaceRecognitionService> _logger;
    // You'll need to add a reference to your facial recognition library/API of choice
    // private readonly IFaceRecognitionClient _faceClient;

    public FaceRecognitionService(
        AppDbContext context,
        ILogger<FaceRecognitionService> logger)
    {
        _context = context;
        _logger = logger;
        // Initialize your face recognition client
    }

    public async Task<bool> IsUserEnrolledAsync(string userId)
    {
        try
        {
            return await _context.FaceEnrollments
                .AnyAsync(e => e.UserId == userId && e.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is enrolled");
            return false;
        }
    }
    public async Task<bool> EnrollUserFaceAsync(string userId, byte[] faceImageData)
    {
        try
        {
            // Check if user is already enrolled
            var existingEnrollment = await _context.FaceEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

            if (existingEnrollment != null)
            {
                _logger.LogInformation("User {UserId} already has an active face enrollment", userId);
                return false; // User already enrolled
            }

            // Process the face image to extract features
            var faceFeatures = await ProcessFaceImage(faceImageData);

            // Get the user from the database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            // Create a new face enrollment
            var enrollment = new FaceEnrollment
            {
                UserId = userId,
                User = user,
                FaceDataJson = JsonSerializer.Serialize(faceFeatures),
                EnrollmentDate = DateTime.Now,
                IsActive = true
            };

            // Add to database and save
            _context.FaceEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully enrolled face for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling face for user {UserId}", userId);
            return false;
        }
    }

    public async Task<(bool Success, string UserId)> IdentifyFaceAsync(byte[] faceImageData)
    {
        try
        {
            // Process the image to extract facial features
            var faceFeatures = await ProcessFaceImage(faceImageData);

            // Get all active face enrollments
            var enrollments = await _context.FaceEnrollments
                .Where(e => e.IsActive)
                .ToListAsync();

            // Compare against stored enrollments
            foreach (var enrollment in enrollments)
            {
                var storedFeatures = JsonSerializer.Deserialize<object>(enrollment.FaceDataJson);

                if (storedFeatures != null) // Ensure storedFeatures is not null
                {
                    bool isMatch = CompareFeatures(faceFeatures, storedFeatures);

                    if (isMatch)
                    {
                        return (true, enrollment.UserId);
                    }
                }
            }

            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying face");
            return (false, string.Empty);
        }
    }


    // Placeholder methods - would be implemented with your face recognition library
    private async Task<object> ProcessFaceImage(byte[] imageData)
    {
        // Simple placeholder implementation until you integrate a real face recognition API
        await Task.Delay(100); // Simulate some processing time

        // Return a simple object with basic face data
        return new
        {
            faceId = Guid.NewGuid().ToString(),
            faceFeatures = new Dictionary<string, double> {
            { "confidence", 0.95 },
            { "quality", 0.8 }
        }
        };
    }

    private bool CompareFeatures(object features1, object features2)
    {
        // Simplified matching that always returns true for demo
        // In production, you'd compare actual facial features
        return true;
    }

    // Services/TimeTrackingService.cs
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

        // Updated CheckInAsync method in TimeTrackingService
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
                User = user, // Set the required 'User' property
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

    // Services/PayrollService.cs
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
                User = user, // Set the required 'User' property
                PayPeriodId = payPeriodId,
                PayPeriod = payPeriod, // Set the required 'PayPeriod' property
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