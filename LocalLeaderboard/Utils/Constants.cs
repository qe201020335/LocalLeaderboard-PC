﻿using UnityEngine;

namespace LocalLeaderboard.Utils
{
    internal static class Constants
    {
        internal const string CONFIG_DIR = "./UserData/LocalLeaderboard/";
        internal const string CONFIG_PATH = CONFIG_DIR + "LocalLeaderboardData.json";
        internal const string REPLAY_PATH = "./UserData/BeatLeader/Replays/";
        internal const string STEAM_API_PATH = "./Beat Saber_Data/Plugins/x86_64/steam_api64.dll";

        internal const string DISCORD_URL = "https://discord.gg/2KyykDXpBk";
        internal const string PATREON_URL = "https://patreon.com/speecil";
        internal const string WEBSITE_URL = "https://speecil.dev";
        internal const string PATRON_LIST_URL = "https://raw.githubusercontent.com/speecil/Patrons/main/patrons.txt";

        internal static readonly Color SPEECIL_COLOUR = new Color(0.156f, 0.69f, 0.46666f, 1);
    }
}