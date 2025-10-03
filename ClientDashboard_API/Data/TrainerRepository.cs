using AutoMapper;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class TrainerRepository(DataContext context, IMapper mapper) : ITrainerRepository
    {
        public async Task<Trainer?> GetTrainerByEmail(string email)
        {
            var trainer = await context.Trainer.Where(x => x.Email == email).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<Trainer?> GetTrainerById(int id)
        {
            var trainer = await context.Trainer.Where(x => x.Id == id).FirstOrDefaultAsync();
            return trainer;
        }
    }
}
