using System;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Configuration
{
    [Serializable]
    public class Filter
    {
        [XmlAttribute("contacts")]
        public string Contacts { get; set; }

        [XmlAttribute("fileName")]
        public string RawFileNames { get; set; }
    }
}
