using System;
using FluentAssertions;
using Infrastructure.Sql;
using Xunit;

namespace Application.UnitTests.Infrastructure
{
    public class SqlLoaderTests
    {
        [Fact]
        public void Load_EnsureBaseSchema_ShouldReturnSqlContent()
        {
            // Act
            var sql = SqlLoader.EnsureBaseSchema;

            // Assert
            sql.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Load_DropSchema_ShouldReturnSqlContent()
        {
            // Act
            var sql = SqlLoader.DropSchema;

            // Assert
            sql.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Load_CreateSchema_ShouldReturnSqlContent()
        {
            // Act
            var sql = SqlLoader.CreateSchema;

            // Assert
            sql.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Load_GrantSchema_ShouldReturnSqlContent()
        {
            // Act
            var sql = SqlLoader.GrantSchema;

            // Assert
            sql.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Load_WipeData_ShouldReturnSqlContent()
        {
            // Act
            var sql = SqlLoader.WipeData;

            // Assert
            sql.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Load_InvalidResource_ShouldThrowInvalidOperationException()
        {
            // Act
            var act = () => SqlLoader.Load("NonExistent.sql");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public void Load_SameResourceMultipleTimes_ShouldReturnConsistentContent()
        {
            // Act
            var sql1 = SqlLoader.EnsureBaseSchema;
            var sql2 = SqlLoader.EnsureBaseSchema;

            // Assert
            sql1.Should().Be(sql2);
        }
    }
}
