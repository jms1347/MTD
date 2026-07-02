/// <summary>
/// 구글 시트 export URL 정의. 런타임(GoogleSheetManager)과 에디터 import가 동일 소스를 사용합니다.
/// </summary>
public static class GoogleSheetDefinitions
{
    private const string SpreadsheetBase =
        "https://docs.google.com/spreadsheets/d/1Yzm3T6znxm6O2XlhYT0HiixkMhG9w09MDXwVJCbBTto/export?format=tsv";

    public const string MonsterDataUrl = SpreadsheetBase + "&gid=0&range=A2:J";
    public const string TowerDataUrl = SpreadsheetBase + "&gid=774552842&range=A2:J";
    public const string SkillDataUrl = SpreadsheetBase + "&gid=900143476&range=A2:N";
    public const string EffectDataUrl = SpreadsheetBase + "&gid=721846049&range=A2:H";
    public const string EffectGroupDataUrl = SpreadsheetBase + "&gid=1627883599&range=A2:C";
    public const string StageMetaDataUrl = SpreadsheetBase + "&gid=1050707762&range=A2:D";
    public const string StageSpawnDataUrl = SpreadsheetBase + "&gid=1445112309&range=A2:C";
    public const string AddressableKeyDataUrl = SpreadsheetBase + "&gid=1338748644&range=A2:E";
    public const string RoguelikeCardDataUrl = SpreadsheetBase + "&gid=1009525947&range=A2:L";

    public const string SoDirectory = "Assets/0UkDefense/1Data/SO";
    public const string MonsterDataAssetPath = SoDirectory + "/MonsterDataSo.asset";
    public const string TowerDataAssetPath = SoDirectory + "/TowerDataSo.asset";
    public const string SkillDataAssetPath = SoDirectory + "/DefenseSkillDataSo.asset";
    public const string EffectDataAssetPath = SoDirectory + "/DefenseEffectDataSo.asset";
    public const string EffectGroupDataAssetPath = SoDirectory + "/DefenseEffectGroupDataSo.asset";
    public const string BossDataAssetPath = SoDirectory + "/BossDataSo.asset";
    public const string BossElementGroupDataAssetPath = SoDirectory + "/BossElementGroupDataSo.asset";
    public const string StageDataAssetPath = SoDirectory + "/StageDataSo.asset";
    public const string AddressableKeyDataAssetPath = SoDirectory + "/DefenseAddressableKeyDataSo.asset";
    public const string RoguelikeCardDataAssetPath = SoDirectory + "/RoguelikeCardDataSo.asset";
    public const string RoguelikeCardTsvExportPath = "Assets/0UkDefense/1Data/SheetExport/RoguelikeCard.tsv";
}
