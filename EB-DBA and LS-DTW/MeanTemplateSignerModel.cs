using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required double Threshold { get; init; }

    public required MultivariateTimeSeries Template { get; set; }

    public required List<double> LocalStability { get; set; }
}
