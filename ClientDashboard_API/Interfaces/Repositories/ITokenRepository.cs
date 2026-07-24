using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;

namespace ClientDashboard_API.Interfaces
{
    public interface ITokenRepository<TToken> where TToken : TokenBase
    {
        Task<TToken?> GetTokenByIdAsync(int tokenId);

        Task<TToken?> GetTokenByTokenHashAsync(string tokenHash);

        Task<List<TToken>> GetAllExpiredOrConsumedTokensAsync();

        Task AddTokenAsync(TToken token);

        void RemoveToken(TToken token);
    }
}
