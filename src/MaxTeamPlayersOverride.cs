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

        public override string ModuleName => "Max Team Players Override (Warmup Only)";
        public override string ModuleVersion => "1.1.6";

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                // 處理 .ctmax
                if (message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        // 核心檢查：是否在熱身中
                        if (IsWarmup()) {
                            EnableOverride();
                        } else {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}指令失敗：只能在{ChatColors.Orange}熱身期間{ChatColors.Default}修改人數限制。");
                        }
                        return HookResult.Handled;
                    }
                }
                // 處理 .unctmax
                else if (message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        if (IsWarmup()) {
                            DisableOverride();
                        } else {
                            player.PrintToChat($" {ChatColors.Red}★ {ChatColors.Default}指令失敗：只能在{ChatColors.Orange}熱身期間{ChatColors.Default}恢復人數限制。");
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

        // 偵測目前是否為熱身時間
        private bool IsWarmup()
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            return gameRules?.WaitDuringWarmup ?? false; 
            // 註：CS2 中 WaitDuringWarmup 或 IsWarmupPeriod 是判斷熱身的標準屬性
        }

        private void EnableOverride()
        {
            _isOverrideEnabled = true;
            ApplyTeamLimits();
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已啟用人數最大化 8 v 8。");
        }

        private void DisableOverride()
        {
            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已禁用人數最大化。");
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
