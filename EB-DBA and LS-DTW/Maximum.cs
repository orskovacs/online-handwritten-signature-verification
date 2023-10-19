﻿using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class Maximum : PipelineBase, ITransformation
{
    [Input]
    required public FeatureDescriptor<List<double>> Input { get; set; }

    [Output("Max")]
    required public FeatureDescriptor<double> OutputMax { get; set; }

    public void Transform(Signature signature)
    {
        var values = signature.GetFeature(Input);
        var max = values.Max();

        this.LogTrace("SigID: {signature.ID} FeatureName: {Input.Name} Max: {max}", signature.ID, Input.Name, max);

        signature.SetFeature(OutputMax, max);
        Progress = 100;
    }
}
