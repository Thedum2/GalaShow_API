using GalaShow.Common.Errors;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Service;

namespace GalaShow.Token.Tests;

public sealed class ViewerAvatarLimitTests
{
    private static List<ViewerAvatarDto> Avatars(int count) =>
        Enumerable.Range(1, count).Select(i => new ViewerAvatarDto { Id = i, Order = i, Name = $"avatar {i}", GifUrl = $"https://example.invalid/{i}.gif" }).ToList();

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(8, false)]
    [InlineData(9, true)]
    [InlineData(20, true)]
    public void ViewerAvatars_AllowUpToEight(int count, bool exceeds)
    {
        Assert.Equal(8, ViewerAvatarService.MaxAvatars);
        Assert.Equal(exceeds, ViewerAvatarService.ExceedsLimit(Avatars(count)));
    }

    [Fact]
    public void ViewerAvatars_LimitErrorUsesDedicatedCode()
    {
        Assert.Equal(462, (int)ErrorCode.AvatarLimitExceeded);
    }
}
