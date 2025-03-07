using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.WebSockets;
using Banking.Worker.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net.WebSockets;
using System.Security.Claims;

public class WebSocketControllerTests
{
    private readonly Mock<IWebSocketService> _webSocketServiceMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly DefaultHttpContext _httpContext;
    private readonly WebSocketController _controller;

    public WebSocketControllerTests()
    {
        _webSocketServiceMock = new Mock<IWebSocketService>();
        _authServiceMock = new Mock<IAuthService>();

        _httpContext = new DefaultHttpContext();
        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(false)); // WebSocket disabled

        _controller = new WebSocketController(_webSocketServiceMock.Object, _authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    [Fact]
    public async Task Connect_ShouldReturn400_WhenNotWebSocketRequest()
    {
        // Arrange: WebSocket not supported
        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(false));

        // Act
        await _controller.ConnectAsync();

        // Assert
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Connect_ShouldReturn401_WhenNoTokenProvided()
    {
        // Arrange: Websoket supported, but no token provided
        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(true));
        _httpContext.Request.QueryString = new QueryString("?access_token=");

        // Act
        await _controller.ConnectAsync();

        // Assert
        _httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Connect_ShouldReturn401_WhenTokenIsInvalid()
    {
        // Arrange
        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(true));
        _httpContext.Request.QueryString = new QueryString("?access_token=invalid_token");

        _authServiceMock.Setup(s => s.ValidateJwtToken("invalid_token")).Returns((ClaimsPrincipal?)null);

        // Act
        await _controller.ConnectAsync();

        // Assert
        _httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Connect_ShouldAcceptWebSocket_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(true));
        _httpContext.Request.QueryString = new QueryString("?access_token=valid_token");

        _authServiceMock.Setup(s => s.ValidateJwtToken("valid_token")).Returns(claimsPrincipal);
        _webSocketServiceMock.Setup(s => s.HandleWebSocketAsync(userId, It.IsAny<WebSocket>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ConnectAsync();

        // Assert
        _httpContext.Response.StatusCode.Should().Be(200); // Switching Protocols
    }

    [Fact]
    public async Task Connect_ShouldCallWebSocketService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        _httpContext.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(true));
        _httpContext.Request.QueryString = new QueryString("?access_token=valid_token");

        _authServiceMock.Setup(s => s.ValidateJwtToken("valid_token")).Returns(claimsPrincipal);

        // Act
        await _controller.ConnectAsync();

        // Assert
        _webSocketServiceMock.Verify(s => s.HandleWebSocketAsync(userId, It.IsAny<WebSocket>()), Times.Once);
    }
}

/// <summary>
/// Fake WebSocketFeature for testing
/// </summary>
public class FakeWebSocketFeature : IHttpWebSocketFeature
{
    private readonly bool _supportsWebSockets;

    public FakeWebSocketFeature(bool supportsWebSockets)
    {
        _supportsWebSockets = supportsWebSockets;
    }

    public bool IsWebSocketRequest => _supportsWebSockets;

    public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
    {
        return Task.FromResult<WebSocket>(new FakeWebSocket());
    }

}

/// <summary>
/// Fake WebSocket for testing purposes
/// </summary>
public class FakeWebSocket : WebSocket
{
    private WebSocketState _state = WebSocketState.Open;
    public override WebSocketState State => _state;
    public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed ? WebSocketCloseStatus.NormalClosure : null;
    public override string CloseStatusDescription => _state == WebSocketState.Closed ? "Closed" : string.Empty;
    public override string SubProtocol => string.Empty;

    public override void Abort()
    {
        _state = WebSocketState.Aborted;
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _state = WebSocketState.Closed;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        // Якщо ще не повернули повідомлення про закриття, повертаємо його і змінюємо стан
        if (_state == WebSocketState.Open)
        {
            _state = WebSocketState.Closed;
            var result = new WebSocketReceiveResult(
                count: 0,
                messageType: WebSocketMessageType.Close,
                endOfMessage: true,
                closeStatus: WebSocketCloseStatus.NormalClosure,
                closeStatusDescription: "Closed"
            );
            return Task.FromResult(result);
        }
        else
        {
            // Якщо вже закрито, повертаємо результат закриття
            return Task.FromResult(new WebSocketReceiveResult(
                count: 0,
                messageType: WebSocketMessageType.Close,
                endOfMessage: true,
                closeStatus: WebSocketCloseStatus.NormalClosure,
                closeStatusDescription: "Closed"
            ));
        }
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        => Task.CompletedTask;
}


