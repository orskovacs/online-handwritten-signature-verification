using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class Maximum : PipelineBase, ITransformation
{
    [Input]
    public required FeatureDescriptor<List<double>> Input { get; init; }

    [Output("Max")]
    public required FeatureDescriptor<double> OutputMax { get; init; }

    public void Transform(Signature signature)
    {
        var values = signature.GetFeature(Input);
        var max = values.Max();

        this.LogTrace("SigID: {signature.ID} FeatureName: {Input.Name} Max: {max}", signature.ID, Input.Name, max);

        signature.SetFeature(OutputMax, max);
        Progress = 100;
    }
}
