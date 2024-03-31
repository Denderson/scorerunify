using BepInEx;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;
using HUD;
using JollyCoop;
using JollyCoop.JollyMenu;
using MoreSlugcats;
using Music;

namespace scorerunify
{
    [BepInPlugin("scorerunify", "Scorerunify", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private OptionsMenu optionsMenuInstance;
        private bool initialized;
        public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (this.initialized)
            {
                return;
            }
            this.initialized = true;
            optionsMenuInstance = new OptionsMenu(this);
            try
            {
                MachineConnector.SetRegisteredOI("scorerunify", optionsMenuInstance);
            } 
            catch (Exception ex)
            {
                Debug.Log($"scorerunify: Hook_OnModsInit options failed init error {optionsMenuInstance}{ex}");
                Logger.LogError(ex);
                Logger.LogMessage("WHOOPS");
            }
        }
        public int cyclesInNegatives(global::Player self)
        {
            if (self.room.game.session is StoryGameSession session)
            {
                int cycleCount = OptionsMenu.startingCycles.Value - session.saveState.cycleNumber;
                if (session.saveState.miscWorldSaveData.SSaiConversationsHad > 0)
                {
                    cycleCount += OptionsMenu.pebblesCycles.Value;
                }
                if (session.saveState.miscWorldSaveData.EverMetMoon)
                {
                    cycleCount += OptionsMenu.moonCycles.Value;
                }
                return -(cycleCount);
            }
            return -1;
        }
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += this.RainWorld_OnModsInit;
            On.Player.ctor += this.Player_ctor;
            On.Player.Update += this.Player_Update;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += this.SlugcatSelectMenu_UpdateStartButtonText;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += this.SlugcatSelectMenu_ContinueStartedGame;
            On.RainWorldGame.GoToRedsGameOver += this.RainWorldGame_GoToRedsGameOver;
            On.RainWorldGame.GameOver += this.RainWorldGame_GameOver;
        }
        private void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if (self.IsStorySession)
            {
                int negativeCycles = OptionsMenu.startingCycles.Value - (self.session as StoryGameSession).saveState.cycleNumber;
                if ((self.session as StoryGameSession).saveState.miscWorldSaveData.SSaiConversationsHad > 0)
                {
                    negativeCycles += OptionsMenu.pebblesCycles.Value;
                }
                if ((self.session as StoryGameSession).saveState.miscWorldSaveData.EverMetMoon)
                {
                    negativeCycles += OptionsMenu.moonCycles.Value;
                }
                if (negativeCycles <= 0)
                {
                    self.GoToRedsGameOver();
                    return;
                }
            }
            orig(self, dependentOnGrasp);
        }
        private void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (self.manager.upcomingProcess != null)
            {
                return;
            }
            if (self.manager.musicPlayer != null)
            {
                self.manager.musicPlayer.FadeOutAllSongs(20f);
            }
            if (self.Players[0].realizedCreature != null && (self.Players[0].realizedCreature as Player).redsIllness != null)
            {
                (self.Players[0].realizedCreature as Player).redsIllness.fadeOutSlow = true;
            }
            if (self.GetStorySession.saveState.saveStateNumber != SlugcatStats.Name.Red)
            {
                if (ModManager.MSC && (self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Gourmand || self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer || self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear || self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet))
                {
                    self.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
                }
                else
                {
                    self.GetStorySession.saveState.deathPersistentSaveData.ascended = true;
                }
                if (ModManager.CoopAvailable)
                {
                    int num = 0;
                    using (IEnumerator<Player> enumerator = (from x in self.session.game.Players select x.realizedCreature as Player).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Player player = enumerator.Current;
                            self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                            num++;
                        }
                        self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
                    }
                }
                self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);
            }
            orig(self);
        }
        private void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            orig(self);
            if (!self.restartChecked && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == SlugcatStats.Name.Yellow && self.saveGameData.ContainsKey(SlugcatStats.Name.Yellow) && self.saveGameData[SlugcatStats.Name.Yellow] != null && (self.saveGameData[SlugcatStats.Name.Yellow].ascended || (ModManager.MSC && self.saveGameData[SlugcatStats.Name.Yellow].altEnding)))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == SlugcatStats.Name.White && self.saveGameData.ContainsKey(SlugcatStats.Name.White) && self.saveGameData[SlugcatStats.Name.White] != null && (self.saveGameData[SlugcatStats.Name.White].ascended || (ModManager.MSC && self.saveGameData[SlugcatStats.Name.White].altEnding)))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Gourmand) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand].altEnding))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Artificer) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer].altEnding))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Spear) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].altEnding))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Rivulet) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet].altEnding))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Saint) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint].ascended))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel].ascended))
            {
                self.startButton.menuLabel.text = "STATISTICS";
                return;
            }
        }
        private void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if (!self.restartChecked && storyGameCharacter == SlugcatStats.Name.Yellow && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == SlugcatStats.Name.Yellow && self.saveGameData.ContainsKey(SlugcatStats.Name.Yellow) && self.saveGameData[SlugcatStats.Name.Yellow] != null && (self.saveGameData[SlugcatStats.Name.Yellow].ascended || (ModManager.MSC && self.saveGameData[SlugcatStats.Name.Yellow].altEnding)))
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(SlugcatStats.Name.Yellow, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && storyGameCharacter == SlugcatStats.Name.White && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == SlugcatStats.Name.White && self.saveGameData.ContainsKey(SlugcatStats.Name.White) && self.saveGameData[SlugcatStats.Name.White] != null && (self.saveGameData[SlugcatStats.Name.White].ascended || self.saveGameData[SlugcatStats.Name.White].altEnding))
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(SlugcatStats.Name.White, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Gourmand) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Gourmand].altEnding) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Gourmand, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Artificer) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Artificer].altEnding) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Artificer, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Spear) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].altEnding) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Spear, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Rivulet) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet].ascended || self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Rivulet].altEnding) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Rivulet, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Saint) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint].ascended) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Saint, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            if (!self.restartChecked && ModManager.MSC && self.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel) && self.slugcatPages[self.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel && self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] != null && (self.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel].ascended) && storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                return;
            }
            orig(self, storyGameCharacter);
        }
        private void Player_Update(On.Player.orig_Update orig, global::Player self, bool eu)
        {
            bool flag = self.slugcatStats.name != global::SlugcatStats.Name.Red && self.redsIllness != null && cyclesInNegatives(self) >= 0;
            if (flag)
            {
                self.redsIllness.Update();
            }
            orig(self, eu);
        }
        private void Player_ctor(On.Player.orig_ctor orig, global::Player self, global::AbstractCreature abstractCreature, global::World world)
        {
            orig(self, abstractCreature, world);
            bool flag = !self.playerState.isGhost && (self.redsIllness == null || self.redsIllness.cycle <= cyclesInNegatives(self)) && cyclesInNegatives(self) >= 0;
            if (flag)
            {
                self.redsIllness = new global::RedsIllness(self, cyclesInNegatives(self));
            }
        }
    }
    public class OptionsMenu : OptionInterface
    {
        public OptionsMenu(Plugin plugin)
        {
            startingCycles = this.config.Bind<int>("scorerunifyStarting_Slider", 19);
            pebblesCycles = this.config.Bind<int>("scorerunifyPebbles_Slider", 5);
            moonCycles = this.config.Bind<int>("scorerunifyMoon_Slider", 0);
        }
        public override void Initialize()
        {
            var opTab1 = new OpTab(this, "Default Canvas");
            this.Tabs = new[] { opTab1 };

            UIelement[] UIArrayElements1 = new UIelement[]
            {
                new OpSlider(startingCycles, new Vector2(0, 550), 250){max = 100, hideLabel = false},
            };
            opTab1.AddItems(UIArrayElements1);
            UIelement[] UIArrayElements2 = new UIelement[]
            {
                new OpSlider(pebblesCycles, new Vector2(0, 475), 250) { max = 100, hideLabel = false },
            };
            opTab1.AddItems(UIArrayElements2);
            UIelement[] UIArrayElements3 = new UIelement[]
            {
                new OpSlider(moonCycles, new Vector2(0, 425), 250) { max = 100, hideLabel = false },
            };
            opTab1.AddItems(UIArrayElements3);
        }
        public static Configurable<int> startingCycles;
        public static Configurable<int> pebblesCycles;
        public static Configurable<int> moonCycles;

    }
}