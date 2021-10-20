using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);

            EventHandler RateAlbumByLovedTracks_Event = new EventHandler(RateAlbumByLovedTracks);
            mbApiInterface.MB_RegisterCommand("Plugin: Rate Album", RateAlbumByLovedTracks_Event);
            mbApiInterface.MB_AddMenuItem("mnuTools/Rate Album", "Tools: Rate Album", RateAlbumByLovedTracks_Event);

            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Rate My Album";
            about.Description = "This plugin uses the number of <Love> files in selected files to determine the <Album Rating>";
            about.Author = "The Incredible Boom Boom";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        /* RATE MY ALBUM PROJECT START */

        public void RateAlbumByLovedTracks(object sender, EventArgs e)
        {
            string[] selectedTracks = new string[] { };
            mbApiInterface.Library_QueryFilesEx("domain=SelectedFiles", out selectedTracks);

            float lovedTracks;
            string albumRating;

            lovedTracks = LovedCounter(lovedTracks = 0, selectedTracks);

            float quotient = lovedTracks / selectedTracks.Length;

            if (quotient > 0.3 && quotient < 0.5)
                albumRating = "60";
            else if (quotient >= 0.5 && quotient < 0.7)
                albumRating = "80";
            else if (quotient > 0.7)
                albumRating = "100";
            else
                albumRating = "";

            CommitRatingToFile(selectedTracks, albumRating);

            mbApiInterface.MB_RefreshPanels();
        }

        private float LovedCounter(float loved, string[] tracks)
        {
            foreach (string track in tracks)
            {
                if (mbApiInterface.Library_GetFileTag(track, MetaDataType.RatingLove) == "L")
                {
                    ++loved;
                }
            }
            return (loved);
        }

        private void CommitRatingToFile(string[] tracks, string rating)
        {
            foreach (string track in tracks)
            {
                mbApiInterface.Library_SetFileTag(track, MetaDataType.RatingAlbum, rating);
                mbApiInterface.Library_CommitTagsToFile(track);
            }

        }

        /* RATE MY ALBUM PROJECT END */

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    // ...
                    break;
            }
        }
    }
}