namespace Ohara.Library.Html
{
    #region Using Directives

    using HtmlAgilityPack;
    using Ohara.Library.Html.Classes;
    using Ohara.Library.Html.Enums;
    using System.Net.Http.Headers;

    #endregion //Using Directives

    public class HtmlKit
    {
        #region Methods

        /// <summary>
        /// This method returns a Dictionary object which consists of a list of properties and their respective values from the HTML.
        /// </summary>
        /// <param name="tableLine">HtmlTableLine object which consists of a an id, row and header html values</param>
        public Dictionary<string, object> GetHtmlFields(HtmlTableLine tableLine)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            HtmlDocument data = new HtmlDocument();
            HtmlDocument headers = new HtmlDocument();

            data.LoadHtml(tableLine.Data);
            headers.LoadHtml(tableLine.Headers);

            List<HtmlNode> valueElements = data.DocumentNode.ChildNodes.Where(x => x.Name == "td" && x.Attributes.Any(y => y.Name == "class" && y.Value == "edit-row")).ToList();
            List<HtmlNode> headerElements = headers.DocumentNode.ChildNodes.Where(x => x.Name == "th").OrderBy(x => x.Line).ToList();

            foreach (var valueElement in valueElements)
            {
                var editElement = valueElement.ChildNodes.First(x => x.Attributes.Any(x => x.Name == "name" && ( x.Value.StartsWith("edit") || x.Value.StartsWith("chk")))); //gives you the entire input element wether its a select or input, as lond as it has a name starting with the test 'edit'
                if (editElement != null)
                {
                    HtmlAttribute valueAttribute = editElement.Attributes.Where((x) => x.Name == "value").FirstOrDefault();
                    string value = valueAttribute != null ? valueAttribute.Value : string.Empty;
                    string header = headerElements.First(x => x.Line == (valueElement.Line * 2 - 2)).InnerText.Replace(" ", "").Replace("\n", "").Replace("\r", ""); //th lines are in multiples of 2
                    output.Add(header, value);
                }
            }
            return output;
        }

        public T GetObjectFromHtml<T>(
            HtmlTableLine tableLine,
            Dictionary<string, string> fieldsToGet,
            Dictionary<string, Type> enumFieldsToGet,
            T originalObject) where T : class, new()
        {
            var manualPropertiesDefault = new Dictionary<string, object>();
            return GetEntitytFromHtml<T>(tableLine, fieldsToGet, enumFieldsToGet, ref manualPropertiesDefault, originalObject);
        }

        /// <summary>
        /// This methods generates a object based on the provided datatype, using a html input
        /// </summary>
        /// <typeparam name="T">output datatype</typeparam>
        /// <param name="tableLine">HtmlTableLine object which consists of a an id, row and header html values</param>
        /// <param name="fieldsToGet">List of keys which will be used to replace table property name(s) with actual property name(s)</param>
        /// <param name="enumFieldsToGet">List of properties which contain Enum related values, which will be used to get int values of enums</param>
        /// <param name="originalObject">An object which consists of property values which wont be updated</param>
        /// <returns></returns>
        public T GetEntitytFromHtml<T>(
            HtmlTableLine tableLine,
            Dictionary<string, string> fieldsToGet,
            Dictionary<string, Type> enumFieldsToGet,
            ref Dictionary<string, object> manualUpdateFields,
            T originalObject) where T : class, new()
        {
            Dictionary<string, object> htmlFields = GetHtmlFields(tableLine);
            if (fieldsToGet != null)
            {
                foreach (var fieldToGet in fieldsToGet)
                {
                    string key = fieldToGet.Key.Replace(" ", "");
                    if (htmlFields.ContainsKey(key))
                    {
                        var htmlValue = htmlFields[key];
                        htmlFields.Remove(key);
                        htmlFields.Add(fieldToGet.Value, htmlValue);
                    }
                }
            }
            T entity = GetEntityFromHtmlFields<T>(htmlFields, enumFieldsToGet, ref manualUpdateFields, originalObject);
            return entity;
        }

