using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.CustomFields.Queries;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.CustomFields;

public class GetCustomFieldsByDomainHandlerTests
{
    [Fact]
    public async Task Handle_ValidDomain_ReturnsFields()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "Priority", FieldType = FieldType.Dropdown, DomainType = DomainType.IT, IsRequired = true });
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "Budget", FieldType = FieldType.Number, DomainType = DomainType.Healthcare, IsRequired = false });
        });
        var handler = new GetCustomFieldsByDomainHandler(db);

        var result = await handler.Handle(new GetCustomFieldsByDomainQuery("IT"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Priority");
    }

    [Fact]
    public async Task Handle_InvalidDomain_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetCustomFieldsByDomainHandler(db);

        var result = await handler.Handle(new GetCustomFieldsByDomainQuery("InvalidDomain"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEntityTypeFilter_ReturnsMatchingAndNullEntityType()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "Epic Field", FieldType = FieldType.Text, DomainType = DomainType.IT, EntityType = "1" });
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "All Levels", FieldType = FieldType.Text, DomainType = DomainType.IT, EntityType = null });
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "Task Only", FieldType = FieldType.Text, DomainType = DomainType.IT, EntityType = "3" });
        });
        var handler = new GetCustomFieldsByDomainHandler(db);

        var result = await handler.Handle(new GetCustomFieldsByDomainQuery("IT", EntityType: "1"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_CaseInsensitiveDomain_Works()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.CustomFields.Add(new CustomField { Id = Guid.NewGuid(), Name = "Field", FieldType = FieldType.Text, DomainType = DomainType.IT });
        });
        var handler = new GetCustomFieldsByDomainHandler(db);

        var result = await handler.Handle(new GetCustomFieldsByDomainQuery("it"), CancellationToken.None);

        result.Should().HaveCount(1);
    }
}

public class GetCustomFieldValuesForEntityHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsValuesForEntity()
    {
        var entityId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.CustomFields.Add(new CustomField { Id = fieldId, Name = "Priority", FieldType = FieldType.Dropdown, DomainType = DomainType.IT, Options = "[\"High\",\"Low\"]" });
            ctx.CustomFieldValues.Add(new CustomFieldValue { Id = Guid.NewGuid(), CustomFieldId = fieldId, EntityId = entityId, EntityType = "WorkItem", Value = "High" });
        });
        var handler = new GetCustomFieldValuesForEntityHandler(db);

        var result = await handler.Handle(new GetCustomFieldValuesForEntityQuery(entityId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("Priority");
        result[0].Value.Should().Be("High");
    }

    [Fact]
    public async Task Handle_NoValues_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetCustomFieldValuesForEntityHandler(db);

        var result = await handler.Handle(new GetCustomFieldValuesForEntityQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
