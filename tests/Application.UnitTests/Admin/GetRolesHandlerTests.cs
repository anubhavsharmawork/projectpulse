using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Admin.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Admin
{
    public class GetRolesHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsRolesWithPermissionsGroupedByCategory()
        {
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                // Permissions across two categories
                var p1 = new Permission { Id = Guid.NewGuid(), Name = "CreateProject", Category = PermissionCategory.Project, Description = "create" };
                var p2 = new Permission { Id = Guid.NewGuid(), Name = "DeleteProject", Category = PermissionCategory.Project, Description = "delete" };
                var p3 = new Permission { Id = Guid.NewGuid(), Name = "ManageUsers", Category = PermissionCategory.Admin, Description = "manage" };
                ctx.Permissions.AddRange(p1, p2, p3);

                // Role with one granted permission
                var role = new AppRole { Id = Guid.NewGuid(), Name = "Admin", SystemRole = SystemRole.SystemAdmin, Description = "admin role" };
                ctx.AppRoles.Add(role);
                ctx.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), AppRole = role, Permission = p3 });
            });

            var handler = new GetRolesHandler(db);

            var result = await handler.Handle(new GetRolesQuery(), CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().ContainSingle(r => r.Name == "Admin");
            var admin = result.Single(r => r.Name == "Admin");
            // Should include both categories
            admin.PermissionCategories.Select(c => c.Category).Should().Contain(new[] { PermissionCategory.Project.ToString(), PermissionCategory.Admin.ToString() });
            // The ManageUsers permission should be marked as granted
            var adminCategory = admin.PermissionCategories.Single(c => c.Category == PermissionCategory.Admin.ToString());
            adminCategory.Permissions.Should().Contain(p => p.Name == "ManageUsers" && p.Granted);
            // A project permission should be present and not granted
            var projCategory = admin.PermissionCategories.Single(c => c.Category == PermissionCategory.Project.ToString());
            projCategory.Permissions.Should().Contain(p => p.Name == "CreateProject" && !p.Granted);
        }
    }
}
