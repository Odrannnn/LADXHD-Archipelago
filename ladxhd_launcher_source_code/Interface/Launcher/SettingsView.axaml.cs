using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LADXHD_Launcher;

public partial class SettingsView : UserControl, IControllerPage
{
    private MainWindow? _parent;

    public SettingsView() { InitializeComponent(); }

    public SettingsView(MainWindow parent)
    {
        InitializeComponent();
        _parent = parent;
    }

    public void FocusInitial() => c_CurrentLanguage.Focus(Avalonia.Input.NavigationMethod.Directional);

    public void OnCancel()
    {
        _parent?.NavigateTo(_parent.HomeView);
        SoundPlayer.PlayXnbSound(SoundPlayer.SoundClose);
    }
    public void FocusBack() => BackButton.Focus(Avalonia.Input.NavigationMethod.Directional);

    private void DamageFactorChanged(object sender, NumericUpDownValueChangedEventArgs e)
    {
        // Translate the damage factor to what the game expects.
        if (n_DamageFactor.Value == null) return;
        decimal rounded = Math.Round(n_DamageFactor.Value.Value * 4) / 4;
        if (n_DamageFactor.Value != rounded)
            n_DamageFactor.Value = rounded;
    }

    public void LoadValues(int maxGameScale = 21)
    {
        // Suppress the sound effects so the checkbox sound doesn't fire a bunch of times.
        SoundPlayer.SuppressSound = true;

        // Update the maximum game scale from the "advanced" file.
        n_GameScale.Maximum = (decimal)(maxGameScale + 1);

        // Game Settings
        c_CurrentLanguage.SelectedIndex    = GameSettings.CurrentLanguage;
        c_CurrentSubLanguage.SelectedIndex = GameSettings.CurrentSubLanguage;
        x_ClassicSword.IsChecked           = GameSettings.ClassicSword;
        x_StoreSavePos.IsChecked           = GameSettings.StoreSavePos;
        x_Autosave.IsChecked               = GameSettings.Autosave;
        x_ItemsOnRight.IsChecked           = GameSettings.ItemsOnRight;
        x_HideAchievement.IsChecked        = GameSettings.HideAchievement;

        // Redux Settings
        x_VarWidthFont.IsChecked           = GameSettings.VarWidthFont;
        x_NoHelperText.IsChecked           = GameSettings.NoHelperText;
        x_DialogSkip.IsChecked             = GameSettings.DialogSkip;
        x_Uncensored.IsChecked             = GameSettings.Uncensored;
        x_Unmissables.IsChecked            = GameSettings.Unmissables;
        x_PhotosColor.IsChecked            = GameSettings.PhotosColor;
        c_MapTeleport.SelectedIndex        = GameSettings.MapTeleport;
        x_NoAnimalDamage.IsChecked         = GameSettings.NoAnimalDamage;

        // Camera Settings
        c_CameraMode.SelectedIndex         = GameSettings.CameraMode;
        c_ClassicBorder.SelectedIndex      = GameSettings.ClassicBorder;
        n_ClassicAlpha.Value               = (decimal)GameSettings.ClassicAlpha;
        c_ClassicBias.SelectedIndex        = GameSettings.ClassicBias;
        x_ClassicScaling.IsChecked         = GameSettings.ClassicScaling;
        x_SmoothCamera.IsChecked           = GameSettings.SmoothCamera;

        // Custom Camera Settings
        x_ClassicOverworld.IsChecked       = GameSettings.ClassicOverworld;
        x_ClassicHouses.IsChecked          = GameSettings.ClassicHouses;
        x_ClassicCaves.IsChecked           = GameSettings.ClassicCaves;
        x_ClassicDungeons.IsChecked        = GameSettings.ClassicDungeons;
        x_ClassicCastle.IsChecked          = GameSettings.ClassicCastle;
        x_ClassicEgg.IsChecked             = GameSettings.ClassicEgg;
        x_Classic2DMaps.IsChecked          = GameSettings.Classic2DMaps;
        x_ClassicBosses.IsChecked          = GameSettings.ClassicBosses;

        // Video Settings
        n_GameScale.Value                  = (decimal)GameSettings.GameScale;
        n_UiScale.Value                    = (decimal)GameSettings.UiScale;
        x_VerticalSync.IsChecked           = GameSettings.VerticalSync;
        x_OpaqueHudBg.IsChecked            = GameSettings.OpaqueHudBg;
        x_PixelSnapping.IsChecked          = GameSettings.PixelSnapping;
        x_PixelSnapping.IsChecked          = GameSettings.PixelGrid;
        c_ScreenMode.SelectedIndex         = GameSettings.ScreenMode;
        x_EpilepsySafe.IsChecked           = GameSettings.EpilepsySafe;

        // Graphics Settings
        x_EnableShadows.IsChecked          = GameSettings.EnableShadows;
        x_FogEffects.IsChecked             = GameSettings.FogEffects;
        x_GlobalLights.IsChecked           = GameSettings.GlobalLights;
        x_ObjectLights.IsChecked           = GameSettings.ObjectLights;
        x_ScreenShake.IsChecked            = GameSettings.ScreenShake;
        x_ExScreenShake.IsChecked          = GameSettings.ExScreenShake;
        x_ClassicSprites.IsChecked         = GameSettings.ClassicSprites;
        n_SeqScaleAmplify.Value            = (decimal)GameSettings.SeqScaleAmplify;

        // Audio Settings
        n_MusicVolume.Value                = (decimal)GameSettings.MusicVolume;
        n_EffectVolume.Value               = (decimal)GameSettings.EffectVolume;
        x_ClassicMusic.IsChecked           = GameSettings.ClassicMusic;
        x_MuteInactive.IsChecked           = GameSettings.MuteInactive;
        x_HeartBeep.IsChecked              = GameSettings.HeartBeep;
        x_MutePowerups.IsChecked           = GameSettings.MutePowerups;
        x_MuteAchievement.IsChecked        = GameSettings.MuteAchievement;

        // Control Settings
        n_DeadZone.Value                   = (decimal)GameSettings.DeadZone;
        c_Controller.SelectedIndex         = GameSettings.Controller switch
        {
            "Playstation" => 1,
            "Nintendo"    => 2,
            _             => 0
        };
        x_TriggersScale.IsChecked          = GameSettings.TriggersScale;
        x_SixButtons.IsChecked             = GameSettings.SixButtons;
        x_OldMovement.IsChecked            = GameSettings.OldMovement;
        x_DigitalAnalog.IsChecked          = GameSettings.DigitalAnalog;
        x_SwapButtons.IsChecked            = GameSettings.SwapButtons;

        // Modifier Settings
        n_EnemyBonusHP.Value               = (decimal)GameSettings.EnemyBonusHP;
        n_MoveSpeedAdded.Value             = (decimal)GameSettings.MoveSpeedAdded;
        n_DamageFactor.Value               = (decimal)GameSettings.DamageFactor / 4;
        n_DmgCooldown.Value                = (decimal)GameSettings.DmgCooldown;
        x_NoHeartDrops.IsChecked           = GameSettings.NoHeartDrops;
        x_NoDamageLaunch.IsChecked         = GameSettings.NoDamageLaunch;
        x_MirrorReflects.IsChecked         = GameSettings.MirrorReflects;

        // Sword Modifier Settings
        x_SwGrabNormal.IsChecked           = GameSettings.SwGrabNormal;
        x_SwGrabWorldItem.IsChecked        = GameSettings.SwGrabWorldItem;
        x_SwGrabFairy.IsChecked            = GameSettings.SwGrabFairy;
        x_SwGrabSmallKey.IsChecked         = GameSettings.SwGrabSmallKey;
        x_SwBoomerang.IsChecked            = GameSettings.SwBoomerang;
        x_SwSmackBombs.IsChecked           = GameSettings.SwSmackBombs;
        x_SwMissileBlock.IsChecked         = GameSettings.SwMissileBlock;
        x_SwBreakPots.IsChecked            = GameSettings.SwBreakPots;
        x_SwBeamShrubs.IsChecked           = GameSettings.SwBeamShrubs;

        // Cheats Settings
        x_ChInvincibility.IsChecked        = GameSettings.ChInvincibility;
        x_ChInfinRupees.IsChecked          = GameSettings.ChInfinRupees;
        x_ChInfinPowder.IsChecked          = GameSettings.ChInfinPowder;
        x_ChInfinBombs.IsChecked           = GameSettings.ChInfinBombs;
        x_ChInfinArrows.IsChecked          = GameSettings.ChInfinArrows;
        x_ChNoClipping.IsChecked           = GameSettings.ChNoClipping;
        x_ChOneHitKills.IsChecked          = GameSettings.ChOneHitKills;
        x_ChGiveAllItems.IsChecked         = GameSettings.ChGiveAllItems;

        // Ok it's fine now.
        SoundPlayer.SuppressSound = false;
    }

