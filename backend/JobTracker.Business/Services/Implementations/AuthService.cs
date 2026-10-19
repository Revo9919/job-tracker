using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JobTracker.Business.DTOs.Auth;
using JobTracker.Business.Services.Interfaces;
using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.UnitOfWork;
using JobTracker.Domain;

namespace JobTracker.Business.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> SignupAsync(SignupDto dto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already in use.");
            }

            var user = new UserProfile
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken
            };

            return response;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is disabled.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken
            };

            return response;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                throw new UnauthorizedAccessException("Refresh token is required.");
            }

            var user = await _userRepository.GetByRefreshTokenAsync(dto.RefreshToken);
            if (user == null || !user.IsActive || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        private string GenerateJwtToken(UserProfile user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret missing"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"] ?? "15")),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}