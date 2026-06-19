using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.SoftDelete.Tests;

public class SoftDeleteTests
{
    [Fact]
    public void Delete_SetsIsDeletedTrue()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        db.Posts.Remove(post);
        db.SaveChanges();

        var result = db.Posts.IgnoreQueryFilters().Single();
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public void Delete_SetsDeletedAtToUtcNow()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        var before = DateTime.UtcNow;
        db.Posts.Remove(post);
        db.SaveChanges();
        var after = DateTime.UtcNow;

        var result = db.Posts.IgnoreQueryFilters().Single();
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt!.Value, before, after);
    }

    [Fact]
    public void Delete_DoesNotRemoveRowFromDatabase()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        db.Posts.Remove(post);
        db.SaveChanges();

        var count = db.Posts.IgnoreQueryFilters().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public void GlobalFilter_HidesSoftDeletedRecords()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        db.Posts.Remove(post);
        db.SaveChanges();

        var count = db.Posts.Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public void GlobalFilter_ReturnsEmptyWhenAllDeleted()
    {
        using var db = TestDbContextFactory.Create();
        db.Posts.AddRange(new Post { Title = "A" }, new Post { Title = "B" });
        db.SaveChanges();

        foreach (var post in db.Posts.ToList())
            db.Posts.Remove(post);
        db.SaveChanges();

        Assert.Empty(db.Posts.ToList());
    }

    [Fact]
    public void IncludeDeleted_ReturnsSoftDeletedRecords()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        db.Posts.Remove(post);
        db.SaveChanges();

        var results = db.IncludeDeleted<Post>().ToList();
        Assert.Single(results);
        Assert.True(results[0].IsDeleted);
    }

    [Fact]
    public void IncludeDeleted_ReturnsBothActiveAndDeleted()
    {
        using var db = TestDbContextFactory.Create();
        db.Posts.AddRange(new Post { Title = "Active" }, new Post { Title = "ToDelete" });
        db.SaveChanges();

        var toDelete = db.Posts.First(p => p.Title == "ToDelete");
        db.Posts.Remove(toDelete);
        db.SaveChanges();

        var results = db.IncludeDeleted<Post>().ToList();
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void OnlyDeleted_ReturnsOnlySoftDeletedRecords()
    {
        using var db = TestDbContextFactory.Create();
        db.Posts.AddRange(new Post { Title = "Active" }, new Post { Title = "ToDelete" });
        db.SaveChanges();

        var toDelete = db.Posts.First(p => p.Title == "ToDelete");
        db.Posts.Remove(toDelete);
        db.SaveChanges();

        var results = db.Posts.OnlyDeleted().ToList();
        Assert.Single(results);
        Assert.Equal("ToDelete", results[0].Title);
    }

    [Fact]
    public void Restore_ClearsIsDeletedAndDeletedAt()
    {
        using var db = TestDbContextFactory.Create();
        var post = new Post { Title = "Hello" };
        db.Posts.Add(post);
        db.SaveChanges();

        db.Posts.Remove(post);
        db.SaveChanges();

        var deleted = db.Posts.IgnoreQueryFilters().Single();
        deleted.Restore();
        db.SaveChanges();

        var restored = db.Posts.Single();
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAt);
    }

    [Fact]
    public void Delete_MultipleEntities_AllSoftDeleted()
    {
        using var db = TestDbContextFactory.Create();
        db.Posts.AddRange(new Post { Title = "A" }, new Post { Title = "B" }, new Post { Title = "C" });
        db.SaveChanges();

        db.Posts.RemoveRange(db.Posts.ToList());
        db.SaveChanges();

        var all = db.Posts.IgnoreQueryFilters().ToList();
        Assert.Equal(3, all.Count);
        Assert.All(all, p => Assert.True(p.IsDeleted));
    }
}
