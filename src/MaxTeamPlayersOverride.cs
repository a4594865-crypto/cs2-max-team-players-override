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
        // 核心開關：預設關閉，由管理員手動開啟
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override Plugin";

        public override void Load(bool hotReload)
        {
            // 每回合開始時執行
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                // 只有在開啟狀態下才執行修改邏輯
                if (!_isOverrideEnabled) return HookResult.Continue;

                ApplyTeamLimits();
                return HookResult.Continue;
            });

            // 插件啟動日誌
            Console.WriteLine("[MaxTeam] 插件載入。指令: .ctmax / .unctmax (僅限熱身期間)");
        }

        // --- 註冊指令：使用相容性最高的 ConsoleCommand 模式 ---
        // 雖然是 . 開頭，但在聊天框輸入仍會觸發

        [ConsoleCommand(".ctmax", "在熱身期間開啟人數覆蓋")]
        [RequiresPermissions("@css/admin")]
        public void OnEnableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能開啟人數最大化。");
                return;
            }

            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數最大化 8 v 8。");
        }

        [ConsoleCommand(".unctmax", "在熱身期間禁用人數覆蓋")]
        [RequiresPermissions("@css/generic")]
        public void OnDisableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能禁用人數最大化。");
                return;
            }

            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數人數最大化。");
        }

        // 偵測是否在熱身期間
        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            // 1.0.362 中最穩定的熱身判斷屬性
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
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
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
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
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
