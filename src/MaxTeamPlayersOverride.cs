using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MaxTeamPlayersOverride
{
    public partial class MaxTeamPlayersOverride : BasePlugin
    {
        // 效能優化：快取 GameRules 實體，避免每回合掃描記憶體浪費 L3 快取
        private CCSGameRulesProxy? _cachedGameRules;

        public override string ModuleName => "Max Team Players Override (1v1 Spectator Fix)";

        public override void Load(bool hotReload)
        {
            // 修正觀戰問題 1：地圖載入時立即強制開放觀戰名額
            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                _cachedGameRules = null; // 換圖必須刷新快取
                Server.ExecuteCommand("mp_spectators_max 10"); // 強制底層觀戰位
                
                // 延遲 1 秒確保地圖實體完全加載後執行初次鎖定
                AddTimer(1.0f, ApplyLimitsNow);
            });

            // 修正觀戰問題 2：玩家「進入伺服器活性狀態」時立刻刷新規則
            // 這是解決「前 2 人選完隊，後 2 人無法選觀戰」的關鍵點
            RegisterListener<Listeners.OnClientActive>(playerSlot =>
            {
                ApplyLimitsNow();
            });

            // 第三重保險：每回合開始時同步規則
            RegisterEventHandler<EventRoundStart>((_, _) =>
            {
                ApplyLimitsNow();
                return HookResult.Continue;
            });
        }

        private void ApplyLimitsNow()
        {
            // 只有快取失效時才執行搜尋，節省 CPU 資源
            if (_cachedGameRules == null || !_cachedGameRules.IsValid)
            {
                _cachedGameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            }

            if (_cachedGameRules?.GameRules != null)
            {
                // 直接讀取你的 Config 設定 (預設 1v1 應為 1)
                int maxTs = Config.MaxTs < 0 ? 1 : Config.MaxTs;
                int maxCTs = Config.MaxCTs < 0 ? 1 : Config.MaxCTs;

                // 核心邏輯：鎖定重生坑位與隊伍上限
                _cachedGameRules.GameRules.NumSpawnableTerrorist = maxTs;
                _cachedGameRules.GameRules.MaxNumTerrorists = maxTs;
                _cachedGameRules.GameRules.NumSpawnableCT = maxCTs;
                _cachedGameRules.GameRules.MaxNumCTs = maxCTs;
            }
        }
    }
}
