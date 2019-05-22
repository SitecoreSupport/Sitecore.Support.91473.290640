using Sitecore.Diagnostics;
using Sitecore.Shell.Web.UI;
using Sitecore.Support.Shell.Applications.Layouts.DeviceEditor;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.Xml;
using System;
using System.Collections;
using System.Web.UI;
using System.Xml;

namespace Sitecore.Support.Shell.Applications.ContentManager.Dialogs.LayoutDetails
{
    public class LayoutDetailsForm : Sitecore.Shell.Applications.ContentManager.Dialogs.LayoutDetails.LayoutDetailsForm
    {
        private enum TabType
        {
            Shared,
            Final,
            Unknown
        }
        protected new void EditRenderingPipeline(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            #region Modified code
            RenderingParameters parameters = new RenderingParameters
            {
                Args = args
            };
            #endregion

            string[] values = new string[] { args.Parameters["deviceid"] };
            parameters.DeviceId = StringUtil.GetString(values);
            string[] textArray2 = new string[] { args.Parameters["index"] };
            parameters.SelectedIndex = MainUtil.GetInt(StringUtil.GetString(textArray2), 0);
            parameters.Item = UIUtil.GetItemFromQueryString(Client.ContentDatabase);
            if (!args.IsPostBack)
            {
                XmlDocument doc = this.GetDoc();
                WebUtil.SetSessionValue("SC_DEVICEEDITOR", doc.OuterXml);
            }
            if (parameters.Show())
            {
                XmlDocument doc = XmlUtil.LoadXml(WebUtil.GetSessionString("SC_DEVICEEDITOR"));
                WebUtil.SetSessionValue("SC_DEVICEEDITOR", null);
                this.SetActiveLayout(GetLayoutValue(doc));
                this.Refresh();
            }
        }

        private XmlDocument GetDoc()
        {
            XmlDocument document = new XmlDocument();
            string activeLayout = this.GetActiveLayout();
            if (activeLayout.Length > 0)
            {
                document.LoadXml(activeLayout);
            }
            else
            {
                document.LoadXml("<r/>");
            }
            return document;
        }

        private static string GetLayoutValue(XmlDocument doc)
        {
            string outerXml;
            Assert.ArgumentNotNull(doc, "doc");
            XmlNodeList xmlNodeLists = doc.SelectNodes("/r/d");
            if (xmlNodeLists == null || xmlNodeLists.Count == 0)
            {
                return string.Empty;
            }
            IEnumerator enumerator = xmlNodeLists.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    XmlNode current = (XmlNode)enumerator.Current;
                    if (current.ChildNodes.Count <= 0 && XmlUtil.GetAttribute("l", current).Length <= 0)
                    {
                        continue;
                    }
                    outerXml = doc.OuterXml;
                    return outerXml;
                }
                return string.Empty;
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            return outerXml;
        }


        private void Refresh()
        {
            string activeLayout = this.GetActiveLayout();
            this.RenderLayoutGridBuilder(activeLayout, (this.ActiveTab == TabType.Final) ? this.FinalLayoutPanel : this.LayoutPanel);
        }

        private void RenderLayoutGridBuilder(string layoutValue, Control renderingContainer)
        {
            string str = renderingContainer.ID + "LayoutGrid";
            LayoutGridBuilder builder1 = new LayoutGridBuilder();
            builder1.ID = str;
            builder1.Value = layoutValue;
            builder1.EditRenderingClick = "EditRendering(\"$Device\", \"$Index\")";
            builder1.EditPlaceholderClick = "EditPlaceholder(\"$Device\", \"$UniqueID\")";
            builder1.OpenDeviceClick = "OpenDevice(\"$Device\")";
            builder1.CopyToClick = "CopyDevice(\"$Device\")";
            renderingContainer.Controls.Clear();
            builder1.BuildGrid(renderingContainer);
            if (Context.ClientPage.IsEvent)
            {
                SheerResponse.SetOuterHtml(renderingContainer.ID, (Control)renderingContainer);
                SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
            }
        }
        private TabType ActiveTab
        {
            get
            {
                int active = this.Tabs.Active;
                return ((active != 0) ? ((active != 1) ? TabType.Unknown : TabType.Final) : TabType.Shared);
            }
        }


    }


}
