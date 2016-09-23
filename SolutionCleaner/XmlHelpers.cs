// Copyright (c) Gregory Sandstrom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SolutionCleaner
{
    public static class XmlHelpers
    {
        private const string build = @"http://schemas.microsoft.com/developer/msbuild/2003";

        private static XmlNamespaceManager ns;
        public static IXmlNamespaceResolver Resolver { get { return ns ?? (ns = BuildNamespaceManager()); } }

        static XmlNamespaceManager BuildNamespaceManager()
        {
            var manager = new XmlNamespaceManager(new System.Xml.NameTable());

            manager.AddNamespace("build", build);

            return manager;
        }

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

        #region SetValue<XElement>
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
        #endregion

        #region SetValue<XAttribute>
        public static void SetValue(this IEnumerable<XAttribute> enumerable, string value)
        {
            foreach (var item in enumerable)
                item.Value = value;
        }
        #endregion

        public static void Reparent(this IEnumerable<XElement> nodes, XElement parent, bool first = false)
        {
            var list = nodes.ToArray();
            list.Remove();
            if (first)
                parent.AddFirst(list);
            else
                parent.Add(list);
        }
    }
}
