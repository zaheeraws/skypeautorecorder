using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Configuration
{
    [Serializable]
    public sealed class Settings
    {
        private const string DateTimePlaceholder = "{date-time}";
        private const string ContactPlaceholder = "{contact}";

        private static readonly string SettingsFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkypeAutoRecorder\\Settings.xml");

        private static readonly Settings SettingsInstance;

        static Settings()
        {
            if (File.Exists(SettingsFileName))
            {
                // Load settings from XML.
                var serializer = new XmlSerializer(typeof(Settings));
                using (var reader = new StreamReader(SettingsFileName))
                {
                    SettingsInstance = (Settings)serializer.Deserialize(reader);
                }
            }
            else
            {
                SettingsInstance = new Settings();
            }
            
            if (SettingsInstance.Filters == null)
            {
                SettingsInstance.Filters = new ObservableCollection<Filter>();
            }
        }

        private Settings()
        {
        }

        public static Settings Instance
        {
            get
            {
                return SettingsInstance;
            }
        }

        #region Serializable fields

        [XmlArray("Filters")]
        public ObservableCollection<Filter> Filters { get; set; }

        [XmlElement("DefaultFileName")]
        public string DefaultRawFileName { get; set; }

        [XmlElement("RecordUnfiltered")]
        public bool RecordUnfiltered { get; set; }

        [XmlElement("ExcludedContacts")]
        public string ExcludedContacts { get; set; }

        #endregion

        public static void Save()
        {
            // Create directory for application settings if it doesn't exists.
            var path = Path.GetFullPath(SettingsFileName);
            if (Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Save settings to XML.
            var serializer = new XmlSerializer(typeof(Settings));
            using (var writer = new StreamWriter("D:\\Settings.xml"))
            {
                serializer.Serialize(writer, SettingsInstance);
            }
        }

        public static string RenderFileName(string rawFileName, string contact, DateTime dateTime)
        {
            var fileName = rawFileName.Replace(DateTimePlaceholder, dateTime.ToString("yyyy-MM-dd HH.mm"));
            return fileName.Replace(ContactPlaceholder, contact);
        }

        public static bool ContactsContain(string contacts, string contact)
        {
            var contactsList = contacts.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(c => c.Trim());

            return contactsList.Contains(contact);
        }

        public string GetFileName(string contact, DateTime dateTime)
        {
            var filter = Filters.FirstOrDefault(f => ContactsContain(f.Contacts, contact));
            return filter == null ? null : RenderFileName(filter.RawFileName, contact, dateTime);
        }
    }
}
