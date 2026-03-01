using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;
using DomainEnums = Domain.Enums;

namespace Application.UnitTests.Domain
{
    public class EntityTests
    {
        #region User Entity Tests
        
        [Fact]
        public void User_DefaultValues_ShouldBeCorrect()
        {
            var user = new User();

            user.Id.Should().Be(Guid.Empty);
            user.Email.Should().BeEmpty();
            user.PasswordHash.Should().BeEmpty();
            user.DisplayName.Should().BeEmpty();
            user.Role.Should().Be(Role.Member);
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void User_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            
            var user = new User
            {
                Id = id,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                DisplayName = "Test User",
                Role = Role.Admin,
                CreatedAt = createdAt
            };

            user.Id.Should().Be(id);
            user.Email.Should().Be("test@example.com");
            user.PasswordHash.Should().Be("hashedpassword");
            user.DisplayName.Should().Be("Test User");
            user.Role.Should().Be(Role.Admin);
            user.CreatedAt.Should().Be(createdAt);
        }

        [Theory]
        [InlineData(Role.Member, 0)]
        [InlineData(Role.Admin, 1)]
        public void Role_Values_ShouldBeCorrect(Role role, int expectedValue)
        {
            ((int)role).Should().Be(expectedValue);
        }

        #endregion

        #region Project Entity Tests

        [Fact]
        public void Project_DefaultValues_ShouldBeCorrect()
        {
            var project = new Project();

            project.Id.Should().Be(Guid.Empty);
            project.Name.Should().BeEmpty();
            project.Description.Should().BeNull();
            project.OwnerId.Should().Be(Guid.Empty);
            project.IsPublic.Should().BeFalse();
            project.WorkItems.Should().NotBeNull().And.BeEmpty();
            project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Project_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var workItems = new List<WorkItem>();

            var project = new Project
            {
                Id = id,
                Name = "Test Project",
                Description = "Test Description",
                OwnerId = ownerId,
                IsPublic = true,
                WorkItems = workItems,
                CreatedAt = createdAt
            };

            project.Id.Should().Be(id);
            project.Name.Should().Be("Test Project");
            project.Description.Should().Be("Test Description");
            project.OwnerId.Should().Be(ownerId);
            project.IsPublic.Should().BeTrue();
            project.WorkItems.Should().BeSameAs(workItems);
            project.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region Comment Entity Tests

        [Fact]
        public void Comment_DefaultValues_ShouldBeCorrect()
        {
            var comment = new Comment();

            comment.Id.Should().Be(Guid.Empty);
            comment.WorkItemId.Should().Be(Guid.Empty);
            comment.AuthorId.Should().Be(Guid.Empty);
            comment.Body.Should().BeEmpty();
            comment.MentionedUserIds.Should().NotBeNull().And.BeEmpty();
            comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Comment_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var mentionedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var createdAt = DateTime.UtcNow.AddDays(-1);

            var comment = new Comment
            {
                Id = id,
                WorkItemId = workItemId,
                AuthorId = authorId,
                Body = "Test comment body",
                MentionedUserIds = mentionedUserIds,
                CreatedAt = createdAt
            };

            comment.Id.Should().Be(id);
            comment.WorkItemId.Should().Be(workItemId);
            comment.AuthorId.Should().Be(authorId);
            comment.Body.Should().Be("Test comment body");
            comment.MentionedUserIds.Should().BeSameAs(mentionedUserIds);
            comment.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region MentionNotification Entity Tests

        [Fact]
        public void MentionNotification_DefaultValues_ShouldBeCorrect()
        {
            var notification = new MentionNotification();

            notification.Id.Should().Be(Guid.Empty);
            notification.UserId.Should().Be(Guid.Empty);
            notification.CommentId.Should().Be(Guid.Empty);
            notification.WorkItemId.Should().Be(Guid.Empty);
            notification.MentionedByUserId.Should().Be(Guid.Empty);
            notification.CommentBody.Should().BeEmpty();
            notification.WorkItemTitle.Should().BeEmpty();
            notification.MentionedByName.Should().BeEmpty();
            notification.IsRead.Should().BeFalse();
            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void MentionNotification_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var workItemId = Guid.NewGuid();
            var mentionedByUserId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);

            var notification = new MentionNotification
            {
                Id = id,
                UserId = userId,
                CommentId = commentId,
                WorkItemId = workItemId,
                MentionedByUserId = mentionedByUserId,
                CommentBody = "Test comment",
                WorkItemTitle = "Test Work Item",
                MentionedByName = "Test User",
                IsRead = true,
                CreatedAt = createdAt
            };

            notification.Id.Should().Be(id);
            notification.UserId.Should().Be(userId);
            notification.CommentId.Should().Be(commentId);
            notification.WorkItemId.Should().Be(workItemId);
            notification.MentionedByUserId.Should().Be(mentionedByUserId);
            notification.CommentBody.Should().Be("Test comment");
            notification.WorkItemTitle.Should().Be("Test Work Item");
            notification.MentionedByName.Should().Be("Test User");
            notification.IsRead.Should().BeTrue();
            notification.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region WorkItem Entity Tests

        [Fact]
        public void EpicWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var epic = new EpicWorkItem();

            epic.Id.Should().Be(Guid.Empty);
            epic.ProjectId.Should().Be(Guid.Empty);
            epic.ParentId.Should().BeNull();
            epic.Parent.Should().BeNull();
            epic.Children.Should().NotBeNull().And.BeEmpty();
            epic.Title.Should().BeEmpty();
            epic.Description.Should().BeNull();
            epic.AttachmentUrl.Should().BeNull();
            epic.IsCompleted.Should().BeFalse();
            epic.AssigneeId.Should().BeNull();
            epic.Comments.Should().NotBeNull().And.BeEmpty();
            epic.CompletedAt.Should().BeNull();
            epic.Type.Should().Be(WorkItemType.Epic);
            epic.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void UserStoryWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var userStory = new UserStoryWorkItem();

            userStory.Type.Should().Be(WorkItemType.UserStory);
            userStory.Title.Should().BeEmpty();
            userStory.ParentId.Should().BeNull();
        }

        [Fact]
        public void TaskWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var task = new TaskWorkItem();

            task.Type.Should().Be(WorkItemType.Task);
            task.Title.Should().BeEmpty();
            task.ParentId.Should().BeNull();
        }

