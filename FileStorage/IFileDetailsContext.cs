using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace FileStorage
{
    public interface IFileDetailsContext : IDbContext
    {
        DbSet<FileDetails> FilesDetails { get; set; }
    }
}