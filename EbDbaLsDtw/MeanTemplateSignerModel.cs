using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

internal class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required double Threshold { get; init; }

    public required MultivariateTimeSeries Template { get; init; }

    public required List<double> LocalStability { get; init; }
}
