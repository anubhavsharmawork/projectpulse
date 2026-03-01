using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.UnitTests.TestHelpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static AppDbContext CreateWithData(Action<AppDbContext> seedAction)
    {
        var context = Create();
        seedAction(context);
        context.SaveChanges();
        return context;
    }
}
