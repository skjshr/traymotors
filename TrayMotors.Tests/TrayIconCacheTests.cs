namespace TrayMotors.Tests;

public sealed class TrayIconCacheTests
{
    [Fact]
    public void Cache_CreatesIconsForAllResourceFramesAndBuckets()
    {
        using var cache = new TrayIconCache();

        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            foreach (var bucket in Enum.GetValues<IconColorBucket>())
            {
                for (var frame = 0; frame < TrayIconCache.FrameCount; frame++)
                {
                    Assert.NotNull(cache.Get(kind, frame, bucket));
                }
            }
        }
    }
}
