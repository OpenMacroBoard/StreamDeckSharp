using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A conditional disposable wrapper.
/// </summary>
/// <remarks>
/// <para>This class is used in situations where the wrapped element is either borrowed (in which case
/// it shouldn't be disposed) or owned (in which case it should be disposed) and abstracts that
/// away from the consumer. The consumer has to make sure to call dispose once they are finished
/// and this wrapped decides whether the wrapped element is in fact disposed or not.</para>
/// </remarks>
/// <typeparam name="T">Disposable type.</typeparam>
public sealed class ConditionalDisposable<T> : IDisposable
    where T : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalDisposable{T}"/> class.
    /// </summary>
    /// <param name="item">The wrapped item.</param>
    /// <param name="disposeItem">A flag that determines if the item will be disposed.</param>
    public ConditionalDisposable(T item, bool disposeItem)
    {
        Item = item;
        DisposeItem = disposeItem;
    }

    /// <summary>
    /// Gets the underlying wrapped item.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Get a value that determines whether the item will be disposed or not.
    /// </summary>
    public bool DisposeItem { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!DisposeItem)
        {
            return;
        }

        Item?.Dispose();
    }
}
