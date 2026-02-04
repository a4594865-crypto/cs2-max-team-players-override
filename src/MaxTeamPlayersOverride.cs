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
    // 使用 partial 確保與 Version.cs 結合，請確認 Version.cs 裡的命名空間也是 MaxTeamPlayersOverride
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override";

        // 注意：此處已移除 ModuleVersion，請統一在 src/Version.cs 修改版本號以觸發發佈

        public override void Load(bool hotReload)
        {
            // 監聽聊天事件處理自定義指令 (.ctmax / .unctmax)
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                // 處理開啟指令
                if (message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        if (IsWarmup()) 
                        {
                            _isOverrideEnabled = true;
                            ApplyTeamLimits();
                            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋。");
                        } 
                        else 
                        {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能修改人數限制。");
                        }
                        return HookResult.Handled;
                    }
                }
                // 處理關閉指令
                else if (message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        if (IsWarmup()) 
                        {
                            _isOverrideEnabled = false;
                            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋。");
                        } 
                        else 
                        {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能恢復預設。");
                        }
                        return HookResult.Handled;
                    }
                }

                return HookResult.Continue;
            });

            // 每回合開始檢查並套用限制
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        // 針對 1.0.362 優化的熱身判斷
        private bool IsWarmup()
        {
            // 獲取 GameRules 實體
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy == null || gameRulesProxy.GameRules == null) return false;

            // 在最新 SDK 中，WarmupPeriod 是標準屬性
            return gameRulesProxy.GameRules.WarmupPeriod;
        }

        private void ApplyTeamLimits()
        {
            // 這些數值來自自動生成的 config.json
            int maxTs = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
            int maxCTs = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                if (ent.GameRules != null)
                {
                    // 強制覆寫隊伍生成與上限人數
                    ent.GameRules.NumSpawnableTerrorist = maxTs;
                    ent.GameRules.MaxNumTerrorists = maxTs;
                    ent.GameRules.NumSpawnableCT = maxCTs;
                    ent.GameRules.MaxNumCTs = maxCTs;
                }
            }
        }
    }
}
