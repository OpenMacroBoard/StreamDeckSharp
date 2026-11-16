using System;
using System.Collections.Generic;

namespace OpenMacroBoard.SDK.Utils;

/// <summary>
/// Some LINQ extensions we use internally.
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// <para>
    /// A useful combination of LINQs Select() and Where().
    /// Prevents invalid object states between those two calls by combining them.
    /// </para>
    /// <para>
    /// If the <paramref name="selector"/> returns <c>(true, value)</c> the value will be yielded.
    /// If the <paramref name="selector"/> returns <c>(false, value)</c> it will not be present in
    /// the ouput enumerble.
    /// </para>
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="source">Incomming source enumerable</param>
    /// <param name="selector">Filter/Selector</param>
    public static IEnumerable<TOut> SelectWhere<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, (bool Success, TOut Output)> selector
    )
    {
        foreach (var item in source)
        {
            var (success, output) = selector(item);

            if (success)
            {
                yield return output;
            }
        }
    }
}
