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
        // 核心狀態開關
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override (Warmup Only)";

        public override void Load(bool hotReload)
        {
            // 監聽聊天事件，處理 .ctmax 與 .unctmax
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                // 檢查是否為目標指令
                bool isEnableCmd = message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase);
                bool isDisableCmd = message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase);

                if (isEnableCmd || isDisableCmd)
                {
                    // 1. 權限檢查
                    if (!AdminManager.PlayerHasPermissions(player, "@css/admin")) return HookResult.Continue;

                    // 2. 熱身狀態檢查 (您的核心邏輯)
                    if (!IsWarmup())
                    {
                        player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能更改人數限制。");
                        return HookResult.Handled;
                    }

                    // 3. 執行對應動作
                    if (isEnableCmd) EnableOverride();
                    else DisableOverride();

                    return HookResult.Handled;
                }

                return HookResult.Continue;
            });

            // 每回合開始時強制套用（如果已開啟）
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        // 精確偵測熱身狀態
        private bool IsWarmup()
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            // 檢查是否處於熱身階段
            return gameRules?.InWarmup ?? false;
        }

        private void EnableOverride()
        {
            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}開啟{ChatColors.Default}人數最大化 8 v 8。");
        }

        private void DisableOverride()
        {
            _isOverrideEnabled = false;
            // 注意：禁用後我們不主動改回 5v5，而是讓遊戲下一回合自行恢復預設
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數最大化。");
        }

        private void ApplyTeamLimits()
        {
            int maxTs = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
            int maxCTs = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;
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
