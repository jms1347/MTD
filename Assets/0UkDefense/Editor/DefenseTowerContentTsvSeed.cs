#if UNITY_EDITOR
/// <summary>
/// 구글 시트 컬럼 형식에 맞춘 타워 콘텐츠 시드 TSV.
/// Effect: A=ID B=이름 C=타입(영문) D=원소(영문) E=duration F=magnitude G=tickDamage H=설명
/// EffectGroup: A=코드 B=이름 C=Effect_ID
/// Skill: A=ID B=이름 C=미사일/장판 D=직선/포물선/즉시타격 E=원소 F~N=수치·이펙트그룹·소환·미사일키
/// Tower: A=ID B=prefabKey C=이름 D=cost E=buildTime F=baseDamage G=fireInterval H=attackRange I=skillId J=설명
/// </summary>
public static class DefenseTowerContentTsvSeed
{
    public const string EffectRows =
        "Burn2\t화상2\tFire\tFire\t3\t18\t0\t2단계 화상 — magnitude=총 피해 18\n" +
        "Burn3\t화상3\tFire\tFire\t4\t28\t0\t3단계 화상 — magnitude=총 피해 28\n" +
        "Frost2\t동상2\tSlow\tIce\t3.5\t8\t0\t2단계 동상 — 이동 감속 스택\n" +
        "Frost3\t동상3\tSlow\tIce\t4.5\t12\t0\t3단계 동상 — 강한 이동 감속\n" +
        "Shock2\t감전2\tStun\tLightning\t1.4\t0\t0\t2단계 감전 — duration=기절 시간\n" +
        "Shock3\t감전3\tStun\tLightning\t2\t0\t0\t3단계 감전 — duration=기절 시간\n" +
        "Poison2\t중독2\tPoison\tPoison\t5\t6\t12\t2단계 중독 — tickDamage=틱 피해\n" +
        "Poison3\t중독3\tPoison\tPoison\t6\t8\t18\t3단계 중독 — tickDamage=틱 피해";

    public const string EffectGroupRows =
        "Burn2\t화상 2단계\tBurn2\n" +
        "Burn3\t화상 3단계\tBurn3\n" +
        "Frost2\t동상 2단계\tFrost2\n" +
        "Frost3\t동상 3단계\tFrost3\n" +
        "Shock2\t감전 2단계\tShock2\n" +
        "Shock3\t감전 3단계\tShock3\n" +
        "Poison2\t중독 2단계\tPoison2\n" +
        "Poison3\t중독 3단계\tPoison3";

