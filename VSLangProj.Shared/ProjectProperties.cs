using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Dzonny.VSLangProj
{
    [Export]
    internal partial class ProjectProperties : StronglyTypedPropertyAccess
    {
        /// <summary>CTor - Initializes a new instance of the <see cref="ProjectProperties"/> class.</summary>
        [ImportingConstructor]
        public ProjectProperties(ConfiguredProject configuredProject)
            : base(configuredProject) { }

        /// <summary>CTor - Initializes a new instance of the <see cref="ProjectProperties"/> class.</summary>
        public ProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName)
            : base(configuredProject, file, itemType, itemName) { }

        /// <summary>CTor - Initializes a new instance of the <see cref="ProjectProperties"/> class.</summary>
        public ProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext)
            : base(configuredProject, projectPropertiesContext) { }

        /// <summary>CTor - Initializes a new instance of the <see cref="ProjectProperties"/> class.</summary>
        public ProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject)
            : base(configuredProject, unconfiguredProject) { }
    }
}
