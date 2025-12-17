-- Users
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid NOT NULL,
    "Email" text NOT NULL,
    "PasswordHash" text NOT NULL,
    "DisplayName" text NOT NULL,
    "Role" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

-- Projects
CREATE TABLE IF NOT EXISTS "Projects" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text NULL,
    "OwnerId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Projects" PRIMARY KEY ("Id")
);

-- WorkItems (replaces Tasks with Epic/UserStory/Task hierarchy)
CREATE TABLE IF NOT EXISTS "WorkItems" (
    "Id" uuid NOT NULL,
    "ProjectId" uuid NOT NULL,
    "ParentId" uuid NULL,
    "Title" text NOT NULL,
    "Description" text NULL,
    "AttachmentUrl" text NULL,
    "IsCompleted" boolean NOT NULL,
    "AssigneeId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone NULL,
    "Type" integer NOT NULL,
    CONSTRAINT "PK_WorkItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkItems_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorkItems_WorkItems_ParentId" FOREIGN KEY ("ParentId") REFERENCES "WorkItems" ("Id") ON DELETE RESTRICT
);

-- Comments (depends on WorkItems)
CREATE TABLE IF NOT EXISTS "Comments" (
    "Id" uuid NOT NULL,
    "WorkItemId" uuid NOT NULL,
    "AuthorId" uuid NOT NULL,
    "Body" text NOT NULL,
    "MentionedUserIds" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Comments_WorkItems_WorkItemId" FOREIGN KEY ("WorkItemId") REFERENCES "WorkItems" ("Id") ON DELETE CASCADE
);

-- MentionNotifications (for @mention notifications)
CREATE TABLE IF NOT EXISTS "MentionNotifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CommentId" uuid NOT NULL,
    "WorkItemId" uuid NOT NULL,
    "MentionedByUserId" uuid NOT NULL,
    "CommentBody" text NOT NULL,
    "WorkItemTitle" text NOT NULL,
    "MentionedByName" text NOT NULL,
    "IsRead" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MentionNotifications" PRIMARY KEY ("Id")
);

-- Indexes
CREATE INDEX IF NOT EXISTS "IX_WorkItems_ProjectId" ON "WorkItems" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_WorkItems_ParentId" ON "WorkItems" ("ParentId");
CREATE INDEX IF NOT EXISTS "IX_Comments_WorkItemId" ON "Comments" ("WorkItemId");
CREATE INDEX IF NOT EXISTS "IX_MentionNotifications_UserId" ON "MentionNotifications" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_MentionNotifications_UserId_IsRead" ON "MentionNotifications" ("UserId", "IsRead");
