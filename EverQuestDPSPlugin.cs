﻿using Advanced_Combat_Tracker;
using EverQuestDPSPlugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

/*
 * Project: EverQuest DPS Plugin
 * Original: EverQuest 2 English DPS Localization plugin developed by EQAditu
 * Description: Missing from the arsenal of the plugin based Advanced Combat Tracker to track EverQuest's current combat messages.  Ignores chat as that is displayed in game.
 */


#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
namespace ACT_EverQuest_DPS_Plugin
{
    public class EverQuestDPSPlugin : UserControl, IActPluginV1
    {
        #region Designer generated code (Avoid editing)
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.varianceChkBx = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // varianceChkBx
            // 
            this.varianceChkBx.AutoSize = true;
            this.varianceChkBx.Location = new System.Drawing.Point(14, 21);
            this.varianceChkBx.Name = "varianceChkBx";
            this.varianceChkBx.Size = new System.Drawing.Size(320, 17);
            this.varianceChkBx.TabIndex = 19;
            this.varianceChkBx.Text = "Population Variance (checked)/Sample Variance (unchecked)";
            this.varianceChkBx.UseVisualStyleBackColor = true;
            this.varianceChkBx.CheckedChanged += new System.EventHandler(this.varianceChkBx_CheckedChanged);
            // 
            // EverQuestDPSPlugin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.varianceChkBx);
            this.Name = "EverQuestDPSPlugin";
            this.Size = new System.Drawing.Size(337, 41);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        #endregion

