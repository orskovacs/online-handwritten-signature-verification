namespace EbDbaLsDtw;

public static class TimeSeriesExtension
{
    public static List<double> ToNormalized(this List<double> timeSeries)
    {
        var maxFinite = timeSeries.Where(x => !double.IsNaN(x) && double.IsFinite(x)).Max();
        var minFinite = timeSeries.Where(x => !double.IsNaN(x) && double.IsFinite(x)).Min();

        var ts = timeSeries.Select(x =>
        {
            if (double.IsNaN(x))
                return 0;
            
            if (double.IsPositiveInfinity(x))
                return maxFinite;

            if (double.IsNegativeInfinity(x))
                return minFinite;
            
            return x;
        }).ToList();

        // Calculate the mean
        var mean = ts.Average();

        // Calculate the corrected empirical standard deviation
        var stdDev = ts.StandardDeviation();

        var normalized = ts.Select(x => (x - mean) / stdDev).ToList();

        return normalized;
    }

    public static List<double> Derivative(this List<double> ts)
    {
        return [
            ts[1] - ts[0],
            ts[2] - ts[1],
            ..Enumerable.Range(2, ts.Count - 4)
                .Select(i => SecondOrderRegressionAtIndex(i, ts)),
            ts[^2] - ts[^3],
            ts[^1] - ts[^2]
        ];
    }

    private static double SecondOrderRegressionAtIndex(int i, List<double> ts) =>
        0.1 * (ts[i + 1] - ts[i - 1] + 2 * (ts[i + 2] - ts[i - 2]));
    
    /// <summary>
    /// Calculates the corrected empirical standard deviation of the given time series.
    /// </summary>
    /// <param name="ts">Time series</param>
    /// <returns>Corrected empirical standard deviation</returns>
    public static double StandardDeviation(this List<double> ts)
    {
        var mean = ts.Average();
        return Math.Sqrt(ts.Sum(x => (x - mean) * (x - mean)) / (ts.Count - 1));
    }
}
