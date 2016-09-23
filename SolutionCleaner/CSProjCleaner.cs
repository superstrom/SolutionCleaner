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
            var frameworkVersion = "v4.5.2";
            var frameworkProfile = "";
            var configurations = new[] { "Debug", "Release" };
            var platforms = new[] { "AnyCPU", "x86", "x64", "ARM" };

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
            proj.XPathSelectElements("//build:NuGetPackageImportStamp", ns).Remove();

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

            proj.DescendantNodes().OfType<XComment>().Where(c => c.Value.Contains("<Target Name=\"BeforeBuild\">")).Remove();

            #region Remove Folders
            proj.XPathSelectElements(@"//build:Folder", ns).Remove();
            #endregion

            #region Clean up main PropertyGroup
            proj.SetAttributeValue("ToolsVersion", "14.0");

            var mainPG = proj.XPathSelectElement("//build:PropertyGroup[build:ProjectGuid]", ns);
            proj.XPathSelectElements("//build:ProjectType", ns).Remove();
            proj.XPathSelectElements("//build:ProductVersion", ns).Remove();
            proj.XPathSelectElements("//build:SchemaVersion", ns).Remove();
            proj.XPathSelectElements("//build:StartupObject", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();
            proj.XPathSelectElements("//build:ApplicationIcon", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();

            proj.XPathSelectElements("//build:PropertyGroup[build:ProjectGuid]/build:FileAlignment", ns).Remove();
            proj.XPathSelectElements("//build:PropertyGroup[build:ProjectGuid]/build:WarningLevel", ns).Remove();

            proj.XPathSelectElements("//build:ApplicationIcon", ns).Reparent(mainPG);
            proj.XPathSelectElements("//build:ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch", ns).Reparent(mainPG);

            if (frameworkVersion != null)
            {
                proj.XPathSelectElements("//build:TargetFrameworkVersion", ns).SetValue(frameworkVersion);
                if (!string.IsNullOrEmpty(frameworkProfile))
                    proj.XPathSelectElements("//build:TargetFrameworkProfile", ns).SetValue(frameworkProfile);
                else
                    proj.XPathSelectElements("//build:TargetFrameworkProfile", ns).Nodes().Remove();
            }

            proj.XPathSelectElements("//build:ProjectTypeGuids", ns).SetValue(e => e.ToLower());

            var mainOrder = new[] { "Configuration", "Platform", "ProjectGuid", "ProjectTypeGuids", "OutputType", "AppDesignerFolder", "RootNamespace", "AssemblyName", "TargetFrameworkVersion", "TargetFrameworkProfile", "AutoGenerateBindingRedirects", "ApplicationIcon", };
            mainPG.Elements().OrderBy(x => IndexOf(mainOrder, x.Name.LocalName) ?? Int32.MaxValue).Reparent(mainPG);
            #endregion

            #region Sort Configuration items
            var pcOrder = platforms.SelectMany(p => configurations.Select(c => String.Format("'{0}|{1}'", c, p))).ToArray();

            var cpgOrder = new[] { "PlatformTarget", "DebugSymbols", "DebugType", "Optimize", "OutputPath", "DefineConstants", "ErrorReport", "WarningLevel", "AllowUnsafeBlocks", "BaseAddress", "CheckForOverflowUnderflow", "ConfigurationOverrideFile", "DocumentationFile", "FileAlignment", "NoStdLib", "NoWarn", "RegisterForComInterop", "RemoveIntegerChecks", "TreatWarningsAsErrors", "UseVSHostingProcess", "CodeAnalysisIgnoreBuiltInRuleSets", "CodeAnalysisIgnoreBuiltInRules", "RunCodeAnalysis", };

            proj.XPathSelectElements("//*[contains(@Condition, '$(Configuration)|$(Platform)')]", ns).Where(e => !pcOrder.Any(o => e.Attribute("Condition").Value.Contains(o))).Remove();
            foreach (var grouping in proj.XPathSelectElements("//*[contains(@Condition, '$(Configuration)|$(Platform)')]", ns).GroupBy(e => new { p = e.Parent, en = e.Name.LocalName }))
            {
                var previous = grouping.First().PreviousNode;

                var children = grouping.OrderBy(e => IndexOf(pcOrder, o => e.Attribute("Condition").Value.Contains(o)) ?? Int32.MaxValue).ThenBy(e => e.Attribute("Condition").Value.Trim()).ToArray();

                children.Remove();
                previous.AddAfterSelf(children);

                foreach (var cgp in children.Where(e => e.Name.LocalName == "PropertyGroup"))
                {
                    cgp.Elements().OrderBy(e => IndexOf(cpgOrder, e.Name.LocalName) ?? Int32.MaxValue).Reparent(cgp);
                }

                continue;
            }

            proj.XPathSelectElements("//build:DefineConstants", ns).SetValue(e => String.Join(";", e.Value.Split(';').OrderBy(v => v.TrimStart('_')).ThenBy(v => v)));
            #endregion

            #region Clean up Items
            proj.XPathSelectElements("//build:SubType", ns).Where(s => String.IsNullOrWhiteSpace(s.Value)).Remove();
            proj.XPathSelectElements("//*[@Include='app.config']/build:SubType", ns).Remove();
            proj.XPathSelectElements("//build:Compile/build:SubType[text()='Code']", ns).Remove();
            var noDesigner = new[] { ".config", ".xml", ".xsd", ".xslt", ".ejs" };
            proj.XPathSelectElements("//build:*[@Include]/build:SubType[text()='Designer']", ns).Where(s => noDesigner.Contains(Path.GetExtension(s.Parent.Attribute("Include").Value), StringComparer.InvariantCultureIgnoreCase)).Remove();

            foreach (var item in proj.XPathSelectElements("//build:*[@Include]", ns).Where(e => e.Attribute("Include").Value.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase) && e.Name.LocalName != "Reference"))
            {
                item.XPathSelectElements(".//build:Generator", ns).Remove();
                item.XPathSelectElements(".//build:SubType", ns).Remove();

                item.AddElement("Generator", content: "MSBuild:Compile", first: true);
                item.AddElement("SubType", content: "Designer", first: true);
            }

            var itemTypes = new[] { "Compile", "Content", "Resource", "EmbeddedResource", "None", "ApplicationDefinition", "Page", "SplashScreen" };

            var items = proj.XPathSelectElements("//build:*[@Include]", ns).Where(e => itemTypes.Contains(e.Name.LocalName)).OrderBy(e => e.Attribute("Include").Value).ToArray();

            foreach (var item in items.Where(e => !e.IsEmpty && !e.HasElements))
            {
                item.RemoveNodes();
            }
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
                proj.XPathSelectElements("//build:Reference", ns).OrderBy(c => c.Attribute("Include").Value.SubstringTill(',').Replace("System", "aaaaaaa")).Reparent(referenceParent);
            }
            #endregion

            #region Clean up ProjectReferences
            proj.XPathSelectElements("//build:ProjectReference/build:Package", ns).Remove();

            proj.XPathSelectElements("//build:ProjectReference[@Include]/build:Name", ns).SetValue(n => Path.GetFileNameWithoutExtension(n.Parent.Attribute("Include").Value));
            foreach (var name in proj.XPathSelectElements("//build:ProjectReference[@Include]/build:Name", ns))
            {
                var parent = name.Parent;
                name.Remove();
                parent.AddFirst(name);
            }

            var projectReferenceParent = proj.XPathSelectElements("//build:ItemGroup[build:ProjectReference]", ns).FirstOrDefault();
            if (projectReferenceParent != null)
            {
                proj.XPathSelectElements("//build:ProjectReference", ns).OrderBy(c => c.Element(XName.Get("Name", ns.LookupNamespace("build"))).Value).Reparent(projectReferenceParent);
            }

            var imports = proj.XPathSelectElements("//build:Import[starts-with(@Project, '..')]", ns).ToArray();
            foreach (var import in imports)
            {
                var path = import.Attribute("Project").Value;
                import.SetAttributeValue("Condition", String.Format("Exists('{0}')", path));

                continue;
            }
            #endregion

            #region Clean up Web Projects
            proj.XPathSelectElements("//build:SaveServerSettingsInUserFile", ns).SetValue(true);

            proj.XPathSelectElements("//build:WebProjectProperties/build:UseIIS", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:AutoAssignPort", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:DevelopmentServerPort", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:DevelopmentServerVPath", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:IISUrl", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:NTLMAuthentication", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:UseCustomServer", ns).Remove();
            proj.XPathSelectElements("//build:WebProjectProperties/build:CustomServerUrl", ns).Remove();
            #endregion

            #region Clean up PreBuildEvent/PostBuildEvent
            proj.XPathSelectElements("//build:PostBuildEvent", ns).Where(e => e.Value.IndexOf("sn.exe", StringComparison.CurrentCultureIgnoreCase) >= 0).SetValue("");

            proj.XPathSelectElements("//build:PreBuildEvent", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();
            proj.XPathSelectElements("//build:PostBuildEvent", ns).Where(e => String.IsNullOrWhiteSpace(e.Value)).Remove();

            if (!proj.XPathSelectElements("//build:PreBuildEvent", ns).Any(e => String.IsNullOrWhiteSpace(e.Value)))
                proj.XPathSelectElements("//build:RunPreBuildEvent", ns).Remove();

            if (!proj.XPathSelectElements("//build:PostBuildEvent", ns).Any(e => String.IsNullOrWhiteSpace(e.Value)))
                proj.XPathSelectElements("//build:RunPostBuildEvent", ns).Remove();
            #endregion

            #region Configure signing
            var canSign = !String.IsNullOrEmpty(relativeSigningKeyPath);

            proj.XPathSelectElements("//build:SignAssembly", ns).Remove();
            proj.XPathSelectElements("//build:DelaySign", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyOriginatorKeyFile", ns).Remove();
            proj.XPathSelectElements("//build:AssemblyKeyContainerName", ns).Remove();

            if (mainPG != null)
            {
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

        static int? IndexOf(string[] order, Func<string, bool> p)
        {
            for (int i = 0; i < order.Length; ++i)
            {
                if (p(order[i]))
                    return i;
            }

            return null;
        }

        static int? IndexOf(string[] order, string localName)
        {
            for (int i = 0; i < order.Length; ++i)
            {
                if (order[i] == localName)
                    return i;
            }

            return null;
        }
    }
}
