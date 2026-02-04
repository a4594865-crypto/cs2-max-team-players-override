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
            // 使用強制路徑引用，解決找不到 EventPlayerChat 的問題
            RegisterEventHandler<CounterStrikeSharp.API.Core.EventPlayerChat>((@event, info) =>
            {
                if (@event == null) return HookResult.Continue;
                
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

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
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能修改。");
                        }
                        return HookResult.Handled;
                    }
                }
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
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}錯誤：{ChatColors.Orange}僅限熱身期間{ChatColors.Default}才能修改。");
                        }
                        return HookResult.Handled;
                    }
                }

                return HookResult.Continue;
            });

            RegisterEventHandler<CounterStrikeSharp.API.Core.EventRoundStart>((@event, info) =>
            {
                if (!_isOverrideEnabled) return HookResult.Continue;
                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        private bool IsWarmup()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            // 針對 1.0.362 版本的實體屬性路徑
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
