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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Maps;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public class UltimateGameManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool _enabled;
        private bool _roundOver;
        private bool _roundRestarted;
        private bool _receivedVotedMap;

        private enumBoolYesNo _enableRandomWeapons;
        private enumBoolYesNo _debugMode;

        private List<MaplistEntry> _currentMapList;
        private Dictionary<string, string> _gameModes;
        private Dictionary<string, string> _gameSettings;
        private Dictionary<string, GameModeSettings> _gameModeSettings;
        private Dictionary<string, List<string>> _playerAllowedWeapons;
        private Dictionary<string, Inventory> _playerInventories;

        private string _hostName;
        private string _port;
        private string _proconVersion;
        private string _currentGameMode;

        private string YELL_DURATION = "5";
        private Inventory DEFAULT_INVENTORY = new Inventory(Kits.None);
        private WeaponDictionary WEAPON_DICTIONARY;
        private DamageTypes[] VEHICLE_DAMAGE_TYPES = new DamageTypes[] { DamageTypes.VehicleAir, DamageTypes.VehicleWater,
                                                                         DamageTypes.VehicleStationary, DamageTypes.VehiclePersonal,
                                                                         DamageTypes.VehicleLight, DamageTypes.VehicleHeavy,
                                                                         DamageTypes.VehicleTransport };
        private List<string> ASSAULT_RIFLE_KEYS;
        private List<string> SMG_KEYS;
        private List<string> LMG_KEYS;
        private List<string> SNIPER_KEYS;

        public string GetPluginAuthor() => "aleDsz";
        public string GetPluginDescription() => "The Ultimate Game Manager";
        public string GetPluginName() => "Ultimate Game Manager";
        public string GetPluginVersion() => "0.1.1";
        public string GetPluginWebsite() => "https://github.com/aleDsz/UltimateGameManager";

        public UltimateGameManager()
        {
            this._enabled = false;
            this._roundOver = false;
            this._roundRestarted = false;
            this._receivedVotedMap = false;

            this._enableRandomWeapons = enumBoolYesNo.No;
            this._debugMode = enumBoolYesNo.No;

            this._currentMapList = new List<MaplistEntry>();
            this._gameModeSettings = new Dictionary<string, GameModeSettings>();
            this._playerAllowedWeapons = new Dictionary<string, List<string>>();
            this._playerInventories = new Dictionary<string, Inventory>();

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

            WEAPON_DICTIONARY = GetWeaponDefines();

            ASSAULT_RIFLE_KEYS = GetWeaponList(DamageTypes.AssaultRifle);
            SMG_KEYS = GetWeaponList(DamageTypes.SMG);
            LMG_KEYS = GetWeaponList(DamageTypes.LMG);
            SNIPER_KEYS = GetWeaponList(DamageTypes.SniperRifle);

            this._currentGameMode = "None";
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> variables = new List<CPluginVariable>();

            variables.Add(new CPluginVariable("Weapon Events|Debug Mode", typeof(enumBoolYesNo), this._debugMode));
            variables.Add(new CPluginVariable("Weapon Events|Enable Random Weapons", typeof(enumBoolYesNo), this._enableRandomWeapons));

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

            variables.Add(new CPluginVariable("Weapon Events|Debug Mode", typeof(enumBoolYesNo), this._debugMode));
            variables.Add(new CPluginVariable("Weapon Events|Enable Random Weapons", typeof(enumBoolYesNo), this._enableRandomWeapons));

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

            this.WritePRoConPluginLog(ColorGreen("Enabled!"));
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnPluginDisable()
        {
            this._enabled = false;

            this.WritePRoConPluginLog(ColorMaroon("Disabled =("));
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this._hostName = strHostName;
            this._port = strPort;
            this._proconVersion = strPRoConVersion;

            this.RegisterEvents(
                this.GetType().Name,
                "OnGlobalChat",
                "OnTeamChat",
                "OnSquadChat",
                "OnPlayerJoin",
                "OnListPlayers",
                "OnPlayerKilled",
                "OnPlayerSpawned",
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
            if (strVariable.Contains("Debug Mode"))
            {
                this._debugMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Contains("Enable Random Weapons"))
            {
                this._enableRandomWeapons = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
            else
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
        }

        // Events

        public override void OnGlobalChat(string speaker, string message)
        {
            if (!this._enabled) return;

            this.EnqueueThread(new WaitCallback(HandlePlayerChat), new object[] { speaker, message });
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            if (!this._enabled) return;

            this.EnqueueThread(new WaitCallback(HandlePlayerChat), new object[] { speaker, message });
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            if (!this._enabled) return;

            this.EnqueueThread(new WaitCallback(HandlePlayerChat), new object[] { speaker, message });
        }

        public override void OnPlayerJoin(string playerName)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            this.InitializePlayer(playerName);
        }

        public override void OnListPlayers(List<CPlayerInfo> playerInfos, CPlayerSubset playerSubset)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            foreach (CPlayerInfo playerInfo in playerInfos)
                this.InitializePlayer(playerInfo.SoldierName);
        }

        public override void OnPlayerSpawned(string playerName, Inventory inventory)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            lock (this._playerInventories)
                this._playerInventories[playerName] = inventory;

            this.EnqueueThread(new WaitCallback(HandlePlayerInventory), playerName);
        }

        public override void OnPlayerKilled(Kill kill)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            this.EnqueueThread(new WaitCallback(HandlePlayerKill), kill);
        }

        public override void OnRoundOver(int winningTeamId)
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            this._playerInventories.Clear();
            this._playerAllowedWeapons.Clear();
        }

        public override void OnRestartLevel()
        {
            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = true;
            this._playerInventories.Clear();
            this._playerAllowedWeapons.Clear();
        }

        public override void OnRunNextLevel()
        {
            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            this._playerInventories.Clear();
            this._playerAllowedWeapons.Clear();
        }

        public override void OnLevelLoaded(string mapFileName, string gameMode, int roundsPlayed, int roundsTotal)
        {
            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = false;
            this._receivedVotedMap = false;
            this._currentGameMode = gameMode;

            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
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
            if (gameModeSetting != null) this.SetPresets(gameModeSetting);
        }

        public override void OnMaplistSave()
        {
            if (!this._enabled) return;

            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        // API

        /// <summary>
        /// Receives a "PluginHook" from "xVoteMap" when the votemap finishes.
        /// </summary>
        ///
        /// <example>this.VotedMapInfo("OperationLocker", "ConquestLarge0");</example>
        ///
        /// <param name="mapFileName">The map's file name.</param>
        /// <param name="gameMode">The game mode selected to the map.</param>
        public void VotedMapInfo(string mapFileName, string gameMode)
        {
            if (!this._enabled) return;
            if (!this._receivedVotedMap) return;
            if (this._currentGameMode.Equals(gameMode)) return;

            GameModeSettings gameModeSetting;

            CMap map = this.GetMapByFilename(mapFileName);
            this._gameModeSettings.TryGetValue(map.GameMode, out gameModeSetting);
            if (gameModeSetting == null) this._gameModeSettings.TryGetValue(gameMode, out gameModeSetting);
            if (gameModeSetting != null) this.SetPresets(gameModeSetting);

            this._receivedVotedMap = true;
        }

        // Thread functions
        private void HandlePlayerInventory(object state)
        {
            if (!(state is string)) return;

            string playerName = state.ToString();

            this._playerInventories.TryGetValue(playerName, out Inventory inventory);

            if (inventory == null) return;
            if (inventory.Equals(DEFAULT_INVENTORY)) return;
            if (this.CheckInventoryAllowedWeapon(playerName, inventory)) return;
        }

        private void HandlePlayerKill(object state)
        {
            if (!(state is Kill)) return;

            Kill kill = (Kill)state;

            string playerName = kill.Killer.SoldierName;

            if (string.IsNullOrEmpty(kill.DamageType))
            {
                this.WritePRoConPluginLog(string.Format("^1Killer ({0}) with a invalid weapon: null or empty", kill.Killer.SoldierName));
                return;
            }

            string weaponKey = kill.DamageType;
            Weapon weapon = WEAPON_DICTIONARY[weaponKey];

            if (weapon == null)
            {
                this.WritePRoConPluginLog(string.Format("^1WEAPON_DICTIONARY ({0}) with a invalid weapon from key: {1}", kill.Killer.SoldierName, weaponKey));
                return;
            }

            if (weapon.Damage == DamageTypes.Melee) return;
            if (this.CheckVehicle(playerName, weapon)) return;
            if (this.CheckAllowedWeapon(playerName, weapon)) return;
        }

        private void HandlePlayerChat(object state)
        {
            if (!(state is string[])) return;

            string[] args = (string[])state;
            string playerName = (string)args[0];
            string message = (string)args[1];

            if (message.ToLower().StartsWith("!weapons"))
            {
                this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);

                if (allowedWeapons == null) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
                if (allowedWeapons.Count == 0) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
                if (allowedWeapons.Count == 0) return;

                string weapons = string.Join(", ", allowedWeapons.ToArray());
                this.SendToPlayer(playerName, string.Format("Armas liberadas: {0}", weapons), false);
            }
        }

        // Private

        private void InitializePlayer(string playerName)
        {
            lock (this._playerInventories)
            {
                this._playerInventories.TryGetValue(playerName, out Inventory inventory);
                if (inventory == null) this._playerInventories.Add(playerName, DEFAULT_INVENTORY);
            }

            lock (this._playerAllowedWeapons)
            {
                this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);
                if (allowedWeapons == null) this._playerAllowedWeapons.Add(playerName, new List<string>());
                if (allowedWeapons.Count == 0) this._playerAllowedWeapons.Add(playerName, new List<string>());
            }
        }

        /// <summary>
        /// Checks if the given inventory has at least one allowed weapon.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="inventory">The Player inventory</param>
        private bool CheckInventoryAllowedWeapon(string playerName, Inventory inventory)
        {
            this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);

            if (allowedWeapons == null) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
            if (allowedWeapons.Count == 0) this.GenerateAllowedWeapons(playerName, out allowedWeapons);

            bool notAllowed = true;
            Weapon weapon = null;

            foreach (Weapon inventoryWeapon in inventory.Weapons)
            {
                if (allowedWeapons.Contains(weapon.Name))
                {
                    notAllowed = false;
                    break;
                }
            }

            if (notAllowed)
            {
                this.SendToPlayer(playerName, string.Format("A arma {0} que você está equipado(a) é proibída, favor removê-la do seu Loadout!", weapon.Name));
                this.KillPlayer(playerName);
            }

            return notAllowed;
        }

        /// <summary>
        /// Checks if the given vehicle weapon is allowed by the server to given player.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="weapon">The used vehicle weapon</param>
        private bool CheckVehicle(string playerName, Weapon weapon)
        {
            if (VEHICLE_DAMAGE_TYPES.Contains(weapon.Damage))
            {
                this.SendToPlayer(playerName, string.Format("O veículo {0} que você está usando é proibído, favor não utilizar!", weapon.Name));
                this.KillPlayer(playerName);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given weapon is allowed by the server to given player.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="weapon">The used weapon</param>
        private bool CheckAllowedWeapon(string playerName, Weapon weapon)
        {
            this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);

            if (allowedWeapons == null) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
            if (allowedWeapons.Count == 0) this.GenerateAllowedWeapons(playerName, out allowedWeapons);

            if (!allowedWeapons.Contains(weapon.Name))
            {
                this.SendToPlayer(playerName, string.Format("A arma {0} que você está usando é proibída, favor removê-la do seu Loadout!", weapon.Name));
                this.KillPlayer(playerName);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Enqueues a thread to be executed in background.
        /// </summary>
        /// <param name="waitCallback">The callback method to be called</param>
        /// <param name="state">The state to be used by callback method</param>
        private void EnqueueThread(WaitCallback waitCallback, object state) => ThreadPool.QueueUserWorkItem(waitCallback, state);

        /// <summary>
        /// Sets the presets from given <c>GameModeSettings</c>.
        /// </summary>
        /// <param name="gameModeSettings">The <c>GameModeSettings</c> object with defined presets</param>
        private void SetPresets(GameModeSettings gameModeSettings)
        {
            if (!this._enabled) return;
            if (!this._roundOver) return;

            foreach (var item in gameModeSettings.All())
                this.ExecuteCommand("procon.protected.send", item.Key, item.Value.ToString());
        }

        /// <summary>
        /// Generates the allowed weapons list for given player.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="allowedWeapons">The out parameter with the allowed weapons list</param>
        private void GenerateAllowedWeapons(string playerName, out List<string> allowedWeapons)
        {
            allowedWeapons = null;

            if (this._enabled)
            {
                allowedWeapons = new List<string>();

                int index;

                index = new Random().Next(ASSAULT_RIFLE_KEYS.Count);
                Weapon assaultRifle = WEAPON_DICTIONARY[ASSAULT_RIFLE_KEYS[index]];

                index = new Random().Next(SMG_KEYS.Count);
                Weapon smg = WEAPON_DICTIONARY[SMG_KEYS[index]];

                index = new Random().Next(LMG_KEYS.Count);
                Weapon lmg = WEAPON_DICTIONARY[LMG_KEYS[index]];

                index = new Random().Next(SNIPER_KEYS.Count);
                Weapon sniper = WEAPON_DICTIONARY[SNIPER_KEYS[index]];

                allowedWeapons.Add(assaultRifle.Name);
                allowedWeapons.Add(smg.Name);
                allowedWeapons.Add(lmg.Name);
                allowedWeapons.Add(sniper.Name);

                this._playerAllowedWeapons[playerName] = allowedWeapons;

                this.WritePRoConPluginLog("GenerateAllowedWeapons", string.Format(
                    "Generated: {0} {1} {2} {3}",
                    assaultRifle.Name,
                    smg.Name,
                    lmg.Name,
                    sniper.Name
                ));
            }
        }

        /// <summary>
        /// Kills the player from given name.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        private void KillPlayer(string playerName)
        {
            if (!this._enabled) return;

            if (this._debugMode.Equals(enumBoolYesNo.Yes))
            {
                this.WritePRoConChatLog("KillPlayer", playerName, "Killed");
                this.WritePRoConPluginLog("KillPlayer", string.Format("{1}: {2}", FontBold(ColorLightBlue(playerName)), FontBold("Killed")));

                return;
            }

            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", playerName);
        }

        /// <summary>
        /// Sends a message to given player name..
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="message">The message</param>
        /// <param name="withYell">The flag to allow sending a Yell to the player</param>
        private void SendToPlayer(string playerName, string message, bool withYell = true)
        {
            if (!this._enabled) return;

            this.WritePRoConChatLog("SendToPlayer", playerName, message);
            this.WritePRoConPluginLog("SendToPlayer", string.Format("{1}: {2}", FontBold(ColorLightBlue(playerName)), FontBold(message)));

            this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", playerName);
            if (withYell) this.ExecuteCommand("procon.protected.send", "admin.yell", message, YELL_DURATION, "target", playerName);
        }

        /// <summary>
        /// Logs the given message to PRoCon Plugin Console.
        /// </summary>
        /// <param name="message">The message</param>
        private void WritePRoConPluginLog(string message) =>
            this.ExecuteCommand("procon.protected.pluginconsole.write", FontBold(this.GetPluginName()) + message);

        /// <summary>
        /// Logs the given message to PRoCon Plugin Console.
        /// </summary>
        /// <param name="methodName">The caller method name</param>
        /// <param name="message">The message</param>
        private void WritePRoConPluginLog(string methodName, string message) =>
            this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format(
                "{0} [{1}]: {2}",
                FontBold(ColorMaroon(this.GetPluginName())),
                FontBold(ColorLightBlue(methodName)),
                message
            ));

        /// <summary>
        /// Logs the given message to PRoCon Plugin Console.
        /// </summary>
        /// <param name="methodName">The caller method name</param>
        /// <param name="playerName">The Player name</param>
        /// <param name="message">The message</param>
        private void WritePRoConChatLog(string methodName, string playerName, string message) =>
            this.ExecuteCommand("procon.protected.chat.write", string.Format(
                "{0} > {1} > {2} > {3}",
                FontBold(ColorMaroon(this.GetPluginName())),
                FontBold(ColorLightBlue(methodName)),
                FontBold(ColorViolet(playerName)),
                message
            ));

        // PRoConChatLog Styles

        public string FontBold(string message) => string.Format("^b{0}^n", message);
        public string FontItalic(string message) => string.Format("^i{0}^n", message);
        public string ColorMaroon(string message) => string.Format("^1{0}^0", message);
        public string ColorGreen(string message) => string.Format("^2{0}^0", message);
        public string ColorOrange(string message) => string.Format("^3{0}^0", message);
        public string ColorBlue(string message) => string.Format("^4{0}^0", message);
        public string ColorLightBlue(string message) => string.Format("^5{0}^0", message);
        public string ColorViolet(string message) => string.Format("^6{0}^0", message);
        public string ColorPink(string message) => string.Format("^7{0}^0", message);
        public string ColorRed(string message) => string.Format("^8{0}^0", message);
        public string ColorGrey(string message) => string.Format("^9{0}^0", message);
    }

    public class GameModeSettings
    {
        private Dictionary<string, int> _presets = new Dictionary<string, int>();

        public void Add(string key, int value) => this._presets.Add(key, value);
        public void Put(string key, int value) => this._presets[key] = value;
        public Dictionary<string, int> All() => this._presets;

        public int Get(string key)
        {
            this._presets.TryGetValue(key, out int value);
            return value;
        }
    }
}
