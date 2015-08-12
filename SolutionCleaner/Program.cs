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

            var signingKeyFileName = "SolutionCleaner.snk";
            var signingKeyPath = Path.Combine(dir, signingKeyFileName);

            var ns = new XmlNamespaceManager(new System.Xml.NameTable());
            ns.AddNamespace("build", @"http://schemas.microsoft.com/developer/msbuild/2003");

            foreach (var csprojFile in Directory.EnumerateFiles(dir, projs, SearchOption.AllDirectories))
            {
                ProcessCSProj(csprojFile, ns, signingKeyPath);
            }
        }

        public static void ProcessCSProj(string csprojFile, IXmlNamespaceResolver ns, string signingKeyPath)
        {
            var projName = Path.GetFileNameWithoutExtension(csprojFile);
            var projDir = Path.GetDirectoryName(csprojFile);

            var projText = File.ReadAllText(csprojFile);
            var proj = XElement.Load(csprojFile);

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

            var contentParent = proj.XPathSelectElements("//build:ItemGroup[build:Reference]", ns).FirstOrDefault();
            if (contentParent != null)
            {
                if (contentParent.XPathSelectElements("//build:ProjectReference", ns).Any())
                {
                    var referenceParent = new XElement(XName.Get("ItemGroup", ns.LookupNamespace("build")));
                    contentParent.AddBeforeSelf(referenceParent);
                    contentParent = referenceParent;
                }

                var contents = proj.XPathSelectElements("//build:Reference", ns).OrderBy(c => c.Attribute("Include").Value.SubstringTill(',').Replace("System", "aaaaaaa")).ToArray();

                contents.Remove();
                contentParent.Add(contents);
            }
            #endregion

            #region Clean up ProjectReferences
            proj.XPathSelectElements("//build:ProjectReference[@Include]/build:Name", ns).SetValue(n => Path.GetFileNameWithoutExtension(n.Parent.Attribute("Include").Value));

            var projectReferenceParent = proj.XPathSelectElements("//build:ItemGroup[build:ProjectReference]", ns).FirstOrDefault();
            if (projectReferenceParent != null)
            {
                var projectReferences = proj.XPathSelectElements("//build:ProjectReference", ns).OrderBy(c => c.Element("Name").Value).ToArray();

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
            proj.XPathSelectElements("//build:SignAssembly", ns).Remove();
            proj.XPathSelectElements("//build:DelaySign", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyOriginatorKeyFile", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyKeyContainerName", ns).Remove();

            var mainPG = proj.XPathSelectElements("//build:PropertyGroup[build:ProjectGuid]", ns).FirstOrDefault();
            if (mainPG != null)
            {
                //mainPG.AddElement("ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch", content: "None");

                mainPG.AddElement("SignAssembly", content: true);
                mainPG.AddElement("DelaySign", content: false);
                mainPG.AddElement("AssemblyOriginatorKeyFile", content: PathTo(signingKeyPath, csprojFile));
            }
            #endregion

            proj.XPathSelectElements("//build:PropertyGroup", ns).Where(e => !e.Nodes().Any()).Remove();
            proj.XPathSelectElements("//build:ItemGroup", ns).Where(e => !e.Nodes().Any()).Remove();

            proj.Save(csprojFile);
        }

        #region XML helpers
        public static void AddElement(this XElement parent, string localName, object content = null, bool first = false, bool nonUnique = false)
        {
            if (nonUnique || !parent.Elements().Any(e => e.Name.LocalName == localName))
            {
                var e = new XElement(XName.Get(localName, parent.Name.NamespaceName), content);
                if (first)
                    parent.AddFirst(e);
                else
                    parent.Add(e);
            }
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

        public static void SetValue(this IEnumerable<XElement> enumerable, Func<XElement, string> value)
        {
            foreach (var e in enumerable)
                e.Value = value(e);
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
        #endregion

        private static string SubstringTill(this string value, char ch)
        {
            var index = value.IndexOf(ch);
            if (index > 0)
                return value.Substring(0, index);
            return value;
        }

        public static string PathTo(string path, string refPath)
        {
            var refFolder = Directory.Exists(refPath) ? refPath : Path.GetDirectoryName(refPath);

            var pathParts = path.Split(Path.DirectorySeparatorChar);
            var refParts = refFolder.Split(Path.DirectorySeparatorChar);

            var common = 0;
            for (int i = 0; i < pathParts.Length && i < refParts.Length; ++i)
            {
                common = i;
                if (String.Compare(pathParts[i], refParts[i], StringComparison.OrdinalIgnoreCase) != 0)
                {
                    break;
                }
            }
            if (String.Compare(pathParts[common], refParts[common], StringComparison.OrdinalIgnoreCase) == 0)
            {
                ++common;
            }

            var result = String.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", (refParts.Length - common)).Concat(pathParts.Skip(common)).ToArray());

            return result;
        }
    }
}
