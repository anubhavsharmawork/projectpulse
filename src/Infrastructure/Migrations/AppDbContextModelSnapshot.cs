using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0");

            modelBuilder.Entity("Domain.Entities.Comment", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid");
                    b.Property<Guid>("AuthorId").HasColumnType("uuid");
                    b.Property<string>("Body").IsRequired().HasColumnType("text");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<Guid>("TaskItemId").HasColumnType("uuid");
                    b.HasKey("Id");
                    b.HasIndex("TaskItemId");
                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Domain.Entities.Project", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("Description").HasColumnType("text");
                    b.Property<string>("Name").IsRequired().HasColumnType("text");
                    b.Property<Guid>("OwnerId").HasColumnType("uuid");
                    b.HasKey("Id");
                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Domain.Entities.TaskItem", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid");
                    b.Property<Guid?>("AssigneeId").HasColumnType("uuid");
                    b.Property<string>("AttachmentUrl").HasColumnType("text");
                    b.Property<DateTime?>("CompletedAt").HasColumnType("timestamp with time zone");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("Description").HasColumnType("text");
                    b.Property<bool>("IsCompleted").HasColumnType("boolean");
                    b.Property<Guid>("ProjectId").HasColumnType("uuid");
                    b.Property<string>("Title").IsRequired().HasColumnType("text");
                    b.HasKey("Id");
                    b.HasIndex("ProjectId");
                    b.ToTable("Tasks");
                });

            modelBuilder.Entity("Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("DisplayName").IsRequired().HasColumnType("text");
                    b.Property<string>("Email").IsRequired().HasColumnType("text");
                    b.Property<string>("PasswordHash").IsRequired().HasColumnType("text");
                    b.Property<int>("Role").HasColumnType("integer");
                    b.HasKey("Id");
                    b.ToTable("Users");
                });

            modelBuilder.Entity("Domain.Entities.TaskItem", b =>
                {
                    b.HasOne("Domain.Entities.Project", null)
                        .WithMany("Tasks")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Comment", b =>
                {
                    b.HasOne("Domain.Entities.TaskItem", null)
                        .WithMany("Comments")
                        .HasForeignKey("TaskItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Project", b =>
                {
                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Domain.Entities.TaskItem", b =>
                {
                    b.Navigation("Comments");
                });
#pragma warning restore 612, 618
        }
    }
}
