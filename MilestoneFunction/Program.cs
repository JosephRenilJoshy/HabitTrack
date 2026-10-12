using HabitService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserService.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        services.AddDbContext<HabitDbContext>(o => o.UseSqlServer(config["HabitDbConnection"]));
        services.AddDbContext<UserDbContext>(o => o.UseSqlServer(config["UserDbConnection"]));
    })
    .Build();

host.Run();