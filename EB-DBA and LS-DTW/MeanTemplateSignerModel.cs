using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required List<double> Thresholds { get; init; }

    public required List<double> XCoordsTemplate { get; init; }

    public required List<double> YCoordsTemplate { get; init; }

    public required List<double> PenPressureTemplate { get; init; }

    public required List<double> PathTangentAngleTemplate { get; init; }

    public required List<double> PathVelocityMagnitudeTemplate { get; init; }

    public required List<double> LogCurvatureRadiusTemplate { get; init; }

    public required List<double> TotalAccelerationMagnitudeTemplate { get; init; }

    public required List<double> XCoordsLocalStability { get; init; }

    public required List<double> YCoordsLocalStability { get; init; }

    public required List<double> PenPressureStability { get; init; }

    public required List<double> PathTangentAngleLocalStability { get; init; }

    public required List<double> PathVelocityMagnitudeLocalStability { get; init; }

    public required List<double> LogCurvatureRadiusLocalStability { get; init; }

    public required List<double> TotalAccelerationMagnitudeLocalStability { get; init; }
}
