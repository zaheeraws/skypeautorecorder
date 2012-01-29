using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Settings
{
    [Serializable]
    internal class Settings
    {
        [XmlArray("Filters")]
        public List<Filter> Filters { get; set; }

        [XmlElement("DefaultFileName")]
        public string DefaultRawFileName { get; set; }

        [XmlElement("RecordUnfiltered")]
        public bool RecordUnfiltered { get; set; }

        [XmlElement("ExcludedContacts")]
        public string ExcludedContacts { get; set; }
    }
}
