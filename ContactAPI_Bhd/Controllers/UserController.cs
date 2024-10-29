using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContactAPI_Bhd.Models;
using Microsoft.AspNetCore.Authorization;

namespace ContactAPI_Bhd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Registro de usuario
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                return BadRequest(new { mensaje = "El usuario ya existe." });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Respuesta con datos del usuario y el ID generado
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                phones = user.Phones
            });
        }

        // Login de usuario y generación de JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || user.Password != loginDto.Password) // Asegúrate de encriptar la contraseña en producción
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Obtener usuario por ID (ejemplo de endpoint protegido)
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _context.Users.Include(u => u.Phones).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(user);
        }
    }
}
