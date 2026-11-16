using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;

namespace OpenMacroBoard.Meta.TestUtils;

[SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "Intentional")]
[SuppressMessage("Naming", "RCS1047:Non-asynchronous method name should not end with 'Async'.", Justification = "They are async!")]
[SuppressMessage("Naming", "RCS1047FadeOut:Non-asynchronous method name should not end with 'Async'.", Justification = "They are async!")]
public static class ExtendedVerifySettingsVerificationExtensions
{
    public static SettingsTask VerifyAsync(this ExtendedVerifySettings settings, byte[] target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync(this ExtendedVerifySettings settings, Task<byte[]> target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync(this ExtendedVerifySettings settings, string target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync(this ExtendedVerifySettings settings, Task<string> target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync<T>(this ExtendedVerifySettings settings, T target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync<T>(this ExtendedVerifySettings settings, Task<T> target)
    {
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyAsync<T>(this ExtendedVerifySettings settings, ValueTask<T> target)
    {
#pragma warning disable AV1840 // ValueTask should be awaited before assignment
        return Verifier.Verify(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
#pragma warning restore AV1840
    }

    public static SettingsTask VerifyFileAsync(this ExtendedVerifySettings settings, string path)
    {
        return Verifier.VerifyFile(path, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyFileAsync(this ExtendedVerifySettings settings, FileInfo path)
    {
        return Verifier.VerifyFile(path, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask VerifyJsonAsync(this ExtendedVerifySettings settings, string target)
    {
        return Verifier.VerifyJson(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask ThrowsAsync(this ExtendedVerifySettings settings, Func<object> target)
    {
        return Verifier.Throws(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask ThrowsTaskAsync(this ExtendedVerifySettings settings, Func<Task> target)
    {
        return Verifier.ThrowsTask(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask ThrowsTaskAsync<T>(this ExtendedVerifySettings settings, Func<Task<T>> target)
    {
        return Verifier.ThrowsTask(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask ThrowsValueTaskAsync(this ExtendedVerifySettings settings, Func<ValueTask> target)
    {
        return Verifier.ThrowsValueTask(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    public static SettingsTask ThrowsValueTaskAsync<T>(this ExtendedVerifySettings settings, Func<ValueTask<T>> target)
    {
        return Verifier.ThrowsValueTask(target, settings.BuildVerifySettings(), settings.GetSourceFileOrThrow());
    }

    private static string GetSourceFileOrThrow(this ExtendedVerifySettings settings)
    {
        if (string.IsNullOrEmpty(settings.CallerFilePath))
        {
            throw new InvalidOperationException("Source file is null or empty. Make sure to initialize the caller information.");
        }

        return settings.CallerFilePath;
    }
}
