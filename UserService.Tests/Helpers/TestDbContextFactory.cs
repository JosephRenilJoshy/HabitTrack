using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static UserDbContext Create()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB name per call = test isolation
                .Options;

            return new UserDbContext(options);
        }
    }
}