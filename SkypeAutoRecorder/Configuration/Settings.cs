using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Configuration
{
    /// <summary>
    /// Provides access to application settings.
    /// </summary>
    [Serializable]
    public partial class Settings : ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
            // Set default values for settings.
            Filters = new ObservableCollection<Filter>();
            RecordUnfiltered = true;
            DefaultRawFileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Skype Records\\" + DefaultFileName);
            ExcludedContacts = "echo123";
            VolumeScale = 1;
            SoundSampleFrequency = "32";
            SoundBitrate = "256";
        }

        /// <summary>
        /// Gets or sets the filters that specify target folders for records of certain contacts.
        /// </summary>
        /// <value>
        /// The filters.
        /// </value>
        [XmlArray("Filters")]
        public ObservableCollection<Filter> Filters { get; set; }

        /// <summary>
        /// Gets or sets the name with placeholders of the target file for all records of contacts
        /// which are not in the filters.
        /// </summary>
        /// <value>
        /// The name of the file with placeholders.
        /// </value>
        [XmlElement("DefaultFileName")]
        public string DefaultRawFileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether application should record conversation with contacts
        /// which are not in the filters.
        /// </summary>
        /// <value>
        /// <c>true</c> if application should record everything; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("RecordUnfiltered")]
        public bool RecordUnfiltered { get; set; }

        /// <summary>
        /// Gets or sets the contacts which are excluded from unfiltered records. These contacts will be recorded
        /// regardless of this field if they are in the filters.
        /// </summary>
        /// <value>
        /// The excluded contacts.
        /// </value>
        [XmlElement("ExcludedContacts")]
        public string ExcludedContacts { get; set; }

        /// <summary>
        /// Gets or sets the volume scale for the recorded file.
        /// </summary>
        /// <value>
        /// The volume scale.
        /// </value>
        [XmlElement("VolumeScale")]
        public int VolumeScale { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether recorder should separate sound channels on MP3 creating or not.
        /// </summary>
        /// <value>
        /// The value indicating whether recorder should separate sound channels on MP3 creating or not.
        /// </value>
        [XmlElement("SeparateSoundChannels")]
        public bool SeparateSoundChannels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether recorder should create MP3 in high quality sound.
        /// </summary>
        /// <value>
        /// <c>true</c> if recorder should create MP3 in high quality sound; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("HighQualityEncoding")]
        public bool HighQualitySound { get; set; }

        /// <summary>
        /// Gets or sets the sound sample frequency.
        /// </summary>
        /// <value>
        /// The sound sample frequency.
        /// </value>
        [XmlElement("SoundSampleFrequency")]
        public string SoundSampleFrequency { get; set; }

        /// <summary>
        /// Gets or sets the sound bitrate.
        /// </summary>
        /// <value>
        /// The sound bitrate.
        /// </value>
        [XmlElement("SoundBitrate")]
        public string SoundBitrate { get; set; }

        /// <summary>
        /// Gets the sound sample frequencies.
        /// </summary>
        [XmlIgnore]
        public List<string> SoundSampleFrequencies
        {
            get
            {
                return AvailableSoundSampleFrequencies;
            }
        }

        /// <summary>
        /// Gets the sound bitrates.
        /// </summary>
        [XmlIgnore]
        public List<string> SoundBitrates
        {
            get
            {
                return AvailableSoundBitrates;
            }
        }

        /// <summary>
        /// Gets the raw name of the file for saving recorded conversation depends on current settings.
        /// </summary>
        /// <param name="contact">The contact name.</param>
        /// <returns>The raw file name for saving record or <c>null</c> if application shouldn't record conversation
        /// according to current settings.</returns>
        public string GetRawFileName(string contact)
        {
            // Find contact filter.
            var filter = Filters.FirstOrDefault(f => contactsContain(f.Contacts, contact));
            
            // If filter is missing then check other settings.
            if (filter == null)
            {
                // Check if conversation with this contact can be auto recorded.
                if (contactsContain(ExcludedContacts, contact))
                {
                    return null;
                }

                // Try to use default file name.
                return RecordUnfiltered ? DefaultRawFileName : null;
            }
            
            return filter.RawFileName;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, Current);
                stream.Position = 0;
                return formatter.Deserialize(stream);
            }
        }
    }
}