    private void SaveValues()
    {
        // Game Settings
        GameSettings.CurrentLanguage    = c_CurrentLanguage.SelectedIndex;
        GameSettings.CurrentSubLanguage = c_CurrentSubLanguage.SelectedIndex;
        GameSettings.ClassicSword       = x_ClassicSword.IsChecked == true;
        GameSettings.StoreSavePos       = x_StoreSavePos.IsChecked == true;
        GameSettings.Autosave           = x_Autosave.IsChecked == true;
        GameSettings.ItemsOnRight       = x_ItemsOnRight.IsChecked == true;
        GameSettings.HideAchievement    = x_HideAchievement.IsChecked == true;

        // Redux Settings
        GameSettings.VarWidthFont       = x_VarWidthFont.IsChecked == true;
        GameSettings.NoHelperText       = x_NoHelperText.IsChecked == true;
        GameSettings.DialogSkip         = x_DialogSkip.IsChecked == true;
        GameSettings.Uncensored         = x_Uncensored.IsChecked == true;
        GameSettings.Unmissables        = x_Unmissables.IsChecked == true;
        GameSettings.PhotosColor        = x_PhotosColor.IsChecked == true;
        GameSettings.MapTeleport        = c_MapTeleport.SelectedIndex;
        GameSettings.NoAnimalDamage     = x_NoAnimalDamage.IsChecked == true;

        // Camera Settings
        GameSettings.CameraMode         = c_CameraMode.SelectedIndex;
        GameSettings.ClassicBorder      = c_ClassicBorder.SelectedIndex;
        GameSettings.ClassicAlpha       = (float)(n_ClassicAlpha.Value ?? 0);
        GameSettings.ClassicBias        = c_ClassicBias.SelectedIndex;
        GameSettings.ClassicScaling     = x_ClassicScaling.IsChecked == true;
        GameSettings.CameraLock         = x_CameraLock.IsChecked == true;
        GameSettings.SmoothCamera       = x_SmoothCamera.IsChecked == true;

        // Custom Camera Settings
        GameSettings.ClassicOverworld   = x_ClassicOverworld.IsChecked == true;
        GameSettings.ClassicHouses      = x_ClassicHouses.IsChecked == true;
        GameSettings.ClassicCaves       = x_ClassicCaves.IsChecked == true;
        GameSettings.ClassicDungeons    = x_ClassicDungeons.IsChecked == true;
        GameSettings.ClassicCastle      = x_ClassicCastle.IsChecked == true;
        GameSettings.ClassicEgg         = x_ClassicEgg.IsChecked == true;
        GameSettings.Classic2DMaps      = x_Classic2DMaps.IsChecked == true;
        GameSettings.ClassicBosses      = x_ClassicBosses.IsChecked == true;

        // Video Settings
        GameSettings.GameScale          = (int)(n_GameScale.Value ?? 0);
        GameSettings.UiScale            = (int)(n_UiScale.Value ?? 0);
        GameSettings.VerticalSync       = x_VerticalSync.IsChecked == true;
        GameSettings.OpaqueHudBg        = x_OpaqueHudBg.IsChecked == true;
        GameSettings.PixelGrid          = x_OpaqueHudBg.IsChecked == true;
        GameSettings.PixelSnapping      = x_PixelSnapping.IsChecked == true;
        GameSettings.ScreenMode         = c_ScreenMode.SelectedIndex;
        GameSettings.EpilepsySafe       = x_EpilepsySafe.IsChecked == true;

        // Graphics Settings
        GameSettings.EnableShadows      = x_EnableShadows.IsChecked == true;
        GameSettings.FogEffects         = x_FogEffects.IsChecked == true;
        GameSettings.GlobalLights       = x_GlobalLights.IsChecked == true;
        GameSettings.ObjectLights       = x_ObjectLights.IsChecked == true;
        GameSettings.ScreenShake        = x_ScreenShake.IsChecked == true;
        GameSettings.ExScreenShake      = x_ExScreenShake.IsChecked == true;
        GameSettings.ClassicSprites     = x_ClassicSprites.IsChecked == true;
        GameSettings.SeqScaleAmplify    = (int)(n_SeqScaleAmplify.Value ?? 0);

        // Audio Settings
        GameSettings.MusicVolume        = (int)(n_MusicVolume.Value ?? 0);
        GameSettings.EffectVolume       = (int)(n_EffectVolume.Value ?? 0);
        GameSettings.ClassicMusic       = x_ClassicMusic.IsChecked == true;
        GameSettings.MuteInactive       = x_MuteInactive.IsChecked == true;
        GameSettings.HeartBeep          = x_HeartBeep.IsChecked == true;
        GameSettings.MutePowerups       = x_MutePowerups.IsChecked == true;
        GameSettings.MuteAchievement    = x_MuteAchievement.IsChecked == true;

        // Control Settings
        GameSettings.DeadZone           = (float)(n_DeadZone.Value ?? 0);
        GameSettings.Controller         = c_Controller.SelectedIndex switch
        {
            1 => "Playstation",
            2 => "Nintendo",
            _ => "XBox"
        };
        GameSettings.TriggersScale      = x_TriggersScale.IsChecked == true;
        GameSettings.SixButtons         = x_SixButtons.IsChecked == true;
        GameSettings.OldMovement        = x_OldMovement.IsChecked == true;
        GameSettings.DigitalAnalog      = x_DigitalAnalog.IsChecked == true;
        GameSettings.SwapButtons        = x_SwapButtons.IsChecked == true;

        // Modifier Settings
        GameSettings.EnemyBonusHP       = (int)(n_EnemyBonusHP.Value ?? 0);
        GameSettings.MoveSpeedAdded     = (float)(n_MoveSpeedAdded.Value ?? 0);
        GameSettings.DamageFactor       = (int)(n_DamageFactor.Value * 4 ?? 0);
        GameSettings.DmgCooldown        = (int)(n_DmgCooldown.Value ?? 0);
        GameSettings.NoHeartDrops       = x_NoHeartDrops.IsChecked == true;
        GameSettings.NoDamageLaunch     = x_NoDamageLaunch.IsChecked == true;
        GameSettings.MirrorReflects     = x_MirrorReflects.IsChecked == true;

        // Sword Modifier Settings
        GameSettings.SwGrabNormal       = x_SwGrabNormal.IsChecked == true;
        GameSettings.SwGrabWorldItem    = x_SwGrabWorldItem.IsChecked == true;
        GameSettings.SwGrabFairy        = x_SwGrabFairy.IsChecked == true;
        GameSettings.SwGrabSmallKey     = x_SwGrabSmallKey.IsChecked == true;
        GameSettings.SwBoomerang        = x_SwBoomerang.IsChecked == true;
        GameSettings.SwSmackBombs       = x_SwSmackBombs.IsChecked == true;
        GameSettings.SwMissileBlock     = x_SwMissileBlock.IsChecked == true;
        GameSettings.SwBreakPots        = x_SwBreakPots.IsChecked == true;
        GameSettings.SwBeamShrubs       = x_SwBeamShrubs.IsChecked == true;

        // Cheats Settings
        GameSettings.ChInvincibility    = x_ChInvincibility.IsChecked == true;
        GameSettings.ChInfinRupees      = x_ChInfinRupees.IsChecked == true;
        GameSettings.ChInfinPowder      = x_ChInfinPowder.IsChecked == true;
        GameSettings.ChInfinBombs       = x_ChInfinBombs.IsChecked == true;
        GameSettings.ChInfinArrows      = x_ChInfinArrows.IsChecked == true;
        GameSettings.ChNoClipping       = x_ChNoClipping.IsChecked == true;
        GameSettings.ChOneHitKills      = x_ChOneHitKills.IsChecked == true;
        GameSettings.ChGiveAllItems     = x_ChGiveAllItems.IsChecked == true;
    }

