using System;
using System.Linq;
using Domain.Entities;
using System.Collections.Generic;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Domain
{
    public class DomainEntityTests
    {
        [Fact]
        public void Attachment_Defaults_And_Properties()
        {
            var a = new Attachment();
            a.FileName.Should().BeEmpty();
            a.StorageUrl.Should().BeEmpty();
            a.ContentType.Should().BeEmpty();
            a.SizeBytes.Should().Be(0);

            var id = Guid.NewGuid();
            a.Id = id;
            a.FileName = "file.txt";
            a.FileName.Should().Be("file.txt");
        }

        [Fact]
        public void AssetCheckout_Defaults_And_Assignments()
        {
            var ac = new AssetCheckout();
            ac.CheckedOutBy.Should().BeEmpty();
            ac.CheckedInBy.Should().BeNull();
            ac.Notes.Should().BeNull();
            ac.Condition.Should().BeEmpty();
            ac.CheckedOutAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));

            var userId = Guid.NewGuid();
            ac.CheckedOutToUserId = userId;
            ac.CheckedOutToUserId.Should().Be(userId);
        }

        [Fact]
        public void AssetHistoryEntry_Properties()
        {
            var e = new AssetHistoryEntry();
            e.ChangeType = AssetChangeType.MaintenancePerformed;
            e.OldValue = "old";
            e.NewValue = "new";
            e.ChangedBy = "tester";
            e.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
            e.Reason = "fix";

            e.OldValue.Should().Be("old");
        }

        [Fact]
        public void AuditLog_Defaults()
        {
            var log = new AuditLog();
            log.EntityType.Should().BeEmpty();
            log.Action.Should().BeEmpty();
            log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));

            log.EntityType = "WorkItem";
            log.Action = "Update";
            log.EntityType.Should().Be("WorkItem");
        }

        [Fact]
        public void CustomField_Defaults_And_Values_List()
        {
            var cf = new CustomField();
            cf.Name.Should().BeEmpty();
            cf.Values.Should().NotBeNull();
            cf.Values.Should().BeEmpty();

            cf.Options = "[\"a\",\"b\"]";
            cf.IsRequired = true;
            cf.FieldType = FieldType.Text;
            cf.DomainType = DomainType.IT;

            cf.FieldType.Should().Be(FieldType.Text);
        }

        [Fact]
        public void CustomFieldValue_Defaults()
        {
            var v = new CustomFieldValue();
            v.EntityType.Should().BeEmpty();
            v.Value.Should().BeNull();

            var entityId = Guid.NewGuid();
            v.EntityId = entityId;
            v.EntityId.Should().Be(entityId);
        }

        [Fact]
        public void DomainAssetConfig_Defaults()
        {
            var cfg = new DomainAssetConfig();
            cfg.DefaultDepreciationMethod.Should().Be(DepreciationMethod.StraightLine);
            cfg.DefaultUsefulLifeYears.Should().Be(5);
            cfg.SortOrder.Should().Be(0);

            cfg.DisplayLabel = "Label";
            cfg.DisplayLabel.Should().Be("Label");
        }

        [Fact]
        public void DomainTemplate_Defaults()
        {
            var t = new DomainTemplate();
            t.Name.Should().BeEmpty();
            t.CustomFields.Should().NotBeNull();
            t.WorkItemTypeLabels = "{\"1\":\"X\"}";
            t.WorkItemTypeLabels.Should().Contain("\"1\"");
        }

        [Fact]
        public void MaintenanceRecord_Defaults()
        {
            var m = new MaintenanceRecord();
            m.Description.Should().BeEmpty();
            m.Cost.Should().Be(0);
            m.NextMaintenanceDate.Should().BeNull();

            m.MaintenanceType = MaintenanceType.Corrective;
            m.MaintenanceType.Should().Be(MaintenanceType.Corrective);
        }

        [Fact]
        public void ProjectCategory_Defaults()
        {
            var pc = new ProjectCategory();
            pc.Name.Should().BeEmpty();
            pc.DefaultTeamRoles.Should().BeNull();
            pc.DomainType = DomainType.IT;
            pc.DomainType.Should().Be(DomainType.IT);
        }

        [Fact]
        public void Relation_Assigns_Type()
        {
            var r = new Relation();
            r.RelationType = RelationType.BlockedBy;
            r.RelationType.Should().Be(RelationType.BlockedBy);
        }

        [Fact]
        public void RolePermission_Assigns_Relations()
        {
            var rp = new RolePermission();
            var role = new AppRole { Id = Guid.NewGuid(), Name = "Role" };
            var perm = new Permission { Id = Guid.NewGuid(), Name = "Perm" };
            rp.AppRole = role;
            rp.Permission = perm;
            rp.AppRole.Name.Should().Be("Role");
            rp.Permission.Name.Should().Be("Perm");
        }

        [Fact]
        public void WorkflowTransition_Comment()
        {
            var wt = new WorkflowTransition();
            wt.Comment.Should().BeNull();
            wt.Comment = "ok";
            wt.Comment.Should().Be("ok");
        }
    }
}
