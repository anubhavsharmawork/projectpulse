using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.CustomFields.Commands;
using Application.CustomFields.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.CustomFields
{
    public class CustomFieldsHandlerTests
    {
        private static Mock<IHttpContextAccessor> CreateHttpAccessor(Guid? userId = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            if (userId.HasValue)
            {
                httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test"));
            }
            mock.Setup(x => x.HttpContext).Returns(httpContext);
            return mock;
        }

        [Fact]
        public async Task GetCustomFieldsByDomain_InvalidDomain_ReturnsEmpty()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new GetCustomFieldsByDomainHandler(db);

            var res = await handler.Handle(new GetCustomFieldsByDomainQuery("NoSuchDomain"), CancellationToken.None);
            res.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCustomFieldsByDomain_FiltersByEntityType()
        {
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "A", DomainType = DomainType.IT, FieldType = FieldType.Text, EntityType = null });
                ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "B", DomainType = DomainType.IT, FieldType = FieldType.Text, EntityType = "WorkItem" });
            });

            var handler = new GetCustomFieldsByDomainHandler(db);
            var res = await handler.Handle(new GetCustomFieldsByDomainQuery(DomainType.IT.ToString(), "WorkItem"), CancellationToken.None);

            res.Select(r => r.Name).Should().Contain(new[] { "A", "B" });
        }

        [Fact]
        public async Task GetCustomFieldValuesForEntity_ReturnsMapped()
        {
            var entityId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var fld = new CustomField { Id = Guid.NewGuid(), Name = "CF", DomainType = DomainType.IT, FieldType = FieldType.Text, IsRequired = true, Options = "opt" };
                ctx.CustomFields.Add(fld);
                ctx.CustomFieldValues.Add(new CustomFieldValue { Id = Guid.NewGuid(), CustomFieldId = fld.Id, EntityId = entityId, Value = "v" });
            });

            var handler = new GetCustomFieldValuesForEntityHandler(db);
            var res = await handler.Handle(new GetCustomFieldValuesForEntityQuery(entityId), CancellationToken.None);

            res.Should().HaveCount(1);
            res[0].FieldName.Should().Be("CF");
            res[0].IsRequired.Should().BeTrue();
        }

        [Fact]
        public async Task SaveCustomFieldValue_CreatesAndUpdatesCorrectly()
        {
            var userId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            using var db = TestDbContextFactory.CreateWithData(ctx =>
            {
                var fld = new CustomField { Id = Guid.NewGuid(), Name = "CF", DomainType = DomainType.IT, FieldType = FieldType.Text };
                ctx.CustomFields.Add(fld);
            });

            var handler = new SaveCustomFieldValueHandler(db, CreateHttpAccessor(userId).Object);
            var fldId = await db.CustomFields.Select(f => f.Id).FirstAsync();

            var newId = await handler.Handle(new SaveCustomFieldValueCommand(entityId, fldId, "val1"), CancellationToken.None);
            newId.Should().NotBe(Guid.Empty);
            var saved = await db.CustomFieldValues.FindAsync(newId);
            saved.Value.Should().Be("val1");
            saved.CreatedBy.Should().Be(userId.ToString());

            // Update existing
            var handler2 = new SaveCustomFieldValueHandler(db, CreateHttpAccessor(userId).Object);
            var returned = await handler2.Handle(new SaveCustomFieldValueCommand(entityId, fldId, "val2"), CancellationToken.None);
            returned.Should().Be(newId);
            var updated = await db.CustomFieldValues.FindAsync(newId);
            updated.Value.Should().Be("val2");
        }

        [Fact]
        public async Task SaveCustomFieldValue_FieldNotFound_Throws()
        {
            using var db = TestDbContextFactory.Create();
            var handler = new SaveCustomFieldValueHandler(db, CreateHttpAccessor().Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new SaveCustomFieldValueCommand(Guid.NewGuid(), Guid.NewGuid(), "x"), CancellationToken.None));
        }
    }
}
