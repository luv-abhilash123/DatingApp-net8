using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers
{
  public class AccountController(DataContext context,ITokenService tokenService):BaseApiController
    {
        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO){

            if(await UserExists(registerDTO.Username))return BadRequest("Username is taken");

            using var hmac=new HMACSHA512();
            var user=new AppUser{
                UserName=registerDTO.Username.ToLower(),
                PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
                PasswordSalt=hmac.Key
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return new UserDTO{
                Username=user.UserName,
                Token=tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDto){
         var user=await context.Users.FirstOrDefaultAsync(x=>x.UserName==loginDto.Username.ToLower());

         if(user==null) return Unauthorized("Invalid username");
         using var hmac=new HMACSHA512(user.PasswordSalt);
        var computeHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
        for(int i=0;i<computeHash.Length;i++){
            if(computeHash[i]!=user.PasswordHash[i])
                return Unauthorized("Invalid password");
            
        }
        return new UserDTO{
                Username=user.UserName,
                Token=tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username){

            return await context.Users.AnyAsync(x=>x.UserName.ToLower()==username.ToLower());

        }
    } 
}
