using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLINQ_Practice;

static class Utils
{
    public static string MeasureExecution<T>(Func<T> action, out T result,out long elapsedTimeMilliseconds)
    {
        Stopwatch sw = Stopwatch.StartNew();

        result = action();

        sw.Stop();
        elapsedTimeMilliseconds = sw.ElapsedMilliseconds;
        return sw.ToString();
    }

    public static string MeasureExecution<T>(Func<T> action, out T result)
    {
        Stopwatch sw = Stopwatch.StartNew();

        result = action();

        sw.Stop();
        return sw.ToString();
    }

}
