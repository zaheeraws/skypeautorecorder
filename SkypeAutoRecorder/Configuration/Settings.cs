using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class Settings : INotifyPropertyChanged, ICloneable
    {
        /// <summary>
        /// Default pattern for name of the recorded file.
        /// </summary>
        public const string DefaultFileName = "{date-time} {contact}.mp3";

        /// <summary>
        /// Folder where application settings are stored.
        /// </summary>
        public static readonly string SettingsFolder;

        private const string DateTimePlaceholder = "{date-time}";
        private const string ContactPlaceholder = "{contact}";
        private const string DateTimeFormat = "yyyy-MM-dd HH.mm.ss";

        /// <summary>
        /// File name where application settings are stored.
        /// </summary>
        private static readonly string SettingsFileName;

        /// <summary>
        /// The application name.
        /// </summary>
        public const string ApplicationName = "SkypeAutoRecorder";

        /// <summary>
        /// Initializes the <see cref="Settings"/> class. Loads saved settings or creates new.
        /// </summary>
        static Settings()
        {
            SettingsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkypeAutoRecorder");
            SettingsFileName = Path.Combine(SettingsFolder, "Settings.xml");

            if (File.Exists(SettingsFileName))
            {
                // Load settings from XML.
                var serializer = new XmlSerializer(typeof(Settings));
                using (var reader = new StreamReader(SettingsFileName))
                {
                    Current = (Settings)serializer.Deserialize(reader);
                }
            }
            else
            {
                Current = new Settings();
            }
        }

        /// <summary>
        /// Gets the name of the temp wav file.
        /// </summary>
        /// <returns></returns>
        public static string GetTempFileName(string i = null)
        {
            return Path.Combine(Path.GetTempPath(),
                "sar_" + DateTime.Now.ToString("HH_mm_ss") + (i == null ? string.Empty : "_" + i) + ".wav");
        }

        /// <summary>
        /// Creates the name of the file by replacing placeholders with actual data.
        /// </summary>
        /// <param name="rawFileName">Name of the file with placeholders.</param>
        /// <param name="contact">The contact name.</param>
        /// <param name="dateTime">The date time.</param>
        /// <returns>The actual file name for settings.</returns>
        public static string RenderFileName(string rawFileName, string contact, DateTime dateTime)
        {
            if (rawFileName == null)
            {
                return null;
            }

            var fileName = rawFileName;

            // Check if file name is missing. Add default one.
            if (string.IsNullOrEmpty(Path.GetFileName(fileName)))
            {
                fileName += DefaultFileName;
            }

            // Add extension if its missing.
            fileName = Path.ChangeExtension(fileName, "mp3");

            // Replace placeholders.
            fileName = fileName.Replace(DateTimePlaceholder, dateTime.ToString(DateTimeFormat));
            return fileName.Replace(ContactPlaceholder, contact);
        }

        /// <summary>
        /// Checks if string with contacts contains specified contact.
        /// </summary>
        /// <param name="contacts">The contacts separated with comma or semicolon.</param>
        /// <param name="contact">The contact to find.</param>
        /// <returns><c>true</c> if contact is present in string; otherwise, <c>false</c>.</returns>
        private static bool contactsContain(string contacts, string contact)
        {
            var contactsList = contacts.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(c => c.Trim().ToLower());

            return contactsList.Contains(contact.ToLower());
        }

        private string _defaultRawFileName;
        private bool _recordUnfiltered;
        private string _excludedContacts;

        /// <summary>
        /// Gets or sets the current settings.
        /// </summary>
        /// <value>
        /// The current settings.
        /// </value>
        public static Settings Current { get; set; }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

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
        public string DefaultRawFileName
        {
            get
            {
                return _defaultRawFileName;
            }
            set
            {
                if (_defaultRawFileName != value)
                {
                    _defaultRawFileName = value;
                    invokePropertyChanged(new PropertyChangedEventArgs("DefaultRawFileName"));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether application should record conversation with contacts
        /// which are not in the filters.
        /// </summary>
        /// <value>
        /// <c>true</c> if application should record everything; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("RecordUnfiltered")]
        public bool RecordUnfiltered
        {
            get
            {
                return _recordUnfiltered;
            }
            set
            {
                if (_recordUnfiltered != value)
                {
                    _recordUnfiltered = value;
                    invokePropertyChanged(new PropertyChangedEventArgs("RecordUnfiltered"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the contacts which are excluded from unfiltered records. These contacts will be recorded
        /// regardless of this field if they are in the filters.
        /// </summary>
        /// <value>
        /// The excluded contacts.
        /// </value>
        [XmlElement("ExcludedContacts")]
        public string ExcludedContacts
        {
            get
            {
                return _excludedContacts;
            }
            set
            {
                if (_excludedContacts != value)
                {
                    _excludedContacts = value;
                    invokePropertyChanged(new PropertyChangedEventArgs("ExcludedContacts"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume scale for the recorded file.
        /// </summary>
        /// <value>
        /// The volume scale.
        /// </value>
        [XmlElement("VolumeScale")]
        public int VolumeScale { get; set; }

        /// <summary>
        /// Saves current settings to file.
        /// </summary>
        public static void Save()
        {
            // Create directory for application settings if it doesn't exists.
            var path = Path.GetDirectoryName(SettingsFileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Save settings to XML.
            var serializer = new XmlSerializer(typeof(Settings));
            using (var writer = new StreamWriter(SettingsFileName))
            {
                serializer.Serialize(writer, Current);
            }
        }

        /// <summary>
        /// Gets the name of the file for saving recorded conversation depends on current settings and specified data.
        /// </summary>
        /// <param name="contact">The contact name.</param>
        /// <param name="dateTime">The date time for placeholder.</param>
        /// <returns>The file name for saving record or <c>null</c> if application shouldn't record conversation
        /// according to current settings.</returns>
        public string GetFileName(string contact, DateTime dateTime)
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
                return RecordUnfiltered ? RenderFileName(DefaultRawFileName, contact, dateTime) : null;
            }
            
            return RenderFileName(filter.RawFileName, contact, dateTime);
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

        private void invokePropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }
    }
}
