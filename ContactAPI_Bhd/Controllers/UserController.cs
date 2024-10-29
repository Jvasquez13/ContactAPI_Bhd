using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ContactAPI_Bhd.Models;
using System.Security.Cryptography;

namespace ContactAPI_Bhd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiresInMinutes"])), // Expira en 1 minuto
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                // Validar si el correo ya existe
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    return BadRequest(new { mensaje = "El usuario ya existe." });
                }

                // Validar formato del correo
                if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { mensaje = "El formato del correo electrónico es inválido." });
                }

                // Validar longitud de la contraseña
                if (user.Password.Length < 6)
                {
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres." });
                }


                // Asignar fechas y otros datos adicionales al usuario
                user.Password = HashPassword(user.Password);
                user.Created = DateTime.Now;
                user.LastLogin = user.Created;
                user.IsActive = true;

                // Inicializar la colección de teléfonos si es nula.
                user.Phones = user.Phones ?? new List<Phone>();

                // Crear el usuario y guardar en la base de datos
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Devolver la respuesta con el usuario creado
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    created = user.Created,
                    last_login = user.LastLogin,
                    isactive = user.IsActive,
                    phones = user.Phones.Select(p => new
                    {
                        number = p.Number,
                        citycode = p.CityCode,
                        countrycode = p.CountryCode
                    })
                });
            }
            catch (Exception ex)
            {
                // Log para el diagnóstico
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new { mensaje = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            // Buscar al usuario por su correo
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == login.Email);

            // Verificar si el usuario existe y si la contraseña es correcta
            if (user == null || user.Password != HashPassword(login.Password))
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });
            }

            // Generar y asignar el token al usuario
            user.Token = GenerateJwtToken(user);
            user.LastLogin = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Devolver los detalles del usuario después del inicio de sesión exitoso
            return Ok(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                token = user.Token,
                last_login = user.LastLogin
            });
        }


        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _context.Users.Include(u => u.Phones).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                created = user.Created,
                modified = user.Modified,
                last_login = user.LastLogin,
                token = user.Token,
                isactive = user.IsActive,
                phones = user.Phones.Select(p => new
                {
                    number = p.Number,
                    citycode = p.CityCode,
                    countrycode = p.CountryCode
                })
            });
        }

        // Endpoint para obtener todos los usuarios (requiere autorización)
        [HttpGet("GetAll")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(user => new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        created = user.Created,
                        modified = user.Modified,
                        last_login = user.LastLogin,
                        isactive = user.IsActive,
                        phones = user.Phones.Select(p => new
                        {
                            number = p.Number,
                            citycode = p.CityCode,
                            countrycode = p.CountryCode
                        })
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error al obtener los usuarios." });
            }
        }
    }
}
