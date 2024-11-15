using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class Minimum : PipelineBase, ITransformation
{
    [Input]
    public required FeatureDescriptor<List<double>> Input { get; init; }

    [Output("Min")]
    public required FeatureDescriptor<double> OutputMin { get; init; }

    public void Transform(Signature signature)
    {
        var values = signature.GetFeature(Input);
        var min = values.Min();

        this.LogTrace("SigID: {signature.ID} FeatureName: {Input.Name} Min: {min}", signature.ID, Input.Name, min);

        signature.SetFeature(OutputMin, min);
        Progress = 100;
    }
}