        [Theory]
        [InlineData(WorkItemType.Epic, 1)]
        [InlineData(WorkItemType.UserStory, 2)]
        [InlineData(WorkItemType.Task, 3)]
        public void WorkItemType_Values_ShouldBeCorrect(WorkItemType type, int expectedValue)
        {
            ((int)type).Should().Be(expectedValue);
        }

        [Fact]
        public void WorkItem_SetAllProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-2);
            var completedAt = DateTime.UtcNow.AddDays(-1);
            var parent = new EpicWorkItem();
            var children = new List<WorkItem> { new TaskWorkItem() };
            var comments = new List<Comment> { new Comment() };

            var task = new TaskWorkItem
            {
                Id = id,
                ProjectId = projectId,
                ParentId = parentId,
                Parent = parent,
                Children = children,
                Title = "Test Task",
                Description = "Test Description",
                AttachmentUrl = "https://example.com/attachment.pdf",
                IsCompleted = true,
                AssigneeId = assigneeId,
                Comments = comments,
                CreatedAt = createdAt,
                CompletedAt = completedAt
            };

            task.Id.Should().Be(id);
            task.ProjectId.Should().Be(projectId);
            task.ParentId.Should().Be(parentId);
            task.Parent.Should().BeSameAs(parent);
            task.Children.Should().BeSameAs(children);
            task.Title.Should().Be("Test Task");
            task.Description.Should().Be("Test Description");
            task.AttachmentUrl.Should().Be("https://example.com/attachment.pdf");
            task.IsCompleted.Should().BeTrue();
            task.AssigneeId.Should().Be(assigneeId);
            task.Comments.Should().BeSameAs(comments);
            task.CreatedAt.Should().Be(createdAt);
            task.CompletedAt.Should().Be(completedAt);
        }

        #endregion

        #region SubTask and Bug WorkItem Tests

        [Fact]
        public void SubTaskWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var subTask = new SubTaskWorkItem();
            subTask.Type.Should().Be(WorkItemType.SubTask);
        }

        [Theory]
        [InlineData(WorkItemType.SubTask, 4)]
        [InlineData(WorkItemType.Bug, 5)]
        public void WorkItemType_AdditionalValues_ShouldBeCorrect(WorkItemType type, int expectedValue)
        {
            ((int)type).Should().Be(expectedValue);
        }

        [Fact]
        public void BugWorkItem_DefaultValues_ShouldBeCorrect()
        {
            var bug = new BugWorkItem();
            bug.Type.Should().Be(WorkItemType.Bug);
            bug.Severity.Should().Be(BugSeverity.Medium);
            bug.StepsToReproduce.Should().BeNull();
            bug.ExpectedBehavior.Should().BeNull();
            bug.ActualBehavior.Should().BeNull();
            bug.Environment.Should().BeNull();
        }

        [Fact]
        public void BugWorkItem_SetProperties_ShouldWorkCorrectly()
        {
            var bug = new BugWorkItem
            {
                Title = "Login Crash",
                Severity = BugSeverity.Critical,
                StepsToReproduce = "Click login",
                ExpectedBehavior = "Login succeeds",
                ActualBehavior = "App crashes",
                Environment = "Chrome 120"
            };

            bug.Severity.Should().Be(BugSeverity.Critical);
            bug.StepsToReproduce.Should().Be("Click login");
            bug.Environment.Should().Be("Chrome 120");
        }

        [Theory]
        [InlineData(BugSeverity.Low, 1)]
        [InlineData(BugSeverity.Medium, 2)]
        [InlineData(BugSeverity.High, 3)]
        [InlineData(BugSeverity.Critical, 4)]
        public void BugSeverity_Values_ShouldBeCorrect(BugSeverity severity, int expected)
        {
            ((int)severity).Should().Be(expected);
        }

        #endregion

        #region Tenant Entity Tests

        [Fact]
        public void Tenant_DefaultValues_ShouldBeCorrect()
        {
            var tenant = new Tenant();
            tenant.Name.Should().BeEmpty();
            tenant.Subdomain.Should().BeEmpty();
            tenant.Tier.Should().Be(global::Domain.Enums.TenantTier.Starter);
            tenant.MaxUsers.Should().Be(5);
            tenant.MaxProjects.Should().Be(10);
            tenant.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Tenant_SetProperties_ShouldWorkCorrectly()
        {
            var id = Guid.NewGuid();
            var tenant = new Tenant
            {
                Id = id,
                Name = "Acme Corp",
                Subdomain = "acme",
                Tier = global::Domain.Enums.TenantTier.Enterprise,
                MaxUsers = -1,
                MaxProjects = -1,
                MaxStorageBytes = -1L,
                IsActive = false,
                Settings = "{\"theme\":\"dark\"}"
            };

            tenant.Id.Should().Be(id);
            tenant.Name.Should().Be("Acme Corp");
            tenant.Tier.Should().Be(global::Domain.Enums.TenantTier.Enterprise);
            tenant.MaxUsers.Should().Be(-1);
            tenant.Settings.Should().Contain("dark");
        }

        #endregion

        #region Asset Entity Tests

        [Fact]
        public void Asset_DefaultValues_ShouldBeCorrect()
        {
            var asset = new Asset();
            asset.Name.Should().BeEmpty();
            asset.AssetTag.Should().BeEmpty();
            asset.Status.Should().Be(global::Domain.Enums.AssetStatus.Available);
            asset.DepreciationMethod.Should().Be(global::Domain.Enums.DepreciationMethod.StraightLine);
            asset.UsefulLifeYears.Should().Be(5);
            asset.Category.Should().Be(global::Domain.Enums.AssetCategory.Physical);
            asset.IsActive.Should().BeTrue();
            asset.MaintenanceRecords.Should().NotBeNull().And.BeEmpty();
            asset.AssetHistory.Should().NotBeNull().And.BeEmpty();
            asset.AssetCheckouts.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Asset_SetAllProperties_ShouldWorkCorrectly()
        {
            var asset = new Asset
            {
                Name = "Laptop",
                AssetTag = "AST-001",
                SerialNumber = "SN123",
                Manufacturer = "Dell",
                Model = "XPS 15",
                Weight = 2.5m,
                Dimensions = "35x24x2 cm",
                BarcodeValue = "BC001",
                LicenseKey = "LK-123",
                LicensedSeats = 5,
                GridReference = "GR-001",
                Capacity = "100kW",
                RegulatoryId = "REG-001"
            };

            asset.SerialNumber.Should().Be("SN123");
            asset.Weight.Should().Be(2.5m);
            asset.LicenseKey.Should().Be("LK-123");
            asset.GridReference.Should().Be("GR-001");
        }

        #endregion

        #region BaseEntity Tests

        [Fact]
        public void BaseEntity_DefaultValues_ShouldBeCorrect()
        {
            var team = new Team();
            team.Id.Should().Be(Guid.Empty);
            team.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            team.CreatedBy.Should().BeEmpty();
            team.IsActive.Should().BeTrue();
        }

        #endregion

        #region Team and TeamMember Tests

        [Fact]
        public void Team_DefaultValues_ShouldBeCorrect()
        {
            var team = new Team();
            team.Name.Should().BeEmpty();
            team.Members.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void TeamMember_DefaultValues_ShouldBeCorrect()
        {
            var member = new TeamMember();
            member.Role.Should().BeEmpty();
            member.DomainExpertise.Should().BeNull();
            member.Skills.Should().BeNull();
            member.AvailabilityHoursPerWeek.Should().Be(0);
            member.CostRate.Should().Be(0);
        }

        #endregion

        #region Notification Entity Tests

        [Fact]
        public void Notification_DefaultValues_ShouldBeCorrect()
        {
            var notif = new Notification();
            notif.Message.Should().BeEmpty();
            notif.IsRead.Should().BeFalse();
            notif.RelatedEntityId.Should().BeNull();
        }

        [Theory]
        [InlineData(NotificationType.StateTransition, 1)]
        [InlineData(NotificationType.AssignmentChange, 2)]
        [InlineData(NotificationType.OverdueTask, 3)]
        [InlineData(NotificationType.Mention, 4)]
        [InlineData(NotificationType.Comment, 5)]
        [InlineData(NotificationType.General, 6)]
        public void NotificationType_Values_ShouldBeCorrect(NotificationType type, int expected)
        {
            ((int)type).Should().Be(expected);
        }

        #endregion

        #region Workflow Entity Tests

        [Fact]
        public void Workflow_DefaultValues_ShouldBeCorrect()
        {
            var workflow = new Workflow();
            workflow.Name.Should().BeEmpty();
            workflow.States.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void WorkflowState_DefaultValues_ShouldBeCorrect()
        {
            var state = new WorkflowState();
            state.Name.Should().BeEmpty();
            state.Color.Should().Be("#6B7280");
            state.IsInitial.Should().BeFalse();
            state.IsFinal.Should().BeFalse();
            state.AllowedTransitions.Should().BeNull();
            state.RequiredFields.Should().BeNull();
            state.NotifyOnEntry.Should().BeFalse();
        }

        #endregion

        #region Feedback Entity Tests

        [Fact]
        public void Feedback_DefaultValues_ShouldBeCorrect()
        {
            var feedback = new Feedback();
            feedback.Message.Should().BeEmpty();
            feedback.UserId.Should().BeNull();
            feedback.UserEmail.Should().BeNull();
            feedback.UserDisplayName.Should().BeNull();
            feedback.ProcessedAt.Should().BeNull();
        }

        #endregion

        #region LegalDocument Entity Tests

        [Fact]
        public void LegalDocument_DefaultValues_ShouldBeCorrect()
        {
            var doc = new LegalDocument();
            doc.Version.Should().BeEmpty();
            doc.Content.Should().BeEmpty();
            doc.IsActive.Should().BeFalse();
        }

        [Theory]
        [InlineData(global::Domain.Enums.LegalDocumentType.TermsOfService)]
        [InlineData(global::Domain.Enums.LegalDocumentType.PrivacyPolicy)]
        public void LegalDocumentType_Values_Exist(global::Domain.Enums.LegalDocumentType type)
        {
            type.Should().BeDefined();
        }

        #endregion

        #region CustomField Entity Tests

        [Fact]
        public void CustomField_DefaultValues_ShouldBeCorrect()
        {
            var field = new CustomField();
            field.Name.Should().BeEmpty();
            field.IsRequired.Should().BeFalse();
            field.Options.Should().BeNull();
            field.ValidationRule.Should().BeNull();
            field.EntityType.Should().BeNull();
            field.Values.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void CustomFieldValue_DefaultValues_ShouldBeCorrect()
        {
            var value = new CustomFieldValue();
            value.EntityType.Should().BeEmpty();
            value.Value.Should().BeNull();
        }

        #endregion

        #region Enum Coverage Tests

        [Theory]
        [InlineData(global::Domain.Enums.DomainType.IT, 1)]
        [InlineData(global::Domain.Enums.DomainType.Healthcare, 2)]
        [InlineData(global::Domain.Enums.DomainType.PublicSafety, 3)]
        [InlineData(global::Domain.Enums.DomainType.Construction, 4)]
        [InlineData(global::Domain.Enums.DomainType.Infrastructure, 5)]
        [InlineData(global::Domain.Enums.DomainType.EconomicDevelopment, 6)]
        [InlineData(global::Domain.Enums.DomainType.Technology, 7)]
        public void DomainType_Values_ShouldBeCorrect(global::Domain.Enums.DomainType type, int expected)
        {
            ((int)type).Should().Be(expected);
        }

        [Theory]
        [InlineData(global::Domain.Enums.TenantTier.Starter, 1)]
        [InlineData(global::Domain.Enums.TenantTier.Business, 2)]
        [InlineData(global::Domain.Enums.TenantTier.Enterprise, 3)]
        public void TenantTier_Values_ShouldBeCorrect(global::Domain.Enums.TenantTier tier, int expected)
        {
            ((int)tier).Should().Be(expected);
        }

        [Theory]
        [InlineData(global::Domain.Enums.AssetStatus.Available)]
        [InlineData(global::Domain.Enums.AssetStatus.InUse)]
        [InlineData(global::Domain.Enums.AssetStatus.UnderMaintenance)]
        [InlineData(global::Domain.Enums.AssetStatus.Retired)]
        public void AssetStatus_Values_AreDefined(global::Domain.Enums.AssetStatus status)
        {
            status.Should().BeDefined();
        }

        [Theory]
        [InlineData(global::Domain.Enums.AssetType.Equipment)]
        [InlineData(global::Domain.Enums.AssetType.Vehicle)]
        [InlineData(global::Domain.Enums.AssetType.ITHardware)]
        [InlineData(global::Domain.Enums.AssetType.Tool)]
        public void AssetType_Values_AreDefined(global::Domain.Enums.AssetType type)
        {
            type.Should().BeDefined();
        }

        [Theory]
        [InlineData(global::Domain.Enums.FieldType.Text)]
        [InlineData(global::Domain.Enums.FieldType.Number)]
        [InlineData(global::Domain.Enums.FieldType.Date)]
        [InlineData(global::Domain.Enums.FieldType.Dropdown)]
        public void FieldType_Values_AreDefined(global::Domain.Enums.FieldType type)
        {
            type.Should().BeDefined();
        }

        #endregion

        #region User Extended Properties Tests

        [Fact]
        public void User_LegalProperties_ShouldWorkCorrectly()
        {
            var user = new User
            {
                TimeZoneId = "Pacific/Auckland",
                TimeZoneOffset = 720,
                TermsAcceptedAt = DateTime.UtcNow,
                TermsVersion = "1.0",
                PrivacyAcceptedAt = DateTime.UtcNow,
                PrivacyVersion = "1.0",
                LegalAcceptanceIp = "192.168.1.1",
                UserName = "testuser"
            };

            user.TimeZoneId.Should().Be("Pacific/Auckland");
            user.TimeZoneOffset.Should().Be(720);
            user.TermsVersion.Should().Be("1.0");
            user.PrivacyVersion.Should().Be("1.0");
            user.LegalAcceptanceIp.Should().Be("192.168.1.1");
            user.UserName.Should().Be("testuser");
        }

        #endregion

        #region Project Extended Properties Tests

        [Fact]
        public void Project_ExtendedProperties_ShouldWorkCorrectly()
        {
            var project = new Project
            {
                DomainType = global::Domain.Enums.DomainType.Construction,
                EstimatedCost = 100000m,
                ActualCost = 80000m,
                WorkflowId = Guid.NewGuid(),
                TemplateId = Guid.NewGuid()
            };

            project.DomainType.Should().Be(global::Domain.Enums.DomainType.Construction);
            project.EstimatedCost.Should().Be(100000m);
            project.ActualCost.Should().Be(80000m);
            project.WorkflowId.Should().NotBeNull();
            project.Teams.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region WorkItem Budget Properties Tests

        [Fact]
        public void WorkItem_BudgetProperties_ShouldWorkCorrectly()
        {
            var epic = new EpicWorkItem
            {
                EstimatedCost = 50000m,
                ActualCost = 45000m,
                CurrentStateId = Guid.NewGuid()
            };

            epic.EstimatedCost.Should().Be(50000m);
            epic.ActualCost.Should().Be(45000m);
            epic.CurrentStateId.Should().NotBeNull();
            epic.Attachments.Should().NotBeNull().And.BeEmpty();
            epic.TimeEntries.Should().NotBeNull().And.BeEmpty();
            epic.CustomFieldValues.Should().NotBeNull().And.BeEmpty();
        }

        #endregion
    }
}
