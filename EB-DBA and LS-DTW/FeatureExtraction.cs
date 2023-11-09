using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class FeatureExtraction : PipelineBase, ITransformation
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

    private static List<double> Derivative(List<double> f)
    {
        var dF = new List<double>();

        dF.Add(f[1] - f[0]);
        dF.Add(f[2] - f[1]);

        for (int i = 2; i < f.Count - 2; i++)
        {
            var df = 0.1 * (f[i + 1] - f[i - 1] + 2 * (f[i + 2] - f[i - 2]));
            dF.Add(df);
        }

        dF.Add(f[^2] - f[^3]);
        dF.Add(f[^1] - f[^2]);

        return dF;
    }

    public void Transform(Signature signature)
    {
        var normX = signature.GetFeature(InputNormalizedX).Normalize();
        var normY = signature.GetFeature(InputNormalizedY).Normalize();

        var dX = Derivative(normX).Normalize();
        var dY = Derivative(normY).Normalize();

        var th = dX.Zip(dY, (dx, dy) => (dx, dy))
            .Select(_ => Math.Atan(_.dy / _.dx))
            .ToList()
            .Normalize();
        signature.SetFeature(OutputPathTangentAngle, th);
        Progress = 25;

        var v = dX.Zip(dY, (dx, dy) => (dx, dy))
            .Select(_ => Math.Sqrt(_.dx *_.dx + _.dy * _.dy))
            .ToList()
            .Normalize();
        signature.SetFeature(OutputPathVelocityMagnitude, v);
        Progress = 50;

        var dTh = Derivative(th).Normalize();
        var rho = dTh.Zip(v, (dth, v) => (dth, v))
            .Select(_ => Math.Log(_.v / _.dth))
            .ToList()
            .Normalize();
        signature.SetFeature(OutputLogCurvatureRadius, rho);
        Progress = 75;

        var dV = Derivative(v).Normalize();
        var alpha = dV.Zip(v, (dv, v) => (v, dv)).Zip(dTh, (_, dth) => (_.v, _.dv, dth))
            .Select(_ => Math.Sqrt(_.dv * _.dv + _.v * _.v * _.dth * _.dth))
            .ToList()
            .Normalize();
        signature.SetFeature(OutputTotalAccelerationMagnitude, alpha);
        Progress = 100;
    }
}

internal static class TimeSeriesExtension
{
    public static List<double> Normalize(this List<double> ts)
    {
        // Calculate the mean
        var mean = ts.Average();

        // Subtract the mean from each data point (zero mean)
        var zeroMean = ts.Select(x => x - mean).ToList();

        // Calculate the standard deviation
        var stdDev = Math.Sqrt(zeroMean.Average(z => z * z));

        // Divide each data point by the standard deviation (unit variance)
        var normalized = zeroMean.Select(z => z / stdDev).ToList();

        var min = normalized.Min();

        var translated = normalized.Select(n => n - min).ToList();

        return translated.Select(t => t == 0 ? 4.94065645841246544E-324 : t).ToList();
    }
}
