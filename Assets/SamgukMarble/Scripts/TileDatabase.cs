using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 100칸 타일 정적 데이터베이스.
    /// </summary>
    public static class TileDatabase
    {
        public struct TileDef
        {
            public int Id;
            public string Name;
            public TileType Type;
            public ColorGroup Group;
            public int BasePrice;
            public int BaseToll;

            public TileDef(int id, string name, TileType type, ColorGroup group, int basePrice = 0, int baseToll = 0)
            {
                Id = id;
                Name = name;
                Type = type;
                Group = group;
                BasePrice = basePrice;
                BaseToll = baseToll;
            }
        }

        public static readonly TileDef[] All = Build();

        static TileDef[] Build()
        {
            var d = new TileDef[101]; // 1-indexed

            // 01~20 위나라 (북)
            d[1] = new TileDef(1, "출발지", TileType.Start, ColorGroup.WeiBlue);
            d[2] = new TileDef(2, "무위성", TileType.Castle, ColorGroup.WeiBlue, 100, 20);
            d[3] = new TileDef(3, "행운카드", TileType.LuckyCard, ColorGroup.WeiBlue);
            d[4] = new TileDef(4, "안정성", TileType.Castle, ColorGroup.WeiBlue, 120, 24);
            d[5] = new TileDef(5, "교차로1", TileType.Crossroad, ColorGroup.WeiBlue);
            d[6] = new TileDef(6, "불행카드", TileType.UnluckyCard, ColorGroup.WeiBlue);
            d[7] = new TileDef(7, "계교성", TileType.Castle, ColorGroup.WeiBlue, 140, 28);
            d[8] = new TileDef(8, "찬스카드", TileType.ChanceCard, ColorGroup.WeiBlue);
            d[9] = new TileDef(9, "양평성", TileType.Castle, ColorGroup.WeiBlue, 160, 32);
            d[10] = new TileDef(10, "마시장", TileType.Market, ColorGroup.WeiBlue, 0, 0);
            d[11] = new TileDef(11, "서량본성", TileType.Castle, ColorGroup.WeiBlue, 280, 70);
            d[12] = new TileDef(12, "행운카드", TileType.LuckyCard, ColorGroup.WeiBlue);
            d[13] = new TileDef(13, "북평본성", TileType.Castle, ColorGroup.WeiBlue, 300, 80);
            d[14] = new TileDef(14, "불행카드", TileType.UnluckyCard, ColorGroup.WeiBlue);
            d[15] = new TileDef(15, "교차로2", TileType.Crossroad, ColorGroup.WeiBlue);
            d[16] = new TileDef(16, "찬스카드", TileType.ChanceCard, ColorGroup.WeiBlue);
            d[17] = new TileDef(17, "안문관문", TileType.Gate, ColorGroup.WeiBlue, 200, 50);
            d[18] = new TileDef(18, "행운카드", TileType.LuckyCard, ColorGroup.WeiBlue);
            d[19] = new TileDef(19, "진양성", TileType.Castle, ColorGroup.WeiBlue, 180, 40);
            d[20] = new TileDef(20, "유배지", TileType.Exile, ColorGroup.WeiBlue);

            // 21~40 오나라 (동)
            d[21] = new TileDef(21, "회계성", TileType.Castle, ColorGroup.WuRed, 100, 20);
            d[22] = new TileDef(22, "불행카드", TileType.UnluckyCard, ColorGroup.WuRed);
            d[23] = new TileDef(23, "오군성", TileType.Castle, ColorGroup.WuRed, 120, 24);
            d[24] = new TileDef(24, "행운카드", TileType.LuckyCard, ColorGroup.WuRed);
            d[25] = new TileDef(25, "교차로3", TileType.Crossroad, ColorGroup.WuRed);
            d[26] = new TileDef(26, "시상성", TileType.Castle, ColorGroup.WuRed, 140, 28);
            d[27] = new TileDef(27, "찬스카드", TileType.ChanceCard, ColorGroup.WuRed);
            d[28] = new TileDef(28, "여릉성", TileType.Castle, ColorGroup.WuRed, 160, 32);
            d[29] = new TileDef(29, "불행카드", TileType.UnluckyCard, ColorGroup.WuRed);
            d[30] = new TileDef(30, "낙양국고", TileType.Treasury, ColorGroup.WuRed);
            d[31] = new TileDef(31, "건업본성", TileType.Castle, ColorGroup.WuRed, 320, 90);
            d[32] = new TileDef(32, "행운카드", TileType.LuckyCard, ColorGroup.WuRed);
            d[33] = new TileDef(33, "구강성", TileType.Castle, ColorGroup.WuRed, 180, 40);
            d[34] = new TileDef(34, "찬스카드", TileType.ChanceCard, ColorGroup.WuRed);
            d[35] = new TileDef(35, "교차로4", TileType.Crossroad, ColorGroup.WuRed);
            d[36] = new TileDef(36, "적벽관문", TileType.Gate, ColorGroup.WuRed, 220, 55);
            d[37] = new TileDef(37, "불행카드", TileType.UnluckyCard, ColorGroup.WuRed);
            d[38] = new TileDef(38, "파양성", TileType.Castle, ColorGroup.WuRed, 170, 36);
            d[39] = new TileDef(39, "행운카드", TileType.LuckyCard, ColorGroup.WuRed);
            d[40] = new TileDef(40, "유수구관문", TileType.Gate, ColorGroup.WuRed, 240, 60);

            // 41~60 형주 (남)
            d[41] = new TileDef(41, "무릉성", TileType.Castle, ColorGroup.JingzhouYellow, 100, 20);
            d[42] = new TileDef(42, "찬스카드", TileType.ChanceCard, ColorGroup.JingzhouYellow);
            d[43] = new TileDef(43, "영릉성", TileType.Castle, ColorGroup.JingzhouYellow, 120, 24);
            d[44] = new TileDef(44, "불행카드", TileType.UnluckyCard, ColorGroup.JingzhouYellow);
            d[45] = new TileDef(45, "교차로5", TileType.Crossroad, ColorGroup.JingzhouYellow);
            d[46] = new TileDef(46, "계양성", TileType.Castle, ColorGroup.JingzhouYellow, 140, 28);
            d[47] = new TileDef(47, "행운카드", TileType.LuckyCard, ColorGroup.JingzhouYellow);
            d[48] = new TileDef(48, "장사성", TileType.Castle, ColorGroup.JingzhouYellow, 160, 32);
            d[49] = new TileDef(49, "찬스카드", TileType.ChanceCard, ColorGroup.JingzhouYellow);
            d[50] = new TileDef(50, "형주수군기지", TileType.Special, ColorGroup.JingzhouYellow, 200, 45);
            d[51] = new TileDef(51, "강릉본성", TileType.Castle, ColorGroup.JingzhouYellow, 300, 80);
            d[52] = new TileDef(52, "불행카드", TileType.UnluckyCard, ColorGroup.JingzhouYellow);
            d[53] = new TileDef(53, "양양성", TileType.Castle, ColorGroup.JingzhouYellow, 190, 42);
            d[54] = new TileDef(54, "행운카드", TileType.LuckyCard, ColorGroup.JingzhouYellow);
            d[55] = new TileDef(55, "교차로6", TileType.Crossroad, ColorGroup.JingzhouYellow);
            d[56] = new TileDef(56, "번성관문", TileType.Gate, ColorGroup.JingzhouYellow, 220, 55);
            d[57] = new TileDef(57, "찬스카드", TileType.ChanceCard, ColorGroup.JingzhouYellow);
            d[58] = new TileDef(58, "신야성", TileType.Castle, ColorGroup.JingzhouYellow, 170, 36);
            d[59] = new TileDef(59, "불행카드", TileType.UnluckyCard, ColorGroup.JingzhouYellow);
            d[60] = new TileDef(60, "화타의의원", TileType.Special, ColorGroup.JingzhouYellow);

            // 61~80 촉나라 (서)
            d[61] = new TileDef(61, "자동성", TileType.Castle, ColorGroup.ShuGreen, 100, 20);
            d[62] = new TileDef(62, "행운카드", TileType.LuckyCard, ColorGroup.ShuGreen);
            d[63] = new TileDef(63, "파서성", TileType.Castle, ColorGroup.ShuGreen, 120, 24);
            d[64] = new TileDef(64, "찬스카드", TileType.ChanceCard, ColorGroup.ShuGreen);
            d[65] = new TileDef(65, "교차로7", TileType.Crossroad, ColorGroup.ShuGreen);
            d[66] = new TileDef(66, "불행카드", TileType.UnluckyCard, ColorGroup.ShuGreen);
            d[67] = new TileDef(67, "한중성", TileType.Castle, ColorGroup.ShuGreen, 180, 40);
            d[68] = new TileDef(68, "행운카드", TileType.LuckyCard, ColorGroup.ShuGreen);
            d[69] = new TileDef(69, "검각관문", TileType.Gate, ColorGroup.ShuGreen, 220, 55);
            d[70] = new TileDef(70, "촉도험로", TileType.Special, ColorGroup.ShuGreen);
            d[71] = new TileDef(71, "성도본성", TileType.Castle, ColorGroup.ShuGreen, 320, 90);
            d[72] = new TileDef(72, "찬스카드", TileType.ChanceCard, ColorGroup.ShuGreen);
            d[73] = new TileDef(73, "강유성", TileType.Castle, ColorGroup.ShuGreen, 160, 32);
            d[74] = new TileDef(74, "불행카드", TileType.UnluckyCard, ColorGroup.ShuGreen);
            d[75] = new TileDef(75, "교차로8", TileType.Crossroad, ColorGroup.ShuGreen);
            d[76] = new TileDef(76, "남중성", TileType.Castle, ColorGroup.ShuGreen, 150, 30);
            d[77] = new TileDef(77, "행운카드", TileType.LuckyCard, ColorGroup.ShuGreen);
            d[78] = new TileDef(78, "영안성", TileType.Castle, ColorGroup.ShuGreen, 170, 36);
            d[79] = new TileDef(79, "찬스카드", TileType.ChanceCard, ColorGroup.ShuGreen);
            d[80] = new TileDef(80, "황실사당", TileType.Special, ColorGroup.ShuGreen);

            // 81~100 중원 (센터)
            d[81] = new TileDef(81, "낙양관문", TileType.Gate, ColorGroup.CentralPurple, 250, 65);
            d[82] = new TileDef(82, "행운카드", TileType.LuckyCard, ColorGroup.CentralPurple);
            d[83] = new TileDef(83, "낙양남문", TileType.Castle, ColorGroup.CentralPurple, 200, 50);
            d[84] = new TileDef(84, "찬스카드", TileType.ChanceCard, ColorGroup.CentralPurple);
            d[85] = new TileDef(85, "낙양본성", TileType.Castle, ColorGroup.CentralPurple, 400, 120);
            d[86] = new TileDef(86, "허창관문", TileType.Gate, ColorGroup.CentralPurple, 250, 65);
            d[87] = new TileDef(87, "행운카드", TileType.LuckyCard, ColorGroup.CentralPurple);
            d[88] = new TileDef(88, "허창동문", TileType.Castle, ColorGroup.CentralPurple, 200, 50);
            d[89] = new TileDef(89, "찬스카드", TileType.ChanceCard, ColorGroup.CentralPurple);
            d[90] = new TileDef(90, "허창본성", TileType.Castle, ColorGroup.CentralPurple, 380, 110);
            d[91] = new TileDef(91, "행운카드", TileType.LuckyCard, ColorGroup.CentralPurple);
            d[92] = new TileDef(92, "찬스카드", TileType.ChanceCard, ColorGroup.CentralPurple);
            d[93] = new TileDef(93, "황제알현실", TileType.Special, ColorGroup.CentralPurple, 300, 80);
            d[94] = new TileDef(94, "행운카드", TileType.LuckyCard, ColorGroup.CentralPurple);
            d[95] = new TileDef(95, "찬스카드", TileType.ChanceCard, ColorGroup.CentralPurple);
            d[96] = new TileDef(96, "중원대첩터", TileType.Special, ColorGroup.CentralPurple, 280, 70);
            d[97] = new TileDef(97, "불행카드", TileType.UnluckyCard, ColorGroup.CentralPurple);
            d[98] = new TileDef(98, "찬스카드", TileType.ChanceCard, ColorGroup.CentralPurple);
            d[99] = new TileDef(99, "행운카드", TileType.LuckyCard, ColorGroup.CentralPurple);
            d[100] = new TileDef(100, "천하통일옥좌", TileType.Throne, ColorGroup.CentralPurple, 500, 150);

            return d;
        }

        public static TileDef Get(int id)
        {
            if (id < 1 || id > 100) return default;
            return All[id];
        }
    }
}
