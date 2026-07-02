구글 시트에 붙여넣기 (각 탭 데이터는 A2부터):

- Effect.tsv      → 이펙트 DB 탭 (gid=721846049) A2
- EffectGroup.tsv → 이펙트그룹 DB 탭 (gid=1627883599) A2
- Skill.tsv       → 스킬 DB 탭 (gid=900143476) A2
- Tower.tsv       → 타워 DB 탭 (gid=774552842) A2
- AddressableKey.tsv → 어드레서블 키 DB 탭 (gid=1338748644) A2

기존 행은 유지되고, 시드 행만 upsert 됩니다.
Unity에서 GoogleSheet/Import All Sheets From Google 으로 다시 가져올 수 있습니다.