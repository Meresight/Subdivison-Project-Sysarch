using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserService _userService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<UserManagementController> _logger;
        private readonly NotificationService _notificationService;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            UserService userService,
            IWebHostEnvironment hostEnvironment,
            ILogger<UserManagementController> logger,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userService = userService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _notificationService = notificationService;
        }

        // GET: /UserManagement
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var roles = await _userManager.GetRolesAsync(user);

                // Prepare the view model
                var viewModel = new UserManagementViewModel
                {
                    FirstName = user.FirstName ?? "Admin",
                    Role = roles.FirstOrDefault() ?? "Admin",
                    NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                    CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    Users = new List<UserViewModel>(),
                    CurrentPage = page,
                    TotalPages = 1,
                    PageStartRecord = 0,
                    PageEndRecord = 0,
                    TotalRecords = 0,
                    Roles = await GetAllRolesAsync(),
                    PendingUsers = new List<PendingUserViewModel>(),
                    ActivityLogs = new List<ActivityLogViewModel>()
                };

                // Directly query users
                var users = await _userManager.Users
                    .OrderBy(u => u.Email)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                foreach (var u in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);

                    viewModel.Users.Add(new UserViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Address = u.Address ?? "",
                        Role = userRoles.FirstOrDefault() ?? "User",
                        Status = u.Status ?? "Active",
                        MemberSince = u.MemberSince.ToString("MMM dd, yyyy"),
                        LastLogin = u.LastLoginDate.HasValue ? u.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never",
                        ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                        PropertyType = u.PropertyType ?? "Unknown", // Added to fix CS9035
                        OwnershipStatus = u.OwnershipStatus ?? "Unknown" // Added to fix CS9035
                    });

                }

                // Calculate pagination
                viewModel.TotalRecords = await _userManager.Users.CountAsync();
                viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalRecords / (double)pageSize);
                viewModel.PageStartRecord = viewModel.TotalRecords == 0 ? 0 : ((page - 1) * pageSize) + 1;
                viewModel.PageEndRecord = Math.Min(page * pageSize, viewModel.TotalRecords);

                return View("~/Views/UserManagement/AddUser.cshtml", viewModel);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
            }

            {
                // Log the exception
                return View(new UserManagementViewModel
                {
                    FirstName = "Error",
                    Role = "Admin",
                    Users = new List<UserViewModel>(),
                    PendingUsers = new List<PendingUserViewModel>(), // Fix for CS9035
                    Roles = new List<RoleViewModel>(), // Fix for CS9035
                    ActivityLogs = new List<ActivityLogViewModel>() // Fix for CS9035
                });

            }
        }

        private async Task<List<RoleViewModel>> GetAllRolesAsync()
        {
            var roles = _roleManager.Roles.ToList();
            var roleViewModels = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                if (role.Name != null) // Ensure role.Name is not null
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);

                    // Create a simplified permissions list
                    var permissions = new List<PermissionViewModel>
            {
                new PermissionViewModel
                {
                    Id = "1",
                    Name = "View",
                    Category = "Basic",
                    IsGranted = true
                }
            };

                    roleViewModels.Add(new RoleViewModel
                    {
                        Name = role.Name,
                        UserCount = usersInRole.Count,
                        Permissions = permissions
                    });
                }
            }

            return roleViewModels;
        }

        // GET: /UserManagement/AddUser
        [HttpGet]
        public async Task<IActionResult> AddUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new UserFormViewModel
            {
                CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}",
                CurrentRole = roles.FirstOrDefault() ?? "Admin",
                CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" }
            };

            return View(viewModel);
        }

        // POST: /UserManagement/SaveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUser(UserFormViewModel model)
        {
            try
            {
                _logger.LogInformation($"Received form data: ID={model.Id}, Email={model.Email}, Role={model.Role}");

                // Clear validations for optional fields
                ModelState.Remove("Id");
                ModelState.Remove("ForcePasswordChange");
                ModelState.Remove("SendCredentials");
                ModelState.Remove("ProfileImage");
                ModelState.Remove("PropertyDocuments");
                ModelState.Remove("ResidentCount"); // Remove validation for this field

                _logger.LogInformation($"Received form with Role: {model.Role}");

                // Remove validations for role-specific fields
                if (!string.IsNullOrEmpty(model.Role))
                {
                    if (model.Role == "Staff")
                    {
                        ModelState.Remove("EmergencyContactName");
                        ModelState.Remove("EmergencyContactPhone");
                        ModelState.Remove("VehicleInfo");
                        ModelState.Remove("ResidentCount");
                        ModelState.Remove("MoveInDate");
                    }
                    else if (model.Role == "Homeowner")
                    {
                        ModelState.Remove("Department");
                        ModelState.Remove("Position");
                        ModelState.Remove("EmployeeId");
                    }
                    else
                    {
                        // For Admin, remove both sets of fields
                        ModelState.Remove("EmergencyContactName");
                        ModelState.Remove("EmergencyContactPhone");
                        ModelState.Remove("VehicleInfo");
                        ModelState.Remove("ResidentCount");
                        ModelState.Remove("MoveInDate");
                        ModelState.Remove("Department");
                        ModelState.Remove("Position");
                        ModelState.Remove("EmployeeId");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        var roles = await _userManager.GetRolesAsync(currentUser);
                        model.CurrentUserName = $"{currentUser.FirstName ?? ""} {currentUser.LastName ?? ""}";
                        model.CurrentRole = roles.FirstOrDefault() ?? "Admin";
                        model.CurrentUserProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png";
                        model.NotificationCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                    }

                    model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };

                    TempData["ErrorMessage"] = "Please fix the validation errors.";
                    return View("AddUser", model);
                }

                // Process profile image if one was uploaded
                string? profileImageUrl = null;
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    profileImageUrl = await ProcessProfileImageUpload(model.ProfileImage);
                }

                if (string.IsNullOrEmpty(model.Id))
                {
                    // Creating a new user
                    _logger.LogInformation("Creating new user with email: " + model.Email);

                    var newUser = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address ?? string.Empty,
                        Unit = model.Unit ?? string.Empty,
                        PropertyType = model.PropertyType ?? string.Empty,
                        OwnershipStatus = model.OwnershipStatus ?? string.Empty,
                        Department = model.Department ?? string.Empty,
                        Position = model.Position ?? string.Empty,
                        EmployeeId = model.EmployeeId ?? string.Empty,
                        MoveInDate = model.MoveInDate,
                        EmergencyContactName = model.Role != "Homeowner" ? "N/A" : (model.EmergencyContactName ?? "N/A"),
                        EmergencyContactPhone = model.Role != "Homeowner" ? "N/A" : (model.EmergencyContactPhone ?? "N/A"),
                        VehicleInfo = model.Role != "Homeowner" ? "N/A" : (model.VehicleInfo ?? "N/A"),
                        ResidentCount = model.ResidentCount ?? 0,
                        Status = model.Status,
                        MemberSince = DateTime.Now,
                        ProfileImageUrl = profileImageUrl ?? "/images/default-avatar.png",
                        ReceiveEmailNotifications = model.ReceiveNotifications,
                        ReceiveSmsNotifications = model.ReceiveSMS,
                        Notes = model.Notes ?? string.Empty,
                        ForcePasswordChange = model.ForcePasswordChange,
                        DateOfBirth = model.DateOfBirth
                    };

                    var result = await _userManager.CreateAsync(newUser, model.Password);

                    if (result.Succeeded)
                    {
                        // Assign role
                        await _userManager.AddToRoleAsync(newUser, model.Role);

                        // Process property documents if any
                        if (model.PropertyDocuments != null && model.PropertyDocuments.Count > 0)
                        {
                            await ProcessPropertyDocumentsUpload(newUser.Id, model.PropertyDocuments);
                        }

                        // Send credentials email if requested
                        if (model.SendCredentials)
                        {
                            await SendAccountCredentialsEmail(newUser, model.Password);
                        }

                        // Log user creation
                        // Ensure `creatorUser` is not null before accessing its properties
                        var creatorUser = await _userManager.GetUserAsync(User);
                        if (creatorUser == null)
                        {
                            TempData["ErrorMessage"] = "Current user could not be identified.";
                            return RedirectToAction("Index");
                        }

                        // Log user creation
                        await _userService.LogActivityAsync(
                            creatorUser.Id,
                            "user-created",
                            $"Created new user: {newUser.Email}",
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                        );

                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        TempData["ErrorMessage"] = "Failed to create user. See error details below.";
                    }
                }
                else
                {
                    // Update existing user
                    _logger.LogInformation("Updating existing user with ID: " + model.Id);

                    var existingUser = await _userManager.FindByIdAsync(model.Id);
                    if (existingUser == null)
                    {
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction("Index");
                    }

                    // Update user properties
                    existingUser.FirstName = model.FirstName;
                    existingUser.LastName = model.LastName;
                    existingUser.PhoneNumber = model.PhoneNumber;
                    existingUser.Address = model.Address;
                    existingUser.Unit = model.Unit;
                    existingUser.PropertyType = model.PropertyType;
                    existingUser.OwnershipStatus = model.OwnershipStatus;
                    existingUser.Department = model.Department ?? string.Empty;
                    existingUser.Position = model.Position ?? string.Empty;
                    existingUser.EmployeeId = model.EmployeeId ?? string.Empty;
                    existingUser.MoveInDate = model.MoveInDate;
                    existingUser.EmergencyContactName = model.EmergencyContactName;
                    existingUser.EmergencyContactPhone = model.EmergencyContactPhone;
                    existingUser.VehicleInfo = model.VehicleInfo;
                    existingUser.ResidentCount = model.ResidentCount;
                    existingUser.Status = model.Status;
                    existingUser.ReceiveEmailNotifications = model.ReceiveNotifications;
                    existingUser.ReceiveSmsNotifications = model.ReceiveSMS;
                    existingUser.Notes = model.Notes;
                    existingUser.DateOfBirth = model.DateOfBirth;
                    existingUser.ForcePasswordChange = model.ForcePasswordChange;

                    // Update profile image if a new one was uploaded
                    if (!string.IsNullOrEmpty(profileImageUrl))
                    {
                        existingUser.ProfileImageUrl = profileImageUrl;
                    }

                    // Update user in the database
                    var updateResult = await _userManager.UpdateAsync(existingUser);
                    if (updateResult.Succeeded)
                    {
                        // Update user's role if it has changed
                        var currentRoles = await _userManager.GetRolesAsync(existingUser);
                        var currentRole = currentRoles.FirstOrDefault();

                        if (currentRole != model.Role)
                        {
                            if (currentRole != null)
                            {
                                await _userManager.RemoveFromRoleAsync(existingUser, currentRole);
                            }
                            await _userManager.AddToRoleAsync(existingUser, model.Role);
                        }

                        // Update password if a new one was provided
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                            var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, model.Password);

                            if (!passwordResult.Succeeded)
                            {
                                foreach (var error in passwordResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                            }
                        }

                        // Process property documents if any
                        if (model.PropertyDocuments != null && model.PropertyDocuments.Count > 0)
                        {
                            await ProcessPropertyDocumentsUpload(existingUser.Id, model.PropertyDocuments);
                        }

                        // Log user update
                        // Log user update
                        var updaterUser = await _userManager.GetUserAsync(User);
                        if (updaterUser == null)
                        {
                            TempData["ErrorMessage"] = "Current user could not be identified.";
                            return RedirectToAction("Index");
                        }
                        await _userService.LogActivityAsync(
                            updaterUser.Id,
                            "user-updated",
                            $"Updated user: {existingUser.Email}",
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                        );

                    }
                    else
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        TempData["ErrorMessage"] = "Failed to update user.";
                    }
                }

                // Populate the model with current user information for displaying the page again
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    model.CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}";
                    model.CurrentRole = userRoles.FirstOrDefault() ?? "Admin";
                    model.CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                    model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                }

                model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };
                return View("AddUser", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;

                // Populate the model with current user information for displaying the page again
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    model.CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}";
                    model.CurrentRole = userRoles.FirstOrDefault() ?? "Admin";
                    model.CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                    model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                }

                model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };
                return View("AddUser", model);
            }
        }
        // The issue arises because there are two methods named `Index` with the same parameter types in the `UserManagementController` class.  
        // To resolve this, we need to rename one of the methods to avoid the conflict.  

        // Renaming the second `Index` method to `IndexWithDirectQuery` for clarity.  

        [HttpGet]
        public async Task<IActionResult> IndexWithDirectQuery(int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var roles = await _userManager.GetRolesAsync(user);

                // Prepare the view model  
                var viewModel = new UserManagementViewModel
                {
                    FirstName = user.FirstName ?? "Admin",
                    Role = roles.FirstOrDefault() ?? "Admin",
                    NotificationCount = 0, // Placeholder  
                    CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    Users = new List<UserViewModel>(),
                    CurrentPage = page,
                    TotalPages = 1,
                    PageStartRecord = 0,
                    PageEndRecord = 0,
                    TotalRecords = 0,
                    Roles = new List<RoleViewModel>(),
                    PendingUsers = new List<PendingUserViewModel>(),
                    ActivityLogs = new List<ActivityLogViewModel>()
                };

                // Directly query users here instead of using the service  
                var users = await _userManager.Users
                    .OrderBy(u => u.Email)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                foreach (var u in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);

                    // Fix for CS9035: Required member 'UserViewModel.PropertyType' must be set in the object initializer or attribute constructor.
                    // Fix for CS9035: Required member 'UserViewModel.OwnershipStatus' must be set in the object initializer or attribute constructor.

                    viewModel.Users.Add(new UserViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Address = u.Address ?? "",
                        Role = userRoles.FirstOrDefault() ?? "User",
                        Status = u.Status ?? "Active",
                        MemberSince = u.MemberSince.ToString("MMM dd, yyyy"),
                        LastLogin = u.LastLoginDate.HasValue ? u.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never",
                        ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                        PropertyType = u.PropertyType ?? "Unknown", // Added to fix CS9035
                        OwnershipStatus = u.OwnershipStatus ?? "Unknown" // Added to fix CS9035
                    });
                }

                // Calculate pagination  
                viewModel.TotalRecords = await _userManager.Users.CountAsync();
                viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalRecords / (double)pageSize);
                viewModel.PageStartRecord = viewModel.TotalRecords == 0 ? 0 : ((page - 1) * pageSize) + 1;
                viewModel.PageEndRecord = Math.Min(page * pageSize, viewModel.TotalRecords);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading User Management page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

        }
        
        // GET: /UserManagement/EditUser/5
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userToEdit = await _userManager.FindByIdAsync(id);
            if (userToEdit == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRoles = await _userManager.GetRolesAsync(userToEdit);

            var viewModel = new UserFormViewModel
            {
                Id = userToEdit.Id,
                FirstName = userToEdit.FirstName,
                LastName = userToEdit.LastName,
                Email = userToEdit.Email ?? string.Empty,
                PhoneNumber = userToEdit.PhoneNumber ?? string.Empty,
                Address = userToEdit.Address ?? string.Empty,
                Unit = userToEdit.Unit ?? string.Empty,
                PropertyType = userToEdit.PropertyType ?? string.Empty,
                OwnershipStatus = userToEdit.OwnershipStatus ?? string.Empty,
                Role = userRoles.FirstOrDefault() ?? "Homeowner",
                Status = userToEdit.Status ?? "Pending",
                Department = userToEdit.Department ?? string.Empty,
                Position = userToEdit.Position ?? string.Empty,
                EmployeeId = userToEdit.EmployeeId ?? string.Empty,
                MoveInDate = userToEdit.MoveInDate,
                EmergencyContactName = userToEdit.EmergencyContactName ?? string.Empty,
                EmergencyContactPhone = userToEdit.EmergencyContactPhone ?? string.Empty,
                VehicleInfo = userToEdit.VehicleInfo ?? string.Empty,
                ResidentCount = userToEdit.ResidentCount,
                DateOfBirth = userToEdit.DateOfBirth,
                Notes = userToEdit.Notes ?? string.Empty,
                ReceiveNotifications = userToEdit.ReceiveEmailNotifications.HasValue ? userToEdit.ReceiveEmailNotifications.Value : true,
                ReceiveSMS = userToEdit.ReceiveSmsNotifications.HasValue ? userToEdit.ReceiveSmsNotifications.Value : false,
                CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}",
                CurrentRole = roles.FirstOrDefault() ?? "Admin",
                CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" },
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return View("AddUser", viewModel);
        }

        // POST: /UserManagement/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword, bool forcePasswordChange, bool sendEmail)
        {
            try
            {
                // Ensure userId is not null/empty
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User ID is required.";
                    return RedirectToAction("Index");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Generate token and reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    // Update force password change flag
                    if (forcePasswordChange)
                    {
                        user.ForcePasswordChange = true;
                        await _userManager.UpdateAsync(user);
                    }

                    // Add logging message to help debugging
                    _logger.LogInformation($"Password reset successful for user {user.Email}");

                    TempData["SuccessMessage"] = "Password reset successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log specific errors
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Password reset error: {error.Description}");
                    }

                    TempData["ErrorMessage"] = $"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        // POST: /UserManagement/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Check if this is the last admin
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count <= 1)
                    {
                        return Json(new { success = false, message = "Cannot delete the last admin user." });
                    }
                }

                // Delete user's profile image if it exists
                if (!string.IsNullOrEmpty(user.ProfileImageUrl) &&
                    !user.ProfileImageUrl.Contains("default-avatar") &&
                    System.IO.File.Exists(_hostEnvironment.WebRootPath + user.ProfileImageUrl))
                {
                    System.IO.File.Delete(_hostEnvironment.WebRootPath + user.ProfileImageUrl);
                }

                // Log user deletion
                // Ensure `adminUser` is not null before accessing its properties
                var adminUser = await _userManager.GetUserAsync(User);
                if (adminUser == null)
                {
                    TempData["ErrorMessage"] = "Current user could not be identified.";
                    return RedirectToAction("Index");
                }

                // Log user deletion
                await _userService.LogActivityAsync(
                    adminUser.Id,
                    "user-deleted",
                    $"Deleted user: {user.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );


                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: /UserManagement/ChangeUserStatus
        [HttpPost]
        public async Task<IActionResult> ChangeUserStatus(string userId, string status)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Check if trying to suspend or deactivate the last admin
                if ((status == "Suspended" || status == "Inactive") && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count <= 1)
                    {
                        return Json(new { success = false, message = "Cannot modify status of the last admin user." });
                    }
                }

                user.Status = status;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var currentUser = await _userManager.GetUserAsync(User);

                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Current user could not be identified.";
                        return RedirectToAction("Index");
                    }

                    // Log status change
                    await _userService.LogActivityAsync(
                        currentUser.Id,
                        "status-change",
                        $"Changed status of user {user.Email} to {status}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    );


                    return Json(new { success = true, message = $"User status changed to {status} successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update user status." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user status");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        #region Helper Methods

        private async Task<string> ProcessProfileImageUpload(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
                return string.Empty;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(profileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new Exception("Only JPG, JPEG, and PNG files are allowed for profile images.");
            }

            string userFolderRelative = "images/users";
            string userFolderAbsolute = Path.Combine(_hostEnvironment.WebRootPath, userFolderRelative);

            if (!Directory.Exists(userFolderAbsolute))
            {
                Directory.CreateDirectory(userFolderAbsolute);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string filePath = Path.Combine(userFolderAbsolute, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }

            return $"/{userFolderRelative}/{uniqueFileName}";
        }

        private async Task ProcessPropertyDocumentsUpload(string userId, List<IFormFile> documents)
        {
            if (documents == null || !documents.Any())
                return;

            string docFolderRelative = $"documents/users/{userId}";
            string docFolderAbsolute = Path.Combine(_hostEnvironment.WebRootPath, docFolderRelative);

            if (!Directory.Exists(docFolderAbsolute))
            {
                Directory.CreateDirectory(docFolderAbsolute);
            }

            foreach (var document in documents)
            {
                if (document.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(document.FileName);
                    string filePath = Path.Combine(docFolderAbsolute, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(fileStream);
                    }

                    // In a real implementation, you'd save document metadata to the database
                    // await _documentService.SaveDocument(userId, document.FileName, uniqueFileName, filePath);
                }
            }
        }

        private async Task SendAccountCredentialsEmail(ApplicationUser user, string password)
        {
            if (user == null)
            {
                _logger.LogError("Cannot send credentials email to null user");
                return;
            }

            // In a real implementation, you would use an email service to send emails
            _logger.LogInformation($"Sending credentials email to {user.Email}");

            // Example of what would be sent
            var emailBody = $@"
            Hello {user.FirstName},

            Your account has been created in the Green Meadows Portal.

            Username: {user.Email}
            Password: {password}

            Please log in at https://yourportal.com/ and change your password as soon as possible.

            Regards,
            Green Meadows Management
            ";

            // Placeholder for actual email sending
            // await _emailService.SendEmail(user.Email, "Your Green Meadows Portal Account", emailBody);
            await Task.CompletedTask;
        }

        private async Task SendPasswordResetEmail(ApplicationUser user, string newPassword)
        {
            if (user == null)
            {
                _logger.LogError("Cannot send password reset email to null user");
                return;
            }

            // In a real implementation, you would use an email service to send emails
            _logger.LogInformation($"Sending password reset email to {user.Email}");

            // Example of what would be sent
            var emailBody = $@"
            Hello {user.FirstName},

            Your password has been reset by an administrator.

            Username: {user.Email}
            New Password: {newPassword}

            Please log in at https://yourportal.com/ and change your password as soon as possible.

            Regards,
            Green Meadows Management
            ";

            // Placeholder for actual email sending
            // await _emailService.SendEmail(user.Email, "Green Meadows Portal - Password Reset", emailBody);
            await Task.CompletedTask;
        }

        #endregion
    }
}