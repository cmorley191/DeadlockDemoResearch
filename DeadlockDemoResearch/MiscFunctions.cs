
namespace DeadlockDemoResearch
{
  public static class MiscFunctions
  {
    public static double GetUnixNowMillis()
    {
      return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
  }
}
