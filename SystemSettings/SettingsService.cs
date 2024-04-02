using Core.Data;
using Core.Exceptions;
using FileStorage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Project.SystemSettings
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbContext _dataContext;
        private readonly IFileStorageService _fileStorageService;
        private readonly string key = "E546C8DF278CD5931069B522E695D4F2";


        public SettingsService(IDbContext dataContext) =>
            _dataContext = dataContext;

        public SettingsService(IDbContext dataContext, IFileStorageService fileStorageService) : this(dataContext) =>
            _fileStorageService = fileStorageService;


        public async Task<SettingsDTO[]> Load(SettingsName[] settingsNames = null)
        {
            var settings = (await GetCurrentConfig(settingsNames)).ToArray();
            foreach (var settingsItem in settings)
                DecryptSettingsSection(settingsItem);

            return settings;
        }

        public async Task<SettingsDTO[]> Save(SettingsDTO[] config)
        {
            var dbSet = _dataContext.Set<AppSettings>();
            var sectionNames = new List<AppSettings>();
            foreach (var configItem in config)
            {
                var sectionName = string.IsNullOrEmpty(configItem.SectionName) ? GetSectionName(configItem.Value.GetType()) : configItem.SectionName;

                var section = string.IsNullOrEmpty(sectionName) ?
                    null : dbSet.FirstOrDefault(s => s.Section.Equals(sectionName));
                if (section == null)
                {
                    section = new AppSettings
                    {
                        Section = sectionName,
                        Value = JsonConvert.SerializeObject(configItem.Value, ServiceCollectionExtensions.JsonSerializerSettings)
                    };
                    dbSet.Add(section);
                }
                else
                {
                    EncryptSettingsValue(configItem.Value, section.EncryptedFields);
                    section.Value = JsonConvert.SerializeObject(configItem.Value, ServiceCollectionExtensions.JsonSerializerSettings);
                }

                sectionNames.Add(section);
            }

            await _dataContext.SaveChangesAsync();

            return (await ConvertAppSettingsToSettingDTOs(sectionNames.ToArray())).ToArray();
        }

        public T GetSettingsSection<T>() where T : class
        {
            var defaultSettings = Activator.CreateInstance(typeof(T)) as T;
            var sectionName = GetSectionName<T>();

            var data = string.IsNullOrEmpty(sectionName)
                    ? null
                    : _dataContext.Set<AppSettings>().FirstOrDefault(s =>
                        string.Equals(s.Section, sectionName));
            if (data != null)
            {
                var obj = JsonConvert.DeserializeObject<T>(data.Value, ServiceCollectionExtensions.JsonSerializerSettings);
                DecryptSettingsValue(obj, data.EncryptedFields);
                return obj;
            }

            return defaultSettings;
        }
    

        public void SaveSettingsSection<T>(T config) where T : class
        {
            var sectionName = GetSectionName<T>();
            if (!string.IsNullOrEmpty(sectionName))
            {
                var dbSetting = _dataContext.Set<AppSettings>().FirstOrDefault(s => s.Section == sectionName) ?? new AppSettings { Section = sectionName };
                EncryptSettingsValue(config, dbSetting.EncryptedFields);
                dbSetting.Value = JsonConvert.SerializeObject(config, ServiceCollectionExtensions.JsonSerializerSettings);
                if (dbSetting.Id == 0)
                {
                    _dataContext.Set<AppSettings>().Add(dbSetting);
                }

                _dataContext.SaveChanges();
            }
        }


        private static Type GetSectionType(string sectionName) =>
            ServiceCollectionExtensions.Sections.ContainsValue(sectionName)
                ? ServiceCollectionExtensions.Sections.FirstOrDefault(s => s.Value == sectionName).Key
                : null;

        private static string GetSectionName<T>() => GetSectionName(typeof(T));

        private static string GetSectionName(Type type) =>
            ServiceCollectionExtensions.Sections.ContainsKey(type)
                ? ServiceCollectionExtensions.Sections[type]
                : null;

        private static string EncryptValue(string text, string keyString)
        {
            using (var aesAlg = Aes.Create())
            using (var encryptor = aesAlg.CreateEncryptor(Encoding.UTF8.GetBytes(keyString), aesAlg.IV))
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(text);

                var iv = aesAlg.IV;

                var decryptedContent = msEncrypt.ToArray();

                var result = new byte[iv.Length + decryptedContent.Length];

                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                return Convert.ToBase64String(result);
            }
        }

        private static string DecryptValue(string cipherText, string keyString)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            var iv = new byte[16];
            //var cipher = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            //Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using (var aesAlg = Aes.Create())
            using (var decryptor = aesAlg.CreateDecryptor(Encoding.UTF8.GetBytes(keyString), iv))
            using (var msDecrypt = new MemoryStream(cipher))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }


        private async Task<IEnumerable<SettingsDTO>> GetCurrentConfig(SettingsName[] settingsNames = null)
        {
            var settings = _dataContext.Set<AppSettings>().AsQueryable();
            if (settingsNames != null)
                settings = settings.Where(settingsItem => settingsNames.Select(settingsNameItem => settingsNameItem.ToString()).Contains(settingsItem.Section));

            return await ConvertAppSettingsToSettingDTOs(settings.ToArray());
        }

        private async Task<IEnumerable<SettingsDTO>> ConvertAppSettingsToSettingDTOs(AppSettings[] settings)
        {
            var result = new List<SettingsDTO>();

            foreach (var settingsItems in settings)
            {
                var sectionType = GetSectionType(settingsItems.Section);

                if (sectionType == null) continue;

                var value = JsonConvert.DeserializeObject(settingsItems.Value, sectionType, ServiceCollectionExtensions.JsonSerializerSettings);

                // Refreshing logo files
                if (value is ProjectSettings projectSettings)
                {
                    projectSettings.LogoImage = projectSettings.LogoImageId == null
                        ? new FileDetailsDTO { Url = ProjectSettings.DefaultLogoImageUrl, ThumbnailUrl = ProjectSettings.DefaultLogoImageUrl }
                        : await _fileStorageService.Get((int)projectSettings.LogoImageId);

                    projectSettings.LogoIcon = projectSettings.LogoIconId == null
                        ? new FileDetailsDTO { Url = ProjectSettings.DefaultLogoIconUrl }
                        : await _fileStorageService.Get((int)projectSettings.LogoIconId);

                    value = projectSettings;
                }

                result.Add(new SettingsDTO
                {
                    SectionName = settingsItems.Section,
                    Value = value
                });
            }

            return result;
        }

        private void DecryptSettingsSection(SettingsDTO settingsSection)
        {
            var dbSet = _dataContext.Set<AppSettings>();
            var sectionName = settingsSection.SectionName;
            var section = string.IsNullOrEmpty(sectionName) ?
                null : dbSet.FirstOrDefault(s => s.Section.Equals(sectionName));
            if (section != null)
            {
                DecryptSettingsValue(settingsSection.Value, section.EncryptedFields);
            }
        }

        private void DecryptSettingsValue(object value, string fields)
        {
            if (string.IsNullOrEmpty(fields) || value == null) return;

            var fieldsList = fields.Split(',').ToList();
            foreach (var field in fieldsList)
            {
                var changingField = value.GetType().GetRuntimeProperty(field);
                if (changingField != null)
                {
                    var newValue = (string)changingField.GetValue(value);

                    if (!string.IsNullOrEmpty(newValue))
                    {
                        newValue = DecryptValue(newValue, key);
                        changingField.SetValue(value, newValue);
                    }
                }
            }
        }

        private void EncryptSettingsValue(object value, string fields)
        {
            if (string.IsNullOrEmpty(fields) || value == null) return;

            var fieldsList = fields.Split(',').ToList();
            foreach (var field in fieldsList)
            {
                var changingField = value.GetType().GetRuntimeProperty(field);
                if (changingField != null)
                {
                    var newValue = (string)changingField.GetValue(value);

                    if (!string.IsNullOrEmpty(newValue))
                    {
                        newValue = EncryptValue(newValue, key);
                        changingField.SetValue(value, newValue);
                    }
                }
            }
        }
    }
}
