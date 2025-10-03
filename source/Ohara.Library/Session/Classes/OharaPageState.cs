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
        public Dictionary<string, string> Properties  { get; set; }

        #endregion //Properties
    }
}
