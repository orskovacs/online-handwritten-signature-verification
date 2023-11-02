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
        if (dF[^1] == 0)
            dF[^1] = 4.94065645841246544E-324;
        dF.Add(f[2] - f[1]);
        if (dF[^1] == 0)
            dF[^1] = 4.94065645841246544E-324;

        for (int i = 2; i < f.Count - 2; i++)
        {
            var df = 0.1 * (f[i + 1] - f[i - 1] + 2 * (f[i + 2] - f[i - 2]));
            dF.Add(df);
            if (dF[^1] == 0)
                dF[^1] = 4.94065645841246544E-324;
        }

        dF.Add(f[^2] - f[^3]);
        if (dF[^1] == 0)
            dF[^1] = 4.94065645841246544E-324;
        dF.Add(f[^1] - f[^2]);
        if (dF[^1] == 0)
            dF[^1] = 4.94065645841246544E-324;

        return dF;
    }

    public void Transform(Signature signature)
    {
        var normX = signature.GetFeature(InputNormalizedX);
        var normY = signature.GetFeature(InputNormalizedY);

        var dX = Derivative(normX);
        var dY = Derivative(normY);

        var th = dX.Zip(dY, (dx, dy) => (dx, dy))
            .Select(_ => Math.Atan(_.dy / _.dx))
            .ToList();
        signature.SetFeature(OutputPathTangentAngle, th);
        Progress = 25;

        var v = dX.Zip(dY, (dx, dy) => (dx, dy))
            .Select(_ => Math.Sqrt(_.dx *_.dx + _.dy * _.dy))
            .ToList();
        signature.SetFeature(OutputPathVelocityMagnitude, v);
        Progress = 50;

        var dTh = Derivative(th);
        var rho = dTh.Zip(v, (dth, v) => (dth, v))
            .Select(_ => Math.Log(_.v / _.dth))
            .ToList();
        signature.SetFeature(OutputLogCurvatureRadius, rho);
        Progress = 75;

        var dV = Derivative(v);
        var alpha = dV.Zip(v, (dv, v) => (v, dv)).Zip(dTh, (_, dth) => (_.v, _.dv, dth))
            .Select(_ => Math.Sqrt(_.dv * _.dv + _.v * _.v * _.dth * _.dth))
            .ToList();
        signature.SetFeature(OutputTotalAccelerationMagnitude, alpha);
        Progress = 100;
    }
}
