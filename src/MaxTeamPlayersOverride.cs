using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        // 效能優化：快取 GameRules 實體，避免每回合遍歷所有實體，降低 i7-13700 的 L3 快取壓力
        private CCSGameRulesProxy? _cachedGameRules;

        public override string ModuleName => "Max Team Players Override (1v1 Spectator Fix)";

        public override void Load(bool hotReload)
        {
            // 解決方案 1：地圖載入時初始化，強制開放觀戰配額
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                _cachedGameRules = null; // 換圖必須重置快取
                
                // 強制開啟觀戰位，確保底層通道可用
                Server.ExecuteCommand("mp_spectators_max 10");
                
                // 延遲 1 秒確保地圖實體加載完畢後執行第一次鎖定
                AddTimer(1.0f, ApplyLimitsNow);
            });

            // 解決方案 2：關鍵修正！玩家進入伺服器時立即更新規則
            // 在玩家看到「選隊 UI」之前，1v1 的規則就已經生效，避免 UI 判斷「人數已滿」而鎖死觀戰
            RegisterListener<Listeners.OnClientPutInServer>(playerSlot =>
            {
                ApplyLimitsNow();
            });

            // 第三重保險：回合開始時同步規則
            RegisterEventHandler<EventRoundStart>((_, _) =>
            {
                ApplyLimitsNow();
                return HookResult.Continue;
            });
        }

        private void ApplyLimitsNow()
        {
            // 如果快取無效，才執行 FindAllEntities (將每回合 2 次搜尋減為 0 次)
            if (_cachedGameRules == null || !_cachedGameRules.IsValid)
            {
                _cachedGameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            }

            if (_cachedGameRules?.GameRules != null)
            {
                // 讀取 Config.cs 裡的數值
                int maxTs = Config.MaxTs;
                int maxCTs = Config.MaxCTs;

                // 如果 Config 設為 -1，自動取伺服器人數一半
                if (maxTs < 0) maxTs = Server.MaxPlayers / 2;
                if (maxCTs < 0) maxCTs = Server.MaxPlayers / 2;

                // 核心邏輯：鎖定重生坑位與隊伍上限
                _cachedGameRules.GameRules.NumSpawnableTerrorist = maxTs;
                _cachedGameRules.GameRules.MaxNumTerrorists = maxTs;
                _cachedGameRules.GameRules.NumSpawnableCT = maxCTs;
                _cachedGameRules.GameRules.MaxNumCTs = maxCTs;
            }
        }
    }
}
