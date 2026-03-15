namespace RCS.Geo.Core;

/// <summary>
/// Handles specific unit normalization tasks independently of formal CRS transformations when necessary.
/// </summary>
public static class UnitNormalizer
{
    public const double UsFootToMeter = 1200.0 / 3937.0;
    public const double MeterToUsFoot = 3937.0 / 1200.0;
    public const double IntlFootToMeter = 0.3048;
    public const double MeterToIntlFoot = 1.0 / 0.3048;

    public static double UsFeetToMeters(double usFeet) => usFeet * UsFootToMeter;
    public static double MetersToUsFeet(double meters) => meters * MeterToUsFoot;
    public static double IntlFeetToMeters(double intlFeet) => intlFeet * IntlFootToMeter;
    public static double MetersToIntlFeet(double meters) => meters * MeterToIntlFoot;
}
