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

            var ns = XmlHelpers.Resolver;

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

            var relativeSigningKeyPath = File.Exists(signingKeyPath) ? PathTo(signingKeyPath, csprojFile) : null;

            CSProjCleaner.Clean(proj, ns, relativeSigningKeyPath);

            proj.Save(csprojFile);
        }

        public static string SubstringTill(this string value, char ch)
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
