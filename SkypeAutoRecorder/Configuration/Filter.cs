using System;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Configuration
{
    /// <summary>
    /// Settings filter that provides file name for saving conversations with specified list of contacts.
    /// </summary>
    [Serializable]
    public class Filter
    {
        /// <summary>
        /// Gets or sets the contacts.
        /// </summary>
        /// <value>
        /// The contacts separated with comma or semicolon.
        /// </value>
        [XmlAttribute("contacts")]
        public string Contacts { get; set; }

        /// <summary>
        /// Gets or sets the file names with placeholders for saving records.
        /// </summary>
        /// <value>
        /// The file names with placeholders.
        /// </value>
        [XmlAttribute("fileName")]
        public string RawFileNames { get; set; }
    }
}
