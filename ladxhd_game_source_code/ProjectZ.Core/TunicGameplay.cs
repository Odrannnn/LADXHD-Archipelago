using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ;

/// <summary>
/// Shared default tunic presentation and the telephone equipment order used by
/// gameplay and the lightweight installed-asset renderer.
/// </summary>
public static class TunicGameplay
{
    public static bool IsTelephoneDialog(string dialogName) =>
        string.Equals(dialogName, "ulrira", StringComparison.Ordinal) ||
        string.Equals(
            dialogName, "ulrira_telephone", StringComparison.Ordinal);

    public static Color GetDefaultColor(int cloakType)
    {
        if (cloakType == GameManager.CloakBlue)
            return new Color(24, 132, 255);
        if (cloakType == GameManager.CloakRed)
            return new Color(255, 8, 41);
        return new Color(16, 173, 66);
    }

    public static int GetNext(
        int currentTunic, bool ownsBlueTunic, bool ownsRedTunic)
    {
        if (currentTunic == GameManager.CloakGreen && ownsBlueTunic)
            return GameManager.CloakBlue;
        if (currentTunic != GameManager.CloakRed && ownsRedTunic)
            return GameManager.CloakRed;
        return GameManager.CloakGreen;
    }
}