    private void ResetSettings(int maxGameScale = 21)
    {
        // Game Settings
        GameSettings.MenuBorder       =  0;
        GameSettings.ClassicSword     =  false;
        GameSettings.StoreSavePos     =  false;
        GameSettings.Autosave         =  true;
        GameSettings.LastSavePos      =  0;
        GameSettings.ItemsOnRight     =  false;
        GameSettings.HideAchievement  =  false;

        // Redux Settings
        GameSettings.MapTeleport      =  0;
        GameSettings.VarWidthFont     =  false;
        GameSettings.NoHelperText     =  false;
        GameSettings.DialogSkip       =  false;
        GameSettings.Uncensored       =  false;
        GameSettings.Unmissables      =  false;
        GameSettings.PhotosColor      =  false;
        GameSettings.NoAnimalDamage   =  false;

        // Camera Settings
        GameSettings.CameraMode       =  0;
        GameSettings.ClassicBorder    =  1;
        GameSettings.ClassicAlpha     =  1.00f;
        GameSettings.ClassicBias      =  0;
        GameSettings.ClassicScaling   =  true;
        GameSettings.CameraLock       =  true;
        GameSettings.SmoothCamera     =  true;

        // Custom Camera Settings
        GameSettings.ClassicOverworld = false;
        GameSettings.ClassicHouses    = false;
        GameSettings.ClassicCaves     = false;
        GameSettings.ClassicDungeons  = false;
        GameSettings.ClassicCastle    = false;
        GameSettings.ClassicEgg       = false;
        GameSettings.Classic2DMaps    = false;
        GameSettings.ClassicBosses    = false;

        // Video Settings
        GameSettings.GameScale        =  maxGameScale + 1;
        GameSettings.UiScale          =  11;
        GameSettings.ScreenMode       =  0;
        GameSettings.VerticalSync     =  true;
        GameSettings.OpaqueHudBg      =  false;
        GameSettings.PixelSnapping    =  false;
        GameSettings.PixelGrid        =  false;
        GameSettings.EpilepsySafe     =  false;

        // Graphics Settings
        GameSettings.EnableShadows    =  true;
        GameSettings.FogEffects       =  true;
        GameSettings.GlobalLights     =  true;
        GameSettings.ObjectLights     =  true;
        GameSettings.ScreenShake      =  true;
        GameSettings.ExScreenShake    =  false;
        GameSettings.ClassicSprites   =  false;

        // Audio Settings
        GameSettings.ClassicMusic     =  false;
        GameSettings.MuteInactive     =  true;
        GameSettings.HeartBeep        =  true;
        GameSettings.MutePowerups     =  false;
        GameSettings.MuteAchievement  =  false;

        // Control Settings
        GameSettings.DeadZone         =  0.10f;
        GameSettings.Controller       =  "XBox";
        GameSettings.TriggersScale    =  false;
        GameSettings.SixButtons       =  false;
        GameSettings.SwapButtons      =  false;
        GameSettings.OldMovement      =  false;
        GameSettings.DigitalAnalog    =  false;

        // Modifiers Settings
        GameSettings.EnemyBonusHP    =  0;
        GameSettings.DamageFactor    =  4;
        GameSettings.DmgCooldown     =  16;
        GameSettings.MoveSpeedAdded  =  0;
        GameSettings.NoHeartDrops    =  false;
        GameSettings.NoDamageLaunch  =  false;
        GameSettings.MirrorReflects  =  false;

        // Sword Collection
        GameSettings.SwGrabNormal    =  true;
        GameSettings.SwGrabWorldItem =  false;
        GameSettings.SwGrabFairy     =  false;
        GameSettings.SwGrabSmallKey  =  false;
        GameSettings.SwBoomerang     =  false;
        GameSettings.SwSmackBombs    =  false;
        GameSettings.SwMissileBlock  =  false;
        GameSettings.SwBreakPots     =  false;
        GameSettings.SwBeamShrubs    =  false;

        // Cheats Settings
        GameSettings.ChInvincibility =  false;
        GameSettings.ChInfinRupees   =  false;
        GameSettings.ChInfinPowder   =  false;
        GameSettings.ChInfinBombs    =  false;
        GameSettings.ChInfinArrows   =  false;
        GameSettings.ChNoClipping    =  false;
        GameSettings.ChOneHitKills   =  false;
        GameSettings.ChGiveAllItems  =  false;
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var maxScale = AdvancedSettings.LoadMaxGameScale(AppContext.BaseDirectory);

        // Show a dialog to confirm reset settings.
        var message = "Reset all settings to their default values?";
        if (!await YesNoWindow.ShowAsync("Reset Settings?", message))
            return;

        // Reset the values and update the GUI.
        ResetSettings(maxScale);
        LoadValues(maxScale);

        // Save the reset values to the "settings" file.
        SaveValues();
        GameSettings.Save(AppContext.BaseDirectory);

        // Show the notification that settings were reset and play a sound.
        await System.Threading.Tasks.Task.Delay(250);
        _parent?.ShowNotification(MainWindow.NotificationType.Reset);
        SoundPlayer.PlayXnbSound(SoundPlayer.SoundReset);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save the settings, show a notification, play a sound, go back to main menu.
        SaveValues();
        GameSettings.Save(AppContext.BaseDirectory);
        _parent?.ShowNotification(MainWindow.NotificationType.Save);
        _parent?.NavigateTo(_parent.HomeView);
        SoundPlayer.PlayXnbSound(SoundPlayer.SoundSave);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Return to the main menu page.
        _parent?.NavigateTo(_parent.HomeView);
        SoundPlayer.PlayXnbSound(SoundPlayer.SoundClose);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _parent?.Close();
    }
}