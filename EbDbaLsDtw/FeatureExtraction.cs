using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class FeatureExtraction : PipelineBase, ITransformation
{
    [Input]
    public required FeatureDescriptor<List<double>> InputNormalizedX { get; init; }

    [Input]
    public required FeatureDescriptor<List<double>> InputNormalizedY { get; init; }

    [Output]
    public required FeatureDescriptor<List<double>> OutputPathTangentAngle { get; init; }

    [Output]
    public required FeatureDescriptor<List<double>> OutputPathVelocityMagnitude { get; init; }

    [Output]
    public required FeatureDescriptor<List<double>> OutputLogCurvatureRadius { get; init; }

    [Output]
    public required FeatureDescriptor<List<double>> OutputTotalAccelerationMagnitude { get; init; }

    public void Transform(Signature signature)
    {
        var dataPointRange = Enumerable.Range(0, signature.GetFeature(InputNormalizedX).Count).ToList();

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