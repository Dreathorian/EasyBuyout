﻿using Pricer.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows;

namespace Pricer {
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window {
        private readonly WebClient webClient;

        public UpdateWindow(WebClient webClient) {
            this.webClient = webClient;

            InitializeComponent();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Main methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        ///  Get latest release and show updater window if version is newer
        /// </summary>
        public void Run() {
            // Can't be null when making calls to github
            webClient.Headers.Add("user-agent", "!null");

            // Fix for https DonwloadString bug (https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Make webrequest
            ReleaseObject latest = GetLatestRelease(webClient);
            if (latest == null) {
                MainWindow.Log("[Updater] Error getting update info...", 2);
                return;
            }

            // Release version is <= current version
            if (!CompareVersions(latest.tag_name)) return;

            // Update UpdateWindow's elements
            MainWindow.Log("[Updater] New version available", 1);
            Dispatcher.Invoke(() => {
                Label_NewVersion.Content = latest.tag_name;
                Label_CurrentVersion.Content = Settings.programVersion;

                HyperLink_URL.NavigateUri = new Uri(latest.html_url);
                HyperLink_URL_Direct.NavigateUri = new Uri(latest.assets[0].browser_download_url);

                ShowDialog();
            });
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Downloads list of releases from Github API and returns latest
        /// </summary>
        /// <param name="webClient">A WebClient instance for requests</param>
        /// <returns>Latest release object</returns>
        private static ReleaseObject GetLatestRelease(WebClient webClient) {
            if (webClient.IsBusy) return null;

            try {
                string jsonString = webClient.DownloadString(Settings.programReleaseAPI);
                List<ReleaseObject> releaseList = new JavaScriptSerializer().Deserialize<List<ReleaseObject>>(jsonString);

                // List contains ~10 releases. Return latest
                return releaseList[0];
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Compares input and version in settings
        /// </summary>
        /// <param name="input">Version string (eg "v2.3.1")</param>
        /// <returns>True if current version is old</returns>
        private static bool CompareVersions(string input) {
            string[] splitNew = input.Substring(1).Split('.');
            string[] splitOld = Settings.programVersion.Substring(1).Split('.');

            int oldLen = splitOld.Length;
            int newLen = splitNew.Length;
            int totLen = newLen > oldLen ? newLen : oldLen;

            for (int i = 0; i < totLen; i++) {
                int resultNew = 0, resultOld = 0;

                if (newLen > i) Int32.TryParse(splitNew[i], out resultNew);
                if (oldLen > i) Int32.TryParse(splitOld[i], out resultOld);

                if (resultNew > resultOld) return true;
                else if (resultNew < resultOld) return false;
            }

            return false;
        }

        /// <summary>
        /// Opens up the webbrowser when URL is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HyperLink_URL_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}