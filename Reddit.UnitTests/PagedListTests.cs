using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Reddit.Models;
using Reddit.Repositories;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Reddit.UnitTests
{

    public class PagedListTests
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly IQueryable<Post> _postsAsQuarable;

        public PagedListTests()
        {
            var dbName = Guid.NewGuid().ToString();     // give unqie name to the database, so that different tests don't interfere with each other
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _dbContext.Posts.Add(new Post { Title = "Title 1", Content = "Content 1", Upvote = 5, Downvote = 1 });
            _dbContext.Posts.Add(new Post { Title = "Title 2", Content = "Content 1", Upvote = 12, Downvote = 1 });
            _dbContext.Posts.Add(new Post { Title = "Title 3", Content = "Content 1", Upvote = 3, Downvote = 1 });
            _dbContext.Posts.Add(new Post { Title = "Title 4", Content = "Content 1", Upvote = 221, Downvote = 1 }); // 221 / 222 = 0.9954954954954955
            _dbContext.Posts.Add(new Post { Title = "Title 5", Content = "Content 1", Upvote = 5, Downvote = 2123 }); // 5 / 2123 = 0.002356344
            _dbContext.SaveChanges();

            _postsAsQuarable = _dbContext.Posts.AsQueryable();
        }

        [Fact]
        public async void PagedList_CreateAsync_Success()
        {
            //Arrange
            var items = _dbContext.Posts.AsQueryable();
            var pgNumber = 1;
            var pgSize = 10;

            //Act
            var pagedList = await PagedList<Post>.CreateAsync(items, pgNumber, pgSize);

            //Assert 
            //using FluentAssertions
            pagedList.Items.Should().NotBeNull();
            pagedList.HasPreviousPage.Should().BeFalse();
            pagedList.HasNextPage.Should().BeFalse();
            pagedList.TotalCount.Should().Be(5);
            pagedList.TotalCount.Should().BeLessThan(pgSize);
        }

        [Fact]
        public async void PagedList_CreateAsync_ReturnsEmptyItems()
        {
            //Arrange
            var items = _dbContext.Posts.Where(p => p.Title == "mock empty list").AsQueryable(); //mezarena fakeiteasy
            var pageNumber = 1;
            var pgSize = 3;

            //Act
            var pagedList = await PagedList<Post>.CreateAsync(items, pageNumber, pgSize);


            //Assert
            pagedList.Should().NotBeNull();
            pagedList.Items.Should().BeEmpty();
        }



        [Fact]
        public async void PagedList_CreateAsync_TotalCountMoreThanPageSize()
        {

            //Arrange
            var items = _dbContext.Posts.AsQueryable();
            var pageNumber = 1;
            var pgSize = 2;

            //Act
            var pagedList = await PagedList<Post>.CreateAsync(items, pageNumber, pgSize);

            //Assert
            pagedList.TotalCount.Should().BeGreaterThan(pgSize);

        }

        [Fact]
        public async void PagedList_CreateAsync_ArgumentOutOfRangeException1()
        {

            //Arrange
            var items = _dbContext.Posts.AsQueryable();
            var pageNumber = 1;
            var pgSize = -1;

            //Act
            var act = async () => await PagedList<Post>.CreateAsync(items, pageNumber, pgSize);

            //Assert
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async void PagedList_CreateAsync_ArgumentOutOfRangeException2()
        {

            //Arrange
            var items = _dbContext.Posts.AsQueryable();
            var pageNumber = 5;
            var pgSize = 100;

            //Act
            var act = async () => await PagedList<Post>.CreateAsync(items, pageNumber, pgSize);

            //Assert
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

    }
}


