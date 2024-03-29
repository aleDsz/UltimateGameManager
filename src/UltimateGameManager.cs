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
using System.Reflection;

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
        private enumBoolYesNo _developerMode;

        private List<MaplistEntry> _currentMapList;
        private Dictionary<string, string> _gameModes;
        private Dictionary<string, string> _gameSettings;
        private Dictionary<string, GameModeSettings> _gameModeSettings;
        private Dictionary<string, List<string>> _playerAllowedWeapons;
        private Dictionary<string, Inventory> _playerInventories;

        private string _hostName;
        private string _port;
        private string _proconVersion;
        private string _votedGameMode;
        private string _votedMapFileName;

        private string WEAPONS_COMMAND = "!weapons";
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
        public string GetPluginVersion() => "0.1.2";
        public string GetPluginWebsite() => "https://github.com/aleDsz/UltimateGameManager";

        public UltimateGameManager()
        {
            this._enabled = false;
            this._roundOver = false;
            this._roundRestarted = false;
            this._receivedVotedMap = false;

            this._enableRandomWeapons = enumBoolYesNo.No;
            this._debugMode = enumBoolYesNo.No;
            this._developerMode = enumBoolYesNo.No;

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

            this._votedMapFileName = "MP_Prison";
            this._votedGameMode = "ConquestSmall0";
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> variables = new List<CPluginVariable>();

            variables.Add(new CPluginVariable("Weapon Events|Debug Mode", typeof(enumBoolYesNo), this._debugMode));
            variables.Add(new CPluginVariable("Weapon Events|Developer Mode", typeof(enumBoolYesNo), this._developerMode));
            // variables.Add(new CPluginVariable("Weapon Events|Enable Random Weapons", typeof(enumBoolYesNo), this._enableRandomWeapons));

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
            variables.Add(new CPluginVariable("Weapon Events|Developer Mode", typeof(enumBoolYesNo), this._developerMode));
            // variables.Add(new CPluginVariable("Weapon Events|Enable Random Weapons", typeof(enumBoolYesNo), this._enableRandomWeapons));

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

            this.WEAPON_DICTIONARY = this.GetWeaponDefines();

            this.ASSAULT_RIFLE_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.AssaultRifle));
            this.SMG_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.SMG));
            this.LMG_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.LMG));
            this.SNIPER_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.SniperRifle));

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
            else if (strVariable.Contains("Developer Mode"))
            {
                this._developerMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
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
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            this.ShowAllowedWeapons(speaker, message);
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            this.ShowAllowedWeapons(speaker, message);
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            if (!this._enabled) return;
            if (this._enableRandomWeapons.Equals(enumBoolYesNo.No)) return;

            this.ShowAllowedWeapons(speaker, message);
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
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0} with: winningTeamId = {1}"),
                FontBold("OnRoundOver"),
                winningTeamId
            ));

            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            this._playerInventories.Clear();
            this._playerAllowedWeapons.Clear();
            this.SetPresets();
        }

        public override void OnRestartLevel()
        {
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0}"),
                FontBold("OnRestartLevel")
            ));

            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = true;
            this._playerInventories.Clear();
            this._playerAllowedWeapons.Clear();

            this.SetPresets();
        }

        public override void OnRunNextLevel()
        {
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0}"),
                FontBold("OnRestartLevel")
            ));

            if (!this._enabled) return;

            this._roundOver = true;
            this._roundRestarted = false;
            
            this.SetPresets();
        }

        public override void OnLevelLoaded(string mapFileName, string gameMode, int roundsPlayed, int roundsTotal)
        {
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0} with: mapFileName = {1}, gameMode = {2}, roundsPlayed = {3} and roundsTotal = {4}"),
                FontBold("OnLevelLoaded"),
                FontBold(mapFileName),
                FontBold(gameMode),
                roundsPlayed,
                roundsTotal
            ));

            if (!this._enabled) return;

            this._roundOver = false;
            this._roundRestarted = false;
            this._receivedVotedMap = false;

            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public override void OnMaplistList(List<MaplistEntry> mapList)
        {
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0} with: mapList.Count = {1}"),
                FontBold("OnMaplistList"),
                mapList.Count
            ));

            if (!this._enabled) return;

            this._currentMapList = mapList;
        }

        public override void OnMaplistGetMapIndices(int currentMapIndex, int nextMapIndex)
        {
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0} with: currentMapIndex = {1} and nextMapIndex = {2}"),
                FontBold("OnMaplistGetMapIndices"),
                currentMapIndex,
                nextMapIndex
            ));

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
            this.WritePRoConPluginLog(string.Format(
                ColorLightBlue("Entered {0}"),
                FontBold("OnMaplistSave")
            ));

            if (!this._enabled) return;

            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        // API

        /// <summary>
        /// Receives a "PluginHook" from "xVoteMap" when the votemap finishes.
        /// </summary>
        ///
        /// <example>this.VotedMapInfo("MP_Prison", "ConquestSmall0");</example>
        ///
        /// <param name="mapFileName">The map's file name.</param>
        /// <param name="gameMode">The game mode selected to the map.</param>
        public void VotedMapInfo(string mapFileName, string gameMode)
        {
            this.WritePRoConPluginLog(string.Format(
                "Entered VotedMapInfo method with map {0} and game mode {1}",
                FontBold(ColorMaroon(mapFileName)),
                FontBold(ColorViolet(gameMode))
            ));

            if (!this._enabled) return;

            if (this._receivedVotedMap)
            {
                this.WritePRoConPluginLog("Already received VotedMapInfo");
                return;
            }

            this._votedMapFileName = mapFileName;
            this._votedGameMode = gameMode;
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

            // TODO: Use Battlelog response instead
            // if (this.CheckInventoryAllowedWeapon(playerName, inventory)) return;
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

        // Private

        /// <summary>
        /// Show allowed weapons if the message starts with <c>!weapons</c>.
        /// </summary>
        /// <param name="playerName">The Player name</param>
        /// <param name="message">The message</param>
        private void ShowAllowedWeapons(string playerName, string message)
        {
            if (!this._enableRandomWeapons.Equals(enumBoolYesNo.Yes)) return;
            if (playerName == "Server") return;

            this.WritePRoConPluginLog("HandlePlayerChat", string.Format(
                "Received Chat => {0}: {1}",
                ColorMaroon(playerName),
                ColorOrange(message)
            ));

            if (message.ToLower().StartsWith(this.WEAPONS_COMMAND))
            {
                this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);

                if (allowedWeapons == null) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
                if (allowedWeapons.Count == 0) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
                if (allowedWeapons.Count == 0) return;

                this.SendToPlayer(playerName, string.Format("Armas liberadas: {0}", string.Join(", ", allowedWeapons.ToArray())), false);
            }
        }

        /// <summary>
        /// Remove some weapons/gadgets from given weapon list.
        /// </summary>
        /// <param name="weaponList">The weapon list</param>
        private List<string> CleanWeaponList(List<string> weaponList)
        {
            List<string> m320 = weaponList.Where(o => o.Contains("M320")).ToList();
            List<string> gadgets = weaponList.Where(o => o.Contains("Gadget")).ToList();
            List<string> powerGuns = weaponList.Where(o => o.StartsWith("AMR") || o.StartsWith("M82A3")).ToList();

            // Shouldn't be listed
            weaponList.Remove("Death");
            weaponList.Remove("Suicide");
            weaponList.Remove("Ammobag");
            weaponList.Remove("Medkit");
            weaponList.Remove("Portable Medkit");
            weaponList.Remove("Roadkill");
            weaponList.Remove("Handflare");
            weaponList.Remove("U_UGS");
            weaponList.Remove("EOD Bot");
            weaponList.Remove("XD-1 Accipiter");
            weaponList.Remove("Melee");

            // No M320/XM25 allowed
            foreach (string weaponName in m320) weaponList.Remove(weaponName);

            // No Gadget shouldn't be listed 
            foreach (string weaponName in gadgets) weaponList.Remove(weaponName);

            // No PowerGun shouldn't be listed 
            foreach (string weaponName in powerGuns) weaponList.Remove(weaponName);

            return weaponList;
        }

        /// <summary>
        /// Returns the localized weapon name.
        /// </summary>
        /// <param name="weapon">The Weapon object</param>
        private string GetLocalizedWeapon(Weapon weapon)
        {
            foreach (KeyValuePair<string, Weapon> keyValuePair in this.WeaponDictionaryByLocalizedName)
            {
                if (keyValuePair.Value.Name == weapon.Name) return keyValuePair.Key;
                if (keyValuePair.Key == weapon.Name) return keyValuePair.Value.Name;
            }

            return null;
        }

        /// <summary>
        /// Initialize the player inventory and allowed weapons list.
        /// </summary>
        /// <param name="playerName">The Player name</param>
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

            if (inventory.Weapons.Count == 0) notAllowed = true;

            foreach (Weapon inventoryWeapon in inventory.Weapons)
            {
                string localizedWeapon = this.GetLocalizedWeapon(inventoryWeapon);

                if (this._developerMode.Equals(enumBoolYesNo.Yes))
                {
                    this.WritePRoConPluginLog("CheckInventoryAllowedWeapon", string.Format("Testing with localized {0} or inventory {1}", localizedWeapon, inventoryWeapon.Name));
                    this.WritePRoConChatLog("CheckInventoryAllowedWeapon", playerName, string.Format("Testing with localized {0} or inventory {1}", localizedWeapon, inventoryWeapon.Name));
                }

                if (allowedWeapons.Contains(inventoryWeapon.Name) || allowedWeapons.Contains(localizedWeapon))
                {
                    notAllowed = false;
                    break;
                }
            }

            if (notAllowed)
            {
                this.SendToPlayer(playerName, string.Format("As armas que voce esta equipado(a) estao proibida. Digite {0} para saber quais estao liberadas para voce.", this.WEAPONS_COMMAND));
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
                string localizedWeapon = this.GetLocalizedWeapon(weapon);

                this.SendToPlayer(playerName, string.Format("O veiculo {0} que voce está usando e proibido, favor nao utilizar!", localizedWeapon));
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
            string localizedWeapon = this.GetLocalizedWeapon(weapon);
            this._playerAllowedWeapons.TryGetValue(playerName, out List<string> allowedWeapons);

            if (allowedWeapons == null) this.GenerateAllowedWeapons(playerName, out allowedWeapons);
            if (allowedWeapons.Count == 0) this.GenerateAllowedWeapons(playerName, out allowedWeapons);

            foreach (string weaponName in allowedWeapons)
                if (localizedWeapon.Equals(weaponName)) return false;

            this.SendToPlayer(playerName, string.Format("A arma {0} que voce esta usando e proibída. Digite {1} para saber quais estão liberadas para voce.", localizedWeapon, this.WEAPONS_COMMAND));
            this.KillPlayer(playerName);

            return true;
        }

        /// <summary>
        /// Enqueues a thread to be executed in background.
        /// </summary>
        /// <param name="waitCallback">The callback method to be called</param>
        /// <param name="state">The state to be used by callback method</param>
        private void EnqueueThread(WaitCallback waitCallback, object state) => ThreadPool.QueueUserWorkItem(waitCallback, state);

        /// <summary>
        /// Gets the voted game mode and set it's pre-defined presets.
        /// </summary>
        private void SetPresets()
        {
            CMap map = this.GetMapByFilename(this._votedMapFileName);

            this._gameModeSettings.TryGetValue(this._votedGameMode, out GameModeSettings gameModeSetting);
            if (gameModeSetting == null) this._gameModeSettings.TryGetValue(map.GameMode, out gameModeSetting);

            if (gameModeSetting != null)
                this.SetPresets(gameModeSetting);
            else
                this.WritePRoConPluginLog(string.Format(
                    "Didn't found any game mode settings for: {0} or {1}",
                    FontBold(ColorOrange(this._votedGameMode)),
                    FontBold(ColorOrange(map.GameMode))
                ));

            this._votedMapFileName = "MP_Prison";
            this._votedGameMode = "ConquestSmall0";
        }

        /// <summary>
        /// Sets the presets from given <c>GameModeSettings</c>.
        /// </summary>
        /// <param name="gameModeSettings">The <c>GameModeSettings</c> object with defined presets</param>
        private void SetPresets(GameModeSettings gameModeSettings)
        {
            if (!this._enabled) return;
            if (!this._roundOver && !this._roundRestarted) return;
            if (this._developerMode.Equals(enumBoolYesNo.Yes)) return;

            foreach (var item in gameModeSettings.All())
            {
                this.WritePRoConPluginLog(string.Format(
                    "Setting preset {0} with value {1}",
                    FontBold(ColorOrange(item.Key)),
                    FontBold(ColorLightBlue(item.Value.ToString()))
                ));

                this.ExecuteCommand("procon.protected.send", item.Key, item.Value.ToString());
            }
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
                Random random = new Random();

                if (this.ASSAULT_RIFLE_KEYS == null)
                {
                    this.ASSAULT_RIFLE_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.AssaultRifle));
                    this.SMG_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.SMG));
                    this.LMG_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.LMG));
                    this.SNIPER_KEYS = this.CleanWeaponList(this.GetWeaponList(DamageTypes.SniperRifle));
                }

                int assaultIndex = random.Next(this.ASSAULT_RIFLE_KEYS.Count);
                int smgIndex = random.Next(this.SMG_KEYS.Count);
                int lmgIndex = random.Next(this.LMG_KEYS.Count);
                int sniperIndex = random.Next(this.SNIPER_KEYS.Count);

                string assaultRifleName = this.ASSAULT_RIFLE_KEYS[assaultIndex];
                string smgName = this.SMG_KEYS[smgIndex];
                string lmgName = this.LMG_KEYS[lmgIndex];
                string sniperName = this.SNIPER_KEYS[sniperIndex];

                Weapon assaultRifle = this.GetWeaponByLocalizedName(assaultRifleName);
                Weapon smg = this.GetWeaponByLocalizedName(smgName);
                Weapon lmg = this.GetWeaponByLocalizedName(lmgName);
                Weapon sniper = this.GetWeaponByLocalizedName(sniperName);

                if (assaultRifle == null || smg == null || lmg == null || sniper == null) return;

                allowedWeapons.Add(assaultRifleName);
                allowedWeapons.Add(smgName);
                allowedWeapons.Add(lmgName);
                allowedWeapons.Add(sniperName);

                this._playerAllowedWeapons[playerName] = allowedWeapons;

                this.WritePRoConPluginLog("GenerateAllowedWeapons", string.Format(
                    FontBold("Generated to Player {0}: {1}  {2}  {3}  {4}"),
                    ColorMaroon(playerName),
                    ColorPink(assaultRifleName),
                    ColorViolet(smgName),
                    ColorLightBlue(lmgName),
                    ColorGrey(sniperName)
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

            if (this._developerMode.Equals(enumBoolYesNo.Yes))
            {
                this.WritePRoConChatLog("KillPlayer", playerName, "Simulated admin.killPlayer");
                this.WritePRoConPluginLog("KillPlayer", string.Format("{0}: {1}", FontBold(ColorLightBlue(playerName)), FontBold("Simulated admin.killPlayer")));

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

            if (this._developerMode.Equals(enumBoolYesNo.Yes))
            {
                this.WritePRoConChatLog("SendToPlayer", playerName, message);
                this.WritePRoConPluginLog("SendToPlayer", string.Format("{0}: {1}", FontBold(ColorLightBlue(playerName)), FontBold(message)));

                return;
            }

            this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", playerName);
            if (withYell) this.ExecuteCommand("procon.protected.send", "admin.yell", message, YELL_DURATION, "target", playerName);
        }

        /// <summary>
        /// Logs the given message to PRoCon Plugin Console.
        /// </summary>
        /// <param name="message">The message</param>
        private void WritePRoConPluginLog(string message)
        {
            if (this._debugMode.Equals(enumBoolYesNo.No)) return;

            this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format(
                "[{0}] {1}",
                FontBold(this.GetPluginName()),
                message
            ));
        }

        /// <summary>
        /// Logs the given message to PRoCon Plugin Console.
        /// </summary>
        /// <param name="methodName">The caller method name</param>
        /// <param name="message">The message</param>
        private void WritePRoConPluginLog(string methodName, string message)
        {
            if (this._debugMode.Equals(enumBoolYesNo.No)) return;

            this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format(
                "[{0}::{1}] {2}",
                FontBold(ColorMaroon(this.GetPluginName())),
                FontBold(ColorLightBlue(methodName)),
                message
            ));
        }

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