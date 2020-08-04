using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private IConfiguration _config;
        private readonly UserDBContext _userDbContext;
        public LoginController(IConfiguration config, UserDBContext userDBContext)
        {
            _config = config;
            _userDbContext = userDBContext;
        }
        [HttpGet]
        public IActionResult Login(string username, string pass)
        {
            UserModel login = new UserModel();
            login.Username = username;
            login.UserPassword = pass;
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);
            if (user!=null)
            {
                var tokenStr = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenStr });
            }
            return response;
        }
        private UserModel AuthenticateUser(UserModel login)
        {
            UserModel user = null;
            UserModel userCon = (from u in _userDbContext.Users 
                       where u.Username==login.Username & u.UserPassword==login.UserPassword
                       select new UserModel
                       {
                           UserId = u.UserId,
                           Username = u.Username,
                           UserPassword = u.UserPassword,
                           UserEmail= u.UserEmail,
                           UserRole = u.UserRole
                       }).FirstOrDefault();
            if (userCon!=null)
            {
                if (login.Username.Equals(userCon.Username) && login.UserPassword.Equals(userCon.UserPassword))
                {
                    user = new UserModel
                    {
                        Username = userCon.Username,
                        UserPassword = userCon.UserPassword,
                        UserEmail = userCon.UserEmail,
                        UserRole = userCon.UserRole
                    };
                }
            }
            return user;
        }
        private string GenerateJSONWebToken(UserModel userInfo)
        {
            //symmetric security Key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            //signing credentials
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            //add claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.UserEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role,userInfo.UserRole)
            };
            //Create token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials
                ) ;
            var encodetoken = new JwtSecurityTokenHandler().WriteToken(token);
            return encodetoken;
        }
        [Authorize]
        [HttpPost("Post")]
        public string Post()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            IList<Claim> claim = identity.Claims.ToList();
            var username = claim[0].Value;
            return "Welcom To: " + username;
        }
        [Authorize]
        [HttpGet("GetValue")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2", "value3" };
        }
    }
}
