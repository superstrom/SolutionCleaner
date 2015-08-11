using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SolutionCleaner
{
    static class Program
    {
        static void Main(string[] args)
        {
            var dir = @"C:\dev\SolutionCleaner";
            var projs = "*.csproj";

            var ns = new XmlNamespaceManager(new System.Xml.NameTable());
            ns.AddNamespace("build", @"http://schemas.microsoft.com/developer/msbuild/2003");

            foreach (var csprojFile in Directory.EnumerateFiles(dir, projs, SearchOption.AllDirectories))
            {
                ProcessCSProj(csprojFile, ns);
            }
        }

        public static void ProcessCSProj(string csprojFile, IXmlNamespaceResolver ns)
        {
            var projName = Path.GetFileNameWithoutExtension(csprojFile);
            var projDir = Path.GetDirectoryName(csprojFile);

            var projText = File.ReadAllText(csprojFile);
            var proj = XElement.Load(csprojFile);

            if (proj.Name.LocalName == "VisualStudioProject")
                return;

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


            proj.XPathSelectElements("//build:Reference[@Include='System.configuration']", ns).Attributes("Include").SetValue("System.Configuration");
            proj.XPathSelectElements("//build:Reference[@Include='System.XML']", ns).Attributes("Include").SetValue("System.Xml");

            proj.XPathSelectElements("//build:AssemblyFolderKey", ns).Remove();
            proj.XPathSelectElements("//build:SpecificVersion", ns).Remove();
            proj.XPathSelectElements("//build:ReferencePath", ns).Remove();
            proj.XPathSelectElements("//build:CurrentPlatform", ns).Remove();
            proj.XPathSelectElements("//build:Reference/build:Name", ns).Remove();
            proj.XPathSelectElements("//build:Reference/build:RequiredTargetFramework", ns).Remove();

            proj.XPathSelectElements("//build:PropertyGroup", ns).Where(e => !e.Nodes().Any()).Remove();
            proj.XPathSelectElements("//build:ItemGroup", ns).Where(e => !e.Nodes().Any()).Remove();

            proj.Save(csprojFile);
        }

        public static void SetValue(this IEnumerable<XElement> enumerable, string value)
        {
            foreach (var e in enumerable)
                e.Value = value;
        }

        public static void SetValue(this IEnumerable<XElement> enumerable, Func<string, string> value)
        {
            foreach (var e in enumerable)
                e.Value = value(e.Value);
        }

        public static void SetValue(this IEnumerable<XElement> enumerable, bool value)
        {
            foreach (var e in enumerable)
                e.Value = value.ToString().ToLower();
        }

        public static void SetValue(this IEnumerable<XAttribute> enumerable, string value)
        {
            foreach (var item in enumerable)
                item.Value = value;
        }
    }
}
