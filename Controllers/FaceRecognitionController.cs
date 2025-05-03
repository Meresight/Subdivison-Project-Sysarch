// Controllers/FaceRecognitionController.cs
using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Services; // Add this for TimeTrackingService and PayrollService





[Authorize(Roles = "Admin,Staff")]
public class FaceRecognitionController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly FaceRecognitionService _faceService;
    private readonly TimeTrackingService _timeService;
    private readonly ILogger<FaceRecognitionController> _logger;
    private readonly IWebHostEnvironment _hostEnvironment;

    public FaceRecognitionController(
        UserManager<ApplicationUser> userManager,
        FaceRecognitionService faceService,
        TimeTrackingService timeService,
        ILogger<FaceRecognitionController> logger,
        IWebHostEnvironment hostEnvironment)
    {
        _userManager = userManager;
        _faceService = faceService;
        _timeService = timeService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Enroll()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Admin") && !roles.Contains("Staff"))
        {
            return Forbid();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(IFormFile faceImage)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        if (faceImage == null || faceImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select an image to enroll.";
            return View();
        }

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await faceImage.CopyToAsync(memoryStream);
                var result = await _faceService.EnrollUserFaceAsync(user.Id, memoryStream.ToArray());

                if (result)
                {
                    TempData["SuccessMessage"] = "Face enrolled successfully.";
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to enroll face. Please try again.";
                    return View();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling face for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "An error occurred during enrollment.";
            return View();
        }
    }

    [HttpGet]
    public IActionResult TimeTracking()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(IFormFile faceImage)
    {

        _logger.LogInformation("Attempting check-in for user with image of size: {Size}",
       faceImage?.Length ?? 0);
        _logger.LogInformation("Face recognition check-in process started for user");

        if (faceImage == null || faceImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Please provide a face image for check-in.";
            return RedirectToAction("TimeTracking");
        }

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await faceImage.CopyToAsync(memoryStream);
                var (success, userId) = await _faceService.IdentifyFaceAsync(memoryStream.ToArray());
                _logger.LogInformation("Face recognition result: {Success}, User ID: {UserId}", success, userId);

                if (success)
                {
                    // Get user's IP address or location
                    string location = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                    // Record time
                    await _timeService.CheckInAsync(userId, "FaceRecognition", location);

                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction("TimeTracking");
                    }

                    TempData["SuccessMessage"] = $"Check-in successful for {user.FirstName} {user.LastName}.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Face not recognized. Please try again or contact an administrator.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in");
            TempData["ErrorMessage"] = "An error occurred during check-in.";
        }

        return RedirectToAction("TimeTracking");
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(IFormFile faceImage)
    {
        _logger.LogInformation("Attempting check-in for user with image of size: {Size}",
       faceImage?.Length ?? 0);
        _logger.LogInformation("Face recognition check-in process started for user");

        if (faceImage == null || faceImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Please provide a face image for check-out.";
            return RedirectToAction("TimeTracking");
        }

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await faceImage.CopyToAsync(memoryStream);
                var (success, userId) = await _faceService.IdentifyFaceAsync(memoryStream.ToArray());
                _logger.LogInformation("Face recognition result: {Success}, User ID: {UserId}", success, userId);



                if (success)
                {
                    // Get user's IP address or location
                    string location = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                    // Record time
                    await _timeService.CheckOutAsync(userId, "FaceRecognition", location);

                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null) // Ensure user is not null
                    {
                        TempData["SuccessMessage"] = $"Check-out successful for {user.FirstName} {user.LastName}.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Face not recognized. Please try again or contact an administrator.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-out");
            TempData["ErrorMessage"] = "An error occurred during check-out.";
        }

        return RedirectToAction("TimeTracking");
    }


    // Controllers/PayrollController.cs
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PayrollService _payrollService;
        private readonly TimeTrackingService _timeService;
        private readonly ILogger<PayrollController> _logger;
        private readonly AppDbContext _context;

        public PayrollController(
            UserManager<ApplicationUser> userManager,
            PayrollService payrollService,
            TimeTrackingService timeService,
            ILogger<PayrollController> logger,
    AppDbContext context) // Change the type here
        {
            _userManager = userManager;
            _payrollService = payrollService;
            _timeService = timeService;
            _logger = logger;
            _context = context; // Assign the context
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var model = new PayrollViewModel
            {
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "Admin",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                PayPeriods = await _context.PayPeriods
                    .OrderByDescending(p => p.EndDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreatePayPeriod()
        {
            var model = new CreatePayPeriodViewModel
            {
                StartDate = DateTime.Today.AddDays(-14), // Default to 2 weeks ago
                EndDate = DateTime.Today
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayPeriod(CreatePayPeriodViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var payPeriod = await _payrollService.CreatePayPeriodAsync(model.StartDate, model.EndDate);
                TempData["SuccessMessage"] = "Pay period created successfully.";
                return RedirectToAction("PayPeriodDetails", new { id = payPeriod.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pay period");
                ModelState.AddModelError("", "An error occurred while creating the pay period.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayPeriodDetails(int id)
        {
            var payPeriod = await _context.PayPeriods
                .Include(p => p.EmployeePayments)
                .ThenInclude(ep => ep.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payPeriod == null)
            {
                return NotFound();
            }

            var model = new PayPeriodDetailsViewModel
            {
                PayPeriod = payPeriod,
                StaffMembers = await _userManager.GetUsersInRoleAsync("Staff")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayroll(int payPeriodId)
        {
            try
            {
                var payments = await _payrollService.ProcessAllPaymentsAsync(payPeriodId);
                TempData["SuccessMessage"] = $"Processed payroll for {payments.Count()} employees.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payroll for period {PayPeriodId}", payPeriodId);
                TempData["ErrorMessage"] = "An error occurred while processing the payroll.";
            }

            return RedirectToAction("PayPeriodDetails", new { id = payPeriodId });
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeTimeRecords(string userId, DateTime startDate, DateTime endDate)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var timeRecords = await _context.TimeRecords
                .Where(tr => tr.UserId == userId &&
                             tr.CheckInTime >= startDate &&
                             tr.CheckInTime <= endDate)
                .OrderBy(tr => tr.CheckInTime)
                .ToListAsync();

            var model = new EmployeeTimeRecordsViewModel
            {
                User = user,
                StartDate = startDate,
                EndDate = endDate,
                TimeRecords = timeRecords
            };

            return View(model);
        }
    }
}