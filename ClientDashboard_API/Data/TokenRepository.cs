using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class TokenRepository<TToken>(DataContext context) : ITokenRepository<TToken> where TToken : TokenBase
    {
        // the DbSet for this token type, exposed to derived repositories so they can add
        // entity-specific queries without capturing their own copy of the DataContext
        protected DbSet<TToken> Entities => context.Set<TToken>();

        public async Task<TToken?> GetTokenByIdAsync(int tokenId)
        {
            var token = await Entities.Where(t => t.Id == tokenId).FirstOrDefaultAsync();
            return token;
        }

        public async Task<TToken?> GetTokenByTokenHashAsync(string tokenHash)
        {
            var token = await Entities.Where(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
            return token;
        }

        public async Task<List<TToken>> GetAllExpiredOrConsumedTokensAsync()
        {
            var invalidTokens = await Entities
                .Where(t => DateTime.UtcNow >= t.ExpiresOnUtc ||
                t.IsConsumed == true)
                .ToListAsync();
            return invalidTokens;
        }

        public async Task AddTokenAsync(TToken token)
        {
            await Entities.AddAsync(token);
        }

        public void RemoveToken(TToken token)
        {
            Entities.Remove(token);
        }
    }
}
