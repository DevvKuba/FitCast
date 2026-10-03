using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    // covers the shared ITokenRepository<PasswordResetToken> surface (inherited from
    // TokenRepository<PasswordResetToken>). No password-reset-specific queries exist yet.
    // IsValid()/Consume() now live on TokenBase and are covered once, generically, in
    // TokenBaseTests.cs rather than duplicated per repo.
    public class PasswordResetTokenRepositoryTests
    {
        private readonly DataContext _context;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly UnitOfWork _unitOfWork;

        public PasswordResetTokenRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _passwordResetTokenRepository = testUnitOfWork.passwordResetTokenRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
        }

        [Fact]
        public async Task TestAddPasswordResetTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };

            await _passwordResetTokenRepository.AddTokenAsync(token);
            await _unitOfWork.Complete();

            var savedToken = await _context.PasswordResetToken.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(trainer.Id, savedToken.UserId);
            Assert.True(savedToken.CreatedOnUtc <= DateTime.UtcNow);
            Assert.True(savedToken.ExpiresOnUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByIdAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _passwordResetTokenRepository.GetTokenByIdAsync(token.Id);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
            Assert.Equal(trainer.Id, retrievedToken.UserId);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByIdReturnsNullForNonExistentIdAsync()
        {
            var token = await _passwordResetTokenRepository.GetTokenByIdAsync(999);

            Assert.Null(token);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();
            var tokenHash = TokenGenerator.HashToken(rawToken);

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = tokenHash,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _passwordResetTokenRepository.GetTokenByTokenHashAsync(tokenHash);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashReturnsNullForUnknownHashAsync()
        {
            var retrievedToken = await _passwordResetTokenRepository.GetTokenByTokenHashAsync(TokenGenerator.HashToken(TokenGenerator.GenerateToken()));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashDoesNotMatchADifferentRawTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var storedRawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(storedRawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var differentRawToken = TokenGenerator.GenerateToken();
            var retrievedToken = await _passwordResetTokenRepository.GetTokenByTokenHashAsync(TokenGenerator.HashToken(differentRawToken));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestRemoveTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            _passwordResetTokenRepository.RemoveToken(token);
            await _unitOfWork.Complete();

            var remainingToken = await _context.PasswordResetToken.FindAsync(token.Id);
            Assert.Null(remainingToken);
        }

        [Fact]
        public async Task TestGetAllExpiredOrConsumedTokensReturnsOnlyInvalidTokensAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var validToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };
            var expiredToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1),
                IsConsumed = false
            };
            var consumedToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = true,
                ConsumedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            await _context.PasswordResetToken.AddRangeAsync(validToken, expiredToken, consumedToken);
            await _unitOfWork.Complete();

            var invalidTokens = await _passwordResetTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            Assert.Equal(2, invalidTokens.Count);
            Assert.Contains(invalidTokens, t => t.Id == expiredToken.Id);
            Assert.Contains(invalidTokens, t => t.Id == consumedToken.Id);
            Assert.DoesNotContain(invalidTokens, t => t.Id == validToken.Id);
        }
    }
}