    public const string SkillRows =
        "M-N-0002\t가틀링 탄막\t미사일\t직선\t물리\t50\t0\t1\t0\t0\t0\t\t\tBulletFatPinkOBJ\n" +
        "M-N-0003\t고폭탄 포탄\t미사일\t포물선\t물리\t15\t0\t1\t0\t0\t2.5\t\t\tNukePinkOBJ\n" +
        "M-N-0004\t레일건 관통탄\t미사일\t직선\t물리\t90\t0\t1\t0\t3\t0\t\t\tLaserBlueOBJ\n" +
        "M-N-0005\t따발 즉시탄\t미사일\t즉시타격\t물리\t0\t0\t0.45\t0\t0\t0\t\t\tBulletFatPinkOBJ\n" +
        "M-F-0002\t화염방사\t미사일\t직선\t화염\t28\t0\t1\t0\t0\t1.2\tBurn2\t\tGasFireOBJ\n" +
        "M-F-0003\t유성 폭격\t미사일\t포물선\t화염\t12\t0\t1\t0\t0\t3.5\tBurn3\t\tNovaFireOBJ\n" +
        "M-I-0002\t빙결 분사\t미사일\t직선\t얼음\t26\t0\t1\t0\t0\t1.2\tFrost2\t\tFrostMissileOBJ\n" +
        "M-I-0003\t눈보라 탄\t미사일\t포물선\t얼음\t14\t5\t1\t0\t6\t4.5\tFrost\tZone_BlizzardSnow\tNovaBlueOBJ\n" +
        "M-L-0003\t연쇄 전류탄\t미사일\t직선\t전기\t42\t0\t1\t1\t0\t0\tShock2\t\tLightningYellowOBJ\n" +
        "M-P-0002\t독액 분사\t미사일\t직선\t독\t24\t0\t1\t0\t0\t1.4\tPoison2\t\tGasGreenOBJ\n" +
        "M-P-0003\t역병 구름탄\t미사일\t포물선\t독\t13\t0\t1\t0\t0\t2.8\tPoison3\t\tSoulPurpleOBJ\n" +
        "M-F-0004\t화산 분출탄\t미사일\t포물선\t화염\t18\t0\t1\t0\t0\t2.2\tBurn2\t\tFireBallFireOBJ\tM-F-0004R\n" +
        "M-F-0004R\t화산 용암 돌\t미사일\t직선\t물리\t22\t0.52\t0.38\t1\t6\t1\t\t\tNukeFireOBJ\t\n" +
        "M-F-0005\t유성 낙하 표식\t미사일\t포물선\t화염\t16\t0\t1\t0\t0\t3.5\tBurn3\t\tFireBallFireOBJ\tM-F-0003\n" +
        "M-F-0006\t지옥불 유도탄\t미사일\t직선\t화염\t22\t0\t1\t1\t0\t1.8\tBurn3\t\tLiquidLavaOBJ\n" +
        "M-I-0004\t빙하 관통창\t미사일\t직선\t얼음\t48\t0\t1\t0\t5\t0\tFrost2\t\tFrostMissileOBJ\n" +
        "M-I-0005\t한파 종탄\t미사일\t포물선\t얼음\t11\t0\t1\t0\t0\t2.5\tFrost3\t\tNovaBlueOBJ\n" +
        "M-I-0006\t서리 유도비\t미사일\t직선\t얼음\t30\t0\t1\t1\t0\t1.6\tFrost3\t\tFrostMissileOBJ\n" +
        "M-I-0007\t디아 오브\t미사일\t직선\t얼음\t8\t0\t0.55\t0\t99\t0\tFrost2\t\tFrozenOrbOBJ\n" +
        "M-L-0004\t천둥 관통창\t미사일\t직선\t전기\t46\t0\t1\t0\t4\t0\tShock2\t\tLaserBlueOBJ\n" +
        "M-L-0005\t정전기 뇌우탄\t미사일\t포물선\t전기\t9\t-1\t1\t0\t0\t0\tShock3\t\tStormMissileOBJ\n" +
        "M-L-0006\t아크 유도핵\t미사일\t직선\t전기\t34\t0\t1\t1\t0\t0\tShock3\t\tLightningYellowOBJ\n" +
        "M-P-0004\t부식 웅덩이탄\t미사일\t포물선\t독\t16\t0\t1\t0\t0\t2\tPoison2\t\tGasGreenOBJ\n" +
        "M-P-0005\t독침 연사\t미사일\t직선\t독\t30\t3\t1\t1\t1\t2.5\tPoison2\t\tGasGreenOBJ\n" +
        "M-P-0006\t역병 폭우탄\t미사일\t포물선\t독\t10\t0\t1\t0\t0\t3.2\tPoison3\t\tLiquidAcidOBJ";

