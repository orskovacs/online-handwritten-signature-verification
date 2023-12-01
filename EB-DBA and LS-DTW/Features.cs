using SigStat.Common;

namespace EbDbaAndLsDtw;

public static class OriginalFeatures
{
    public static readonly FeatureDescriptor<List<double>> NormalizedX =
        FeatureDescriptor.Get<List<double>>("X_norm");

    public static readonly FeatureDescriptor<List<double>> NormalizedY =
        FeatureDescriptor.Get<List<double>>("Y_norm");

    public static readonly FeatureDescriptor<List<double>> PenPressure =
        FeatureDescriptor<List<double>>.Get("Pressure");
}

public static class DerivedFeatures
{
    public static readonly FeatureDescriptor<List<double>> PathTangentAngle =
        FeatureDescriptor.Get<List<double>>("PathTangentAngle");

    public static readonly FeatureDescriptor<List<double>> PathVelocityMagnitude =
        FeatureDescriptor.Get<List<double>>("PathVelocityMagnitude");

    public static readonly FeatureDescriptor<List<double>> LogCurvatureRadius =
        FeatureDescriptor.Get<List<double>>("LogCurvatureRadius");

    public static readonly FeatureDescriptor<List<double>> TotalAccelerationMagnitude =
        FeatureDescriptor.Get<List<double>>("TotalAccelerationMagnitude");
};

public static class UtilityFeatures
{
    public static readonly FeatureDescriptor<List<double>> Centroid = FeatureDescriptor.Get<List<double>>("Centroid");
    public static readonly FeatureDescriptor<double> MinX = FeatureDescriptor.Get<double>("X_min");
    public static readonly FeatureDescriptor<double> MinY = FeatureDescriptor.Get<double>("Y_min");
    public static readonly FeatureDescriptor<double> MaxX = FeatureDescriptor.Get<double>("X_max");
    public static readonly FeatureDescriptor<double> MaxY = FeatureDescriptor.Get<double>("Y_max");
};
