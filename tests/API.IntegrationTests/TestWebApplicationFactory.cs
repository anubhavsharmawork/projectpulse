using System;
using System.IO;
using System.Threading;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace API.IntegrationTests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(AppDbContext));
                services.RemoveAll(typeof(IAppDbContext));

                // Add in-memory database with fixed name per factory instance
                var dbName = _databaseName;
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

                // Mock S3 storage service to avoid AWS dependency in tests
                var storageMock = new Mock<IStorageService>();
                storageMock.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("https://test-bucket.s3.amazonaws.com/test-file");
                services.RemoveAll(typeof(IStorageService));
                services.AddScoped(_ => storageMock.Object);
            });
        }
    }
}
