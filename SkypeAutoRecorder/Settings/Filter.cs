using System;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Settings
{
    [Serializable]
    internal class Filter
    {
        [XmlAttribute("contacts")]
        public string Contacts { get; set; }

        [XmlAttribute("fileName")]
        public string RawFileName { get; set; }
    }
}
