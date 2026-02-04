using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands; // 新增：為了處理指令
using CounterStrikeSharp.API.Modules.Utils;    // 新增：為了聊天顏色

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        // 關鍵修改 1：預設為 false，代表「不允許」突破 5v5
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override (Custom Logic)";

        public override void Load(bool hotReload)
        {
            // 關鍵修改 2：註冊指令。只有輸入指令，才會把開關打開
            AddCommand("css_ctmax", "啟用人數覆蓋 (10v10)", (player, info) => 
            {
                if (player == null) return;
                _isOverrideEnabled = true;
                ApplyLimitsNow(); // 立即套用
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}人數限制已{ChatColors.Lime}解鎖{ChatColors.Default}。");
            });

            AddCommand("css_unctmax", "禁用人數覆蓋 (鎖定 5v5)", (player, info) => 
            {
                if (player == null) return;
                _isOverrideEnabled = false;
                ApplyLimitsNow(); // 立即套用 (會回歸 5v5)
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}人數限制已{ChatColors.Red}鎖定回 5v5{ChatColors.Default}。");
            });

            RegisterEventHandler<EventRoundStart>((_, _) =>
            {
                ApplyLimitsNow();
                return HookResult.Continue;
            });
        }

        private void ApplyLimitsNow()
        {
            // 如果開關沒開，強制設定為 5 (鎖死)
            // 如果開關開了，才去讀 Config 的數值 (例如 10)
            int maxTs = _isOverrideEnabled ? Config.MaxTs : 5;
            int maxCTs = _isOverrideEnabled ? Config.MaxCTs : 5;

            // 如果 Config 設為 -1，自動取伺服器人數一半
            if (_isOverrideEnabled && maxTs < 0) maxTs = Server.MaxPlayers / 2;
            if (_isOverrideEnabled && maxCTs < 0) maxCTs = Server.MaxPlayers / 2;

            SetMaxTs(maxTs);
            SetMaxCTs(maxCTs);
        }

        private static void SetMaxTs(int num)
        {
            IEnumerable<CCSGameRulesProxy> ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (CCSGameRulesProxy ent in ents)
            {
                if (ent.GameRules != null)
                {
                    ent.GameRules.NumSpawnableTerrorist = num;
                    ent.GameRules.MaxNumTerrorists = num;
                }
            }
        }

        private static void SetMaxCTs(int num)
        {
            IEnumerable<CCSGameRulesProxy> ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (CCSGameRulesProxy ent in ents)
            {
                if (ent.GameRules != null)
                {
                    ent.GameRules.NumSpawnableCT = num;
                    ent.GameRules.MaxNumCTs = num;
                }
            }
        }
    }
}
