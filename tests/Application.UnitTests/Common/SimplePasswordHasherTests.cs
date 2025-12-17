using Application.Common.Security;
using FluentAssertions;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Application.UnitTests.Common
{
    public class SimplePasswordHasherTests
    {
        [Fact]
        public void Hash_WithValidInputs_ShouldReturnBCryptHash()
        {
            var password = "testPassword123";
            var salt = "test-salt";

            var hash = SimplePasswordHasher.Hash(password, salt);

            hash.Should().NotBeNullOrEmpty();
            // BCrypt hashes start with $2a$, $2b$, or $2y$ followed by cost factor
            hash.Should().StartWith("$2");
            hash.Should().Contain("$"); // BCrypt format contains sections separated by $
        }

        [Fact]
        public void Hash_SameInputs_ShouldReturnDifferentHashes_DueToBCryptSalt()
        {
            var password = "testPassword123";
            var salt = "test-salt";

            var hash1 = SimplePasswordHasher.Hash(password, salt);
            var hash2 = SimplePasswordHasher.Hash(password, salt);

            // BCrypt uses random salt, so same input produces different hashes
            // But both should verify correctly
            hash1.Should().NotBe(hash2);
            SimplePasswordHasher.Verify(password, salt, hash1).Should().BeTrue();
            SimplePasswordHasher.Verify(password, salt, hash2).Should().BeTrue();
        }

        [Fact]
        public void Hash_DifferentPasswords_ShouldReturnDifferentHashes()
        {
            var salt = "test-salt";

            var hash1 = SimplePasswordHasher.Hash("password1", salt);
            var hash2 = SimplePasswordHasher.Hash("password2", salt);

            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void Hash_DifferentSalts_ShouldReturnDifferentHashes()
        {
            var password = "testPassword123";

            var hash1 = SimplePasswordHasher.Hash(password, "salt1");
            var hash2 = SimplePasswordHasher.Hash(password, "salt2");

            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void Hash_EmptyPassword_ShouldStillWork()
        {
            var hash = SimplePasswordHasher.Hash("", "salt");

            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Hash_EmptySalt_ShouldStillWork()
        {
            var hash = SimplePasswordHasher.Hash("password", "");

            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Verify_CorrectPassword_ShouldReturnTrue()
        {
            var password = "testPassword123";
            var salt = "test-salt";
            var hash = SimplePasswordHasher.Hash(password, salt);

            var result = SimplePasswordHasher.Verify(password, salt, hash);

            result.Should().BeTrue();
        }

        [Fact]
        public void Verify_WrongPassword_ShouldReturnFalse()
        {
            var password = "testPassword123";
            var salt = "test-salt";
            var hash = SimplePasswordHasher.Hash(password, salt);

            var result = SimplePasswordHasher.Verify("wrongPassword", salt, hash);

            result.Should().BeFalse();
        }

        [Fact]
        public void Verify_WrongSalt_ShouldReturnFalse()
        {
            var password = "testPassword123";
            var hash = SimplePasswordHasher.Hash(password, "correct-salt");

            var result = SimplePasswordHasher.Verify(password, "wrong-salt", hash);

            result.Should().BeFalse();
        }

        [Fact]
        public void Verify_WrongHash_ShouldReturnFalse()
        {
            var password = "testPassword123";
            var salt = "test-salt";

            var result = SimplePasswordHasher.Verify(password, salt, "invalidhash");

            result.Should().BeFalse();
        }

        [Fact]
        public void Verify_LegacySha256Hash_ShouldStillWork()
        {
            // Test backward compatibility with legacy SHA256 hashes
            var password = "testPassword123";
            var salt = "test-salt";
            
            // Generate a legacy SHA256 hash manually
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{password}|{salt}");
            var legacyHash = Convert.ToBase64String(sha.ComputeHash(bytes));

            // Should still verify legacy hashes
            var result = SimplePasswordHasher.Verify(password, salt, legacyHash);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("simple")]
        [InlineData("Complex@Password#123!")]
        [InlineData("unicode-??-??????")]
        [InlineData("   spaces   ")]
        public void Hash_VariousPasswords_ShouldWork(string password)
        {
            var salt = "test-salt";

            var hash = SimplePasswordHasher.Hash(password, salt);
            var verified = SimplePasswordHasher.Verify(password, salt, hash);

            hash.Should().NotBeNullOrEmpty();
            verified.Should().BeTrue();
        }
    }
}
