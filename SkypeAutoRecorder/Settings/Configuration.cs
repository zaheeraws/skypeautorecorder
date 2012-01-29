using System;
using System.Linq;

namespace SkypeAutoRecorder.Settings
{
    [Serializable]
    internal sealed class Configuration
    {
        #region Singleton

        private static readonly Configuration SingletonInstance = new Configuration();

        private Configuration()
        {
        }

        public static Configuration Instance
        {
            get
            {
                return SingletonInstance;
            }
        }

        #endregion

        private static Settings _settings;

        private const string DateTimePlaceholder = "{date-time}";
        private const string ContactPlaceholder = "{contact}";

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
            var filter = _settings.Filters.FirstOrDefault(f => ContactsContain(f.Contacts, contact));
            return filter == null ? null : RenderFileName(filter.RawFileName, contact, dateTime);
        }
    }
}
