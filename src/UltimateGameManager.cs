//******************************************************************************************************
// UltimateGameManager PRoCon plugin, by aleDsz
//******************************************************************************************************
/*  Copyright 2023 Alexandre de Souza

    This file is part of my UltimateGameManager plugin for PRoCon.

    UltimateGameManager plugin for PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    UltimateGameManager plugin for PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with ProconRulz plugin for ProCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Maps;
using System.Linq;

namespace PRoConEvents
{
    public class GameModeSettings
    {
        protected Dictionary<string, int?> _settings;

        public void Add(string key, int value) => this._settings.Add(key, value);
        public void Put(string key, int value) => this._settings[key] = value;
        public int? Get(string key) => this._settings[key];
        public Dictionary<string, int?> All() => this._settings;
    }

    public class UltimateGameManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        protected bool _enabled;
        protected List<MaplistEntry> _currentMapList;
        protected bool _roundOver;
        protected bool _roundRestarted;
        protected Dictionary<string, string> _gameModes;
        protected Dictionary<string, string> _gameSettings;
        protected Dictionary<string, GameModeSettings> _gameModeSettings;

        public string GetPluginAuthor() => "aleDsz";
        public string GetPluginDescription() => "The Ultimate Game Manager";
        public string GetPluginName() => "Ultimate Game Manager";
        public string GetPluginVersion() => "0.1.0";
        public string GetPluginWebsite() => "https://github.com/aleDsz/UltimateGameManager";

        public UltimateGameManager()
        {
            this._enabled = false;
            this._currentMapList = new List<MaplistEntry>();
            this._roundOver = false;
            this._roundRestarted = false;
            this._gameModeSettings = new Dictionary<string, GameModeSettings>();

            this._gameModes = new Dictionary<string, string>()
            {
                {"ConquestLarge0", "Conquest Large"},
                {"ConquestSmall0", "Conquest Small"},
                {"Domination0", "Domination"},
                {"Elimination0", "Defuse"},
                {"Obliteration", "Obliteration"},
                {"RushLarge0", "Rush"},
                {"SquadDeathMatch0", "Squad DeathMatch"},
                {"TeamDeathMatch0", "Team DeathMatch"},
                {"AirSuperiority0", "Air Superiority"},
                {"CaptureTheFlag0", "Capture The Flag"},
                {"CarrierAssaultLarge0", "Carrier Assault Large"},
                {"CarrierAssaultSmall0", "Carrier Assault Small"},
                {"Chainlink0", "Chainlink"},
                {"GunMaster0", "GunMaster"},
                {"GunMaster1", "GunMaster v2"}
            };

            this._gameSettings = new Dictionary<string, string>()
            {
                {"var.soldierHealth", "Soldier Health"},
                {"var.gameModeCounter", "Game Mode Counter"},
                {"var.roundTimeLimit", "Round Time Limit"}
            };
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            var variables = new List<CPluginVariable>();

            foreach (var gameModeItem in _gameModes)
            {
                var gameModeSettings = this._gameModeSettings[gameModeItem.Key];
                if (gameModeSettings == null) gameModeSettings = new GameModeSettings();

                foreach (var gameSettingsItem in _gameSettings)
                {
                    var value = gameModeSettings.Get(gameSettingsItem.Key);
                    if (value == null) value = 100;

                    variables.Add(new CPluginVariable(
                        string.Format("{0}|{1}", gameModeItem.Key, gameSettingsItem.Key),
                        value.GetType(),
                        value
                    ));
                }
            }

            return variables;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            var variables = new List<CPluginVariable>();

            foreach (var gameModeItem in _gameModes)
            {
                var gameModeSettings = this._gameModeSettings[gameModeItem.Key];
                if (gameModeSettings == null) gameModeSettings = new GameModeSettings();

                foreach (var gameSettingsItem in _gameSettings)
                {
                    var value = gameModeSettings.Get(gameSettingsItem.Key);
                    if (value == null) value = 100;

                    variables.Add(new CPluginVariable(
                        string.Format("{0}|{1}", gameModeItem.Value, gameSettingsItem.Value),
                        value.GetType(),
                        value
                    ));
                }
            }

            return variables;
        }

        public void OnPluginEnable()
        {
            this._enabled = true;

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bUltimateGameManager: ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnPluginDisable()
        {
            this._enabled = false;

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bUltimateGameManager: ^1Disabled =(");
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(
                this.GetType().Name,
                "OnRoundOverTeamScores",
                "OnRestartLevel",
                "OnRunNextLevel",
                "OnLevelLoaded",
                "OnMaplistList",
                "OnMaplistGetMapIndices",
                "OnMaplistSave"
            );
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            var splittedVar = strVariable.Split('|');
            var gameModeLabel = splittedVar[0];
            var gameSettingLabel = splittedVar[1];

            if (!int.TryParse(strValue, out int value)) return;

            var gameMode = this._gameModes.Where(o => o.Value == gameModeLabel).FirstOrDefault();
            var gameSetting = this._gameSettings.Where(o => o.Value == gameSettingLabel).FirstOrDefault();

            if (gameMode.Key == null || gameSetting.Key == null) return;

            var gameModeSetting = this._gameModeSettings[gameMode.Key];
            if (gameModeSetting == null) return;

            if (gameModeSetting.Get(gameSetting.Key) == null)
                gameModeSetting.Add(gameSetting.Key, value);
            else
                gameModeSetting.Put(gameSetting.Key, value);

            this._gameModeSettings[gameMode.Key] = gameModeSetting;
        }

        // Events

        public override void OnRoundOverTeamScores(List<TeamScore> teamScores)
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            GetMapInfo();
        }

        public override void OnRestartLevel()
        {
            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = true;
            GetMapInfo();
        }

        public override void OnRunNextLevel()
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            GetMapInfo();
        }

        public override void OnLevelLoaded(string mapFileName, string gameMode, int roundsPlayed, int roundsTotal)
        {
            if (!this._enabled) return;
            this.VotedMapInfo(mapFileName, gameMode);
        }

        public override void OnMaplistList(List<MaplistEntry> mapList)
        {
            if (!this._enabled) return;

            this._currentMapList = mapList;
        }

        public override void OnMaplistGetMapIndices(int currentMapIndex, int nextMapIndex)
        {
            if (!this._enabled) return;

            var mapListIndex = nextMapIndex;
            if (this._roundRestarted) mapListIndex = currentMapIndex;

            var mapListItem = this._currentMapList[mapListIndex];
            if (mapListItem != null) this.SetMapPresets(mapListItem);

            GetMapInfo();
        }

        public override void OnMaplistSave()
        {
            if (!this._enabled) return;

            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        // API

        public void VotedMapInfo(string mapFileName, string gameMode)
        {
            if (!this._enabled) return;

            var map = this.GetMapByFilename(mapFileName);
            var item = this._gameModeSettings[map.GameMode];

            if (item != null) this.SetMapPresets(item);

            GetMapInfo();
        }

        // Private

        private void SetMapPresets(MaplistEntry item) => this.VotedMapInfo(item.MapFileName, item.Gamemode);

        private void SetMapPresets(GameModeSettings gameModeSettings)
        {
            foreach (var item in gameModeSettings.All())
                this.ExecuteCommand("procon.protected.send", item.Key, item.Value.ToString());
        }

        private void GetMapInfo()
        {
            this.ExecuteCommand("procon.protected.send", "mapList.list");
            this.ExecuteCommand("procon.protected.send", "mapList.list", "100");
            this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
            this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
        }
    }
}