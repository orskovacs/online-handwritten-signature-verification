using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class MeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; init; }

    public required IEnumerable<double> XCoordsTemplate { get; init; }

    public required IEnumerable<double> YCoordsTemplate { get; init; }

    public required IEnumerable<double> PathTangentAngleTemplate { get; init; }

    public required IEnumerable<double> PathVelocityMagnitudeTemplate { get; init; }

    public required IEnumerable<double> LogCurvatureRadiusTemplate { get; init; }

    public required IEnumerable<double> TotalAccelerationMagnitudeTemplate { get; init; }

    public required IEnumerable<double> XCoordsLocalStability { get; init; }

    public required IEnumerable<double> YCoordsLocalStability { get; init; }

    public required IEnumerable<double> PathTangentAngleLocalStability { get; init; }

    public required IEnumerable<double> PathVelocityMagnitudeLocalStability { get; init; }

    public required IEnumerable<double> LogCurvatureRadiusLocalStability { get; init; }

    public required IEnumerable<double> TotalAccelerationMagnitudeLocalStability { get; init; }
}
