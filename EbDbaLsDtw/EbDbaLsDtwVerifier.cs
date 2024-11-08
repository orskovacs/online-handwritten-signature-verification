using SigStat.Common;
using SigStat.Common.Transforms;
using SigStat.Common.Model;

namespace EbDbaLsDtw;

public class EbDbaLsDtwVerifier : Verifier
{
    public EbDbaLsDtwVerifier()
    {
        Classifier = new EbDbaLsDtwClassifier();
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
        ];
    }
}