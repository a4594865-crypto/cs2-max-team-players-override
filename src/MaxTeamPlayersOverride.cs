using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
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
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });

            // 伺服器啟動時，在黑視窗印出提示，確認 Load 真的有執行
            Console.WriteLine("##############################################");
            Console.WriteLine("[MaxTeam] 插件已啟動！請嘗試在遊戲內輸入 .ctmax");
            Console.WriteLine("##############################################");
        }

        // 修改為通用 Command，手動判斷權限
        [ConsoleCommand("css_ctmax", "開啟人數覆蓋")]
        [ConsoleCommand(".ctmax", "開啟人數覆蓋")]
        public void OnEnableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            // 手動檢查權限，若失敗則回傳提示，不再保持沉默
            if (!AdminManager.PlayerHasPermissions(player, "@css/generic"))
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：您沒有權限執行此指令。");
                return;
            }

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能開啟。");
                return;
            }

            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員 {ChatColors.Blue}{player.PlayerName} {ChatColors.Default}已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋。");
        }

        [ConsoleCommand("css_unctmax", "禁用人數覆蓋")]
        [ConsoleCommand(".unctmax", "禁用人數覆蓋")]
        public void OnDisableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (!AdminManager.PlayerHasPermissions(player, "@css/generic")) return;

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：僅限熱身期間。");
                return;
            }

            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋。");
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
