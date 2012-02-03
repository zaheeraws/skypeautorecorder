﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace SkypeAutoRecorder.Configuration
{
    [Serializable]
    public class Settings : ICloneable
    {
        private const string DateTimePlaceholder = "{date-time}";
        private const string ContactPlaceholder = "{contact}";
        private const string DateTimeFormat = "yyyy-MM-dd HH.mm.ss";

        /// <summary>
        /// File name where application settings are stored.
        /// </summary>
        private static readonly string SettingsFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkypeAutoRecorder\\Settings.xml");

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

            if (Current.Filters == null)
            {
                Current.Filters = new ObservableCollection<Filter>();
            }
        }

        public static Settings Current { get; set; }

        //[XmlIgnore]
        //public bool Autostart { get; set; }

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
            var path = Path.GetDirectoryName(SettingsFileName);
            if (path == null)
            {
                return;
            }

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

        public static string RenderFileName(string rawFileName, string contact, DateTime dateTime)
        {
            var fileName = rawFileName.Replace(DateTimePlaceholder, dateTime.ToString(DateTimeFormat));
            fileName = fileName.Replace(ContactPlaceholder, contact);
            if (!string.IsNullOrEmpty(Path.GetExtension(fileName)))
            {
                fileName = fileName + ".mp3";
            }

            return fileName;
        }

        public static bool ContactsContain(string contacts, string contact)
        {
            var contactsList = contacts.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(c => c.Trim().ToLower());

            return contactsList.Contains(contact.ToLower());
        }

        public string GetFileName(string contact, DateTime dateTime)
        {
            var filter = Filters.FirstOrDefault(f => ContactsContain(f.Contacts, contact));

            if (filter == null)
            {
                // Check if conversation with this contact can be auto recorded.
                if (ContactsContain(ExcludedContacts, contact))
                {
                    return null;
                }

                // Try to use default file name.
                if (RecordUnfiltered && !string.IsNullOrEmpty(DefaultRawFileName))
                {
                    return RenderFileName(DefaultRawFileName, contact, dateTime);
                }

                return null;
            }

            var fileName = RenderFileName(filter.RawFileNames, contact, dateTime);

            CreateFilePath(fileName);

            return fileName;
        }

        private static void CreateFilePath(string fileName)
        {
            var path = Path.GetDirectoryName(fileName);

            if (path == null)
            {
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
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
