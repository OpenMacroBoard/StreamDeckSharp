using Nuke.Common.ProjectModel;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable S3903 // Types should be defined in named namespaces

internal static class SolutionExtensions
{
    public static Project GetExactProject(this Solution solution, string projectName)
    {
        return solution.AllProjects.First(p => p.Name == projectName)
            ?? throw new KeyNotFoundException()
            ;
    }
}

