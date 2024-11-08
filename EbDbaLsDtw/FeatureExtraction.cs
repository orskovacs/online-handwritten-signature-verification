using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class FeatureExtraction : PipelineBase, ITransformation
{
    [Input]
    required public FeatureDescriptor<List<double>> InputNormalizedX { get; set; }

    [Input]
    required public FeatureDescriptor<List<double>> InputNormalizedY { get; set; }

    [Output]
    required public FeatureDescriptor<List<double>> OutputPathTangentAngle { get; set; }

    [Output]
    required public FeatureDescriptor<List<double>> OutputPathVelocityMagnitude { get; set; }

    [Output]
    required public FeatureDescriptor<List<double>> OutputLogCurvatureRadius { get; set; }

    [Output]
    required public FeatureDescriptor<List<double>> OutputTotalAccelerationMagnitude { get; set; }

    public void Transform(Signature signature)
    {
        var dataPointRange = Enumerable.Range(0, signature.GetFeature(InputNormalizedX).Count);

        var x = signature.GetFeature(InputNormalizedX);
        var y = signature.GetFeature(InputNormalizedY);
        var pressure = signature.GetFeature(OriginalFeatures.PenPressure);
        
        var dx = x.Derivative();
        var dy = y.Derivative();

        var th = dataPointRange
            .Select(i => Math.Atan(dy[i] / dx[i]))
            .ToList();

        var dth = th.Derivative();

        var v = dataPointRange
            .Select(i => Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]))
            .ToList();

        var dv = v.Derivative();

        var rho = dataPointRange
            .Select(i => Math.Log(v[i] / dth[i]))
            .ToList();
            
        var alpha = dataPointRange
            .Select(i => Math.Sqrt(dv[i] * dv[i] + v[i] * v[i] * dth[i] * dth[i]))
            .ToList();

        signature.SetFeature(OriginalFeatures.NormalizedX, x.Normalize());
        signature.SetFeature(OriginalFeatures.NormalizedY, y.Normalize());
        signature.SetFeature(OriginalFeatures.PenPressure, pressure.Normalize());
        signature.SetFeature(OutputPathTangentAngle, th.Normalize());
        signature.SetFeature(OutputPathVelocityMagnitude, v.Normalize());
        signature.SetFeature(OutputLogCurvatureRadius, rho.Normalize());
        signature.SetFeature(OutputTotalAccelerationMagnitude, alpha.Normalize());

        Progress = 100;
    }
}

internal static class TimeSeriesExtension
{
    public static List<double> Normalize(this List<double> timeSeries)
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
        var stdDev = Math.Sqrt(ts.Sum(x => (x - mean) * (x - mean)) / (ts.Count - 1));

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
}
