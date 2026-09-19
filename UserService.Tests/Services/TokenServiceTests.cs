using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using UserService.Models;
using UserService.Services;
using UserService.Settings;
using Xunit;

namespace UserService.Tests.Services
{
    public class TokenServiceTests
    {
        private static TokenService CreateTokenService(int expiryMinutes = 60)
        {
            var settings = Options.Create(new JwtSettings
            {
                Key = "test-signing-key-at-least-32-characters-long",
                Issuer = "UserService",
                Audience = "HabitTrackServices",
                ExpiryMinutes = expiryMinutes
            });

            return new TokenService(settings);
        }

        [Fact]
        public void GenerateToken_ProducesValidJwtStructure()
        {
            // Arrange
            var tokenService = CreateTokenService();
            var user = new User { Id = 1, Name = "Test User", Email = "test@example.com" };

            // Act
            var token = tokenService.GenerateToken(user);

            // Assert - a JWT always has exactly 3 dot-separated parts
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        public void GenerateToken_IncludesCorrectUserClaims()
        {
            // Arrange
            var tokenService = CreateTokenService();
            var user = new User { Id = 42, Name = "Claims Test", Email = "claims@example.com" };

            // Act
            var token = tokenService.GenerateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal("claims@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        }

        [Fact]
        public void GenerateToken_SetsCorrectIssuerAndAudience()
        {
            // Arrange
            var tokenService = CreateTokenService();
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com" };

            // Act
            var token = tokenService.GenerateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            Assert.Equal("UserService", jwt.Issuer);
            Assert.Contains("HabitTrackServices", jwt.Audiences);
        }

        [Fact]
        public void GenerateToken_SetsExpiryBasedOnConfiguredMinutes()
        {
            // Arrange
            var tokenService = CreateTokenService(expiryMinutes: 30);
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com" };
            var beforeGeneration = DateTime.UtcNow;

            // Act
            var token = tokenService.GenerateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert - expiry should be approximately 30 minutes from now
            var expectedExpiry = beforeGeneration.AddMinutes(30);
            var actualExpiry = jwt.ValidTo;

            // Allow a few seconds of tolerance for test execution time
            Assert.True(Math.Abs((expectedExpiry - actualExpiry).TotalSeconds) < 5);
        }

        [Fact]
        public void GenerateToken_ProducesDifferentJtiForEachCall()
        {
            // Arrange
            var tokenService = CreateTokenService();
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com" };

            // Act - generate two tokens for the same user
            var token1 = tokenService.GenerateToken(user);
            var token2 = tokenService.GenerateToken(user);

            var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(token1);
            var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(token2);

            var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            // Assert - each login should produce a unique token identifier
            Assert.NotEqual(jti1, jti2);
        }
    }
}