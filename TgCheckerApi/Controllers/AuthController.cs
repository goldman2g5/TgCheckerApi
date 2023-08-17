using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TgCheckerApi.Models.BaseModels;

namespace ToDoApp.Server.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {


        private readonly TgDbContext _context;

        public AuthController(TgDbContext context)
        {
            _context = context;
        }




    }
}