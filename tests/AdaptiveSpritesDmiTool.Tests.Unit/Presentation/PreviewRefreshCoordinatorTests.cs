using AdaptiveSpritesDmiTool.Presentation.Wpf;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Presentation;

public sealed class PreviewRefreshCoordinatorTests
{
    [Fact]
    public async Task RequestShouldDebounceAndKeepOnlyLatestRefresh()
    {
        using var coordinator = new PreviewRefreshCoordinator(TimeSpan.FromMilliseconds(40));
        var calls = new List<int>();

        coordinator.Request(async token =>
        {
            calls.Add(1);
            await Task.Delay(5, token);
        });

        coordinator.Request(async token =>
        {
            calls.Add(2);
            await Task.Delay(5, token);
        });

        coordinator.Request(async token =>
        {
            calls.Add(3);
            await Task.Delay(5, token);
        });

        await Task.Delay(120);

        calls.Should().Equal(3);
    }

    [Fact]
    public async Task RefreshNowShouldCancelAnyQueuedDebouncedRefresh()
    {
        using var coordinator = new PreviewRefreshCoordinator(TimeSpan.FromMilliseconds(80));
        var calls = new List<string>();

        coordinator.Request(async token =>
        {
            calls.Add("debounced");
            await Task.Delay(5, token);
        });

        await Task.Delay(20);

        await coordinator.RefreshNowAsync(async token =>
        {
            calls.Add("immediate");
            await Task.Delay(5, token);
        });

        await Task.Delay(120);

        calls.Should().ContainSingle().Which.Should().Be("immediate");
    }
}
