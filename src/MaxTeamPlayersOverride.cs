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
        // 核心開關：預設為 false，確保插件在「怠速」狀態，不執行任何修改
        private bool _isOverrideEnabled = false;

        public override string ModuleName => "Max Team Players Override (Manual Toggle)";
        public override string ModuleVersion => "1.1.5";

        public override void Load(bool hotReload)
        {
            // 監聽聊天事件，實現自定義的 '.' 前綴指令
            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                string message = @event.Text.Trim();

                // 處理 .ctmax (開啟)
                if (message.Equals(".ctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        EnableOverride();
                        return HookResult.Handled; // 隱藏管理員輸入的指令訊息
                    }
                }
                // 處理 .unctmax (關閉)
                else if (message.Equals(".unctmax", StringComparison.OrdinalIgnoreCase))
                {
                    if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    {
                        DisableOverride();
                        return HookResult.Handled;
                    }
                }

                return HookResult.Continue;
            });

            // 註冊回合開始事件
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                // 【物理斷路器】只要沒開啟，此處直接返回，不搜尋實體、不消耗性能、不修改人數
                if (!_isOverrideEnabled) return HookResult.Continue;

                ApplyTeamLimits();
                return HookResult.Continue;
            });

            // 伺服器啟動日誌，方便管理員確認狀態
            Console.WriteLine("[MaxTeam] 插件載入成功。初始狀態：待命中（未啟用）。");
        }

        private void EnableOverride()
        {
            _isOverrideEnabled = true;

            int t = Config.MaxTs < 0 ? Server.MaxPlayers / 2 : Config.MaxTs;
            int ct = Config.MaxCTs < 0 ? Server.MaxPlayers / 2 : Config.MaxCTs;

            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Lime}啟用{ChatColors.Default}人數覆蓋：{ChatColors.Red}{t}T {ChatColors.Default}v {ChatColors.Blue}{ct}CT");
            
            // 啟用後立即套用一次，不需等下回合
            ApplyTeamLimits();
        }

        private void DisableOverride()
        {
            _isOverrideEnabled = false;
            Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Default}管理員已{ChatColors.Red}禁用{ChatColors.Default}人數覆蓋，下回合將恢復預設。");
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
