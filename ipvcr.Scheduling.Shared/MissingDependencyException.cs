using System;
using System.Diagnostics.CodeAnalysis;

namespace ipvcr.Scheduling
{
    public class MissingDependencyException : Exception
    {
        [ExcludeFromCodeCoverage]
        public string DependencyName { get; }

        public MissingDependencyException(string dependencyName) 
            : base($"The required dependency '{dependencyName}' is missing.") 
        {
            DependencyName = dependencyName;
        }

        [ExcludeFromCodeCoverage]
        public MissingDependencyException(string dependencyName, string message) 
            : base(message) 
        {
            DependencyName = dependencyName;
        }

        [ExcludeFromCodeCoverage]
        public MissingDependencyException(string dependencyName, string message, Exception innerException) 
            : base(message, innerException) 
        {
            DependencyName = dependencyName;
        }
    }
}