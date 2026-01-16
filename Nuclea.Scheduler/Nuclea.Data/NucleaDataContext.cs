using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;
using Nuclea.Data.Interfaces;

namespace Nuclea.Data
{
    public sealed class NucleaDataContext(
        DbContextOptions<NucleaDataContext> options,
        IHttpContextAccessor httpContextAccessor)
        : DbContext(options)
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                    var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}