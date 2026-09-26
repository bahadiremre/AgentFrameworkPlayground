using AgentFw.Data;
using AgentFw.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgentFw.Tests
{
    [Collection(AppCollection.Name)]
    public class TodoTests(AgentFwAppFactory app) : IAsyncLifetime
    {
        private const string HomeUrl = "/";

        private readonly HttpClient _client = app.CreateBrowserClient();

        public Task InitializeAsync() => app.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Add_CreatesOpenItem()
        {
            var response = await AddAsync("  Süt al  ");

            response.AssertRedirectsTo(HomeUrl);
            var todo = await SingleTodoAsync();
            Assert.Equal("Süt al", todo.Title);
            Assert.False(todo.IsCompleted);
        }

        [Fact]
        public async Task Add_EmptyTitle_IsRejected()
        {
            var response = await AddAsync("   ");

            await response.AssertShowsErrorAsync("Görev boş olamaz.");
            Assert.Equal(0, await app.QueryAsync(db => db.Todos.CountAsync()));
        }

        [Fact]
        public async Task Add_TooLongTitle_IsRejected()
        {
            var response = await AddAsync(new string('a', 201));

            await response.AssertShowsErrorAsync("Görev en fazla 200 karakter olabilir.");
        }

        [Fact]
        public async Task Toggle_FlipsCompletedState()
        {
            await AddAsync("Süt al");
            var id = (await SingleTodoAsync()).Id;

            await PostHandlerAsync("Toggle", id);
            Assert.True((await SingleTodoAsync()).IsCompleted);

            await PostHandlerAsync("Toggle", id);
            Assert.False((await SingleTodoAsync()).IsCompleted);
        }

        [Fact]
        public async Task Delete_RemovesItem()
        {
            await AddAsync("Süt al");
            var id = (await SingleTodoAsync()).Id;

            var response = await PostHandlerAsync("Delete", id);

            response.AssertRedirectsTo(HomeUrl);
            Assert.Equal(0, await app.QueryAsync(db => db.Todos.CountAsync()));
        }

        [Fact]
        public async Task HomePage_ListsOpenItemsBeforeCompletedOnes()
        {
            await AddAsync("Birinci");
            await AddAsync("İkinci");
            var firstId = (await app.QueryAsync(db => db.Todos.SingleAsync(t => t.Title == "Birinci"))).Id;
            await PostHandlerAsync("Toggle", firstId);

            var html = await (await _client.GetAsync(HomeUrl)).ReadDecodedAsync();

            Assert.True(html.IndexOf("İkinci", StringComparison.Ordinal) < html.IndexOf("Birinci", StringComparison.Ordinal));
            Assert.Contains("1 / 2 kaldı", html);
        }

        private Task<HttpResponseMessage> AddAsync(string title) =>
            _client.PostFormAsync(HomeUrl, "/?handler=Add", new Dictionary<string, string?> { ["NewTitle"] = title });

        private Task<HttpResponseMessage> PostHandlerAsync(string handler, int id) =>
            _client.PostFormAsync(HomeUrl, $"/?id={id}&handler={handler}", new Dictionary<string, string?>());

        private Task<TodoItem> SingleTodoAsync() => app.QueryAsync(db => db.Todos.AsNoTracking().SingleAsync());
    }
}
