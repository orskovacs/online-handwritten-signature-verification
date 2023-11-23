using SigStat.Common.Loaders;
using SigStat.Common;
using SigStat.Common.Pipeline;
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
        Pipeline = new SequentialTransformPipeline
        {
            new CentroidExtraction
            {
                Inputs = new() { Features.X, Features.Y, },
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
                OutputNormalizedX = AdditionalFeatures.NormalizedX,
                OutputNormalizedY = AdditionalFeatures.NormalizedY,
            },
            new FeatureExtraction
            {
                InputNormalizedX = AdditionalFeatures.NormalizedX,
                InputNormalizedY = AdditionalFeatures.NormalizedY,
                OutputPathTangentAngle = AdditionalFeatures.PathTangentAngle,
                OutputPathVelocityMagnitude = AdditionalFeatures.PathVelocityMagnitude,
                OutputLogCurvatureRadius = AdditionalFeatures.LogCurvatureRadius,
                OutputTotalAccelerationMagnitude = AdditionalFeatures.TotalAccelerationMagnitude,
            }
        },
    },
    Sampler = new FirstNSampler()
};

BenchmarkResults result = benchmark.Execute(true);

Console.WriteLine($"AER: {result.FinalResult.Aer}");
Console.WriteLine($"FAR: {result.FinalResult.Far}");
Console.WriteLine($"FRR: {result.FinalResult.Frr}");
