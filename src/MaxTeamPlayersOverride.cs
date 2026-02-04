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
    // 使用 partial 確保能與 Version.cs 結合
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override";

        // 移除 ModuleVersion 定義，避免與 Version.cs 重複

        public override void Load(bool hotReload)
        {
            // 監聽聊天事件
            // 如果編譯器在此處報錯，代表你的 SDK 版本可能不支援 EventPlayerChat
            // 建議檢查 .csproj 中的 CounterStrikeSharp.API 版本
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                if (message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        if (IsWarmup()) {
                            _isOverrideEnabled = true;
                            ApplyTeamLimits();
                            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋。");
                        } else {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能修改。");
                        }
                        return HookResult.Handled;
                    }
                }
                else if (message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        if (IsWarmup()) {
                            _isOverrideEnabled = false;
                            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋。");
                        } else {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能修改。");
                        }
                        return HookResult.Handled;
                    }
                }

                return HookResult.Continue;
            });

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        // --- 核心修復：相容性最高的熱身判斷 ---
        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy == null || gameRulesProxy.GameRules == null) return false;

            // 嘗試使用不同的屬性名稱來獲取熱身狀態
            // 這是 CS2 中最底層的熱身判斷標誌
            return gameRulesProxy.GameRules.WarmupPeriod; 
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
