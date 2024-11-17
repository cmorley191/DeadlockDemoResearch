
namespace DeadlockDemoResearch
{
  public static class EnumerableExtensions
  {

    public static IEnumerable<T> Yield<T>(this T x)
    {
      yield return x;
    }

    public static IEnumerable<(int index, T value)> Indexed<T>(this IEnumerable<T> values)
    {
      int i = 0;
      foreach (var value in values)
      {
        yield return (index: i++, value);
      }
    }

    public static bool TryGet<T>(this List<T> values, int index, out T value)
    {
      if (index < 0 || index >= values.Count)
      {
        value = default(T);
        return false;
      }
      else
      {
        value = values[index];
        return true;
      }
    }

    public static bool TryFirst<T>(this IEnumerable<T> values, Func<T, bool> predicate, out T value)
    {
      foreach (var x in values)
      {
        if (predicate(x))
        {
          value = x;
          return true;
        }
      }
      value = default;
      return false;
    }

    public static List<T> ForEachRet<T>(this List<T> values, Action<T> action)
    {
      foreach (var x in values)
      {
        action(x);
      }
      return values;
    }

    public static IEnumerable<int> Through(this int start, int end)
    {
      while (start <= end) yield return start++;
    }

    public enum QueryLogTime
    {
      BeforeExecution = 1,
      AfterExecution = 2,
      AfterExecutionException = 3,
    }
    public static TContinuationResult LogAndThen<TQueryable, TContinuationResult>(
      this IQueryable<TQueryable> query,
      QueryLogTime logTime,
      Action<string> log,
      Func<IQueryable<TQueryable>, TContinuationResult> continuation
    )
    {
      if (logTime == QueryLogTime.BeforeExecution)
      {
        log(query.ToString() ?? "");
      }

      TContinuationResult continuationResult;
      try
      {
        continuationResult = continuation(query);
      }
      catch
      {
        if (logTime == QueryLogTime.AfterExecution || logTime == QueryLogTime.AfterExecutionException)
        {
          log(query.ToString() ?? "");
        }
        throw;
      }

      if (logTime == QueryLogTime.AfterExecution)
      {
        log(query.ToString() ?? "");
      }
      return continuationResult;
    }
    public static async Task<TContinuationResult> LogAndThenAsync<TQueryable, TContinuationResult>(
      this IQueryable<TQueryable> query,
      QueryLogTime logTime,
      Action<string> log,
      Func<IQueryable<TQueryable>, Task<TContinuationResult>> continuation
    )
    {
      if (logTime == QueryLogTime.BeforeExecution)
      {
        log(query.ToString() ?? "");
      }

      TContinuationResult continuationResult;
      try
      {
        continuationResult = await continuation(query);
      }
      catch
      {
        if (logTime == QueryLogTime.AfterExecution || logTime == QueryLogTime.AfterExecutionException)
        {
          log(query.ToString() ?? "");
        }
        throw;
      }

      if (logTime == QueryLogTime.AfterExecution)
      {
        log(query.ToString() ?? "");
      }
      return continuationResult;
    }
  }
}
