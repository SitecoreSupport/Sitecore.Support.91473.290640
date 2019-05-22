// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderingParameters.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>
// <summary>
//   Defines the rendering parameters options class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.Shell.Applications.Layouts.DeviceEditor
{
    #region Namespaces

    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using Diagnostics;
    using SecurityModel;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Layouts;
    using Sitecore.Shell;
    using Sitecore.Shell.Applications.WebEdit;
    using Sitecore.Web;
    using Sitecore.Web.UI.Sheer;
    using Text;
    using WebEdit;

    using WebEditUtil = Sitecore.Web.WebEditUtil;

    #endregion Namespaces

    /// <summary>
    /// Defines the rendering parameters options class.
    /// </summary>
    public class RenderingParameters
    {
        #region Constants and Fields

        /// <summary>
        /// The current pipeline arguments.
        /// </summary>
        private ClientPipelineArgs args;

        /// <summary>
        /// The selected device ID.
        /// </summary>
        private string deviceId;

        /// <summary>
        /// The name of the handle.
        /// </summary>
        private string handleName;

        /// <summary>
        /// The current layout definition.
        /// </summary>
        private LayoutDefinition layoutDefinition;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the args.
        /// </summary>
        /// <value>
        /// The args.
        /// </value>
        [NotNull]
        public ClientPipelineArgs Args
        {
            get
            {
                return this.args;
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");

                this.args = value;
            }
        }

        /// <summary>
        /// Gets or sets the  item.
        /// </summary>
        /// <value>The item.</value>
        public Item Item { set; private get; }

        /// <summary>
        /// Gets or sets the device ID.
        /// </summary>
        /// <value>
        /// The device ID.
        /// </value>
        [NotNull]
        public string DeviceId
        {
            get
            {
                return this.deviceId ?? string.Empty;
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");

                this.deviceId = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the handle.
        /// </summary>
        /// <value>
        /// The name of the handle.
        /// </value>
        [NotNull]
        public string HandleName
        {
            get
            {
                return this.handleName ?? "SC_DEVICEEDITOR";
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");

                this.handleName = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected.
        /// </summary>
        /// <value>
        /// The index of the selected.
        /// </value>
        public int SelectedIndex { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows this instance.
        /// </summary>
        /// <returns>
        /// The boolean.
        /// </returns>
        public bool Show()
        {
            if (this.Args.IsPostBack)
            {
                if (this.Args.HasResult)
                {
                    this.Save();
                }

                return true;
            }

            if (this.SelectedIndex < 0)
            {
                return true;
            }

            var renderingDefinition = this.GetRenderingDefinition();

            if (renderingDefinition == null)
            {
                return true;
            }

            string urlPath = null;

            if (!string.IsNullOrEmpty(renderingDefinition.ItemID))
            {
                Item renderingItem = Client.ContentDatabase.GetItem(renderingDefinition.ItemID, this.Item.Language);
                if (renderingItem != null)
                {
                    LinkField linkField = renderingItem.Fields["Customize Page"];
                    Assert.IsNotNull(linkField, "linkField");
                    if (!string.IsNullOrEmpty(linkField.Url))
                    {
                        urlPath = linkField.Url;
                    }
                }
            }

            var parameters = GetParameters(renderingDefinition);

            var fields = GetFields(renderingDefinition, parameters);

            var options = new RenderingParametersFieldEditorOptions(fields)
            {
                DialogTitle = Texts.ControlProperties,
                HandleName = this.HandleName,
                PreserveSections = true
            };

            this.SetCustomParameters(renderingDefinition, options);

            UrlString url;
            if (!string.IsNullOrEmpty(urlPath))
            {
                url = new UrlString(urlPath);

                options.ToUrlHandle().Add(url, this.HandleName);
            }
            else
            {
                url = options.ToUrlString();
            }

            SheerResponse.ShowModalDialog(new ModalDialogOptions(url.ToString())
            {
                Width = "720",
                Height = "480",
                Response = true,
                Header = options.DialogTitle
            });

            this.args.WaitForPostBack();
            return false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the custom parameters.
        /// </summary>
        /// <param name="renderingDefinition">The rendering definition.</param>
        /// <param name="options">The options.</param>
        private void SetCustomParameters([NotNull] RenderingDefinition renderingDefinition, [NotNull] RenderingParametersFieldEditorOptions options)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");
            Assert.ArgumentNotNull(options, "options");

            var renderingItem = renderingDefinition.ItemID != null ? Client.ContentDatabase.GetItem(renderingDefinition.ItemID) : null;
            if (renderingItem != null)
            {
                options.Parameters["rendering"] = renderingItem.Uri.ToString();
            }

            if (this.Item != null)
            {
                options.Parameters["contentitem"] = this.Item.Uri.ToString();
            }

            if (WebEditUtil.IsRenderingPersonalized(renderingDefinition))
            {
                options.Parameters["warningtext"] = Texts.PersonalizationConditionsDefinedWarning;
            }

            if (!string.IsNullOrEmpty(renderingDefinition.MultiVariateTest))
            {
                options.Parameters["warningtext"] = Texts.Thereisamultivariatetestsetupforthiscontro;
            }
        }

        /// <summary>
        /// Gets the additional parameters.
        /// </summary>
        /// <param name="fieldDescriptors">
        /// The field descriptors.
        /// </param>
        /// <param name="standardValues">
        /// The standard values.
        /// </param>
        /// <param name="additionalParameters">
        /// The addtional parameters.
        /// </param>
        private static void GetAdditionalParameters(
          [NotNull] List<FieldDescriptor> fieldDescriptors,
          [NotNull] Item standardValues,
          [NotNull] Dictionary<string, string> additionalParameters)
        {
            Assert.ArgumentNotNull(fieldDescriptors, "fieldDescriptors");
            Assert.ArgumentNotNull(standardValues, "standardValues");
            Assert.ArgumentNotNull(additionalParameters, "additionalParameters");

            string additionalParametersFieldName = "Additional Parameters";

            if (standardValues.Fields[additionalParametersFieldName] == null && !additionalParameters.Any())
            {
                // Ignore adding additional parameters in case standard value doesn't contain Additional Parameters field and no additional parameters passed
                // in order to support SPEAK controls.
                return;
            }

            var value = new UrlString();

            foreach (var key in additionalParameters.Keys)
            {
                value[key] = HttpUtility.UrlDecode(additionalParameters[key]);
            }

            fieldDescriptors.Add(new FieldDescriptor(standardValues, additionalParametersFieldName) { Value = value.ToString() });
        }

        /// <summary>
        /// Gets the caching.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <returns>
        /// The caching.
        /// </returns>
        [NotNull]
        private static string GetCaching([NotNull] RenderingDefinition renderingDefinition)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");

            return (renderingDefinition.Cachable == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.ClearOnIndexUpdate == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByData == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByDevice == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByLogin == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByParameters == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByQueryString == "1" ? "1" : "0") + "|" +
                   (renderingDefinition.VaryByUser == "1" ? "1" : "0");
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The fields.
        /// </returns>
        [NotNull]
        private List<FieldDescriptor> GetFields(
          [NotNull] RenderingDefinition renderingDefinition,
          [NotNull] Dictionary<string, string> parameters)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");
            Assert.ArgumentNotNull(parameters, "parameters");

            var result = new List<FieldDescriptor>();
            Item standardValues;

            using (new SecurityDisabler())
            {
                standardValues = this.GetStandardValuesItem(renderingDefinition);
            }

            if (standardValues == null)
            {
                return result;
            }

            var fieldCollection = standardValues.Fields;
            fieldCollection.ReadAll();
            fieldCollection.Sort();

            var additionalParameters = new Dictionary<string, string>(parameters);

            foreach (Field field in fieldCollection)
            {
                if (field.Name == "Additional Parameters")
                {
                    continue;
                }

                if (field.Name == "Personalization" && !UserOptions.View.ShowPersonalizationSection)
                {
                    continue;
                }

                if (field.Name == "Tests" && !UserOptions.View.ShowTestLabSection)
                {
                    continue;
                }

                if (!RenderingItem.IsRenderingParameterField(field))
                {
                    continue;
                }

                var value = GetValue(field.Name, renderingDefinition, parameters);

                var fieldDescriptor = new FieldDescriptor(standardValues, field.Name)
                {
                    Value = value ?? field.Value,
                    ContainsStandardValue = (value == null) ? true : false,
                };

                result.Add(fieldDescriptor);
                additionalParameters.Remove(field.Name);
            }

            GetAdditionalParameters(result, standardValues, additionalParameters);
            return result;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <returns>
        /// The parameters.
        /// </returns>
        [NotNull]
        private static Dictionary<string, string> GetParameters([NotNull] RenderingDefinition renderingDefinition)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");

            var result = new Dictionary<string, string>();
            var parameterCollection = WebUtil.ParseUrlParameters(
              renderingDefinition.Parameters ?? string.Empty);

            foreach (string key in parameterCollection.Keys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = parameterCollection[key];
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the rendering item.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <returns>
        /// The rendering item.
        /// </returns>
        [CanBeNull]
        private Item GetRenderingItem([NotNull] RenderingDefinition renderingDefinition)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");

            var itemId = renderingDefinition.ItemID;
            return string.IsNullOrEmpty(itemId) ? null : Client.ContentDatabase.GetItem(itemId, this.Item.Language);
        }

        /// <summary>
        /// Gets the standard values item.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <returns>
        /// The standard values item.
        /// </returns>
        [CanBeNull]
        private Item GetStandardValuesItem([NotNull] RenderingDefinition renderingDefinition)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");

            Item renderingItem = this.GetRenderingItem(renderingDefinition);

            if (renderingItem == null)
            {
                return null;
            }

            return RenderingItem.GetStandardValuesItemFromParametersTemplate(renderingItem);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="fieldName">
        /// The name.
        /// </param>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        [CanBeNull]
        private static string GetValue(
          [NotNull] string fieldName,
          [NotNull] RenderingDefinition renderingDefinition,
          [NotNull] Dictionary<string, string> parameters)
        {
            Assert.ArgumentNotNull(fieldName, "fieldName");
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");
            Assert.ArgumentNotNull(parameters, "parameters");

            switch (fieldName.ToLowerInvariant())
            {
                case "placeholder":
                    return renderingDefinition.Placeholder ?? string.Empty;
                case "data source":
                    return renderingDefinition.Datasource ?? string.Empty;
                case "caching":
                    return GetCaching(renderingDefinition);
                case "personalization":
                    return renderingDefinition.Conditions ?? string.Empty;
                case "tests":
                    return renderingDefinition.MultiVariateTest ?? string.Empty;
            }

            string value;
            parameters.TryGetValue(fieldName, out value);
            return value;
        }

        /// <summary>
        /// Sets the caching.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private static void SetCaching([NotNull] RenderingDefinition renderingDefinition, [NotNull] string value)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");
            Assert.ArgumentNotNull(value, "value");

            if (string.IsNullOrEmpty(value))
            {
                value = "0|0|0|0|0|0|0|0";
            }

            var parts = value.Split('|');
            Assert.IsTrue(parts.Length == 8, "Invalid caching value format");

            renderingDefinition.Cachable = parts[0] == "1" ? "1" : renderingDefinition.Cachable != null ? "0" : null;
            renderingDefinition.ClearOnIndexUpdate = parts[1] == "1" ? "1" : renderingDefinition.ClearOnIndexUpdate != null ? "0" : null;
            renderingDefinition.VaryByData = parts[2] == "1" ? "1" : renderingDefinition.VaryByData != null ? "0" : null;
            renderingDefinition.VaryByDevice = parts[3] == "1" ? "1" : renderingDefinition.VaryByDevice != null ? "0" : null;
            renderingDefinition.VaryByLogin = parts[4] == "1" ? "1" : renderingDefinition.VaryByLogin != null ? "0" : null;
            renderingDefinition.VaryByParameters = parts[5] == "1" ? "1" : renderingDefinition.VaryByParameters != null ? "0" : null;
            renderingDefinition.VaryByQueryString = parts[6] == "1" ? "1" : renderingDefinition.VaryByQueryString != null ? "0" : null;
            renderingDefinition.VaryByUser = parts[7] == "1" ? "1" : renderingDefinition.VaryByUser != null ? "0" : null;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="renderingDefinition">
        /// The rendering definition.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="fieldName">
        /// Name of the field.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private static void SetValue(
          [NotNull] RenderingDefinition renderingDefinition,
          [NotNull] UrlString parameters,
          [NotNull] string fieldName,
          [NotNull] string value)
        {
            Assert.ArgumentNotNull(renderingDefinition, "renderingDefinition");
            Assert.ArgumentNotNull(fieldName, "fieldName");
            Assert.ArgumentNotNull(value, "value");
            Assert.ArgumentNotNull(parameters, "parameters");

            var name = fieldName.ToLowerInvariant();

            switch (name)
            {
                case "placeholder":
                    renderingDefinition.Placeholder = value;
                    return;
                case "data source":
                    renderingDefinition.Datasource = value;
                    return;
                case "caching":
                    SetCaching(renderingDefinition, value);
                    return;
                case "personalization":
                    renderingDefinition.Conditions = value;
                    return;
                case "tests":
                    if (string.IsNullOrEmpty(value))
                    {
                        renderingDefinition.MultiVariateTest = string.Empty;
                    }

                    var item = Client.ContentDatabase.GetItem(value);
                    if (item != null)
                    {
                        renderingDefinition.MultiVariateTest = item.ID.ToString();
                        return;
                    }

                    renderingDefinition.MultiVariateTest = value;
                    return;
            }

            if (name == "additional parameters")
            {
                #region Modified code

                // add Rendering Parameters as parameters instead of appending them to the rendering
                var additionalParameters = new UrlString(value).Parameters;
                parameters.Parameters.Add(additionalParameters);

                #endregion
                return;
            }

            parameters[fieldName] = value;
        }

        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <returns>
        /// The definition.
        /// </returns>
        [NotNull]
        private LayoutDefinition GetLayoutDefinition()
        {
            if (this.layoutDefinition == null)
            {
                var sessionValue = WebUtil.GetSessionString(this.HandleName);
                Assert.IsNotNull(sessionValue, "sessionValue");

                this.layoutDefinition = LayoutDefinition.Parse(sessionValue);
            }

            return this.layoutDefinition;
        }

        /// <summary>
        /// Gets the rendering definition.
        /// </summary>
        /// <returns>
        /// The rendering definition.
        /// </returns>
        [CanBeNull]
        private RenderingDefinition GetRenderingDefinition()
        {
            var renderings = this.GetLayoutDefinition().GetDevice(this.DeviceId).Renderings;

            if (renderings == null)
            {
                return null;
            }

            return renderings[MainUtil.GetInt(this.SelectedIndex, 0)] as RenderingDefinition;
        }

        /// <summary>
        /// Sets the values.
        /// </summary>
        private void Save()
        {
            var renderingDefinition = this.GetRenderingDefinition();

            if (renderingDefinition == null)
            {
                return;
            }

            Item standardValues;

            using (new SecurityDisabler())
            {
                standardValues = GetStandardValuesItem(renderingDefinition);
            }

            if (standardValues == null)
            {
                return;
            }

            var parameters = new UrlString();

            foreach (var field in RenderingParametersFieldEditorOptions.Parse(this.args.Result).Fields)
            {
                SetValue(renderingDefinition, parameters, standardValues.Fields[field.FieldID].Name, field.Value);
            }

            renderingDefinition.Parameters = parameters.ToString();
            var layout = this.GetLayoutDefinition();
            this.SetLayoutDefinition(layout);
        }

        /// <summary>
        /// Sets the definition.
        /// </summary>
        /// <param name="layout">
        /// The layout.
        /// </param>
        private void SetLayoutDefinition([NotNull] LayoutDefinition layout)
        {
            Assert.ArgumentNotNull(layout, "layout");

            WebUtil.SetSessionValue(this.HandleName, layout.ToXml());
        }

        #endregion
    }
}