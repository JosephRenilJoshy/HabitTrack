using Microsoft.Extensions.Options;
using Moq;
using UserService.DTOs;
using UserService.Exceptions;
using UserService.Services;
using UserService.Settings;
using UserService.Tests.Helpers;
using Xunit;

namespace UserService.Tests.Services
{
    public class UserServiceTests
    {
        private static ITokenService CreateTokenService()
        {
            var settings = Options.Create(new JwtSettings
            {
                Key = "test-signing-key-at-least-32-characters-long",
                Issuer = "UserService",
                Audience = "HabitTrackServices",
                ExpiryMinutes = 60
            });

            return new TokenService(settings);
        }

        [Fact]
        public async Task RegisterAsync_WithNewEmail_CreatesUserSuccessfully()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            var dto = new RegisterRequestDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "SecurePass123!"
            };

            // Act
            var result = await service.RegisterAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            var dto = new RegisterRequestDto
            {
                Name = "First User",
                Email = "duplicate@example.com",
                Password = "SecurePass123!"
            };

            await service.RegisterAsync(dto); // first registration succeeds

            var secondDto = new RegisterRequestDto
            {
                Name = "Second User",
                Email = "duplicate@example.com", // same email
                Password = "AnotherPass456!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegisterAsync(secondDto));
        }

        [Fact]
        public async Task LoginAsync_WithCorrectCredentials_ReturnsToken()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "Login Test",
                Email = "login@example.com",
                Password = "CorrectPassword1!"
            });

            // Act
            var result = await service.LoginAsync(new LoginRequestDto
            {
                Email = "login@example.com",
                Password = "CorrectPassword1!"
            });

            // Assert
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "Wrong Pass Test",
                Email = "wrongpass@example.com",
                Password = "CorrectPassword1!"
            });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "wrongpass@example.com",
                    Password = "WrongPassword!"
                }));
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "doesnotexist@example.com",
                    Password = "AnyPassword1!"
                }));
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            var created = await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "Findable User",
                Email = "findme@example.com",
                Password = "Password123!"
            });

            // Act
            var result = await service.GetByIdAsync(created.Id);

            // Assert
            Assert.Equal(created.Id, result?.Id);
            Assert.Equal("Findable User", result?.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ThrowsNotFoundException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(9999));
        }

        [Fact]
        public async Task UpdateAsync_WithValidChanges_UpdatesUser()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            var created = await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "Original Name",
                Email = "original@example.com",
                Password = "Password123!"
            });

            // Act
            var updated = await service.UpdateAsync(created.Id, new UpdateRequestDto
            {
                Name = "Updated Name",
                Email = "updated@example.com"
            });

            // Assert
            Assert.Equal("Updated Name", updated.Name);
            Assert.Equal("updated@example.com", updated.Email);
        }

        [Fact]
        public async Task UpdateAsync_WithEmailTakenByAnotherUser_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "User One",
                Email = "userone@example.com",
                Password = "Password123!"
            });

            var userTwo = await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "User Two",
                Email = "usertwo@example.com",
                Password = "Password123!"
            });

            // Act & Assert - user two tries to take user one's email
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateAsync(userTwo.Id, new UpdateRequestDto
                {
                    Name = "User Two",
                    Email = "userone@example.com"
                }));
        }

        [Fact]
        public async Task DeleteAsync_WithExistingUser_RemovesUser()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            var created = await service.RegisterAsync(new RegisterRequestDto
            {
                Name = "To Delete",
                Email = "delete@example.com",
                Password = "Password123!"
            });

            // Act
            await service.DeleteAsync(created.Id);

            // Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentUser_ThrowsNotFoundException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new global::UserService.Services.UserService(context, CreateTokenService());

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(9999));
        }
    }
}