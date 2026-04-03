using Moq;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class MediatorTests
{
    [Fact]
    public void Constructor_WithNullDependencies_ShouldThrow()
    {
        // Arrange
        var commandDispatcher = new Mock<DevSource.Dispatcher.Commands.ICommandDispatcher>().Object;
        var queryDispatcher = new Mock<DevSource.Dispatcher.Queries.IQueryDispatcher>().Object;
        var notificationDispatcher = new Mock<DevSource.Dispatcher.Notifications.INotificationDispatcher>().Object;

        // Act
        var commandAction = () => new DevSource.Dispatcher.Engine.Mediator(null!, queryDispatcher, notificationDispatcher);
        var queryAction = () => new DevSource.Dispatcher.Engine.Mediator(commandDispatcher, null!, notificationDispatcher);
        var notificationAction = () => new DevSource.Dispatcher.Engine.Mediator(commandDispatcher, queryDispatcher, null!);

        // Asserts
        Assert.Throws<ArgumentNullException>(commandAction);
        Assert.Throws<ArgumentNullException>(queryAction);
        Assert.Throws<ArgumentNullException>(notificationAction);
    }

    [Fact]
    public async Task Methods_ShouldDelegateToUnderlyingDispatchers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var commandDispatcher = new Mock<DevSource.Dispatcher.Commands.ICommandDispatcher>();
        var queryDispatcher = new Mock<DevSource.Dispatcher.Queries.IQueryDispatcher>();
        var notificationDispatcher = new Mock<DevSource.Dispatcher.Notifications.INotificationDispatcher>();
        var command = new TestCommand(new TrackingState());
        var responseCommand = new TestCommandWithResponse("value", new TrackingState());
        var query = new TestQuery(5, new TrackingState());
        var notification = new TestNotification(new TrackingState());
        commandDispatcher.Setup(x => x.DispatchAsync(command, It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        commandDispatcher.Setup(x => x.DispatchAsync<TestCommandWithResponse, string>(responseCommand, It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult("ok"));
        queryDispatcher.Setup(x => x.DispatchAsync<TestQuery, int>(query, It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(10));
        notificationDispatcher.Setup(x => x.PublishAsync(notification, It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        var mediator = new DevSource.Dispatcher.Engine.Mediator(commandDispatcher.Object, queryDispatcher.Object, notificationDispatcher.Object);

        // Act
        await mediator.SendAsync(command, cancellationToken);
        var commandResponse = await mediator.SendAsync<TestCommandWithResponse, string>(responseCommand, cancellationToken);
        var queryResponse = await mediator.QueryAsync<TestQuery, int>(query, cancellationToken);
        await mediator.PublishAsync(notification, cancellationToken);

        // Asserts
        Assert.Equal("ok", commandResponse);
        Assert.Equal(10, queryResponse);
        commandDispatcher.VerifyAll();
        queryDispatcher.VerifyAll();
        notificationDispatcher.VerifyAll();
    }
}
