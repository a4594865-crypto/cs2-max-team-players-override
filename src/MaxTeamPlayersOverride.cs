using System;
using System.Linq;
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

        public override string ModuleName => "Max Team Players Override";

        public override void Load(bool hotReload)
        {
            // 註冊回合開始事件
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });

            Console.WriteLine("[MaxTeam] 插件已啟動。支援指令: .ctmax, .unctmax (僅限熱身)");
        }

        // --- 使用 ConsoleCommand 註冊指令，這在 1.0.362 中最穩定 ---
        
        [ConsoleCommand(".ctmax", "在熱身期間啟用人數覆蓋")]
        [RequiresPermissions("@css/generic")]
        public void OnEnableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能開啟。");
                return;
            }

            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋。");
        }

        [ConsoleCommand(".unctmax", "在熱身期間禁用人數覆蓋")]
        [RequiresPermissions("@css/generic")]
        public void OnDisableCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (!IsWarmup())
            {
                player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能關閉。");
                return;
            }

            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋（下回合生效）。");
        }

        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            // 使用 m_bWarmupPeriod 的底層屬性映射，這是最保險的熱身偵測
            return gameRulesProxy?.GameRules?.WarmupPeriod ?? false;
        }

        private void ApplyTeamLimits()
        {
            int maxTs = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
            int maxCTs = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                if (ent.GameRules != null)
                {
                    ent.GameRules.NumSpawnableTerrorist = maxTs;
                    ent.GameRules.MaxNumTerrorists = maxTs;
                    ent.GameRules.NumSpawnableCT = maxCTs;
                    ent.GameRules.MaxNumCTs = maxCTs;
                }
            }
        }
    }
}
