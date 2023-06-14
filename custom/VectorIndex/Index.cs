using FASTER.core;

namespace NesterovskyBros.VectorIndex;

/// <summary>
/// An API to build vector index.
/// </summary>
public class Index
{
    public record Settings
    {
        public string? TempFolder { get; init; }
        public bool DeleteTempFolderOnComplete { get; init; } = true;
        public int? Dimensions { get; init; }
        public ReadOnlyMemory<float>? MinValues { get; init; }
        public ReadOnlyMemory<float>? MaxValues { get; init; }
        public float? MinValue { get; init; }
        public float? MaxValue { get; init; }
    }

    public static async IAsyncEnumerable<Range> Build(
        IAsyncEnumerable<(long id, ReadOnlyMemory<float> point)> points,
        Settings? settings = null)
    {
        var dimensions = 0;
        var calculateMinMax = true;

        // Validate settings
        if (settings != null)
        {
            if ((settings.MinValue == null) != (settings.MaxValue == null))
            {
                throw new ArgumentException(
                    "MaxValue should be specified along with MinValue.");
            }

            if ((settings.MinValues == null) != (settings.MaxValues == null))
            {
                throw new ArgumentException(
                    "MaxValues should be specified along with MinValues.");
            }

            if ((settings.MinValue != null) && (settings.MaxValues != null))
            {
                throw new ArgumentException(
                    "(MinValue, MaxValue) and (MinValues, MaxValues) are " +
                    "mutually exclusive.");
            }

            if (settings.MinValues?.Length != settings.MaxValues?.Length)
            {
                throw new ArgumentException(
                    "Dimensions of  MinValues and MaxValues should match.");
            }

            if (settings.Dimensions != null &&
                settings.MinValues != null &&
                settings.Dimensions != settings.MinValues.Value.Length)
            {
                throw new ArgumentException(
                    "Dimensions should match with boundaty dimensions " +
                    "of (MinValues, MaxValues).");
            }

            if (settings.Dimensions < 0)
            {
                throw new ArgumentException("Invalid dimensions.");
            }

            dimensions = settings.Dimensions ?? settings.MinValues?.Length ?? -1;
            calculateMinMax = settings.MinValue != null || settings.MinValues != null;
        }

        // Create a key-value store for primary sequence of point.
        using FasterKVSettings<long, ReadOnlyMemory<float>> indexSettings = new(
            settings?.TempFolder ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            settings?.DeleteTempFolderOnComplete ?? true);

        using FasterKV<long, ReadOnlyMemory<float>> index = new(indexSettings);
        var first = true;

        using(var session = index.NewSession(
            new SimpleFunctions<long, ReadOnlyMemory<float>>()))
        {
            await foreach (var item in points)
            {
                if (first)
                {
                    first = false;

                    if (dimensions < 0)
                    {
                        dimensions = item.point.Length;
                    }

                    if (calculateMinMax)
                    {

                    }
                }
                else
                {
                    if (dimensions != item.point.Length)
                    {
                        throw new ArgumentException("Dimensions mismatch.");
                    }
                }



                await session.UpsertAsync(item.id, item.point).ConfigureAwait(false);
            }
        }

        throw new ArgumentException("TODO:");
    }
}
