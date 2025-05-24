using Microsoft.EntityFrameworkCore;


namespace Enquiry.API.Models
{
    public class EnquiryDbContext: DbContext
    {
        public EnquiryDbContext(DbContextOptions<EnquiryDbContext> options) : base(options)
        {
        }

        public DbSet<EnquiryStatus> EnquiryStatuses { get; set; }
        public DbSet<EnquiryType> EnquiryTypes { get; set; }
        public DbSet<Enquiry> Enquiries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LoyalClient> LoyalClients { get; set; }
        public DbSet<PromotionSent> PromotionsSent { get; set; }
        public DbSet<AppConfig> AppConfig { get; set; }
        public DbSet<EnquiryImage> EnquiryImages { get; set; }

    }
}
