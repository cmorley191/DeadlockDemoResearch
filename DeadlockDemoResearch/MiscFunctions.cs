
namespace DeadlockDemoResearch
{
  public static class MiscFunctions
  {
    public static double GetUnixNowMillis()
    {
      return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
    }

    public static System.Numerics.Vector3 ConvertVector(DemoFile.Vector v) => new(v.X, v.Y, v.Z);
  }
}
