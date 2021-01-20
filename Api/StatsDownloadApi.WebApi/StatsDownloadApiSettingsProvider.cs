﻿namespace StatsDownloadApi.WebApi
{
    using System.IO;
    using System.Reflection;

    using Microsoft.Extensions.Configuration;

    using StatsDownload.Core.Interfaces;

    public class StatsDownloadApiSettingsProvider : IDatabaseConnectionSettingsService, IDownloadSettingsService,
                                                    IDataStoreSettings, IStatsFileDateTimeFormatsAndOffsetSettings,
                                                    INoPaymentAddressUsersFilterSettings, IAzureDataStoreSettingsService
    {
        private readonly IConfiguration configuration;

        public StatsDownloadApiSettingsProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string ConnectionString => configuration.GetConnectionString("FoldingCoin.Storage");

        public string ContainerName => GetAppSetting("AzureDataStore.ContainerName");

        public string DataStoreType => GetAppSetting("DataStoreType");

        bool INoPaymentAddressUsersFilterSettings.Enabled => GetBoolAppSetting("EnableNoPaymentAddressUsersFilter");

        public string UploadDirectory => GetAppSetting("UploadDirectory");

        public string GetAcceptAnySslCert()
        {
            return GetAppSetting("AcceptAnySslCert");
        }

        public int? GetCommandTimeout()
        {
            string commandTimeoutString = GetAppSetting("DbCommandTimeout");
            if (int.TryParse(commandTimeoutString, out int commandTimeoutValue))
            {
                return commandTimeoutValue;
            }

            return null;
        }

        public string GetConnectionString()
        {
            return configuration.GetConnectionString("FoldingCoin.Database");
        }

        public string GetDatabaseType()
        {
            return GetAppSetting("DatabaseType");
        }

        public string GetDownloadDirectory()
        {
            return GetAppSetting("DownloadDirectory")
                   ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public string GetDownloadTimeout()
        {
            return GetAppSetting("DownloadTimeoutSeconds");
        }

        public string GetDownloadUri()
        {
            return GetAppSetting("DownloadUri");
        }

        public string GetMinimumWaitTimeInHours()
        {
            return GetAppSetting("MinimumWaitTimeInHours");
        }

        public string GetStatsFileTimeZoneAndOffsetSettings()
        {
            return GetAppSetting("StatsFileTimeZoneAndOffset");
        }

        private string GetAppSetting(string name)
        {
            return configuration.GetSection("AppSettings").GetValue<string>(name);
        }

        private bool GetBoolAppSetting(string appSettingName)
        {
            bool.TryParse(GetAppSetting(appSettingName), out bool value);
            return value;
        }
    }
}