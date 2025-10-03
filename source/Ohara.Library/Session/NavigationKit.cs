namespace Ohara.Library.Session
{
    #region Using Directives

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters.Internal;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Newtonsoft.Json;
    using Ohara.Library.Base.Methods;
    using Ohara.Library.Session.Classes;

    #endregion //Using Directives
    public class NavigationKit
    {
        #region Constructors

        public NavigationKit(HttpContext httpContext)
        {
            _httpContext = httpContext;
            string requestPath = httpContext.Request.Path.Value;
            _currentPage = _defaultPage = _defaultName = requestPath.Replace("/", "").ToSentence();
        }

        #endregion //Constructors

        #region Constants

        private const string SESSION_NAME = "Ohara.Navigation.History";

        #endregion //Constants

        #region Fields

        private HttpContext _httpContext;
        private string _currentPage;

        private OharaPageState _currentPageState;
        private string _defaultName;
        private string _defaultPage;

        #endregion //Fields

        #region Properties

        public string CurrentPage { get { return _currentPage; } set { _currentPage = value; } }
        public OharaPageState CurrentPageState { get { return _currentPageState; } set { _currentPageState = value; } }

        #endregion //Properties

        #region Methods

        #region Sessions

        #region Store Page State

        /// <summary>
        /// Method to save Page State to the HttpContext session storage.
        /// </summary>
        /// <param name="pageState">Details on page state being stored</param>
        public void StoreHistoryPageState(OharaPageState pageState)
        {
            List<OharaPageState> pageStates = GetPageHistorySession();
            int pageCount = pageStates.Count() + 1;
            pageStates.RemoveAll(x => x.Index >= pageCount || x.Page == pageState.Page); //Clears all the pages where the index of the page is greater or equal to the page count or where the page is the same as the page being saved i.e. we're overwriting the existing page if it exists with the new one.
            pageCount = pageStates.Count() + 1; //Refresh the page count.
            pageState.Index = pageCount; //We place this page at the end of the list.
            pageState.Url = GetPageUrl();
            pageStates.Add(pageState);
            byte[] sessionBytes = ToByteArray(pageStates);
            _httpContext.Session.Remove(SESSION_NAME);
            _httpContext.Session.Set(SESSION_NAME, sessionBytes);
        }

        /// <summary>
        /// Method to save Page State to the HttpContext session storage.
        /// </summary>
        /// <param name="properties">Different properties to be stored on session (e.g search text, start date, end date)</param>
        public void StoreHistoryPageState(Dictionary<string, string> properties)
        {
            this.StoreHistoryPageState(null, null, properties);
        }

        /// <summary>
        /// Method to save Page State to the HttpContext session storage.
        /// </summary>
        /// <param name="page">Name of page the session is being stored for</param>
        /// <param name="name">Display name for navigation</param>
        /// <param name="properties">Different properties to be stored on session (e.g search text, start date, end date)</param>
        public void StoreHistoryPageState(string page, string name, Dictionary<string, string> properties)
        {
            _currentPageState = new OharaPageState();
            _currentPageState.Properties = properties;
            _currentPage = page;
            _currentPageState.Page = string.IsNullOrEmpty(page) ? _defaultPage : page;
            _currentPageState.Name = string.IsNullOrEmpty(name) ? _defaultName : name;
            this.StoreHistoryPageState(_currentPageState);
        }

        /// <summary>
        /// Method to save Page State to the HttpContext session storage.
        /// </summary>
        /// <param name="page">Name of page the state is being stored for</param>
        /// <param name="name">Display name for navigation</param>
        /// <param name="properties">Different properties to be stored on page state (e.g search text, start date, end date)</param>
        /// <param name="defaultProperties">Default properties which will be combined with the main properties.</param>
        public void StoreHistoryPageState(string page, string name, Dictionary<string, string> properties, Dictionary<string, string> defaultProperties)
        {
            _currentPageState = new OharaPageState();
            if (defaultProperties != null)
            {
                foreach (var property in properties)
                {
                    if (defaultProperties.ContainsKey(property.Key))
                    {
                        defaultProperties.Remove(property.Key);
                    }
                }
                _currentPageState = new OharaPageState(defaultProperties);
            }
            _currentPageState.Properties.Add(properties);
            _currentPage = page;
            _currentPageState.Page = string.IsNullOrEmpty(page) ? _defaultPage : page;
            _currentPageState.Name = string.IsNullOrEmpty(name) ? _defaultName : name;
            this.StoreHistoryPageState(_currentPageState);
        }

        #endregion //Store Page State

        #region Get Page States

        /// <summary>
        /// Get a list of all stored sessions for the application
        /// </summary>
        /// <returns></returns>
        public List<OharaPageState> GetPageHistorySession()
        {
            List<OharaPageState> result = new List<OharaPageState>();
            bool sessionExists = _httpContext.Session.TryGetValue(SESSION_NAME, out byte[] navigationSessionList);
            if (!sessionExists || (navigationSessionList == null) || navigationSessionList.Length < 1)
            {
                return result;
            }
            string jsonValue = Encoding.ASCII.GetString(navigationSessionList);
            if (string.IsNullOrEmpty(jsonValue))
            {
                return result;
            }
            result = JsonConvert.DeserializeObject<List<OharaPageState>>(jsonValue);
            result = result.OrderBy(x => x.Index).ToList();
            return result;
        }

        /// <summary>
        /// Get the current session values based on the current page.
        /// </summary>
        /// <param name="clearNewerPages">Whether or not to clear page states in the current session after this page i.e. pages with an index greater than this one will be cleared.</param>
        public OharaPageState GetCurrentPageState(bool clearNewerPages)
        {
            return GetPageState(_currentPage, clearNewerPages);
        }

        /// <summary>
        /// Gets page state based on provided pageName property.
        /// </summary>
        /// <param name="pageName">Name of the page the method is searching for</param>
        /// <param name="clearNewerPages">Whether or not to clear page states in the current session after this page i.e. pages with an index greater than this one will be cleared.</param>
        public OharaPageState GetPageState(string pageName, bool clearNewerPages)
        {
            List<OharaPageState> pageStates = GetPageHistorySession();
            OharaPageState result = pageStates.Where(s => s.Page == pageName).FirstOrDefault();
            if (result != null && clearNewerPages)
            {
                ClearHistoryByIndex(result.Index);
            }
            return result;
        }

        #endregion //Get Page States

        #region Clear Page States

        /// <summary>
        /// Clear all page states from the session currently stored.
        /// </summary>
        public void ClearPageHistorySession()
        {
            _currentPage = null;
            _currentPageState = null;
            _httpContext.Session.Clear();
        }

        /// <summary>
        /// Clears all page states from the current session greater than the specified index.
        /// </summary>
        public void ClearHistoryByIndex(int maxIndex)
        {
            List<OharaPageState>? pageStates = GetPageHistorySession()?.ToList();
            if (!pageStates.Any())
            {
                return;
            }
            pageStates.RemoveAll(x => x.Index > maxIndex);
            byte[] sessionBytes = ToByteArray(pageStates);
            _httpContext.Session.Remove(SESSION_NAME);
            _httpContext.Session.Set(SESSION_NAME, sessionBytes);
        }

        #endregion //Clear Page States

        #endregion //Sessions

        #region Navigation Elements

        /// <summary>
        /// Generates HTML to show the page states as breadcrumbs that can be displayed on a web page.
        /// </summary>
        public string GenerateBreadCrumbsFromPageHistorySession()
        {
            List<OharaPageState> pageStates = GetPageHistorySession();
            string links = "";
            string seperator = "<span> / </span> ";
            OharaPageState currentPageState = GetCurrentPageState(clearNewerPages: true);
            int count = pageStates.Count();
            count -= currentPageState != null ? 1 : 0;
            foreach (OharaPageState session in pageStates)
            {
                if (currentPageState != null && session.Index >= currentPageState.Index)
                {
                    continue;
                }
                string link = $"<a class=\"navigation_link\" id=\"{session.Page}\" href=\"{session.Url}\">{session.Name ?? session.Page}</a>";
                count -= 1;
                if (count > 0)
                {
                    link += seperator;
                }
                links += link;
            }
            if (_currentPage != null)
            {
                links += $"{(!string.IsNullOrEmpty(links) ? seperator : "")}<a class=\"navigation_current_link\" id=\"current\" href=\"#\" style=\"cursor: default\">{_currentPage}</a>";
            }
            return links;
        }

        #endregion //Navigation Elements

        #region Utilities

        public string GetPageUrl()
        {
            string output;
            HttpRequest request = _httpContext.Request;
            output = $"{request.Scheme}://";
            output += $"{request.Host.Value}{request.Path.Value}";
            output += $"{request.QueryString}";
            return output;
        }

        public byte[] ToByteArray<T>(T obj)
        {
            return obj != null ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)) : null;
        }

        #endregion //Utilities

        #endregion //Methods
    }
}
