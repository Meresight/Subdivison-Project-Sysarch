using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    public class DirectUserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DirectUserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login or handle null user appropriately
            }

            var users = await _userManager.Users.ToListAsync();

            var usersList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersList.Add(new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Status = user.Status,
                    Role = string.Join(", ", roles),
                    RegisteredDate = user.MemberSince.ToString("MMM dd, yyyy"),
                    LastLogin = user.LastLoginDate.HasValue ? user.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never"
                });
            }

            ViewBag.Users = usersList;
            ViewBag.CurrentUser = new
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                FullName = $"{currentUser.FirstName} {currentUser.LastName}",
                Email = currentUser.Email,
                ProfileImage = currentUser.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            return View();
        }

    }
}