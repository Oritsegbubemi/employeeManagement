using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;

namespace EmployeeManagement.Controllers
{
    //[Authorize(Roles ="Admin,User")]    //only admins or users can access this route
    //[Authorize(Roles = "Admin")]    //when we have another Authorize attribute, both will have to be satisfied before we can access this route
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "AdminRolePolicy")] //using claims based authorization instead of role based authorization
    public class AdministrationController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<AdministrationController> logger;


        //CONSTRUCTOR
        public AdministrationController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ILogger<AdministrationController> logger)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.logger = logger;
        }


        //LIST ROLES
        [HttpGet]
        public IActionResult ListRoles()
        {
            var roles = roleManager.Roles;
            return View(roles);
        }


        //CREATE ROLE GET
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }


        //CREATE ROLE POST
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if(ModelState.IsValid)
            {
                IdentityRole identityRole = new IdentityRole
                {
                    Name = model.Rolename
                };
                IdentityResult result = await roleManager.CreateAsync(identityRole);
                if(result.Succeeded)
                {
                    return RedirectToAction("ListRoles", "Administration");
                }
                
                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }


        //EDIT ROLE GET
        [HttpGet]
        public async Task<IActionResult> EditRole(string Id)
        {
            var role = await roleManager.FindByIdAsync(Id);
            if(role == null)
            {
                ViewBag.ErrorMessage = $"Role with id = {Id} cannot be found";
                return View("NotFound");
            }
            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name,                
            };
            List<ApplicationUser> users = userManager.Users.ToList();
            foreach (var user in users)
            {
                if(await userManager.IsInRoleAsync(user, role.Name))
                {
                   model.Users.Add(user.UserName);
                }
            }
            return View(model);
        }


        //EDIT ROLE POST
        [HttpPost]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {            
            var role = await roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role with id = {model.Id} cannot be found";
                return View("NotFound");
            }
            role.Name = model.RoleName;
            var result = await roleManager.UpdateAsync(role);
            if(result.Succeeded)
            {
                return RedirectToAction("ListRoles");
            }
            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }


        //DELETE ROLE POST
        [HttpPost]
        [Authorize(Policy = "DeleteRolePolicy")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Role with id = {id} cannot be found";
                return View("NotFound");
            }

            try
            {
                var result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("ListRoles");
            }
            catch (DbUpdateException e)
            {
                logger.LogError($"Error Deleting role {e}");
                ViewBag.ErrorTitle = $"{role.Name} role is in use";
                ViewBag.ErrorMessage = $"{role.Name} role cannot be deleted as there are users in this role." +
                    "If you want to delete this role, Please remove the users from the role and then try to delete";
                return View("Error");
            }
            
        }


        //EDIT USERS IN ROLE GET
        [HttpGet]
        public async Task<IActionResult> EditUsersInRole(string roleId)
        {
            ViewBag.roleId = roleId;
            var role = await roleManager.FindByIdAsync(roleId);
            if(role == null)
            {
                ViewBag.ErrorMessage = $"Role with Id = {roleId} cannot be found";
                return View("NotFound");
            }
            var model = new List<UserRoleViewModel>();
            List<ApplicationUser> users = userManager.Users.ToList();
            foreach (var user in users)
            {
                var userRoleViewModel = new UserRoleViewModel()
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    IsSelected = await userManager.IsInRoleAsync(user, role.Name)
                };

                model.Add(userRoleViewModel);
            }
            return View(model);
        }


        //EDIT USERS IN ROLE POST
        [HttpPost]
        public async Task<IActionResult> EditUsersInRole(List<UserRoleViewModel> model, string roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if(role == null)
            {
                ViewBag.ErrorMessage = $"Role with Id = {roleId} cannot be found";
                return View("NotFound");
            }
            for (int i = 0; i < model.Count; i++)
            {
                var user = await userManager.FindByIdAsync(model[i].UserId);
                IdentityResult result;
                if (model[i].IsSelected && !(await userManager.IsInRoleAsync(user, role.Name)))
                {
                    result = await userManager.AddToRoleAsync(user, role.Name);
                }
                else if(!model[i].IsSelected && await userManager.IsInRoleAsync(user, role.Name))
                {
                    result = await userManager.RemoveFromRoleAsync(user, role.Name);

                } else
                {
                    continue;
                }

                if(result.Succeeded)
                {
                    if(i < (model.Count - 1))
                    {
                        continue;
                    } else
                    {
                        return RedirectToAction("EditRole", new { Id = roleId });
                    }
                }
            }
            return RedirectToAction("EditRole", new { Id = roleId });

        }


        //LIST USERS
        public IActionResult ListUsers()
        {
            var users = userManager.Users;
            return View(users);
        }


        //EDIT USERS GET
        [HttpGet]
        public async Task<IActionResult> EditUser(string Id)
        {
            var user = await userManager.FindByIdAsync(Id);
            if(user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {Id} cannot be found";
                return View("NotFound");
            }

            var userClaims = await userManager.GetClaimsAsync(user);
            var userRoles = await userManager.GetRolesAsync(user);
            EditUserViewModel model = new EditUserViewModel
            {
                Username = user.UserName,
                Id = user.Id,
                Email = user.Email,
                City = user.City,
                Claims = userClaims.Select(c => c.Type + " : " + c.Value).ToList(),
                Roles = userRoles
                
            };
            return View(model);
        }


        //EDIT USERS POST
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {model.Id} cannot be found";
                return View("NotFound");
            }
            user.Email = model.Email;
            user.UserName = model.Username;
            user.City = model.City;

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("ListUsers");
            }
            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
          
            return View(model);
        }


        //DELETE USER
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if(user == null)
            {
                ViewBag.ErrorMessage = $"The user with id = {id} cannot be found";
                return View("NotFound");
            }
            var result = await userManager.DeleteAsync(user);
            if(result.Succeeded)
            {
                return RedirectToAction("ListUsers");
            }
            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View("ListUsers");

        }


        //MANAGE USER ROLE GET
        [HttpGet]
        [Authorize("EditRolePolicy")]
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            ViewBag.userId = userId;
            var user = await userManager.FindByIdAsync(userId);
            if(user == null)
            {
                return View("NotFound");
            }
            var model = new List<UserRolesViewModel>();
            var roles = roleManager.Roles.ToList();
            foreach(var role in roles)
            {
                var userRolesViewModel = new UserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = await userManager.IsInRoleAsync(user, role.Name)
                };
                model.Add(userRolesViewModel);
            }
            return View(model);
        }


        //MANAGE USER ROLE POST
        [HttpPost]
        [Authorize("EditRolePolicy")]
        public async Task<IActionResult> ManageUserRoles(List<UserRolesViewModel> model, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"The user with id = {userId} cannot be found";
                return View("NotFound");
            }
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            if(!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await userManager.AddToRolesAsync(user, model.Where(x => x.IsSelected).Select(y => y.RoleName));
            if(!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("EditUser", new { Id = userId });
        }


        //MANAGE USER CLAIMS GET
        [HttpGet]
        public async Task<IActionResult> ManageUserClaims(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if(user == null)
            {
                ViewBag.ErrorMessage = $"The user with id = {userId} cannot be found";
                return View("NotFound");
            }

            var existingUserClaims = await userManager.GetClaimsAsync(user);
            var model = new UserClaimsViewModel
            {
                UserId = userId,
            };
            foreach(Claim claim in ClaimsStore.AllClaims)
            {
                UserClaim userClaim = new UserClaim
                {
                    ClaimType = claim.Type
                };
                if(existingUserClaims.Any(c => (c.Type == claim .Type) && c.Value == "true"))
                {
                    userClaim.IsSelected = true;
                };
                model.Claims.Add(userClaim);
            }
            return View(model);
        }


        //MANAGE USER CLAIMS POST
        [HttpPost]
        public async Task<IActionResult> ManageUserClaims(UserClaimsViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"The user with id = {model.UserId} cannot be found";
                return View("NotFound");
            }

            var claims = await userManager.GetClaimsAsync(user);
            var result = await userManager.RemoveClaimsAsync(user, claims);
            if(!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing claims");
                return View(model);
            }
            //result = await userManager.AddClaimsAsync(user, model.Claims.Where(x => x.IsSelected).Select(y => new Claim(y.ClaimType, y.ClaimType)));
            result = await userManager.AddClaimsAsync(user, model.Claims.Select(y => new Claim(y.ClaimType, y.IsSelected ? "true" : "false")));
            if(!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected claims to user");
                return View(model);
            }
            return RedirectToAction("EditUser", new { Id = model.UserId});
        }


        //ACCESS DENIED
        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}