        #region class members
        //readonly int pluginId = -1;
        readonly char[] chrApos = new char[] { '\'', '’' };
        readonly char[] chrSpaceApos = new char[] { ' ', '\'', '’', '`' };
        //List<Tuple<Color, Regex>> regexTupleList = new List<Tuple<Color, Regex>>();
        delegate void matchParse(Match regexMatch);
        List<Tuple<Color, Regex>> regexTupleList = new List<Tuple<Color, Regex>>();
        readonly static String attackTypes = @"stroke|pierce|gore|crush|slash|hit|kick|slam|bash|shoot|strike|bite|grab|punch|scratch|rake|swipe|claw|smack";
        readonly static String evasionTypes = @"block(|s)|dodge(|s)|parr(ies|y)|riposte(|s)";
        readonly String DamageShield = @"(?<attacker>.+) is (?<damageShieldDamageType>\S+) by (?<victim>.+) (?<damageShieldType>\S+) for (?<damagePoints>[\d]+) points of non-melee damage.";
        readonly String eqDateTimeStampFormat = @"ddd MMM dd HH:mm:ss yyyy";
        readonly String Heal = @"(?<healer>.*?) healed (?<healingTarget>.*?)(?:\s(?<overTime>over time)){0,1} for (?<healingPoints>[\d]+)(?:\s\((?<overHealPoints>[\d]+)\)){0,1} hit point(?:|s) by (?<healingSpell>.*)\.(?:[\s][\(](?<healingSpecial>.+)[\)]){0,1}";
        readonly String MeleeAttack = @"(?<attacker>.+) (?<attackType>" + $@"{attackTypes}" + @")(|s|es|bed) (?<victim>.+) for (?<damageAmount>[\d]+) (?:point[|s]) of damage.(?:\s\((?<damageSpecial>.+)\)){0,1}";
        readonly String MissedMeleeAttack = @"(?<attacker>.+) (?:tr(?:ies|y)) to (?<attackType>\S+) (?<victim>.+), but (?:miss(?:|es))!(?:\s\((?<damageSpecial>.+)\)){0,1}";
        readonly static String PluginSettingsFileName = @"Config\ACT_EverQuest_English_Parser.config.xml";
        readonly String PluginSettingsSectionName = @"Data Correction\EQ English Settings";
        readonly String SlainMessage = @"(?<attacker>.+) ha(ve|s) slain (?<victim>.+)!";
        readonly String SpecialCripplingBlow = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Crippling_Blow).Replace("_", " ");
        readonly String SpecialCritical = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Critical);
        readonly String SpecialDoubleBowShot = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Double_Bow_Shot).Replace("_", " ");
        readonly String SpecialFlurry = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Flurry);
        readonly String SpecialLocked = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Locked);
        readonly String SpecialLucky = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Lucky);
        readonly String SpecialRiposte = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Riposte);
        readonly String SpecialStrikethrough = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Strikethrough);
        readonly String SpecialTwincast = Enum.GetName(typeof(SpecialAttacks), (SpecialAttacks)SpecialAttacks.Twincast);
        readonly String SpellDamage = @"(?<attacker>.+) hit (?<victim>.*) for (?<damagePoints>[\d]+) (?:point[|s]) of (?<typeOfDamage>.+) damage by (?<damageEffect>.*)\.(?:[\s][\(](?<spellSpecial>.+)[\)]){0,1}";
        readonly String SpellDamageOverTime = @"(?<attacker>.+) has taken (?<damagePoints>[\d]+) damage from (?<damageEffect>.*) by (?<victim>.*)\.(?:[\s][\(](?<spellSpecial>.+)[\)]){0,1}";
        static readonly String TimeStamp = @"\[(?<dateTimeOfLogLine>.+)\]";
        readonly String ZoneChange = @"You have entered (?!.*the Drunken Monkey stance adequately)(?<zoneName>.*)\.";
        readonly String LoadingPleaseWait = @"LOADING, PLEASE WAIT...";
        readonly String Unknown = @"(?<Unknown>(u|U)nknown)";
        readonly String logTimestamp = "logTimestamp";
        readonly String Evasion = @"(?<attacker>.*) tries to (?<attackType>\S+) (?:(?<victim>(.+)), but \1) (?:(?<evasionType>" + $@"{evasionTypes}" + @"))!(?:[\s][\(](?<evasionSpecial>.+)[\)]){0,1}";
        readonly String Banestrike = @"You hit (?<victim>.+) for (?<baneDamage>[\d]+) points of (?<typeOfDamage>.+) by Banestrike (?<baneAbilityRank>.+\.)";
        readonly Regex dateTimeRegex = new Regex(TimeStamp, RegexOptions.Compiled);
        readonly Regex selfCheck = new Regex(@"(You|you|yourself|Yourself|YOURSELF|YOU)", RegexOptions.Compiled);
        readonly String pluginName = "EverQuest Damage Per Second Parser";
        readonly String possessivePetString = @"`s pet";
        readonly String fallDamage = @"(?<victim>.*) (?:ha[s|ve]) taken (?<pointsOfDamage>[\d]+) (?point[|s]) of fall damage.";
        bool populationVariance = false;
        SortedList<string, AposNameFix> aposNameList = new SortedList<string, AposNameFix>();
        TreeNode optionsNode = null;
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        string settingsFile;
        private CheckBox varianceChkBx;
        SettingsSerializer xmlSettings;
        #endregion

        public EverQuestDPSPlugin()
        {
            InitializeComponent();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            EverQuest_DPS_Plugin_Localization.EditLocalizations();
            settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, PluginSettingsFileName);
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
            pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;

            int dcIndex = -1;   // Find the Data Correction node in the Options tab
            for (int i = 0; i < ActGlobals.oFormActMain.OptionsTreeView.Nodes.Count; i++)
            {
                if (ActGlobals.oFormActMain.OptionsTreeView.Nodes[i].Text == "Data Correction")
                    dcIndex = i;
            }
            if (dcIndex != -1)
            {
                // Add our own node to the Data Correction node
                optionsNode = ActGlobals.oFormActMain.OptionsTreeView.Nodes[dcIndex].Nodes.Add($"{pluginName} Settings");
                // Register our user control(this) to our newly create node path.  All controls added to the list will be laid out left to right, top to bottom
                ActGlobals.oFormActMain.OptionsControlSets.Add($@"{PluginSettingsSectionName}", new List<Control> { this });
                Label lblConfig = new Label
                {
                    AutoSize = true,
                    Text = "Find the applicable options in the Options tab, Data Correction section."
                };
                pluginScreenSpace.Controls.Add(lblConfig);
            }

            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();

            PopulateRegexArray();
            SetupEverQuestEnvironment();   // Not really needed since ACT has this code internalized as well.
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(oFormActMain_BeforeLogLineRead);
            ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(oFormActMain_UpdateCheckClicked);
            ActGlobals.oFormActMain.GetDateTimeFromLog += new FormActMain.DateTimeLogParser(ParseDateTime);

            if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())   // If ACT is set to automatically check for updates, check for updates to the plugin
                new Thread(new ThreadStart(oFormActMain_UpdateCheckClicked)).Start();   // If we don't put this on a separate thread, web latency will delay the plugin init phase
            ActGlobals.oFormActMain.CharacterFileNameRegex = new Regex(@"(?:.+)[\\]eqlog_(?<characterName>\S+)_(?<server>.+).txt", RegexOptions.Compiled);
            ActGlobals.oFormActMain.ZoneChangeRegex = new Regex($@"{ZoneChange}", RegexOptions.Compiled);
            lblStatus.Text = $"{pluginName} Plugin Started";
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= oFormActMain_BeforeLogLineRead;
            ActGlobals.oFormActMain.UpdateCheckClicked -= oFormActMain_UpdateCheckClicked;
            ActGlobals.oFormActMain.GetDateTimeFromLog -= ParseDateTime;

            if (optionsNode != null)    // If we added our user control to the Options tab, remove it
            {
                optionsNode.Remove();
                ActGlobals.oFormActMain.OptionsControlSets.Remove($@"{PluginSettingsSectionName}");
            }

            SaveSettings();
            lblStatus.Text = $@"{pluginName} Plugin Exited";
        }
        void oFormActMain_UpdateCheckClicked()
        {
            int pluginId = 92;

            try
            {
                Version remoteVersion = new Version(ActGlobals.oFormActMain.PluginGetRemoteVersion(pluginId));
                AssemblyFileVersionAttribute currentVersion = Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                Version currentVersionv = new Version(currentVersion.Version);
                if (remoteVersion > currentVersionv)
                {
                    DialogResult result = MessageBox.Show($"There is an updated version of the {pluginName}.  Update it now?\n\n(If there is an update to ACT, you should click No and update ACT first.)", "New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        FileInfo updatedFile = ActGlobals.oFormActMain.PluginDownload(pluginId);
                        //String githubData = ActGlobals.oFormActMain.PluginGetGithubApi(pluginId);
                        ActPluginData pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
                        pluginData.pluginFile.Delete();
                        updatedFile.MoveTo(pluginData.pluginFile.FullName);
                        ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, false);
                        Application.DoEvents();
                        ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, true);
                    }
                }
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(ex, "Plugin Update Check");
            }
        }

        private DateTime ParseDateTime(String logLine)
        {
            DateTime currentEQTimeStamp;
            DateTime.TryParseExact(dateTimeRegex.Match(logLine).Groups["dateTimeOfLogLine"].Value, eqDateTimeStampFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out currentEQTimeStamp);
            return currentEQTimeStamp;
        }

        private void PopulateRegexArray()
        {
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Clear();
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex($@"{TimeStamp} {MeleeAttack}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.ForestGreen, new Regex($@"{TimeStamp} {DamageShield}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Plum, new Regex($@"{TimeStamp} {MissedMeleeAttack}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Goldenrod, new Regex($@"{TimeStamp} {SlainMessage}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex($@"{TimeStamp} {SpellDamage}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Maroon, new Regex($@"{TimeStamp} {ZoneChange}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DarkBlue, new Regex($@"{TimeStamp} {Heal}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Azure, new Regex($@"{TimeStamp} {LoadingPleaseWait}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Silver, new Regex($@"{TimeStamp} {Unknown}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DeepSkyBlue, new Regex($@"{TimeStamp} {Evasion}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.LightBlue, new Regex($@"{TimeStamp} {Banestrike}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.AliceBlue, new Regex($@"{TimeStamp} {SpellDamageOverTime}", RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            //regexTupleList.Add(new Tuple<Color, Regex>(Color.PaleVioletRed, new Regex($@"{TimeStamp} {fallDamage}", RegexOptions.Compiled)));
            //ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
        }

        void oFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            for (int i = 0; i < regexTupleList.Count; i++)
            {
                Match regexMatch = regexTupleList[i].Item2.Match(logInfo.logLine);
                if (regexMatch.Success)
                {
                    logInfo.detectedType = i + 1;
                    ParseEverQuestLogLine(regexMatch, i + 1);
                    break;
                }
            }
#if DEBUG
            logInfo.detectedType = 0;
#endif
        }

        Tuple<EverQuestSwingType, String> GetTypeAndNameForPet(String nameToSetTypeTo)
        {
            int indexOfPetInCombatantName = nameToSetTypeTo.IndexOf(possessivePetString);
            if (indexOfPetInCombatantName > 0)
            {
                return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.PetMelee, nameToSetTypeTo.Substring(0, indexOfPetInCombatantName));
            }
            else
            {
                return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Melee, nameToSetTypeTo);
            }
        }

        private void ParseEverQuestLogLine(Match regexMatch, int logMatched)
        {
            switch (logMatched)
            {
                case 1:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Tuple<EverQuestSwingType, String> attackerAndType = GetTypeAndNameForPet(regexMatch.Groups["attacker"].Value);
                        MasterSwing masterSwingMelee = new MasterSwing((int)attackerAndType.Item1
                            , regexMatch.Groups["damageSpecial"].Success ? regexMatch.Groups["damageSpecial"].Value.Contains(SpecialCritical) : false
                            , regexMatch.Groups["damageSpecial"].Success ? regexMatch.Groups["damageSpecial"].Value : String.Empty
                            , new Dnum(Int64.Parse(regexMatch.Groups["damageAmount"].Value))
                            , ActGlobals.oFormActMain.LastEstimatedTime
                            , ActGlobals.oFormActMain.GlobalTimeSorter
                            , regexMatch.Groups["attackType"].Value
                            , CharacterNamePersonaReplace(attackerAndType.Item2)
                            , "Hitpoints"
                            , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                        masterSwingMelee.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingMelee);
                    }
                    break;
                //Non-melee damage shield
                case 2:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        MasterSwing masterSwingDamageShield = new MasterSwing(EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue()
                            , regexMatch.Groups["damageSpecial"].Value.Contains(SpecialCritical)
                            , regexMatch.Groups["damageSpecial"].Value
                            , new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value))
                            , ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter
                            , regexMatch.Groups["damageShieldDamageType"].Value
                            , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)
                            , "Hitpoints"
                            , CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value));
                        masterSwingDamageShield.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingDamageShield);
                    }
                    break;
                //Missed melee
                case 3:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        MasterSwing masterSwingMissedMelee = new MasterSwing(EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue()
                            , false
                            , regexMatch.Groups["damageSpecial"].Success ? regexMatch.Groups["damageSpecial"].Value : String.Empty
                            , new Dnum(Dnum.Miss)
                            , ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter
                            , regexMatch.Groups["attackType"].Value
                            , CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value)
                            , "Miss"
                            , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                        masterSwingMissedMelee.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingMissedMelee);
                    }
                    break;
                case 4:
                    MasterSwing masterSwingSlain = new MasterSwing(0, false, new Dnum(Dnum.Death), ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter, String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                    masterSwingSlain.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                    ActGlobals.oFormActMain.AddCombatAction(masterSwingSlain);
                    break;
                //Spell Cast
                case 5:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value), regexMatch.Groups["typeOfDamage"].Value);
                        MasterSwing masterSwingSpellcast = new MasterSwing(EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue()
                            , regexMatch.Groups["spellSpecial"].Success ? regexMatch.Groups["spellSpecial"].Value.Contains(SpecialCritical) : false
                            , regexMatch.Groups["spellSpecial"].Success ? regexMatch.Groups["spellSpeical"].Value : String.Empty
                            , damage, ActGlobals.oFormActMain.LastEstimatedTime
                            , ActGlobals.oFormActMain.GlobalTimeSorter
                            , regexMatch.Groups["damageEffect"].Value
                            , CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value)
                            , "Hitpoints"
                            , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)
                        );
                        masterSwingSpellcast.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingSpellcast);
                    }
                    break;
                case 6:
                    //when checking the HistoryRecord the EndTime should be compared against default(DateTime) to determine if it an exact value among other methods such does the default(DateTime) take place before the StartTime for the HistoryRecord
                    //ActGlobals.oFormActMain.ZoneDatabaseAdd(new HistoryRecord(0, ActGlobals.oFormActMain.LastKnownTime, new DateTime(), regexMatch.Groups["zoneName"].Value != String.Empty ? regexMatch.Groups["zoneName"].Value : throw new Exception("Zone regex triggered but zone name not found."), ActGlobals.charName));
                    ActGlobals.oFormActMain.ChangeZone(regexMatch.Groups["zoneName"].Value);
                    break;
                //heal
                case 7:
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        MasterSwing masterSwingHeal = new MasterSwing(regexMatch.Groups["overTime"].Success ? EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue() : EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue()
                            , regexMatch.Groups["healingSpecial"].Success ? regexMatch.Groups["healingSpecial"].Value.Contains(SpecialCritical) : false
                            , regexMatch.Groups["healingSpecial"].Success ? regexMatch.Groups["healingSpecial"].Value : String.Empty
                            , new Dnum(Int64.Parse(regexMatch.Groups["healingPoints"].Value))
                            , ActGlobals.oFormActMain.LastEstimatedTime
                            , ActGlobals.oFormActMain.GlobalTimeSorter
                            , regexMatch.Groups["healingSpell"].Value
                            , CharacterNamePersonaReplace(regexMatch.Groups["healer"].Value)
                            , "Hitpoints"
                            , CharacterNamePersonaReplace(regexMatch.Groups["healingTarget"].Value)
                        );
                        masterSwingHeal.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        if (regexMatch.Groups["overHealPoints"].Success)
                            masterSwingHeal.Tags["overheal"] = Int64.Parse(regexMatch.Groups["overHealPoints"].Value);
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingHeal);
                    }
                    break;
                case 8:
                    //_ = ActGlobals.oFormActMain.ZoneDatabase[ActGlobals.oFormActMain.ZoneDatabase[ActGlobals.oFormActMain.ZoneDatabase.Max().Key].Label.Equals(ActGlobals.oFormActMain.CurrentZone) ? ActGlobals.oFormActMain.ZoneDatabase[ActGlobals.oFormActMain.ZoneDatabase.Max().Key].EndTime = ActGlobals.oFormActMain.LastKnownTime : throw new Exception("unable to determine last zone and time from log file")];
                    break;
                case 9:
                    MasterSwing masterSwingUnknown = new MasterSwing(EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), false, new Dnum(Dnum.Unknown)
                    {
                        DamageString2 = regexMatch.Value
                    }, ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter, "Unknown", "Unknown", "Unknown", "Unknown");
                    ActGlobals.oFormActMain.AddCombatAction(masterSwingUnknown);
                    break;
                case 10:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        MasterSwing masterSwingEvasion = new MasterSwing(EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue()
                            , false, regexMatch.Groups["evasionSpecial"].Value, new Dnum(Dnum.NoDamage, regexMatch.Groups["evasionType"].Value), ActGlobals.oFormActMain.LastEstimatedTime
                            , ActGlobals.oFormActMain.GlobalTimeSorter, regexMatch.Groups["attackType"].Value, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), "Hitpoints", CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                        masterSwingEvasion.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingEvasion);
                    }
                    break;
                case 11:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value));
                        MasterSwing masterSwingSpellcast = new MasterSwing(EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue()
                            , regexMatch.Groups["spellSpecial"].Value.Contains(SpecialCritical)
                            , regexMatch.Groups["spellSpecial"].Value
                            , damage, ActGlobals.oFormActMain.LastEstimatedTime
                            , ActGlobals.oFormActMain.GlobalTimeSorter
                            , "Damage over time"
                            , CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value)
                            , "Hitpoints"
                            , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)
                        );
                        masterSwingSpellcast.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingSpellcast);
                    }
                    break;
                //case 12:
                //    MasterSwing masterSwingFallDamage = new MasterSwing(EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(),
                //        false,
                //        String.Empty,
                //         new Dnum(Int64.Parse(regexMatch.Groups["pointsOfDamage"].Value)),
                //         ActGlobals.oFormActMain.LastEstimatedTime,
                //         ActGlobals.oFormActMain.GlobalTimeSorter
                //         , "Fall Damage"
                //         , CharacterNamePersonaReplace("Fall Damage")
                //         , "Hitpoints"
                //         , CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)
                //    );
                //    masterSwingFallDamage.Tags[logTimestamp] = ActGlobals.oFormActMain.LastKnownTime;
                //    ActGlobals.oFormActMain.AddCombatAction(masterSwingFallDamage);
                //    break;
                default:
                    break;
            }
        }

        private string CharacterNamePersonaReplace(string PersonaString)
        {
            return selfCheck.Match(PersonaString).Success ? ActGlobals.charName : PersonaString;
        }

        void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                using (FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (XmlTextReader xReader = new XmlTextReader(fs))
                {
                    try
                    {
                        while (xReader.Read())
                        {
                            if (xReader.NodeType == XmlNodeType.Element)
                            {
                                if (xReader.LocalName == "SettingsSerializer")
                                    xmlSettings.ImportFromXml(xReader);
                                if (xReader.LocalName == "ApostropheNameFix")
                                    LoadXmlApostropheNameFix(xReader);

                            }
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        lblStatus.Text = "Argument Null for " + ex.ParamName + " with message: " + ex.Message;
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "With message: " + ex.Message;
                    }
                }
            }
        }

        void SaveSettings()
        {
            using (FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    xWriter.Formatting = Formatting.Indented;
                    xWriter.Indentation = 1;
                    xWriter.IndentChar = '\t';
                    xWriter.WriteStartDocument(true);
                    xWriter.WriteStartElement("Config");    // <Config>
                    xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
                    xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
                    xWriter.WriteEndElement();  // </SettingsSerializer>
                    SaveXmlApostropheNameFix(xWriter);  // Create and fill the ApostropheNameFix node
                    xWriter.WriteEndElement();  // </Config>
                    xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
                }
            }
        }

        internal class AposNameFix : IEquatable<AposNameFix>
        {
            string left, right, fullName;
            public string FullName
            {
                get
                {
                    return fullName;
                }
                set
                {
                    fullName = value;
                }
            }
            public string Right
            {
                get
                {
                    return right;
                }
                set
                {
                    right = value;
                }
            }
            public string Left
            {
                get
                {
                    return left;
                }
                set
                {
                    left = value;
                }
            }
            bool active = true;
            public bool Active
            {
                get
                {
                    return active;
                }
                set
                {
                    active = value;
                }
            }

            public AposNameFix(string FullName, string Left, string Right)
            {
                this.left = Left;
                this.right = Right;
                this.fullName = FullName;
            }
            public bool IsMatch(CombatActionEventArgs e)
            {
                if (e.attacker == left && e.theAttackType.StartsWith(right))
                    return true;
                return false;
            }
            public bool Fix(CombatActionEventArgs e)
            {
                if (!active)
                    return false;
                if (e.theAttackType == right)
                {
                    e.swingType = (int)SwingTypeEnum.Melee;
                    if (e.attacker[e.attacker.Length - 1] == 's' || e.attacker[e.attacker.Length - 1] == 'z')
                        e.attacker += "' ";
                    else
                        e.attacker += "'s ";
                    e.attacker += right;
                    e.theAttackType = e.theDamageType.ToString();
                    return true;
                }
                if (e.theAttackType.StartsWith(right + "'"))
                {
                    int trimLen = right.Length;
                    if (e.attacker[e.attacker.Length - 1] == 's' || e.attacker[e.attacker.Length - 1] == 'z')
                    {
                        e.attacker += "' ";
                        trimLen += 2;
                    }
                    else
                    {
                        e.attacker += "'s ";
                        trimLen += 3;
                    }
                    e.attacker += right;
                    e.theAttackType = e.theAttackType.Substring(trimLen);
                    return true;
                }
                return false;
            }
            public bool Equals(AposNameFix other)
            {
                return this.fullName.Equals(other.fullName);
            }
            public override string ToString()
            {
                return fullName;
            }
        }

        private int LoadXmlApostropheNameFix(XmlTextReader xReader)
        {
            int errorCount = 0;
            if (xReader.IsEmptyElement)
                return errorCount;
            while (xReader.Read())
            {
                if (xReader.NodeType == XmlNodeType.EndElement)
                    return errorCount;
                if (xReader.NodeType == XmlNodeType.Element)
                {
                    if (xReader.LocalName == "AposFix")
                    {
                        try
                        {
                            AposNameFix newItem = new AposNameFix(xReader.GetAttribute("FullName"), xReader.GetAttribute("Left"), xReader.GetAttribute("Right"))
                            {
                                Active = Boolean.Parse(xReader.GetAttribute("Active"))
                            };
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            ActGlobals.oFormActMain.WriteExceptionLog(ex, "AposFix" + xReader.ReadOuterXml());
                        }
                    }
                    else
                        break;
                }
            }
            return errorCount;
        }

        private void SaveXmlApostropheNameFix(XmlTextWriter xWriter)
        {
            xWriter.WriteStartElement("ApostropheNameFix");
            foreach (KeyValuePair<string, AposNameFix> pair in aposNameList)
            {
                xWriter.WriteStartElement("AposFix");
                xWriter.WriteAttributeString("Active", pair.Value.Active.ToString());
                xWriter.WriteAttributeString("FullName", pair.Value.FullName);
                xWriter.WriteAttributeString("Left", pair.Value.Left);
                xWriter.WriteAttributeString("Right", pair.Value.Right);
                xWriter.WriteEndElement();
            }
            xWriter.WriteEndElement();
        }

        private string GetIntCommas()
        {
            return ActGlobals.mainTableShowCommas ? "#,0" : "0";
        }

        private void SetupEverQuestEnvironment()
        {
            CultureInfo usCulture = new CultureInfo("en-US");   // This is for SQL syntax; do not change

            EncounterData.ColumnDefs.Clear();
            //                                                                                      Do not change the SqlDataName while doing localization
            EncounterData.ColumnDefs.Add("EncId", new EncounterData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.EncId; }));
            EncounterData.ColumnDefs.Add("Title", new EncounterData.ColumnDef("Title", true, "VARCHAR(64)", "Title", (Data) => { return Data.Title; }, (Data) => { return Data.Title; }));
            EncounterData.ColumnDefs.Add("StartTime", new EncounterData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : String.Format("{0} {1}", Data.StartTime.ToShortDateString(), Data.StartTime.ToLongTimeString()); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("EndTime", new EncounterData.ColumnDef("EndTime", true, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("Duration", new EncounterData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            EncounterData.ColumnDefs.Add("Damage", new EncounterData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
            EncounterData.ColumnDefs.Add("EncDPS", new EncounterData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            EncounterData.ColumnDefs.Add("Zone", new EncounterData.ColumnDef("Zone", false, "VARCHAR(64)", "Zone", (Data) => { return Data.ZoneName; }, (Data) => { return Data.ZoneName; }));
            EncounterData.ColumnDefs.Add("Kills", new EncounterData.ColumnDef("Kills", true, "INT", "Kills", (Data) => { return Data.AlliedKills.ToString(GetIntCommas()); }, (Data) => { return Data.AlliedKills.ToString(); }));
            EncounterData.ColumnDefs.Add("Deaths", new EncounterData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.AlliedDeaths.ToString(); }, (Data) => { return Data.AlliedDeaths.ToString(); }));

            EncounterData.ExportVariables.Clear();
            EncounterData.ExportVariables.Add("n", new EncounterData.TextExportFormatter("n", "New Line", "Formatting after this element will appear on a new line.", (Data, SelectiveAllies, Extra) => { return "\n"; }));
            EncounterData.ExportVariables.Add("t", new EncounterData.TextExportFormatter("t", "Tab Character", "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)", (Data, SelectiveAllies, Extra) => { return "\t"; }));

            CombatantData.ColumnDefs.Clear();
            CombatantData.ColumnDefs.Add("EncId", new CombatantData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.EncId; }, (Left, Right) => { return 0; }));
            CombatantData.ColumnDefs.Add("Ally", new CombatantData.ColumnDef("Ally", false, "CHAR(1)", "Ally", (Data) => { return Data.Parent.GetAllies().Contains(Data).ToString(); }, (Data) => { return Data.Parent.GetAllies().Contains(Data) ? "T" : "F"; }, (Left, Right) => { return Left.Parent.GetAllies().Contains(Left).CompareTo(Right.Parent.GetAllies().Contains(Right)); }));
            CombatantData.ColumnDefs.Add("Name", new CombatantData.ColumnDef("Name", true, "VARCHAR(64)", "Name", (Data) => { return Data.Name; }, (Data) => { return Data.Name; }, (Left, Right) => { return Left.Name.CompareTo(Right.Name); }));
            CombatantData.ColumnDefs.Add("StartTime", new CombatantData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            CombatantData.ColumnDefs.Add("EndTime", new CombatantData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            CombatantData.ColumnDefs.Add("Duration", new CombatantData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            CombatantData.ColumnDefs.Add("Damage", new CombatantData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("Damage%", new CombatantData.ColumnDef("Damage%", true, "DOUBLE", "DamagePercent", (Data) => { return string.Format("{0:000.000}", Data.DamagePercent); }, (Data) => { return string.Format("{0:000.000}", Data.DamagePercent); }, (Left, Right) => { return Left.DamagePercent.CompareTo(Right.DamagePercent); }));
            CombatantData.ColumnDefs.Add("Kills", new CombatantData.ColumnDef("Kills", false, "INT", "Kills", (Data) => { return Data.Kills.ToString(GetIntCommas()); }, (Data) => { return Data.Kills.ToString(); }, (Left, Right) => { return Left.Kills.CompareTo(Right.Kills); }));
            CombatantData.ColumnDefs.Add("Healed", new CombatantData.ColumnDef("Healed", false, "BIGINT", "Healed", (Data) => { return Data.Healed.ToString(GetIntCommas()); }, (Data) => { return Data.Healed.ToString(); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("Healed%", new CombatantData.ColumnDef("Healed%", false, "VARCHAR(4)", "HealedPerc", (Data) => { return Data.HealedPercent; }, (Data) => { return Data.HealedPercent; }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("CritHeals", new CombatantData.ColumnDef("CritHeals", false, "INT", "CritHeals", (Data) => { return Data.CritHeals.ToString(GetIntCommas()); }, (Data) => { return Data.CritHeals.ToString(); }, (Left, Right) => { return Left.CritHeals.CompareTo(Right.CritHeals); }));
            CombatantData.ColumnDefs.Add("Heals", new CombatantData.ColumnDef("Heals", false, "INT", "Heals", (Data) => { return Data.Heals.ToString(GetIntCommas()); }, (Data) => { return Data.Heals.ToString(); }, (Left, Right) => { return Left.Heals.CompareTo(Right.Heals); }));
            CombatantData.ColumnDefs.Add("DPS", new CombatantData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            CombatantData.ColumnDefs.Add("EncDPS", new CombatantData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.EncDPS.CompareTo(Right.EncDPS); }));
            //CombatantData.ColumnDefs.Add("EncHPS", new CombatantData.ColumnDef("EncHPS", true, "DOUBLE", "EncHPS", (Data) => { return Data.EncHPS.ToString(); }, (Data) => { return Data.EncHPS.ToString(usCulture); }, (Left, Right) => { return Left.EncHPS.CompareTo(Right.EncHPS); }));
            CombatantData.ColumnDefs.Add("Hits", new CombatantData.ColumnDef("Hits", false, "INT", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
            CombatantData.ColumnDefs.Add("CritHits", new CombatantData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
            CombatantData.ColumnDefs.Add("Avoids", new CombatantData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
            CombatantData.ColumnDefs.Add("Misses", new CombatantData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
            CombatantData.ColumnDefs.Add("Swings", new CombatantData.ColumnDef("Swings", false, "INT", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
            CombatantData.ColumnDefs.Add("HealingTaken", new CombatantData.ColumnDef("HealingTaken", false, "BIGINT", "HealsTaken", (Data) => { return Data.HealsTaken.ToString(GetIntCommas()); }, (Data) => { return Data.HealsTaken.ToString(); }, (Left, Right) => { return Left.HealsTaken.CompareTo(Right.HealsTaken); }));
            CombatantData.ColumnDefs.Add("DamageTaken", new CombatantData.ColumnDef("DamageTaken", true, "BIGINT", "DamageTaken", (Data) => { return Data.DamageTaken.ToString(GetIntCommas()); }, (Data) => { return Data.DamageTaken.ToString(); }, (Left, Right) => { return Left.DamageTaken.CompareTo(Right.DamageTaken); }));
            CombatantData.ColumnDefs.Add("Deaths", new CombatantData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.Deaths.ToString(GetIntCommas()); }, (Data) => { return Data.Deaths.ToString(); }, (Left, Right) => { return Left.Deaths.CompareTo(Right.Deaths); }));
            
            CombatantData.ColumnDefs.Add("CritTypes", new CombatantData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", CombatantDataGetCritTypes, CombatantDataGetCritTypes, (Left, Right) => { return CombatantDataGetCritTypes(Left).CompareTo(CombatantDataGetCritTypes(Right)); }));

            CombatantData.ColumnDefs["Damage"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["Damage%"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["Healed"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
            CombatantData.ColumnDefs["Healed%"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
            CombatantData.ColumnDefs["DPS"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["DamageTaken"].GetCellForeColor = (Data) => { return Color.DarkOrange; };

            CombatantData.OutgoingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Auto-Attack (Out)", new CombatantData.DamageTypeDef("Auto-Attack (Out)", -1, Color.DarkGoldenrod)},
            {"Skill/Ability (Out)", new CombatantData.DamageTypeDef("Skill/Ability (Out)", -1, Color.DarkOrange)},
            {"Outgoing Damage", new CombatantData.DamageTypeDef("Outgoing Damage", 0, Color.Orange)},
            {"Direct Damage Spell (Out)", new CombatantData.DamageTypeDef("Direct Damage Spell (Out)", -1, Color.LightCyan) },
            {"Damage Over Time Spell (Out)", new CombatantData.DamageTypeDef("Damage Over Time Spell (Out)", -1, Color.ForestGreen) },
            {"Bane (Out)", new CombatantData.DamageTypeDef("Bane (Out)", -1, Color.LightGreen) },
            {"Instant Healed (Out)", new CombatantData.DamageTypeDef("Instant Healed (Out)", 1, Color.Blue)},
            {"Heal Over Time (Out)", new CombatantData.DamageTypeDef("Heal Over Time (Out)", 1, Color.DarkBlue)},
                {"Pet Melee (Out)", new CombatantData.DamageTypeDef("Pet Melee (Out)", -1, Color.GreenYellow)},
            {"All Outgoing (Ref)", new CombatantData.DamageTypeDef("All Outgoing (Ref)", 0, Color.Black)}
        };
            CombatantData.IncomingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Incoming Damage", new CombatantData.DamageTypeDef("Incoming Damage", -1, Color.Red)},
            {"Incoming NonMelee Damage", new CombatantData.DamageTypeDef("Incoming NonMelee Damage", -1 , Color.DarkRed) },
            {"Direct Damage Spell (Inc)", new CombatantData.DamageTypeDef("Direct Damage Spell (Inc)", -1, Color.LightCyan) },
            {"Damage Over Time Spell (Inc)", new CombatantData.DamageTypeDef("Damage Over Time Spell (Inc)", -1, Color.Orchid) },
            {"Instant Healed (Inc)",new CombatantData.DamageTypeDef("Instant Healed (Inc)", 1, Color.LimeGreen)},
            {"Heal Over Time (Inc)",new CombatantData.DamageTypeDef("Heal Over Time (Inc)", 1, Color.DarkGreen)},
            {"All Incoming (Ref)",new CombatantData.DamageTypeDef("All Incoming (Ref)", 0, Color.Black)}
        };
            CombatantData.SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>
        {
            {EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Auto-Attack (Out)", "Outgoing Damage" } },
            {EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Skill/Ability (Out)", "Outgoing Damage" } },
            {EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Direct Damage Spell (Out)" , "Outgoing Damage"} },
            {EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string>{"Damage Over Time Spell (Out)", "Outgoing Damage"} },
            {EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Instant Healed (Out)" } },
            {EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Heal Over Time (Out)" } },
            {EverQuestSwingType.PetMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Pet Melee (Out)" } }
        };
            CombatantData.SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>
        {
            {EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Incoming Damage" } },
            {EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Incoming NonMelee Damage" } },
            {EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Direct Damage Spell (Inc)" } },
            {EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Instant Healed (Inc)" } },
            {EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> {"Damage Over Time Spell (Inc)"} },
            {EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Heal Over Time (Inc)" } },
        };

            CombatantData.DamageSwingTypes = new List<int> {
                EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.Bane.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.PetMelee.GetEverQuestSwingTypeExtensionIntValue() };
            CombatantData.HealingSwingTypes = new List<int> { EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue() };

            CombatantData.ExportVariables.Clear();
            CombatantData.ExportVariables.Add("n", new CombatantData.TextExportFormatter("n", "New Line", "Formatting after this element will appear on a new line.", (Data, Extra) => { return "\n"; }));
            CombatantData.ExportVariables.Add("t", new CombatantData.TextExportFormatter("t", "Tab Character", "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)", (Data, Extra) => { return "\t"; }));
            
            DamageTypeData.ColumnDefs.Clear();
            DamageTypeData.ColumnDefs.Add("EncId", new DamageTypeData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.EncId; }));
            DamageTypeData.ColumnDefs.Add("Combatant", new DamageTypeData.ColumnDef("Combatant", false, "VARCHAR(64)", "Combatant", (Data) => { return Data.Parent.Name; }, (Data) => { return Data.Parent.Name; }));
            DamageTypeData.ColumnDefs.Add("Type", new DamageTypeData.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }));
            DamageTypeData.ColumnDefs.Add("StartTime", new DamageTypeData.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("EndTime", new DamageTypeData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("Duration", new DamageTypeData.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            DamageTypeData.ColumnDefs.Add("Damage", new DamageTypeData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
            DamageTypeData.ColumnDefs.Add("EncDPS", new DamageTypeData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("CharDPS", new DamageTypeData.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(); }, (Data) => { return Data.CharDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("DPS", new DamageTypeData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Average", new DamageTypeData.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(); }, (Data) => { return Data.Average.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Median", new DamageTypeData.ColumnDef("Median", false, "BIGINT", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MinHit", new DamageTypeData.ColumnDef("MinHit", true, "BIGINT", "MinHit", (Data) => { return Data.MinHit.ToString(GetIntCommas()); }, (Data) => { return Data.MinHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MaxHit", new DamageTypeData.ColumnDef("MaxHit", true, "BIGINT", "MaxHit", (Data) => { return Data.MaxHit.ToString(GetIntCommas()); }, (Data) => { return Data.MaxHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Hits", new DamageTypeData.ColumnDef("Hits", true, "INT", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }));
            DamageTypeData.ColumnDefs.Add("CritHits", new DamageTypeData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Avoids", new DamageTypeData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Misses", new DamageTypeData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Swings", new DamageTypeData.ColumnDef("Swings", true, "INT", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }));
            DamageTypeData.ColumnDefs.Add("ToHit", new DamageTypeData.ColumnDef("ToHit", false, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(); }, (Data) => { return Data.ToHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("AvgDelay", new DamageTypeData.ColumnDef("AvgDelay", false, "FLOAT", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(); }, (Data) => { return Data.AverageDelay.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Crit%", new DamageTypeData.ColumnDef("Crit%", false, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }));
            DamageTypeData.ColumnDefs.Add("CritTypes", new DamageTypeData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", DamageTypeDataGetCritTypes, DamageTypeDataGetCritTypes));


            AttackType.ColumnDefs.Clear();
            AttackType.ColumnDefs.Add("EncId", new AttackType.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.Parent.EncId; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Attacker", new AttackType.ColumnDef("Attacker", false, "VARCHAR(64)", "Attacker", (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Victim", new AttackType.ColumnDef("Victim", false, "VARCHAR(64)", "Victim", (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("SwingType", new AttackType.ColumnDef("SwingType", false, "TINYINT", "SwingType", GetAttackTypeSwingType, GetAttackTypeSwingType, (Left, Right) => { return GetAttackTypeSwingType(Left).CompareTo(GetAttackTypeSwingType(Right)); }));
            AttackType.ColumnDefs.Add("Type", new AttackType.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }, (Left, Right) => { return Left.Type.CompareTo(Right.Type); }));
            AttackType.ColumnDefs.Add("StartTime", new AttackType.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            AttackType.ColumnDefs.Add("EndTime", new AttackType.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            AttackType.ColumnDefs.Add("Duration", new AttackType.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            AttackType.ColumnDefs.Add("Damage", new AttackType.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            AttackType.ColumnDefs.Add("EncDPS", new AttackType.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.EncDPS.CompareTo(Right.EncDPS); }));
            AttackType.ColumnDefs.Add("CharDPS", new AttackType.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(); }, (Data) => { return Data.CharDPS.ToString(usCulture); }, (Left, Right) => { return Left.CharDPS.CompareTo(Right.CharDPS); }));
            AttackType.ColumnDefs.Add("DPS", new AttackType.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            AttackType.ColumnDefs.Add("Average", new AttackType.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(); }, (Data) => { return Data.Average.ToString(usCulture); }, (Left, Right) => { return Left.Average.CompareTo(Right.Average); }));
            AttackType.ColumnDefs.Add("Median", new AttackType.ColumnDef("Median", true, "BIGINT", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }, (Left, Right) => { return Left.Median.CompareTo(Right.Median); }));
            AttackType.ColumnDefs.Add("Variance", new AttackType.ColumnDef("Variance", true, "DOUBLE", "Variance", AttackTypeGetVariance, AttackTypeGetVariance, (Left, Right) => { return AttackTypeGetVariance(Left).CompareTo(AttackTypeGetVariance(Right)); }));
            AttackType.ColumnDefs.Add("CritTypes", new AttackType.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", AttackTypeGetCritTypes, AttackTypeGetCritTypes, (Left, Right) => { return AttackTypeGetCritTypes(Left).CompareTo(AttackTypeGetCritTypes(Right)); }));


            MasterSwing.ColumnDefs.Clear();
            MasterSwing.ColumnDefs.Add("EncId", new MasterSwing.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.ParentEncounter.EncId; }, (Left, Right) => { return 0; }));
            MasterSwing.ColumnDefs.Add("Time", new MasterSwing.ColumnDef("Time", true, "TIMESTAMP", "STime", (Data) => { return Data.Time.ToString("T"); }, (Data) => { return Data.Time.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
            MasterSwing.ColumnDefs.Add("RelativeTime", new MasterSwing.ColumnDef("RelativeTime", true, "FLOAT", "RelativeTime", (Data) => { return Data.ParentEncounter != null ? (Data.Time - Data.ParentEncounter.StartTime).ToString("g") : String.Empty; }, (Data) => { return Data.ParentEncounter != null ? (Data.Time - Data.ParentEncounter.StartTime).TotalSeconds.ToString(usCulture) : String.Empty; }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
            MasterSwing.ColumnDefs.Add("Attacker", new MasterSwing.ColumnDef("Attacker", true, "VARCHAR(64)", "Attacker", (Data) => { return Data.Attacker; }, (Data) => { return Data.Attacker; }, (Left, Right) => { return Left.Attacker.CompareTo(Right.Attacker); }));
            MasterSwing.ColumnDefs.Add("SwingType", new MasterSwing.ColumnDef("SwingType", false, "TINYINT", "SwingType", (Data) => { return Data.SwingType.ToString(); }, (Data) => { return Data.SwingType.ToString(); }, (Left, Right) => { return Left.SwingType.CompareTo(Right.SwingType); }));
            MasterSwing.ColumnDefs.Add("AttackType", new MasterSwing.ColumnDef("AttackType", true, "VARCHAR(64)", "AttackType", (Data) => { return Data.AttackType; }, (Data) => { return Data.AttackType; }, (Left, Right) => { return Left.AttackType.CompareTo(Right.AttackType); }));
            MasterSwing.ColumnDefs.Add("DamageType", new MasterSwing.ColumnDef("DamageType", true, "VARCHAR(64)", "DamageType", (Data) => { return Data.DamageType; }, (Data) => { return Data.DamageType; }, (Left, Right) => { return Left.DamageType.CompareTo(Right.DamageType); }));
            MasterSwing.ColumnDefs.Add("Victim", new MasterSwing.ColumnDef("Victim", true, "VARCHAR(64)", "Victim", (Data) => { return Data.Victim; }, (Data) => { return Data.Victim; }, (Left, Right) => { return Left.Victim.CompareTo(Right.Victim); }));
            MasterSwing.ColumnDefs.Add("DamageNum", new MasterSwing.ColumnDef("DamageNum", false, "BIGINT", "Damage", (Data) => { return ((long)Data.Damage).ToString(); }, (Data) => { return ((long)Data.Damage).ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            MasterSwing.ColumnDefs.Add("Damage", new MasterSwing.ColumnDef("Damage", true, "VARCHAR(128)", "DamageString", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            MasterSwing.ColumnDefs.Add("Critical", new MasterSwing.ColumnDef("Critical", false, "BOOLEAN", "Critical", (Data) => { return Data.Critical.ToString(); }, (Data) => { return Data.Critical.ToString(usCulture)[0].ToString(); }, (Left, Right) => { return Left.Critical.CompareTo(Right.Critical); }));
            MasterSwing.ColumnDefs.Add("Special", new MasterSwing.ColumnDef("Special", true, "VARCHAR(90)", "Special", (Data) => { return Data.Special; }, (Data) => { return Data.Special; }, (Left, Right) => { return Left.Special.CompareTo(Left.Special); }));
            MasterSwing.ColumnDefs.Add("Overheal", new MasterSwing.ColumnDef("Overheal", true, "BIGINT", "Overheal", (Data) => { return Data.Tags.ContainsKey("overheal") ? ((long)Data.Tags["overheal"]).ToString() : string.Empty; }, (Data) => { return Data.Tags.ContainsKey("overheal") ? ((long)Data.Tags["overheal"]).ToString() : string.Empty; }, (Left, Right) =>
            {
                return (Left.Tags.ContainsKey("overheal") && Right.Tags.ContainsKey("overheal")) ? ((long)Left.Tags["overheal"]).CompareTo((long)Right.Tags["overheal"]) : 0;
            }));

            foreach (KeyValuePair<string, MasterSwing.ColumnDef> pair in MasterSwing.ColumnDefs)
                pair.Value.GetCellForeColor = (Data) => { return GetSwingTypeColor(Data.SwingType); };

            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
        }

        private string AttackTypeGetVariance(AttackType Data)
        {
            if (populationVariance && Data.Items.Count > 1)
                return Data.Items.Sum((item) =>
                {
                    return Math.Pow(Data.Average - item.Damage, 2.0) / Data.Items.Count;
                }).ToString();

            else if (!populationVariance && Data.Items.Count > 0)
                return Data.Items.Sum((item) =>
                {
                    return Math.Pow(Data.Average - item.Damage, 2.0) / (Data.Items.Count - 1);
                }).ToString();
            else
                return String.Empty;
        }

        private string CombatantDataGetCritTypes(CombatantData Data)
        {
            AttackType at;
            if (Data.AllOut.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out at))
            {
                return AttackTypeGetCritTypes(at);
            }
            else
                return String.Empty;
        }

        private string DamageTypeDataGetCritTypes(DamageTypeData Data)
        {
            AttackType at;
            if (Data.Items.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out at))
            {
                return AttackTypeGetCritTypes(at);
            }
            else
                return String.Empty;
        }

        private string AttackTypeGetCritTypes(AttackType Data)
        {
            int specialCripplingBlow = 0;
            int specialLocked = 0;
            int specialCritical = 0;
            int specialStrikethrough = 0;
            int specialRiposte = 0;
            int specialNonDefined = 0;
            int specialFlurry = 0;
            int specialLucky = 0;
            int specialDoubleBowShot = 0;
            int specialTwincast = 0;
            int count = Data.Items.Count;
            if (count.Equals(0))
                return String.Empty;
            specialCritical = Data.Items.Where((critital) =>
            {
                return critital.Special.Contains(SpecialCritical);
            }).Count();
            specialFlurry = Data.Items.Where((flurry) =>
            {
                return flurry.Special.Contains(SpecialFlurry);
            }).Count();
            specialLucky = Data.Items.Where((lucky) =>
            {
                return lucky.Special.Contains(SpecialLucky);
            }).Count();
            specialCripplingBlow = Data.Items.Where((cripplingBlow) =>
            {
                return cripplingBlow.Special.Contains(SpecialCripplingBlow);
            }).Count();
            specialLocked = Data.Items.Where((locked) =>
            {
                return locked.Special.Contains(SpecialLocked);
            }).Count();
            specialStrikethrough = Data.Items.Where((srikethrough) =>
            {
                return srikethrough.Special.Contains(SpecialStrikethrough);
            }).Count();
            specialRiposte = Data.Items.Where((riposte) =>
            {
                return riposte.Special.Contains(SpecialRiposte);
            }).Count();
            specialDoubleBowShot = Data.Items.Where((doubleBowShot) =>
            {
                return doubleBowShot.Special.Contains(SpecialDoubleBowShot);
            }).Count();
            specialTwincast = Data.Items.Where((twincast) =>
            {
                return twincast.Special.Contains(SpecialTwincast);
            }).Count();
            specialNonDefined = Data.Items.Where((nondefined) =>
            {
                return !nondefined.Special.Contains(SpecialTwincast) &&
                    !nondefined.Special.Contains(SpecialDoubleBowShot) &&
                    !nondefined.Special.Contains(SpecialRiposte) &&
                    !nondefined.Special.Contains(SpecialRiposte) &&
                    !nondefined.Special.Contains(SpecialCripplingBlow) &&
                    !nondefined.Special.Contains(SpecialLucky) &&
                    !nondefined.Special.Contains(SpecialFlurry) &&
                    !nondefined.Special.Contains(SpecialCritical)
                    && nondefined.Special.Length > ActGlobals.ActLocalization.LocalizationStrings["specialAttackTerm-none"].DisplayedText.Length;

            }).Count();

            float specialCripplingBlowPerc = ((float)specialCripplingBlow / (float)count) * 100f;
            float specialLockedPerc = ((float)specialLocked / (float)count) * 100f;
            float specialCriticalPerc = ((float)specialCritical / (float)count) * 100f;
            float specialNonDefinedPerc = ((float)specialNonDefined / (float)count) * 100f;
            float specialStrikethroughPerc = ((float)specialStrikethrough / (float)count) * 100f;
            float specialRipostePerc = ((float)specialRiposte / (float)count) * 100f;
            float specialFlurryPerc = ((float)specialFlurry / (float)count) * 100f;
            float speicalLuckyPerc = ((float)specialLucky / (float)count) * 100f;
            float specialDoubleBowShotPerc = ((float)specialDoubleBowShot / (float)count) * 100f;
            float specialTwincastPerc = ((float)specialTwincast / (float)count) * 100f;

            return $"{specialCripplingBlowPerc:000.0}%CB-{specialLockedPerc:000.0}%Locked-{specialCriticalPerc:000.0}%C-{specialStrikethroughPerc:000.0}%S-{specialRipostePerc:000.0}%R-{specialFlurryPerc:000.0}%F-{speicalLuckyPerc:000.0}%Lucky-{specialDoubleBowShotPerc:000.0}%DB-{specialTwincastPerc:000.0}%TC-{specialNonDefinedPerc:000.0}%ND";
        }

        private Color GetSwingTypeColor(int SwingType)
        {
            switch (SwingType)
            {
                case 1:
                case 2:
                    return Color.Crimson;
                case 3:
                    return Color.Blue;
                case 4:
                    return Color.DarkRed;
                case 5:
                    return Color.DarkOrange;
                case 8:
                    return Color.DarkOrchid;
                case 9:
                    return Color.DodgerBlue;
                default:
                    return Color.Black;
            }
        }

        private string GetAttackTypeSwingType(AttackType Data)
        {
            int? swingType = null;
            List<String> swingTypes = Data.Items.Select(o => o.AttackType).Distinct().ToList();
            List<MasterSwing> cachedItems = new List<MasterSwing>();
            for (int i = 0; i < Data.Items.Count; i++)
            {
                MasterSwing s = Data.Items[i];
                if (swingTypes.Contains(Data.Items[i].SwingType.ToString()) == false)
                    swingTypes.Add(Data.Items[i].SwingType.ToString());
            }
            if (swingTypes.Count == 1)
                swingType = Data.Items[0].SwingType;
            if (swingType == null)
                return String.Empty;
            else
               return swingType == null ? String.Empty : swingType.ToString();
        }

        private void varianceChkBx_CheckedChanged(object sender, EventArgs e)
        {
            Task task = new Task(() =>
            {
                this.populationVariance = (sender as CheckBox).Checked;
            });
        }
    }
}
