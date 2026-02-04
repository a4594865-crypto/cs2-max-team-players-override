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
        // 運行時狀態：地圖更換後會自動重置為 false
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override Plugin";

        public override void Load(bool hotReload)
        {
            // 註冊回合開始事件
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                // 如果未開啟覆蓋，則跳過邏輯
                if (!_isOverrideEnabled) return HookResult.Continue;

                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        // 修改指令為 "ctmax"
        // 權限：@css/generic (管理員)
        [ConsoleCommand("ctmax", "啟用或禁用人數覆蓋 (僅限本地圖生效)")]
        [CommandHelper(minArgs: 1, usage: "<1/0>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/admin")]
        public void OnMaxOverrideCommand(CCSPlayerController? player, CommandInfo info)
        {
            string arg = info.ArgByIndex(1);
            _isOverrideEnabled = arg == "1";

            if (_isOverrideEnabled)
            {
                // 從 Config 獲取數值，若為 -1 則自動平分伺服器最大人數
                int t = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
                int ct = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

                // 全服廣播
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已啟用人數覆蓋：{ChatColors.Red}{t}T {ChatColors.Default}v {ChatColors.Blue}{ct}CT");
                
                // 立即套用設定
                ApplyTeamLimits();
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已禁用人數覆蓋，下回合將恢復預設。");
            }
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
                if (ent.GameRules != null) // 安全檢查防止崩潰
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
