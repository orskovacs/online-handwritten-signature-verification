using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class Preprocessing : PipelineBase, ITransformation
{
    [Input]
    public required FeatureDescriptor<List<double>> InputX { get; init; }

    [Input]
    public required FeatureDescriptor<List<double>> InputY { get; init; }

    [Input]
    public required FeatureDescriptor<List<double>> Centroid { get; init; }

    [Input]
    public required FeatureDescriptor<double> MaxX { get; init; }

    [Input]
    public required FeatureDescriptor<double> MaxY { get; init; }

    [Input]
    public required FeatureDescriptor<double> MinX { get; init; }

    [Input]
    public required FeatureDescriptor<double> MinY { get; init; }

    [Output("NormalizedX")]
    public required FeatureDescriptor<List<double>> OutputNormalizedX { get; init; }

    [Output("NormalizedY")]
    public required FeatureDescriptor<List<double>> OutputNormalizedY { get; init; }

    public void Transform(Signature signature)
    {
        var centroid = signature.GetFeature(Centroid);
        var centroidX = centroid[0];
        var centroidY = centroid[1];
        var maxX = signature.GetFeature(MaxX);
        var minX = signature.GetFeature(MinX);
        var maxY = signature.GetFeature(MaxY);
        var minY = signature.GetFeature(MinY);

        var xValues = signature.GetFeature(InputX);
        var xValuesNormalized = new List<double>();
        foreach (var x in xValues)
        {
            var xNormalized = (x - centroidX) / (maxX - minX);
            xValuesNormalized.Add(xNormalized);
        }
        signature.SetFeature(OutputNormalizedX, xValuesNormalized);
        Progress = 50;

        var yValues = signature.GetFeature(InputY);
        var yValuesNormalized = new List<double>();
        foreach (var y in yValues)
        {
            var yNormalized = (y - centroidY) / (maxY - minY);
            yValuesNormalized.Add(yNormalized);
        }
        signature.SetFeature(OutputNormalizedY, yValuesNormalized);
        Progress = 100;
    }
}
