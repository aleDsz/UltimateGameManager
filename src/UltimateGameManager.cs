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
    public class UltimateGameManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool _enabled;
        private List<MaplistEntry> _currentMapList;
        private bool _roundOver;
        private bool _roundRestarted;
        private bool _receivedVotedMap;
        private Dictionary<string, string> _gameModes;
        private Dictionary<string, string> _gameSettings;
        private Dictionary<string, GameModeSettings> _gameModeSettings;
        private string _hostName;
        private string _port;
        private string _proconVersion;
        private string _currentGameMode;

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
            this._receivedVotedMap = false;
            this._gameModeSettings = new Dictionary<string, GameModeSettings>();
            this._currentGameMode = "None";

            this._gameModes = new Dictionary<string, string>()
            {
                {"ConquestLarge0", "Conquest Large"},
                {"ConquestSmall0", "Conquest Small"},
                {"Domination0", "Domination"},
                {"Elimination0", "Defuse"},
                {"Obliteration", "Obliteration"},
                {"SquadObliteration0", "Squad Obliteration"},
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
                {"vars.soldierHealth", "Soldier Health"},
                {"vars.gameModeCounter", "Game Mode Counter"},
                {"vars.roundTimeLimit", "Round Time Limit"}
            };
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> variables = new List<CPluginVariable>();

            foreach (string gameModeKey in this._gameModes.Keys)
            {
                string gameModeLabel = this._gameModes[gameModeKey];

                this._gameModeSettings.TryGetValue(gameModeKey, out GameModeSettings gameModeSettings);
                if (gameModeSettings == null) gameModeSettings = new GameModeSettings();

                foreach (string gameSettingsKey in this._gameSettings.Keys)
                {
                    string gameSettingsLabel = this._gameSettings[gameSettingsKey];
                    int value = gameModeSettings.Get(gameSettingsKey);
                    if (value <= 0) value = 100;

                    variables.Add(new CPluginVariable(
                        gameModeLabel + "|" + gameModeKey + "|" + gameSettingsLabel,
                        value.GetType(),
                        value
                    ));
                }
            }

            return variables;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> variables = new List<CPluginVariable>();

            foreach (string gameModeKey in this._gameModes.Keys)
            {
                string gameModeLabel = this._gameModes[gameModeKey];

                this._gameModeSettings.TryGetValue(gameModeKey, out GameModeSettings gameModeSettings);
                if (gameModeSettings == null) gameModeSettings = new GameModeSettings();

                foreach (string gameSettingsKey in this._gameSettings.Keys)
                {
                    string gameSettingsLabel = this._gameSettings[gameSettingsKey];
                    int value = gameModeSettings.Get(gameSettingsKey);
                    if (value <= 0) value = 100;

                    variables.Add(new CPluginVariable(
                        gameModeLabel + "|" + gameModeKey + "|" + gameSettingsLabel,
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

            this.Log("^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnPluginDisable()
        {
            this._enabled = false;

            this.Log("^1Disabled =(");
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this._hostName = strHostName;
            this._port = strPort;
            this._proconVersion = strPRoConVersion;

            this.RegisterEvents(
                this.GetType().Name,
                "OnRoundOver",
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
            string[] splittedVar = new string[] { };
            string gameModeKey;
            string gameSettingLabel;

            if (strVariable.Contains("|")) splittedVar = strVariable.Split('|');

            if (splittedVar.Length > 2)
            {
                gameModeKey = splittedVar[1];
                gameSettingLabel = splittedVar[2];
            }
            else
            {
                gameModeKey = splittedVar[0];
                gameSettingLabel = splittedVar[1];
            }

            if (!int.TryParse(strValue, out int value)) return;

            KeyValuePair<string, string> gameSetting = this._gameSettings.Where(o => o.Value == gameSettingLabel).FirstOrDefault();
            if (gameSetting.Key == null) return;

            this._gameModeSettings.TryGetValue(gameModeKey, out GameModeSettings gameModeSetting);

            if (gameModeSetting == null)
            {
                gameModeSetting = new GameModeSettings();
                gameModeSetting.Add(gameSetting.Key, value);
            }
            else
            {
                gameModeSetting.Put(gameSetting.Key, value);
            }

            this._gameModeSettings[gameModeKey] = gameModeSetting;
        }

        // Events

        public override void OnRoundOver(int winningTeamId)
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
        }

        public override void OnRestartLevel()
        {
            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = true;
        }

        public override void OnRunNextLevel()
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
        }

        public override void OnLevelLoaded(string mapFileName, string gameMode, int roundsPlayed, int roundsTotal)
        {
            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = false;
            this._receivedVotedMap = false;
            this._currentGameMode = gameMode;
        }

        public override void OnMaplistList(List<MaplistEntry> mapList)
        {
            if (!this._enabled) return;

            this._currentMapList = mapList;
        }

        public override void OnMaplistGetMapIndices(int currentMapIndex, int nextMapIndex)
        {
            if (!this._enabled) return;

            int mapListIndex = nextMapIndex;
            if (this._roundRestarted) mapListIndex = currentMapIndex;

            MaplistEntry item = this._currentMapList[mapListIndex];
            if (item == null) return;

            CMap map = this.GetMapByFilename(item.MapFileName);

            this._gameModeSettings.TryGetValue(map.GameMode, out GameModeSettings gameModeSetting);
            if (gameModeSetting != null) this.SetMapPresets(gameModeSetting);
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
            if (!this._receivedVotedMap) return;
            if (this._currentGameMode.Equals(gameMode)) return;

            GameModeSettings gameModeSetting;

            CMap map = this.GetMapByFilename(mapFileName);
            this._gameModeSettings.TryGetValue(map.GameMode, out gameModeSetting);
            if (gameModeSetting == null) this._gameModeSettings.TryGetValue(gameMode, out gameModeSetting);
            if (gameModeSetting != null) this.SetMapPresets(gameModeSetting);

            this._receivedVotedMap = true;
        }

        // Private

        private void SetMapPresets(GameModeSettings gameModeSettings)
        {
            if (!this._roundOver) return;

            foreach (var item in gameModeSettings.All())
                this.ExecuteCommand("procon.protected.send", item.Key, item.Value.ToString());
        }

        private void Log(string message) => this.ExecuteCommand("procon.protected.pluginconsole.write", "^bUltimateGameManager: ^n" + message);
    }

    public class GameModeSettings
    {
        private Dictionary<string, int> _settings = new Dictionary<string, int>();

        public void Add(string key, int value) => this._settings.Add(key, value);
        public void Put(string key, int value) => this._settings[key] = value;
        public Dictionary<string, int> All() => this._settings;

        public int Get(string key)
        {
            this._settings.TryGetValue(key, out int value);
            return value;
        }
    }
}