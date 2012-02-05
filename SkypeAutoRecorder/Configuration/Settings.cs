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
        private const string DateTimePlaceholder = "{date-time}";
        private const string ContactPlaceholder = "{contact}";
        private const string DateTimeFormat = "yyyy-MM-dd HH.mm.ss";

        /// <summary>
        /// File name where application settings are stored.
        /// </summary>
        private static readonly string SettingsFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkypeAutoRecorder\\Settings.xml");

        /// <summary>
        /// Initializes the <see cref="Settings"/> class. Loads saved settings or creates new.
        /// </summary>
        static Settings()
        {
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
        /// Creates the name of the file by replacing placeholders with actual data.
        /// </summary>
        /// <param name="rawFileName">Name of the file with placeholders.</param>
        /// <param name="contact">The contact name.</param>
        /// <param name="dateTime">The date time.</param>
        /// <returns>The actual file name for settings.</returns>
        private static string renderFileName(string rawFileName, string contact, DateTime dateTime)
        {
            // Replace placeholders.
            var fileName = rawFileName.Replace(DateTimePlaceholder, dateTime.ToString(DateTimeFormat));
            fileName = fileName.Replace(ContactPlaceholder, contact);

            // Add extension if its missing.
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(extension) || extension != ".mp3")
            {
                fileName = fileName + ".mp3";
            }

            return fileName;
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
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Skype Records\\{date-time} {contact}");
            ExcludedContacts = "echo123";
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
                if (RecordUnfiltered && !string.IsNullOrEmpty(DefaultRawFileName))
                {
                    return renderFileName(DefaultRawFileName, contact, dateTime);
                }

                return null;
            }
            
            return renderFileName(filter.RawFileName, contact, dateTime);
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
