using SigStat.Common.Loaders;
using SigStat.Common;
using SigStat.Common.Transforms;
using EbDbaAndLsDtw;
using SigStat.Common.Framework.Samplers;
using SigStat.Common.Model;

var path = args[0];

var benchmark = new VerifierBenchmark()
{
    Loader = new Svc2004Loader(path, true),
    Verifier = new Verifier()
    {
        Classifier = new EbDbaAndLsDtwClassifier(),
        Pipeline =
        [
            new CentroidExtraction
            {
                Inputs = [Features.X, Features.Y,],
                OutputCentroid = UtilityFeatures.Centroid,
            },
            new Minimum { Input = Features.X, OutputMin = UtilityFeatures.MinX },
            new Minimum { Input = Features.Y, OutputMin = UtilityFeatures.MinY },
            new Maximum { Input = Features.X, OutputMax = UtilityFeatures.MaxX },
            new Maximum { Input = Features.Y, OutputMax = UtilityFeatures.MaxY },
            new Preprocessing {
                InputX = Features.X,
                InputY = Features.X,
                Centroid = UtilityFeatures.Centroid,
                MaxX = UtilityFeatures.MaxX,
                MinX = UtilityFeatures.MinX,
                MaxY = UtilityFeatures.MaxY,
                MinY = UtilityFeatures.MinY,
                OutputNormalizedX = OriginalFeatures.NormalizedX,
                OutputNormalizedY = OriginalFeatures.NormalizedY,
            },
            new FeatureExtraction
            {
                InputNormalizedX = OriginalFeatures.NormalizedX,
                InputNormalizedY = OriginalFeatures.NormalizedY,
                OutputPathTangentAngle = DerivedFeatures.PathTangentAngle,
                OutputPathVelocityMagnitude = DerivedFeatures.PathVelocityMagnitude,
                OutputLogCurvatureRadius = DerivedFeatures.LogCurvatureRadius,
                OutputTotalAccelerationMagnitude = DerivedFeatures.TotalAccelerationMagnitude,
            }
        ],
    },
    Sampler = new AllSignaturesSampler()
};

var result = benchmark.Execute(true);

Console.WriteLine($"AER: {result.FinalResult.Aer}");
Console.WriteLine($"FAR: {result.FinalResult.Far}");
Console.WriteLine($"FRR: {result.FinalResult.Frr}");


public class AllSignaturesSampler : Sampler
{
    public AllSignaturesSampler() : base(null,null,null)
    {
        TrainingFilter = signatures => signatures;
        GenuineTestFilter = signatures => signatures;
        ForgeryTestFilter = signatures => signatures;
    }
}
