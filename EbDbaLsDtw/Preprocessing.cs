using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class Preprocessing : PipelineBase, ITransformation
{
    [Input]
    required public FeatureDescriptor<List<double>> InputX { get; set; }

    [Input]
    required public FeatureDescriptor<List<double>> InputY { get; set; }

    [Input]
    required public FeatureDescriptor<List<double>> Centroid { get; set; }

    [Input]
    required public FeatureDescriptor<double> MaxX { get; set; }

    [Input]
    required public FeatureDescriptor<double> MaxY { get; set; }

    [Input]
    required public FeatureDescriptor<double> MinX { get; set; }

    [Input]
    required public FeatureDescriptor<double> MinY { get; set; }

    [Output("NormalizedX")]
    required public FeatureDescriptor<List<double>> OutputNormalizedX { get; set; }

    [Output("NormalizedY")]
    required public FeatureDescriptor<List<double>> OutputNormalizedY { get; set; }

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
