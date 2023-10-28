using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required List<double> XCoordsTemplate { get; init; }

    public required List<double> YCoordsTemplate { get; init; }

    public required List<double> PathTangentAngleTemplate { get; init; }

    public required List<double> PathVelocityMagnitudeTemplate { get; init; }

    public required List<double> LogCurvatureRadiusTemnplate { get; init; }

    public required List<double> TotalAccelerationMagnitudeTemplate { get; init; }
}
