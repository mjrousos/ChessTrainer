using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using IngestionFunctions;
using IngestionFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MjrChess.Engine;
using MjrChess.Engine.Models;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;
using Xunit;

namespace IngestionFunctions.Test
{
    public class IngestionFunctionsTests
    {
        [Fact]
        public async Task AddPlayersGames_WhenPlayerIdIsInvalid_ReturnsBadRequest()
        {
            var sut = CreateFunctions(new StubPlayerRepository());
            var request = new DefaultHttpContext().Request;
            request.QueryString = new QueryString("?PlayerId=not-an-int");

            var result = await sut.AddPlayersGames(request);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task GetPlayerGamesAsync_WhenPlayerNameIsEmpty_ThrowsArgumentException()
        {
            var service = new LiChessService(
                new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, string.Empty)),
                NullLogger<LiChessService>.Instance,
                new ChessEngine());

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (var game in service.GetPlayerGamesAsync(string.Empty, null, 1))
                {
                    _ = game;
                }
            });
        }

        [Fact]
        public async Task GetPlayerGamesAsync_WhenResponseContainsPgn_YieldsParsedGame()
        {
            const string Pgn = """
                [Event "Rated Blitz game"]
                [Site "https://lichess.org/testgame"]
                [Date "2026.01.02"]
                [Round "-"]
                [White "whitePlayer"]
                [Black "blackPlayer"]
                [Result "1-0"]
                [UTCDate "2026.01.02"]
                [UTCTime "03:04:05"]
                [WhiteElo "1500"]
                [BlackElo "1500"]
                [TimeControl "180+2"]
                [ECO "C20"]
                [Termination "Normal"]
                
                1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 1-0
                """;

            var service = new LiChessService(
                new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, Pgn)),
                NullLogger<LiChessService>.Instance,
                new ChessEngine());

            var games = new List<ChessGame>();
            await foreach (var game in service.GetPlayerGamesAsync("whitePlayer", null, 1))
            {
                games.Add(game);
            }

            var parsed = Assert.Single(games);
            Assert.Equal("https://lichess.org/testgame", parsed.Site);
            Assert.Equal(2026, parsed.StartDate.Year);
            Assert.Equal(1, parsed.StartDate.Month);
            Assert.Equal(2, parsed.StartDate.Day);
            Assert.Equal(3, parsed.StartDate.Hour);
            Assert.Equal(4, parsed.StartDate.Minute);
            Assert.Equal(5, parsed.StartDate.Second);
        }

        [Fact]
        public async Task GetPlayerGamesAsync_WhenCalledForChessCom_ReturnsNoGames()
        {
            var service = new ChessComService(
                new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, string.Empty)),
                NullLogger<ChessComService>.Instance,
                new ChessEngine());

            var games = new List<ChessGame>();
            await foreach (var game in service.GetPlayerGamesAsync("player", null, 10))
            {
                games.Add(game);
            }

            Assert.Empty(games);
        }

        private static GameIngestionFunctions CreateFunctions(IRepository<Player> playerRepository)
        {
            return new GameIngestionFunctions(
                playerRepository,
                new TableClient("UseDevelopmentStorage=true", "games"),
                new QueueClient("UseDevelopmentStorage=true", "games"),
                _ => new StubChessService(),
                NullLogger<GameIngestionFunctions>.Instance);
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content),
                });
            }
        }

        private sealed class StubPlayerRepository : IRepository<Player>
        {
            public Task<Player> AddAsync(Player item) => throw new NotSupportedException();

            public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();

            public Task<Player?> GetAsync(int id) => Task.FromResult<Player?>(null);

            public IQueryable<Player> Query() => Enumerable.Empty<Player>().AsQueryable();

            public IQueryable<Player> Query(Expression<Func<Player, bool>>? filter) => Query();

            public Task<Player?> UpdateAsync(Player item) => throw new NotSupportedException();
        }

        private sealed class StubChessService : IChessService
        {
            public async IAsyncEnumerable<ChessGame> GetPlayerGamesAsync(string playerName, DateTimeOffset? since, int max)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }
}
