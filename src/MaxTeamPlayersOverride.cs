using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        // 優化：快取 GameRules 實體，避免每回合遍歷所有實體，降低 i7-13700 的 L3 快取壓力
        private CCSGameRulesProxy? _cachedGameRules;

        public override string ModuleName => "Max Team Players Override (1v1 Spectator Fix)";

        public override void Load(bool hotReload)
        {
            // 解決方案 1：地圖載入時立即初始化，確保觀戰配額
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                _cachedGameRules = null; // 換圖後必須重置快取
                
                // 強制開放觀戰位，並縮短選隊猶豫時間，強迫 UI 刷新狀態
                Server.ExecuteCommand("mp_spectators_max 10");
                Server.ExecuteCommand("mp_force_pick_time 5"); 
                
                // 延遲 1 秒確保地圖實體加載完畢後執行第一次鎖定
                AddTimer(1.0f, ApplyLimitsNow);
            });

            // 解決方案 2：關鍵修正！玩家進入伺服器活性狀態（看到選隊 UI 前）立刻執行
            // 這能解決「前 2 人選完，後 2 人不能選觀戰」的 Bug，因為規則在此時已穩定
            RegisterListener<Listeners.OnClientActive>(playerSlot =>
            {
                ApplyLimitsNow();
            });

            // 每回合開始時進行三重保險檢查
            RegisterEventHandler<EventRoundStart>((_, _) =>
            {
                ApplyLimitsNow();
                return HookResult.Continue;
            });
        }

        private void ApplyLimitsNow()
        {
            // 如果快取無效，才執行 FindAllEntities (大幅減少搜尋次數)
            if (_cachedGameRules == null || !_cachedGameRules.IsValid)
            {
                _cachedGameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            }

            if (_cachedGameRules?.GameRules != null)
            {
                // 1v1 專用：直接讀取 Config 數值（建議 Config 內 MaxTs/CTs 設為 1）
                int maxTs = Config.MaxTs < 0 ? 1 : Config.MaxTs;
                int maxCTs = Config.MaxCTs < 0 ? 1 : Config.MaxCTs;

                // 同步鎖定重生點與隊伍上限
                _cachedGameRules.GameRules.NumSpawnableTerrorist = maxTs;
                _cachedGameRules.GameRules.MaxNumTerrorists = maxTs;
                _cachedGameRules.GameRules.NumSpawnableCT = maxCTs;
                _cachedGameRules.GameRules.MaxNumCTs = maxCTs;
            }
        }
    }
}
