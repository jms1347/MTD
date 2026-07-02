#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DefenseRoguelikeSetupMenu
{
    private const string VisualCatalogPath = "Assets/0UkDefense/5Roguelike/Resources/RoguelikeCardVisualCatalog.asset";
    private const string CardRedPath = "Assets/AssetKits/ParticleImage/Demo/Sprites/CardRed.png";
    private const string CardBluePath = "Assets/AssetKits/ParticleImage/Demo/Sprites/CardBlue.png";
    private const string CardGreenPath = "Assets/AssetKits/ParticleImage/Demo/Sprites/CardGreen.png";
    private const string CardYellowPath = "Assets/AssetKits/ParticleImage/Demo/Sprites/CardYellow.png";
    private const string DataManagerPrefabPath = "Assets/0UkDefense/1Data/Prefab/DataManager.prefab";

    [MenuItem("UkDefense/Roguelike/Import Card TSV + Setup Assets")]
    public static void ImportAndSetup()
    {
        DefenseSheetTsvUtility.EnsureFolder(GoogleSheetDefinitions.SoDirectory);
        DefenseSheetTsvUtility.EnsureFolder("Assets/0UkDefense/5Roguelike/Resources");

        var cardSo = LoadOrCreate<RoguelikeCardDataSo>(GoogleSheetDefinitions.RoguelikeCardDataAssetPath);
        if (File.Exists(GoogleSheetDefinitions.RoguelikeCardTsvExportPath))
        {
            var tsv = File.ReadAllText(GoogleSheetDefinitions.RoguelikeCardTsvExportPath);
            cardSo.ImportFromTsv(tsv);
            EditorUtility.SetDirty(cardSo);
        }

        WireDataManagerAndVisuals(cardSo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DefenseRoguelikeSetupMenu] Roguelike card SO, visual catalog, DataManager wiring complete.");
    }

    public static void WriteSheetExportTsv(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
            return;

        DefenseSheetTsvUtility.EnsureFolder("Assets/0UkDefense/1Data/SheetExport");
        File.WriteAllText(GoogleSheetDefinitions.RoguelikeCardTsvExportPath, tsv.TrimEnd() + "\n");
        AssetDatabase.ImportAsset(GoogleSheetDefinitions.RoguelikeCardTsvExportPath);
    }

    public static void WireDataManagerAndVisuals(RoguelikeCardDataSo cardSo)
    {
        var catalog = LoadOrCreate<RoguelikeCardVisualCatalog>(VisualCatalogPath);
        catalog.cardRed = LoadSprite(CardRedPath);
        catalog.cardBlue = LoadSprite(CardBluePath);
        catalog.cardGreen = LoadSprite(CardGreenPath);
        catalog.cardYellow = LoadSprite(CardYellowPath);
        EditorUtility.SetDirty(catalog);

        WireDataManager(cardSo);
    }

    private static void WireDataManager(RoguelikeCardDataSo cardSo)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataManagerPrefabPath);
        if (prefab == null)
            return;

        var dataManager = prefab.GetComponent<DataManager>();
        if (dataManager == null)
            return;

        var so = new SerializedObject(dataManager);
        so.FindProperty("roguelikeCardDataSo").objectReferenceValue = cardSo;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
