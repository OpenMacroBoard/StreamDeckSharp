using System;

namespace OpenMacroBoard.SDK.Internals;

internal static class ConstrainedContext
{
    /// <summary>
    /// Creates a context which might depend on the provided item or a clone of that item.
    /// </summary>
    /// <remarks>
    /// <para>This is useful in situation where you only want to do an expensive operation
    /// for <see cref="IDisposable"/> types when needed. The <see cref="ConditionalDisposable{T}"/>
    /// makes sure that elements that depend on the parent (borrow, no copy) will not be disposed
    /// but cloned (owned copies) will be disposed.</para>
    /// </remarks>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TOutput">Output type.</typeparam>
    public static ConditionalDisposable<TOutput> For<TInput, TOutput>(
        TInput item,
        Func<TInput, TOutput?> borrow,
        Func<TInput, TOutput> ownedCopy
    )
        where TInput : class, IDisposable
        where TOutput : class, IDisposable
    {
        var borrowedResult = borrow(item);

        if (borrowedResult is not null)
        {
            return new ConditionalDisposable<TOutput>(borrowedResult, false);
        }

        var ownedResult = ownedCopy(item);

        return new ConditionalDisposable<TOutput>(ownedResult, true);
    }
}
