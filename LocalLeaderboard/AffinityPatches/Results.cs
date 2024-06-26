﻿using IPA.Utilities;
using LocalLeaderboard.Services;
using LocalLeaderboard.UI.ViewControllers;
using LocalLeaderboard.Utils;
using SiraUtil.Affinity;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LocalLeaderboard.AffinityPatches
{
    internal class Results : IAffinity
    {
        public static float GetModifierScoreMultiplier(LevelCompletionResults results, GameplayModifiersModelSO modifiersModel)
        {
            if(modifiersModel == null || results == null)
            {
                return 1;
            }
            return modifiersModel.GetTotalMultiplier(modifiersModel.CreateModifierParamsList(results.gameplayModifiers), results.energy);
        }

        public static int GetOriginalIdentifier(BeatmapKey key)
        {
            if (key == null)
            {
                return 0;
            }
            return key.difficulty switch
            {
                BeatmapDifficulty.Easy => 1,
                BeatmapDifficulty.Normal => 3,
                BeatmapDifficulty.Hard => 5,
                BeatmapDifficulty.Expert => 7,
                BeatmapDifficulty.ExpertPlus => 9,
                _ => 0,
            };
        }

        public static string GetModifiersString(LevelCompletionResults levelCompletionResults)
        {
            string mods = "";

            if (levelCompletionResults.gameplayModifiers.noFailOn0Energy && levelCompletionResults.energy == 0)
            {
                mods += "NF";
            }
            if (levelCompletionResults.gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery)
            {
                mods += "BE ";
            }
            if (levelCompletionResults.gameplayModifiers.instaFail)
            {
                mods += "IF ";
            }
            if (levelCompletionResults.gameplayModifiers.failOnSaberClash)
            {
                mods += "SC ";
            }
            if (levelCompletionResults.gameplayModifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles)
            {
                mods += "NO ";
            }
            if (levelCompletionResults.gameplayModifiers.noBombs)
            {
                mods += "NB ";
            }
            if (levelCompletionResults.gameplayModifiers.strictAngles)
            {
                mods += "SA ";
            }
            if (levelCompletionResults.gameplayModifiers.disappearingArrows)
            {
                mods += "DA ";
            }
            if (levelCompletionResults.gameplayModifiers.ghostNotes)
            {
                mods += "GN ";
            }
            if (levelCompletionResults.gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.Slower)
            {
                mods += "SS ";
            }
            if (levelCompletionResults.gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.Faster)
            {
                mods += "FS ";
            }
            if (levelCompletionResults.gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.SuperFast)
            {
                mods += "SF ";
            }
            if (levelCompletionResults.gameplayModifiers.smallCubes)
            {
                mods += "SC ";
            }
            if (levelCompletionResults.gameplayModifiers.proMode)
            {
                mods += "PM ";
            }
            if (levelCompletionResults.gameplayModifiers.noArrows)
            {
                mods += "NA ";
            }
            return mods.TrimEnd();
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(PrepareLevelCompletionResults), nameof(PrepareLevelCompletionResults.FillLevelCompletionResults))]
        private void Postfix(ref LevelCompletionResults __result, ref IScoreController ____scoreController, ref GameplayModifiersModelSO ____gameplayModifiersModelSO, ref IReadonlyBeatmapData ____beatmapData)
        {
            if(ExtraSongDataHolder.beatmapKey == null || ExtraSongDataHolder.beatmapLevel == null || __result == null || ExtraSongData.IsLocalLeaderboardReplay || ____beatmapData == null || ____gameplayModifiersModelSO == null || ____scoreController == null)
            {
                ExtraSongData.IsLocalLeaderboardReplay = false;
                return;
            }
            float maxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(____beatmapData);
            float modifiedScore = __result.modifiedScore;
            if (modifiedScore == 0 || maxScore == 0)
                return;
            float acc = (modifiedScore / maxScore) * 100;
            int score = __result.modifiedScore;
            int badCut = __result.badCutsCount;
            int misses = __result.missedCount;
            bool fc = __result.fullCombo;

            DateTime currentDateTime = DateTime.Now;
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            long unixTimestampSeconds = (long)(currentDateTime.ToUniversalTime() - unixEpoch).TotalSeconds;

            string currentTime = unixTimestampSeconds.ToString();

            string mapId = ExtraSongDataHolder.beatmapLevel.levelID;

            int difficulty = GetOriginalIdentifier(ExtraSongDataHolder.beatmapKey);
            string mapType = ExtraSongDataHolder.beatmapKey.beatmapCharacteristic.serializedName;

            string balls = mapType + difficulty.ToString(); // BeatMap Allocated Level Label String

            int pauses = ExtraSongDataHolder.pauses;
            float rightHandAverageScore = ExtraSongDataHolder.GetAverageFromList(ExtraSongDataHolder.rightHandAverageScore);
            float leftHandAverageScore = ExtraSongDataHolder.GetAverageFromList(ExtraSongDataHolder.leftHandAverageScore);
            int perfectStreak = ExtraSongDataHolder.perfectStreak;

            float rightHandTimeDependency = ExtraSongDataHolder.GetAverageFromList(ExtraSongDataHolder.rightHandTimeDependency);
            float leftHandTimeDependency = ExtraSongDataHolder.GetAverageFromList(ExtraSongDataHolder.leftHandTimeDependency);
            float fcAcc;
            if (fc) fcAcc = acc;
            else fcAcc = ExtraSongDataHolder.GetFcAcc(GetModifierScoreMultiplier(__result, ____gameplayModifiersModelSO));

            bool didFail = __result.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed;
            int maxCombo = __result.maxCombo;
            float averageHitscore = __result.averageCutScoreForNotesWithFullScoreScoringType;

            string destinationFileName = "BL REPLAY NOT FOUND";

            if (Directory.Exists(Constants.BLREPLAY_PATH) && Plugin.beatLeaderInstalled)
            {
                var directory = new DirectoryInfo(Constants.BLREPLAY_PATH);
                var filePath = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
                var replayFileName = filePath.Name;

                if (!Directory.Exists(Constants.LLREPLAYS_PATH))
                {
                    Directory.CreateDirectory(Constants.LLREPLAYS_PATH);
                }

                string timestamp = DateTime.UtcNow.Ticks.ToString();
                destinationFileName = Path.GetFileNameWithoutExtension(ExtraSongDataHolder.beatmapKey.levelId + difficulty) + "_" + timestamp + Path.GetExtension(filePath.Name);
                string destinationFilePath = Path.Combine(Constants.LLREPLAYS_PATH, destinationFileName);
                File.Copy(filePath.FullName, destinationFilePath, true);
            }
            ExtraSongData.IsLocalLeaderboardReplay = false;
            LeaderboardData.LeaderboardData.UpdateBeatMapInfo(mapId, balls, misses, badCut, fc, currentTime, acc, score, GetModifiersString(__result), maxCombo, averageHitscore, didFail, destinationFileName, rightHandAverageScore, leftHandAverageScore, perfectStreak, rightHandTimeDependency, leftHandTimeDependency, fcAcc, pauses);
            var lb = Resources.FindObjectsOfTypeAll<LeaderboardView>().FirstOrDefault();
            lb.OnLeaderboardSet(lb.currentBeatmapKey);
        }
    }
}
