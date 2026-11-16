using Nuke.Common.Tooling;
using System.ComponentModel;

#pragma warning disable S3903 // Types should be defined in named namespaces

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public readonly static Configuration Debug = new() { Value = nameof(Debug) };
    public readonly static Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration)
    {
        return configuration.Value;
    }
}
