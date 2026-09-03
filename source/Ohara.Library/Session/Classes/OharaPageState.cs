namespace Ohara.Library.Session.Classes
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Ohara.Library.Base.Methods;
    #region Using Directives

    using System.Collections.Generic;

    #endregion //Using Directives

    public class OharaPageState
    {
        #region Constructors

        public OharaPageState()
        {
            Properties = new Dictionary<string, string>();
        }

        public OharaPageState(Dictionary<string, string> defaultProperties)
        {
            this.Properties = defaultProperties;
        }

        #endregion //Constructors

        #region Properties
        public string Page { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        #endregion //Properties

        #region Utilities

        /// <summary>
        /// Checks whether a specific property key exists in the page state dictionary,
        /// regardless of whether its stored string value is empty or null.
        /// </summary>
        public bool HasProperty(string propertyName)
        {
            return Properties != null && Properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Attempts to retrieve a property's string value.
        /// Returns true if the key exists in the dictionary, even if the value is string.Empty.
        /// </summary>
        public bool TryGetProperty(string propertyName, out string value)
        {
            value = null;
            if (Properties != null && Properties.TryGetValue(propertyName, out string rawValue))
            {
                value = rawValue;
                return true; // Key explicitly exists in session storage
            }
            return false; // Key was never stored in session storage
        }

        #endregion // Utilities
    }
}
