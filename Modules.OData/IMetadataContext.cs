using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Data;

namespace Module.Metadata
{
    public interface IMetadataContext<TMetadata, TUser> : IDbContext
        where TMetadata : MetadataModel<TUser>
        where TUser : IdentityUser
    {
        DbSet<TMetadata> Metadata { get; set; }
    }
}