        public T GetEntityFromHtmlFields<T>(
            Dictionary<string, object> htmlFields,
            Dictionary<string, Type> enumProperties,
            ref Dictionary<string, object> manualUpdateFields,
            T originalEntity) where T : class, new()
        {
            var entity = originalEntity ?? new T(); //If the original entity is not null, use it to update its properties , otherwise create a new entity.
            var entityType = entity.GetType();
            var entityProperties = entityType.GetProperties().ToList();

            foreach (KeyValuePair<string, object> htmlField in htmlFields)
            {
                var entityProperty = entityType.GetProperty(htmlField.Key); //Check if the entity has a property with the html field name.
                if (entityProperty == null)
                {
                    continue; //There is not property on the entity with the given html field name.
                }
                if (manualUpdateFields != null && manualUpdateFields.ContainsKey(htmlField.Key))
                {
                    manualUpdateFields[htmlField.Key] = htmlField.Value;
                    continue;
                }
                object entityPropertyValue;
                bool valueDefaulted = false;
                if (enumProperties != null && (entityProperty.PropertyType.IsEnum || enumProperties.ContainsKey(htmlField.Key))) //Handle enum properties
                {
                    string objStringVal = htmlField.Value.ToString().Replace(" ", "");
                    entityPropertyValue = (int)(Enum.Parse(enumProperties[htmlField.Key] ?? entityProperty.PropertyType, objStringVal));
                    entityProperty.SetValue(entity, valueDefaulted ? null : entityPropertyValue, null);
                    continue;
                }
                var entityPropertyType = entityProperty.PropertyType;
                if (entityPropertyType.IsGenericType && entityPropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) //Handle nunllable properties.
                {
                    object defaultValue = Activator.CreateInstance(entityPropertyType);
                    if (Type.GetTypeCode(Nullable.GetUnderlyingType(entityPropertyType)) == TypeCode.Double
                        || Type.GetTypeCode(Nullable.GetUnderlyingType(entityPropertyType)) == TypeCode.Decimal
                        || Type.GetTypeCode(Nullable.GetUnderlyingType(entityPropertyType)) == TypeCode.Int64
                        || Type.GetTypeCode(Nullable.GetUnderlyingType(entityPropertyType)) == TypeCode.Int32)
                    {
                        defaultValue = 0;
                        valueDefaulted = true;
                    }
                    entityPropertyValue = Convert.ChangeType(string.IsNullOrEmpty(htmlField.Value.ToString()) ? defaultValue : htmlField.Value, Nullable.GetUnderlyingType(entityPropertyType));
                    if (entityPropertyValue != defaultValue) {
                        valueDefaulted = false;
                    }
                }
                else //Handle non-nullable properties.
                {
                    entityPropertyValue = Convert.ChangeType(htmlField.Value, entityPropertyType);
                }
                entityProperty.SetValue(entity, valueDefaulted ? null : entityPropertyValue, null);
            }
            return entity;
        }

        public string GetInlineEditHtmlControls(Guid elementId, object elementValue, string elementName, HtmlElementType type)
        {
            return GetInlineEditHtmlControls(elementId, elementValue, elementName, type, "onInlineEditClick(this)");
        }

        public string GetInlineEditHtmlControls(Guid elementId, object elementValue, string elementName, HtmlElementType type, string javascriptFunction)
        {
            string output;
            switch (type)
            {
                case HtmlElementType.Text: output = "<input style='display:none' name='edit_" + elementId + "' value='" + elementValue + "'  /><span name='select_" + elementId + "'>" + elementValue + "<span/>"; break;
                case HtmlElementType.Number: output = "<input style='display:none' name='edit_" + elementId + "' value='" + elementValue + "' type='number'  /><span name='select_" + elementId + "'>" + elementValue + "<span/>"; break;
                case HtmlElementType.CheckboxAlwaysOn: output = $"<input id='{elementId}' name='chk_{elementId}' onChange='{javascriptFunction};' type='checkbox' {(((bool)elementValue == true) ? "checked" : "")} />"; break;
                case HtmlElementType.Date: output = output = $"<input style='display:none'  name='edit_{elementId}' type='date' value='{Convert.ToDateTime(elementValue).ToString("yyyy-MM-dd")}'><span name='select_{elementId}'>{Convert.ToDateTime(elementValue)}<span/>"; break;
                case HtmlElementType.DropDown: output = $"<select class='{elementName}' style='display:none' name='edit_{elementId}' value='{elementValue}'></select><span name='select_{elementId}'>{elementValue}<span/>"; break;
                case HtmlElementType.CheckBox: output = $"<input style='display:none' name='edit_{elementId}' {((bool)elementValue ? "checked" : "sd")} type='checkbox' /><input name='select_{elementId}' type='checkbox' {(((bool)elementValue == true) ? "checked" : "")} disabled='disabled' />"; break;
                default: output = ""; break;
            }
            return output;
        }

        #endregion //Methods
    }
}
