﻿using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace SteamHelper
{
    internal class SteamApplicationInfo
    {
        private SteamApplicationInfo(int appId)
        {
            AppId = appId;
        }

        public static SteamApplicationInfo FromAppId(int appId)
        {
            var output = new SteamApplicationInfo(appId);

            var appIdStr = appId.ToString("G");

            var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {appIdStr}") ??
                      Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {appIdStr}");
            if (key == null)
                throw new ArgumentException("AppID is not registered properly");

            using (key)
            {
                output.UninstallString = key.GetValue(@"UninstallString") as string;
                output.InstallDirectory = key.GetValue(@"InstallLocation") as string;
                if (output.InstallDirectory == null || !Directory.Exists(output.InstallDirectory))
                    throw new ArgumentException($"Install location is invalid or doesn't exist: {output.InstallDirectory}");
            }

            var dir = new DirectoryInfo(output.InstallDirectory);
            while (!dir.Name.Equals(@"steamapps", StringComparison.InvariantCultureIgnoreCase))
            {
                dir = dir.Parent;
                if (dir == null)
                    throw new ArgumentException("Could not find the SteamApps directory");
            }

            output.ManifestPath = dir.GetFiles($@"appmanifest_{appIdStr}.acf").Single().FullName;
            output.DownloadDirectory = dir.GetDirectories(@"downloading").SingleOrDefault()?.GetDirectories(appIdStr).SingleOrDefault()?.FullName;

            var workshopDir = dir.GetDirectories(@"workshop").SingleOrDefault();
            if (workshopDir != null)
            {
                output.WorkshopManifestPath = workshopDir.GetFiles($@"appworkshop_{appIdStr}.acf").SingleOrDefault()?.FullName;
                output.WorkshopDirectory = workshopDir.GetDirectories(@"content").SingleOrDefault()?.GetDirectories(appIdStr).SingleOrDefault()?.FullName;
            }

            return output;
        }

        public int AppId { get; }
        public string UninstallString { get; private set; }
        public string ManifestPath { get; private set; }
        public string InstallDirectory { get; private set; }
        public string DownloadDirectory { get; private set; }
        public string WorkshopManifestPath { get; private set; }
        public string WorkshopDirectory { get; private set; }
    }
}