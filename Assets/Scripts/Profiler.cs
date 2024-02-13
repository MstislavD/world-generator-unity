using System;
using System.Collections.Generic;
using System.Linq;

public class Profiler
{
    List<string> log = new List<string>();
    int log_length = 0;

    public T Profile<T>(Func<T> function, string description)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        T result = function();
        add_to_frame_log($"{description} complete in {stopwatch.ElapsedMilliseconds} ms");
        return result;
    }

    public void Profile(Action action, string description)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        add_to_frame_log($"{description} complete in {stopwatch.ElapsedMilliseconds} ms");
    }

    void add_to_frame_log(string str)
    {
        log_length += 1;
        if (log.Count < log_length)
        {
            log.Add(str);
        }
        else
        {
            log[log_length - 1] = str;
        }
    }

    public void PrintLog(Action<string> logger)
    {
        if (log_length != 0)
        {
            logger(log.Take(log_length).Aggregate((s1, s2) => s1 + ". " + s2));
            log_length = 0;
        }
    }
}
