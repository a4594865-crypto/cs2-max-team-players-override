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
        // 運行時狀態：此變數僅存在於記憶體中，地圖更換後會自動重置為 false
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override Plugin";

        public override void Load(bool hotReload)
        {
            // 註冊回合開始事件
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                // 如果管理員未輸入指令開啟，則不執行任何邏輯
                if (!_isOverrideEnabled) return HookResult.Continue;

                ApplyTeamLimits();
                return HookResult.Continue;
            });
        }

        // 註冊指令：css_maxoverride <1/0>
        // 權限設定：@css/generic (通常是擁有 flag 'b' 的管理員)
        [ConsoleCommand("css_maxoverride", "啟用或禁用人數覆蓋 (僅限本地圖生效)")]
        [CommandHelper(minArgs: 1, usage: "<1/0>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/generic")]
        public void OnMaxOverrideCommand(CCSPlayerController? player, CommandInfo info)
        {
            string arg = info.ArgByIndex(1);
            _isOverrideEnabled = arg == "1";

            if (_isOverrideEnabled)
            {
                // 計算預期人數
                int t = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
                int ct = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

                // 立即廣播給所有玩家
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已啟用人數覆蓋：{ChatColors.Red}{t}T {ChatColors.Default}v {ChatColors.Blue}{ct}CT");
                
                // 立即套用，不必等待下一局
                ApplyTeamLimits();
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已禁用人數覆蓋，下回合將恢復伺服器預設。");
            }
        }

        /// <summary>
        /// 核心邏輯：從 Config 讀取設定並套用到 GameRules
        /// </summary>
        private void ApplyTeamLimits()
        {
            int maxTs = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
            int maxCTs = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

            SetMaxTs(maxTs);
            SetMaxCTs(maxCTs);
        }

        private static void SetMaxTs(int num)
        {
            // 透過查找實體來修改遊戲規則
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                // 架構師提醒：安全檢查防止空引用導致伺服器崩潰
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
