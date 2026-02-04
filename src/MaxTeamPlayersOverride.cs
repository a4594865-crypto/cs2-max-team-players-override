using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override Plugin";

        public override void Load(bool hotReload)
        {
            // --- 核心手動註冊：強制將指令寫入伺服器 ---
            AddCommand("css_ctmax", "開啟人數覆蓋", CommandEnable);
            AddCommand(".ctmax", "開啟人數覆蓋", CommandEnable);
            AddCommand("css_unctmax", "禁用人數覆蓋", CommandDisable);
            AddCommand(".unctmax", "禁用人數覆蓋", CommandDisable);

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });

            Console.WriteLine("##############################################");
            Console.WriteLine("[MaxTeam] 核心註冊版載入！請在控制台測試 css_ctmax");
            Console.WriteLine("##############################################");
        }

        private void CommandEnable(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            // 1. 先噴一句 Debug 訊息，證明指令系統「活著」
            player.PrintToChat($" {ChatColors.Yellow} [Debug] 收到指令！正在檢查權限與狀態...");

            // 2. 檢查權限
            if (!AdminManager.PlayerHasPermissions(player, "@css/generic"))
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：您沒有權限執行此指令。");
                return;
            }

            // 3. 檢查熱身
            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能開啟。");
                return;
            }

            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員 {ChatColors.Blue}{player.PlayerName} {ChatColors.Default}已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋。");
        }

        private void CommandDisable(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !AdminManager.PlayerHasPermissions(player, "@css/generic")) return;
            
            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}人數覆蓋已由管理員禁用。");
        }

        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            return gameRulesProxy?.GameRules?.WarmupPeriod ?? false;
        }

        private void ApplyTeamLimits()
        {
            int maxTs = Config.MaxTs;
            int maxCTs = Config.MaxCTs;
            if (maxTs < 0) maxTs = Server.MaxPlayers / 2;
            if (maxCTs < 0) maxCTs = Server.MaxPlayers / 2;
            SetMaxTs(maxTs);
            SetMaxCTs(maxCTs);
        }

        private static void SetMaxTs(int num)
        {
            foreach (var ent in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
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
            foreach (var ent in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
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
