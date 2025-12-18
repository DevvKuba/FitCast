using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientDashboard_API.Helpers
{
    internal sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
    {
        public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
        {
            builder.HasKey(e => e.Id);

            // use-case for re-sending token
            // where trainer has multiple assigned verification tokens
            builder.HasOne(e => e.Trainer).WithMany().HasForeignKey(e => e.TrainerId);
        }
    }
}
