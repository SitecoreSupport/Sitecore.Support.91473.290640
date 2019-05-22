using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Support.Shell.Applications.Layouts.DeviceEditor;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.Support.Shell.Applications.WebEdit.Commands
{
    public class EditRenderingProperties : Sitecore.Shell.Applications.WebEdit.Commands.EditRenderingProperties
    {
        protected new void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            int @int = MainUtil.GetInt(args.Parameters["selectedindex"], -1);
            if (@int >= 0)
            {
                Item clientContentItem = WebEditUtil.GetClientContentItem(Client.ContentDatabase);

                #region Modified code
                RenderingParameters parameters1 = new RenderingParameters();
                #endregion

                parameters1.Args = args;
                parameters1.DeviceId = args.Parameters["device"];
                parameters1.SelectedIndex = @int;
                parameters1.HandleName = args.Parameters["handle"];
                parameters1.Item = clientContentItem;
                if (parameters1.Show())
                {
                    if (!args.HasResult)
                    {
                        SheerResponse.SetAttribute("scLayoutDefinition", "value", string.Empty);
                    }
                    else
                    {
                        string layout = GetLayout(WebUtil.GetSessionString(args.Parameters["handle"]));
                        SheerResponse.SetAttribute("scLayoutDefinition", "value", layout);
                        SheerResponse.Eval("window.parent.Sitecore.PageModes.ChromeManager.handleMessage('chrome:rendering:propertiescompleted');");
                    }
                    WebUtil.RemoveSessionValue(args.Parameters["handle"]);
                }
            }
        }

        private static string GetLayout(string layout)
        {
            Assert.ArgumentNotNull(layout, "layout");
            return WebEditUtil.ConvertXMLLayoutToJSON(layout);
        }


    }
}