using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MoreSaves;

[BepInPlugin("Isbjorn52.MoreSaves", "More Saves", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private const int NUMBER_OF_SAVES = 20;

    public void OnEnable()
    {
        new Hook(typeof(SaveUICellTABS).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic), SaveUICellTABS_Start);
        new ILHook(typeof(SaveSystem).GetMethod("LoadSavesFromDisk"), IL_SaveSystem_LoadSavesFromDisk);
    }

    private void SaveUICellTABS_Start(Action<SaveUICellTABS> orig, SaveUICellTABS self)
    {
        // Expand RectTransform
        self.GetComponent<RectTransform>().sizeDelta = new Vector2(5f * 320f, 320f);

        // Remove HorizontalLayoutGroup so a GridLayoutGroup can be added later
        Destroy(self.GetComponent<HorizontalLayoutGroup>());

        // Move Save slots up
        self.transform.localPosition = new Vector3(0f, 100f, 0f);

        // Instantiate new save slots
        for (int i = 3; i < NUMBER_OF_SAVES; i++)
        {
            Instantiate(self.transform.GetChild(0).gameObject, self.transform);
        }

        // The rest of the function needs to be delayed
        // One frame needs to pass between removing a LayoutGroup and adding a new one
        // + The save slot text wouldn't update otherwise
        StartCoroutine(SaveUICellTABS_Start_Delay(self));


        orig(self);
    }

    private IEnumerator SaveUICellTABS_Start_Delay(SaveUICellTABS self)
    {
        // Wait one frame.
        yield return null;

        // Create and configure GridLayoutGroup.
        GridLayoutGroup gridLayoutGroup = self.gameObject.AddComponent<GridLayoutGroup>();
        gridLayoutGroup.cellSize = new Vector2(310f, 150f);
        gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        gridLayoutGroup.spacing = new Vector2(10f, 10f);

        // Update text and sizes of all save slots.
        for (int i = 0; i < NUMBER_OF_SAVES; i++)
        {
            SaveUICell saveCell = self.transform.GetChild(i).transform.GetComponent<SaveUICell>();

            // Set save text number
            saveCell.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = $"SAVE {i + 1}";

            // Move delete button
            saveCell.m_deleteButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(135f, 55f);

            // Move text elements
            saveCell.dateText.rectTransform.anchoredPosition = new Vector2(-9.5f, 30f);
            saveCell.dayText.rectTransform.anchoredPosition = new Vector2(-9.5f, -5f);
            saveCell.moneyText.rectTransform.anchoredPosition = new Vector2(-9.5f, -35f);

            // Downscale font size
            saveCell.dayText.fontSize = 28;
            saveCell.moneyText.fontSize = 28;

            saveCell.m_emtpySave.GetComponent<TextMeshProUGUI>().fontSize = 28;
        }
    }

    private void IL_SaveSystem_LoadSavesFromDisk(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.Index = 4;

        cursor.EmitDelegate(() =>
        {
            // Make sure the array can hold all the new saves
            SaveSystem.SavesOnFile = new Save[NUMBER_OF_SAVES];
        });
    }
}
