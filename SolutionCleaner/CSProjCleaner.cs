using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SolutionCleaner
{
    public static class CSProjCleaner
    {
        public static void Clean(XElement proj, IXmlNamespaceResolver ns, string relativeSigningKeyPath = null)
        {
            if (proj.Name.LocalName == "VisualStudioProject")
                return;

            #region Remove Cruft
            proj.XPathSelectElements("//build:AutorunEnabled", ns).Remove();
            proj.XPathSelectElements("//build:PublishWizardCompleted", ns).Remove();
            proj.XPathSelectElements("//build:TargetZone", ns).Remove();
            proj.XPathSelectElements("//build:ApplicationManifest", ns).Remove();
            proj.XPathSelectElements("//build:DefaultClientScript", ns).Remove();
            proj.XPathSelectElements("//build:DefaultHTMLPageLayout", ns).Remove();
            proj.XPathSelectElements("//build:DefaultTargetSchema", ns).Remove();
            proj.XPathSelectElements("//build:FileUpgradeFlags", ns).Remove();
            proj.XPathSelectElements("//build:OldToolsVersion", ns).Remove();
            proj.XPathSelectElements("//build:UpgradeBackupLocation", ns).Remove();
            proj.XPathSelectElements("//build:IsWebBootstrapper", ns).Remove();
            proj.XPathSelectElements("//build:PublishUrl", ns).Remove();
            proj.XPathSelectElements("//build:Install", ns).Remove();
            proj.XPathSelectElements("//build:InstallFrom", ns).Remove();
            proj.XPathSelectElements("//build:UpdateEnabled", ns).Remove();
            proj.XPathSelectElements("//build:UpdateMode", ns).Remove();
            proj.XPathSelectElements("//build:UpdateInterval", ns).Remove();
            proj.XPathSelectElements("//build:UpdateIntervalUnits", ns).Remove();
            proj.XPathSelectElements("//build:UpdatePeriodically", ns).Remove();
            proj.XPathSelectElements("//build:UpdateRequired", ns).Remove();
            proj.XPathSelectElements("//build:MapFileExtensions", ns).Remove();
            proj.XPathSelectElements("//build:ApplicationRevision", ns).Remove();
            proj.XPathSelectElements("//build:ApplicationVersion", ns).Remove();
            proj.XPathSelectElements("//build:UseApplicationTrust", ns).Remove();
            proj.XPathSelectElements("//build:BootstrapperEnabled", ns).Remove();
            proj.XPathSelectElements("//build:BootstrapperPackage", ns).Remove();
            proj.XPathSelectElements("//build:SccProjectName", ns).Remove();
            proj.XPathSelectElements("//build:SccLocalPath", ns).Remove();
            proj.XPathSelectElements("//build:SccAuxPath", ns).Remove();
            proj.XPathSelectElements("//build:SccProvider", ns).Remove();

            proj.XPathSelectElements("//build:NoWin32Manifest", ns).Remove();

            proj.XPathSelectElements("//build:EnableSecurityDebugging", ns).Remove();
            proj.XPathSelectElements("//build:StartAction", ns).Remove();
            proj.XPathSelectElements("//build:HostInBrowser", ns).Remove();
            proj.XPathSelectElements("//build:CreateWebPageOnPublish", ns).Remove();
            proj.XPathSelectElements("//build:WebPage", ns).Remove();
            proj.XPathSelectElements("//build:OpenBrowserOnPublish", ns).Remove();
            proj.XPathSelectElements("//build:TrustUrlParameters", ns).Remove();

            proj.XPathSelectElements("//build:UseIISExpress", ns).Remove();
            proj.XPathSelectElements("//build:IISExpressSSLPort", ns).Remove();
            proj.XPathSelectElements("//build:IISExpressAnonymousAuthentication", ns).Remove();
            proj.XPathSelectElements("//build:IISExpressWindowsAuthentication", ns).Remove();
            proj.XPathSelectElements("//build:IISExpressUseClassicPipelineMode", ns).Remove();
            #endregion

            #region Clean up References
            proj.XPathSelectElements("//build:Reference[@Include='System.configuration']", ns).Attributes("Include").SetValue("System.Configuration");
            proj.XPathSelectElements("//build:Reference[@Include='System.XML']", ns).Attributes("Include").SetValue("System.Xml");

            proj.XPathSelectElements("//build:AssemblyFolderKey", ns).Remove();
            proj.XPathSelectElements("//build:SpecificVersion", ns).Remove();
            proj.XPathSelectElements("//build:ReferencePath", ns).Remove();
            proj.XPathSelectElements("//build:CurrentPlatform", ns).Remove();
            proj.XPathSelectElements("//build:Reference/build:Name", ns).Remove();
            proj.XPathSelectElements("//build:Reference/build:RequiredTargetFramework", ns).Remove();

            var referenceParent = proj.XPathSelectElements("//build:ItemGroup[build:Reference]", ns).FirstOrDefault();
            if (referenceParent != null)
            {
                var references = proj.XPathSelectElements("//build:Reference", ns).OrderBy(c => c.Attribute("Include").Value.SubstringTill(',').Replace("System", "aaaaaaa")).ToArray();

                references.Remove();
                referenceParent.Add(references);
            }
            #endregion

            #region Clean up ProjectReferences
            proj.XPathSelectElements("//build:ProjectReference[@Include]/build:Name", ns).SetValue(n => Path.GetFileNameWithoutExtension(n.Parent.Attribute("Include").Value));

            var projectReferenceParent = proj.XPathSelectElements("//build:ItemGroup[build:ProjectReference]", ns).FirstOrDefault();
            if (projectReferenceParent != null)
            {
                var projectReferences = proj.XPathSelectElements("//build:ProjectReference", ns).OrderBy(c => c.Element(XName.Get("Name", ns.LookupNamespace("build"))).Value).ToArray();

                projectReferences.Remove();
                projectReferenceParent.Add(projectReferences);
            }
            #endregion

            #region Clean up PreBuildEvent/PostBuildEvent
            proj.XPathSelectElements("//build:PostBuildEvent", ns).Where(e => e.Value.IndexOf("sn.exe", StringComparison.CurrentCultureIgnoreCase) > 0).SetValue("");

            proj.XPathSelectElements("//build:PreBuildEvent", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();
            proj.XPathSelectElements("//build:PostBuildEvent", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();

            if (!proj.XPathSelectElements("//build:PreBuildEvent", ns).Any(e => String.IsNullOrWhiteSpace(e.Value)))
                proj.XPathSelectElements("//build:RunPreBuildEvent", ns).Remove();

            if (!proj.XPathSelectElements("//build:PostBuildEvent", ns).Any(e => String.IsNullOrWhiteSpace(e.Value)))
                proj.XPathSelectElements("//build:RunPostBuildEvent", ns).Remove();
            #endregion

            #region Configure signing
            var canSign = String.IsNullOrEmpty(relativeSigningKeyPath);

            proj.XPathSelectElements("//build:SignAssembly", ns).Remove();
            proj.XPathSelectElements("//build:DelaySign", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyOriginatorKeyFile", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyKeyContainerName", ns).Remove();

            var mainPG = proj.XPathSelectElements("//build:PropertyGroup[build:ProjectGuid]", ns).FirstOrDefault();
            if (mainPG != null)
            {
                //mainPG.AddElement("ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch", content: "None");

                mainPG.AddElement("SignAssembly", content: canSign);
                if (canSign)
                {
                    mainPG.AddElement("DelaySign", content: false);

                    mainPG.AddElement("AssemblyOriginatorKeyFile", content: relativeSigningKeyPath);
                }
            }
            #endregion

            proj.XPathSelectElements("//build:PropertyGroup", ns).Where(e => !e.Nodes().Any()).Remove();
            proj.XPathSelectElements("//build:ItemGroup", ns).Where(e => !e.Nodes().Any()).Remove();
        }
    }
}
