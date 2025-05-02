using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DiagnosticController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var userCount = await _userManager.Users.CountAsync();
                var users = await _userManager.Users.ToListAsync();

                var userDetails = new List<object>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDetails.Add(new
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Status = user.Status,
                        Roles = roles.ToList()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    TotalCount = userCount,
                    UsersRetrieved = users.Count,
                    Users = userDetails
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}