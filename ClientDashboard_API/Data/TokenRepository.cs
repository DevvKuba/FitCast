using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class TokenRepository<TToken>(DataContext context) : ITokenRepository<TToken> where TToken : TokenBase
    {
        public async Task<TToken?> GetTokenByIdAsync(int tokenId)
        {
            var token = await context.Set<TToken>().Where(t => t.Id == tokenId).FirstOrDefaultAsync();
            return token;
        }

        public async Task<TToken?> GetTokenByTokenHashAsync(string tokenHash)
        {
            var token = await context.Set<TToken>().Where(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
            return token;
        }

        public async Task<List<TToken>> GetAllExpiredOrConsumedTokensAsync()
        {
            var invalidTokens = await context.Set<TToken>()
                .Where(t => DateTime.UtcNow >= t.ExpiresOnUtc ||
                t.IsConsumed == true)
                .ToListAsync();
            return invalidTokens;
        }

        public async Task AddTokenAsync(TToken token)
        {
            await context.AddAsync(token);
        }

        public void RemoveToken(TToken token)
        {
            context.Remove(token);
        }
    }
}
