using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        async static Task Main(string[] args)
        {
            await using (var db = new SampleDbContext())
            {
                await db.Database.EnsureCreatedAsync();

                Expression<Func<IBook, BookViewModel>> projection = b => new BookViewModel
                {
                    FirstPage = b.FrontCover.Illustrations.FirstOrDefault(i => i.State >= IllustrationState.Approved) != null
                        ? new PageViewModel
                        {
                            Uri = b.FrontCover.Illustrations.FirstOrDefault(i => i.State >= IllustrationState.Approved).Uri
                        }
                        : null,
                };

                var result = await db.Books.Where(b => b.Id == 1).Select(projection).SingleOrDefaultAsync();
            }
        }
    }

    public class BookViewModel
    {
        public PageViewModel FirstPage { get; set; }
    }

    public class PageViewModel
    {
        public string Uri { get; set; }
    }

    public class SampleDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(local);Database=Sample.Bug;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.NoAction;
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<BookCover> BookCovers { get; set; }
        public DbSet<CoverIllustration> CoverIllustrations { get; set; }
    }

    public interface IBook
    {
        public int Id { get; set; }

        public IBookCover FrontCover { get; }
        public int FrontCoverId { get; set; }

        public IBookCover BackCover { get; }
        public int BackCoverId { get; set; }
    }

    public interface IBookCover
    {
        public int Id { get; set; }
        public IEnumerable<ICoverIllustration> Illustrations { get; }
    }

    public interface ICoverIllustration
    {
        public int Id { get; set; }
        public IBookCover Cover { get; }
        public int CoverId { get; set; }
        public string Uri { get; set; }
        public IllustrationState State { get; set; }
    }

    public class Book : IBook
    {
        public int Id { get; set; }

        public BookCover FrontCover { get; set; }
        public int FrontCoverId { get; set; }

        public BookCover BackCover { get; set; }
        public int BackCoverId { get; set; }

        IBookCover IBook.FrontCover => FrontCover;
        IBookCover IBook.BackCover => BackCover;
    }

    public class BookCover : IBookCover
    {
        public int Id { get; set; }
        public ICollection<CoverIllustration> Illustrations { get; set; }
        IEnumerable<ICoverIllustration> IBookCover.Illustrations => Illustrations;
    }

    public class CoverIllustration : ICoverIllustration
    {
        public int Id { get; set; }
        public BookCover Cover { get; set; }
        public int CoverId { get; set; }
        public string Uri { get; set; }
        public IllustrationState State { get; set; }

        IBookCover ICoverIllustration.Cover => Cover;
    }

    public enum IllustrationState
    {
        New,
        PendingApproval,
        Approved,
        Printed
    }
}