    public const string TowerRows =
        "N-0002\tN-0002\t가틀링 포탑\t400\t5\t10\t1\t5\tM-N-0002\t매우 빠른 공속으로 단일 적을 갈아버림. 근거리 DPS 특화.\n" +
        "N-0003\tN-0003\t중장갑 캐논\t400\t5\t40\t5\t30\tM-N-0003\t포물선 고폭탄으로 범위 물리 피해. 느리지만 광역 제압.\n" +
        "N-0004\tN-0004\t스나이퍼 레일건\t500\t5\t25\t7\t50\tM-N-0004\t초장거리 관통 레이저. 느린 공속, 긴 사거리, 3체 관통.\n" +
        "N-0005\tN-0005\t스트라이커 따발총\t380\t4\t8\t0.11\t7\tM-N-0005\t즉발 탄막. 미사일 비행 없이 초고속 연사.\n" +
        "F-0002\tF-0002\t화염방사기\t380\t4\t18\t0.35\t8\tM-F-0002\t짧은 사거리 화염 분사. 빠른 연사 + 화상2.\n" +
        "F-0003\tF-0003\t유성 투척기\t520\t6\t55\t6\t16\tM-F-0003\t포물선 유성탄으로 넓은 화염 장판. 화상3.\n" +
        "I-0002\tI-0002\t빙결 방사기\t380\t4\t16\t0.35\t8\tM-I-0002\t짧은 사거리 냉기 분사. 빠른 연사 + 동상2.\n" +
        "I-0003\tI-0003\t눈보라 포드\t500\t6\t32\t5.5\t15\tM-I-0003\t눈보라 탄 공중 폭발 후 눈보라 장판. 범위 내 최대 6체 슬로우 중첩.\n" +
        "L-0003\tL-0003\t아크 리피터\t480\t5\t28\t2.2\t20\tM-L-0003\t연쇄 전류 3타. 감전2.\n" +
        "P-0002\tP-0002\t독액 스프레이\t360\t4\t12\t0.45\t9\tM-P-0002\t독액 분사. 빠른 연사 + 중독2.\n" +
        "P-0003\tP-0003\t역병 투석기\t500\t6\t38\t5\t14\tM-P-0003\t역병 구름탄. 범위 중독3.\n" +
        "F-0004\tF-0004\t화산식\t420\t4\t44\t10\t13\tM-F-0004\t화산 분출 후 주변에 용암 돌이 랜덤 낙하. 화상2+추가 물리 피해.\n" +
        "F-0005\tF-0005\t화염 선돌이\t460\t5\t16\t4.8\t16\tM-F-0005\t표식 탄 착지 후 빨간 경고 영역, 2.5초 뒤 유성 낙하. 화상3.\n" +
        "F-0006\tF-0006\t지옥문 연성로\t540\t6\t48\t4.5\t22\tM-F-0006\t유도 지옥불. 단일 대상 화상3.\n" +
        "I-0004\tI-0004\t빙하 관통포\t410\t4\t12\t1.8\t20\tM-I-0004\t얼음 창 5관통. 동상2.\n" +
        "I-0005\tI-0005\t한파 종루\t480\t5\t28\t6.5\t18\tM-I-0005\t한파 종탑에서 눈보라 장판. 동상3.\n" +
        "I-0006\tI-0006\t서리 거미\t500\t5\t36\t3.8\t24\tM-I-0006\t유도 서릿발. 동상3.\n" +
        "I-0007\tI-0007\t디아 오브\t500\t5\t22\t3.8\t22\tM-I-0007\t빙결 오브가 날아가며 사방으로 얼음 탄환을 뿜습니다. 동상2.\n" +
        "L-0004\tL-0004\t천둥 관통창\t400\t4\t15\t1.6\t19\tM-L-0004\t전기 광선 4관통. 감전2.\n" +
        "L-0005\tL-0005\t정전기 뇌운\t450\t5\t42\t8\t14\tM-L-0005\t공중 고정 뇌우탄. 미사일에서 번개. 감전3.\n" +
        "L-0006\tL-0006\t아크 유도구\t520\t6\t32\t3.2\t21\tM-L-0006\t유도 전기구체. 단일 감전3.\n" +
        "P-0004\tP-0004\t부식 웅덩이\t390\t4\t20\t3\t12\tM-P-0004\t산성 웅덩이 장판. 중독2.\n" +
        "P-0005\tP-0005\t독침 벌집\t370\t4\t10\t0.55\t10\tM-P-0005\t독침 3발 추적 후 폭발. 중독2.\n" +
        "P-0006\tP-0006\t역병 폭우\t530\t6\t45\t5.5\t16\tM-P-0006\t광역 역병 폭탄. 중독3.";
}
#endif
