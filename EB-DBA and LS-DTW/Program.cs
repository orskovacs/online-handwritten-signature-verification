using SigStat.Common.Loaders;
using SigStat.Common;
using SigStat.Common.Pipeline;
using SigStat.Common.Transforms;
using EbDbaAndLsDtw;

var path = args[0];
var loader = new Svc2004Loader(path, true);
var signers = new List<Signer>(loader.EnumerateSigners());
var signaturesOfUser1 = signers[0].Signatures;

var signature = signaturesOfUser1[0];

new SequentialTransformPipeline
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
}.Transform(signature);

Console.WriteLine();
