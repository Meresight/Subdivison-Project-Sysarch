using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize] // Ensures only authenticated users can access
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AnnouncementService _announcementService;
        private readonly NotificationService _notificationService;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            AnnouncementService announcementService,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _announcementService = announcementService;
            _notificationService = notificationService;
        }

        // Default action - redirect to appropriate dashboard based on role
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("AdminDashboard");
            else if (roles.Contains("Staff"))
                return RedirectToAction("StaffDashboard");
            else
                return RedirectToAction("HomeownerDashboard");
        }

        // ✅ Profile View for Dashboard
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var profileModel = new ProfileViewModel
            {
                FullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim(),
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? "No address available",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                MemberSince = user.MemberSince != default ? user.MemberSince.ToString("MMMM yyyy") : "Unknown",
                Status = user.Status ?? "Unknown",
                Role = roles.FirstOrDefault() ?? "User"
            };

            if (profileModel == null)
            {
                TempData["ErrorMessage"] = "Failed to load profile data.";
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Dashboard/Profile.cshtml", profileModel);
        }

        // ✅ Homeowner Dashboard
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> HomeownerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var model = new HomeownerDashboardViewModel
            {
                HomeownerUser = user,
                FirstName = user.FirstName ?? "Homeowner",
                Role = roles.FirstOrDefault() ?? "Homeowner",
                TotalPropertiesOwned = 2,
                PendingRequests = 3,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            // Add notification count - null check to avoid dereference of possibly null reference (line 276)
            model.NotificationCount = user != null
                ? await _notificationService.GetUnreadCountAsync(user.Id)
                : 0;

            // Load recent announcements - proper null handling to fix line 278
            var recentAnnouncements = await _announcementService.GetRecentAnnouncementsAsync(3);
            // Explicit null check instead of null-coalescing operator to handle type compatibility
            model.RecentAnnouncements = recentAnnouncements != null
                ? recentAnnouncements
                : new List<AnnouncementDetailsViewModel>();

            return View(model);
        }

        // ✅ Admin Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var model = new AdminDashboardViewModel
            {
                AdminUser = user,
                FirstName = user.FirstName ?? "Admin",
                Role = roles.FirstOrDefault() ?? "Admin",
                TotalUsers = 150,
                ActiveReservations = 20,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            // Add notification count - null check to avoid dereference of possibly null reference
            model.NotificationCount = user != null
                ? await _notificationService.GetUnreadCountAsync(user.Id)
                : 0;

            return View(model);
        }

        // ✅ Staff Dashboard
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var model = new StaffDashboardViewModel
            {
                StaffUser = user,
                FirstName = user.FirstName ?? "Staff",
                Role = roles.FirstOrDefault() ?? "Staff",
                NotificationCount = user != null
                    ? await _notificationService.GetUnreadCountAsync(user.Id)
                    : 0,
                TotalResidents = 100,
                PendingRequests = 10,
                ProfileImageUrl = user?.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            return View(model);
        }

        // Redirects to the Announcement controller's Index action instead of having a duplicate view
        public IActionResult Announcement()
        {
            return RedirectToAction("Index", "Announcement");
        }

        // ✅ Billing
        [Authorize]
        public async Task<IActionResult> Billing(int? year)
        {
            // Redirect to the role-specific billing page instead of showing content directly
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminBilling", new { year });
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("StaffBilling", new { year });
            }
            else
            {
                return RedirectToAction("HomeownerBilling", new { year });
            }
        }

        // Helper methods to populate role-specific billing data
        private Task PopulateAdminBillingData(BillingViewModel model, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        private Task PopulateHomeownerBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }

            if (model.CurrentBilling?.DueDate != null && model.CurrentBilling.DueDate != default)
            {
                var daysUntil = (model.CurrentBilling.DueDate - DateTime.Now).Days;
                model.DaysUntilDue = daysUntil > 0 ? daysUntil : 0;
            }
            else
            {
                model.DaysUntilDue = 0;
            }

            return Task.CompletedTask;
        }

        private Task PopulateStaffBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        private Task PopulateDefaultBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        // Add this to your DashboardController.cs
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                // Fix for line 387 - null check before GetRolesAsync
                var roles = user != null
                    ? await _userManager.GetRolesAsync(user)
                    : new List<string>();

                // Create a simplified model - we'll bypass complex models for now
                var viewModel = new UserManagementViewModel
                {
                    FirstName = user?.FirstName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Admin",
                    NotificationCount = user != null ? await _notificationService.GetUnreadCountAsync(user.Id) : 0,
                    CurrentUserProfileImageUrl = user?.ProfileImageUrl ?? "/images/default-avatar.png",
                    Users = new List<UserViewModel>(), // Initialize required property
                    PendingUsers = new List<PendingUserViewModel>(), // Initialize required property
                    Roles = new List<RoleViewModel>(), // Initialize required property
                    ActivityLogs = new List<ActivityLogViewModel>(), // Initialize required property
                    CurrentPage = 1,
                    TotalPages = 1,
                    TotalRecords = 0,
                    PageStartRecord = 1,
                    PageEndRecord = 0,
                    PendingRequestsCount = 0,
                    LogsCurrentPage = 1,
                    LogsTotalPages = 1,
                    LogsPageStartRecord = 1,
                    LogsPageEndRecord = 0,
                    TotalLogs = 0
                };

                // DIRECTLY fetch users without using the service
                var allUsers = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserViewModel>();

                foreach (var u in allUsers)
                {
                    // Skip null users
                    if (u == null) continue;

                    var userRoles = await _userManager.GetRolesAsync(u);
                    userViewModels.Add(new UserViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Address = u.Address ?? "",
                        Role = userRoles.FirstOrDefault() ?? "User",
                        Status = u.Status ?? "Active",
                        MemberSince = u.MemberSince != default ? u.MemberSince.ToString("MMM dd, yyyy") : "Unknown",
                        LastLogin = u.LastLoginDate.HasValue ?
                            u.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never",
                        ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                        PropertyType = u.PropertyType ?? "Unknown", // Added to fix CS9035
                        OwnershipStatus = u.OwnershipStatus ?? "Unknown" // Added to fix CS
                    });
                }

                viewModel.Users = userViewModels;

                // Simplified for now
                viewModel.CurrentPage = 1;
                viewModel.TotalPages = 1;
                viewModel.TotalRecords = userViewModels.Count;
                viewModel.PageStartRecord = 1;
                viewModel.PageEndRecord = userViewModels.Count;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Return a simple model with error information
                return View(new UserManagementViewModel
                {
                    FirstName = "Error occurred: " + ex.Message,
                    Role = "Admin",
                    Users = new List<UserViewModel>(),
                    PendingUsers = new List<PendingUserViewModel>(), // Initialize required property
                    Roles = new List<RoleViewModel>(), // Initialize required property
                    ActivityLogs = new List<ActivityLogViewModel>(), // Initialize required property
                    CurrentPage = 1,
                    TotalPages = 1,
                    TotalRecords = 0,
                    PageStartRecord = 1,
                    PageEndRecord = 0,
                    PendingRequestsCount = 0,
                    LogsCurrentPage = 1,
                    LogsTotalPages = 1,
                    LogsPageStartRecord = 1,
                    LogsPageEndRecord = 0,
                    TotalLogs = 0
                });
            }
        }
        // Add this to your DashboardController.cs

        // This method will handle redirecting users to the appropriate billing page based on role
        [Authorize]
        public async Task<IActionResult> BillingRedirect()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminBilling");
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("StaffBilling");
            }
            else
            {
                return RedirectToAction("HomeownerBilling");
            }
        }
        // Add these methods to your DashboardController.cs

        // Method to prepare Admin billing view model
        private async Task<BillingViewModel> PrepareAdminBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Admin",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            // Fix for line 411 - null check before GetRolesAsync is now handled with the early return above
            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Admin",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate admin-specific billing data
            await PopulateAdminBillingData(model, selectedYear);

            return model;
        }

        // Method to prepare Staff billing view model
        private async Task<BillingViewModel> PrepareStaffBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Staff",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            // Fix for line 435 - null check before GetRolesAsync is now handled with the early return above
            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Staff",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate staff-specific billing data
            await PopulateStaffBillingData(model, user.Id, selectedYear);

            return model;
        }

        // Method to prepare Homeowner billing view model
        private async Task<BillingViewModel> PrepareHomeownerBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Homeowner",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Homeowner",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate homeowner-specific billing data
            await PopulateHomeownerBillingData(model, user.Id, selectedYear);

            return model;
        }
        // Create specific billing actions for each role
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminBilling(int? year)
        {
            // Admin-specific billing view
            var model = await PrepareAdminBillingViewModel(year);
            return View("AdminBilling", model);
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffBilling(int? year)
        {
            // Staff-specific billing view
            var model = await PrepareStaffBillingViewModel(year);
            return View("StaffBilling", model);
        }

        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> HomeownerBilling(int? year)
        {
            // Homeowner-specific billing view
            var model = await PrepareHomeownerBillingViewModel(year);
            return View("Billing", model);
        }

        // Then update your sidebar navigation in all layouts to use BillingRedirect
        // <a asp-controller="Dashboard" asp-action="BillingRedirect">
        //    <i class="fas fa-file-invoice-dollar"></i> Billing
        // </a>
        // ✅ Logout Method
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}