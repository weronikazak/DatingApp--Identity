using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DatingApp.API.Data
{
    public class Seed
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public Seed(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void SeedUsers(){
            if (!_userManager.Users.Any()){
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);
                
                var roles = new List<Role>{
                    new Role{Name ="Member"},
                    new Role{Name ="Admin"},
                    new Role{Name ="Moderator"},
                    new Role{Name ="VIP"}
                };

                foreach (var role in roles){
                    _roleManager.CreateAsync(role).Wait();
                }

                foreach (var user in users)
                {
                    _userManager.CreateAsync(user, "password").Wait();
                    _userManager.AddToRoleAsync(user, "Member").Wait();
                    user.Photos.SingleOrDefault().IsAccepted = true;
                }

                var adminUser = new User{
                    UserName = "Admin"
                };

                IdentityResult result = _userManager.CreateAsync(adminUser, "password").Result;

                if (result.Succeeded){
                    var admin = _userManager.FindByNameAsync("Admin").Result;
                    _userManager.AddToRolesAsync(admin, new[] {"Admin", "Moderator"}).Wait();
                }
            }         
        }

    }
}