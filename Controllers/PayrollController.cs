// Create a new file: PayrollController.cs
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Data;
using System;
using System.Threading.Tasks;
using GreenMeadowsPortal.Services; // Add this using directive


namespace GreenMeadowsPortal.Controllers
{
    [Route("Payroll")] // Add proper route attribute
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
            AppDbContext context)
        {
            _userManager = userManager;
            _payrollService = payrollService;
            _timeService = timeService;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
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

        // Include the other methods from the nested controller
        [HttpGet]
        [Route("CreatePayPeriod")]
        public IActionResult CreatePayPeriod()
        {
            var model = new CreatePayPeriodViewModel
            {
                StartDate = DateTime.Today.AddDays(-14),
                EndDate = DateTime.Today
            };

            return View(model);
        }
        [HttpGet]
        [Route("PayPeriodDetails/{id}")]
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
        [Route("CreatePayPeriod")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayPeriod(CreatePayPeriodViewModel model)
        {
            // Rest of your method
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

        // Add the remaining methods...
    }
}