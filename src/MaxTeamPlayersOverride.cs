using System;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities;

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        // 核心狀態開關：預設關閉 (5v5)
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override (Warmup Locked)";

        // 注意：這裡不定義 ModuleVersion，因為它已經在 src/Version.cs 裡面了，避免重複定義錯誤

        public override void Load(bool hotReload)
        {
            // 監聽聊天事件，處理自定義指令
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                bool isEnableCmd = message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase);
                bool isDisableCmd = message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase);

                if (isEnableCmd || isDisableCmd)
                {
                    // 1. 權限檢查：需具備 generic 管理權限
                    if (!AdminManager.PlayerHasPermissions(player, "@css/generic")) return HookResult.Continue;

                    // 2. 熱身檢查邏輯
                    if (!IsWarmup())
                    {
                        player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能更改人數限制。");
                        return HookResult.Handled;
                    }

                    // 3. 執行動作
                    if (isEnableCmd) EnableOverride();
                    else DisableOverride();

                    return HookResult.Handled;
                }

                return HookResult.Continue;
            });

            // 每回合開始時強制套用人數限制
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });

            Console.WriteLine("[MaxTeam] 插件載入成功。目前狀態：等待管理員於熱身期間啟用。");
        }

        // 修改後的熱身判斷邏輯：使用 WaitDuringWarmup 提高相容性
        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            var gameRules = gameRulesProxy?.GameRules;

            // CS2 引擎判斷是否在熱身階段的關鍵屬性
            return gameRules?.WaitDuringWarmup ?? false;
        }

        private void EnableOverride()
        {
            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋（突破 5v5 限制）。");
        }

        private void DisableOverride()
        {
            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋（下回合恢復 5v5）。");
        }

        private void ApplyTeamLimits()
        {
            // 這些數值會自動從你的 config.json 讀取
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
