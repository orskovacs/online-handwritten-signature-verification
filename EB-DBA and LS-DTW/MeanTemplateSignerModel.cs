using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required Dictionary<FeatureDescriptor, double> Thresholds { get; init; }

    public required Dictionary<FeatureDescriptor, List<double>> Templates { get; set; }

    public required Dictionary<FeatureDescriptor, List<double>> LocalStabiltyValues { get; set; }
}
