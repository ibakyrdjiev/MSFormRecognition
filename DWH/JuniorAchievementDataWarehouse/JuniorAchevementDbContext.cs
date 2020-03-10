namespace JuniorAchievementDataWarehouse
{
    using JuniorAchevement.Entities;
    using Microsoft.EntityFrameworkCore;

    public class JuniorAchevementDbContext : DbContext
    {
        public DbSet<Answer> Answers { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<Template> Templates { get; set; }

        public DbSet<QuestionAnswers> QuestionAnswers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=tcp:wbc-demo.database.windows.net,1433;Initial Catalog=junior-achievement;Persist Security Info=False;User ID=ibakyrdjiev;Password=59nNcKAKMB;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }
    }
}