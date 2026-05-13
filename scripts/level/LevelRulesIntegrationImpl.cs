using GFrameworkGodotTemplate.scripts.core.controller.level;
using GFrameworkGodotTemplate.scripts.core.controller.level.interfaces;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.level.controllers;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡规则集成器实现（从BaseLevelController提取）
///     <para>
///         负责关卡全局规则系统的初始化、更新和超时处理
///         遵循单一职责原则(SRP)，专注于规则系统集成管理
///     </para>
/// </summary>
public class LevelRulesIntegrationImpl : ILevelRulesIntegration
{
    #region 私有字段

    /// <summary>日志记录器</summary>
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logError;
    private readonly Action<string> _logWarn;
    private readonly Action<string> _logDebug;

    /// <summary>关卡规则控制器实例（接口类型）</summary>
    private ILevelRulesController? _rulesController;

    /// <summary>是否已初始化规则控制器</summary>
    private bool _rulesControllerInitialized;

    /// <summary>是否已完成游戏</summary>
    private bool _isGameCompleted;
    /// <summary>获取当前阶段状态的委托（与BaseLevelController实时同步）</summary>
    private readonly Func<LevelPhase> _getCurrentPhase;

    /// <summary>设置当前阶段的回调（通知BaseLevelController更新状态）</summary>
    private readonly Action<LevelPhase> _setCurrentPhase;

    /// <summary>获取成功阶段静态标志的委托</summary>
    private readonly Func<bool> _getIsSuccessPhaseActive;

    /// <summary>设置成功阶段静态标志的回调</summary>
    private readonly Action<bool> _setIsSuccessPhaseActive;

    /// <summary>显示失败界面回调</summary>
    private Func<Task>? _showDefeatUiCallback;

    /// <summary>禁止玩家输入回调</summary>
    private Action? _disablePlayerInputCallback;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建关卡规则集成器实例
    /// </summary>
    /// <param name="getCurrentPhase">获取当前阶段状态的委托</param>
    /// <param name="setCurrentPhase">设置当前阶段的回调</param>
    /// <param name="getIsSuccessPhaseActive">获取成功阶段标志的委托</param>
    /// <param name="setIsSuccessPhaseActive">设置成功阶段标志的回调</param>
    /// <param name="logInfo">信息日志</param>
    /// <param name="logError">错误日志</param>
    /// <param name="logWarn">警告日志</param>
    /// <param name="logDebug">调试日志</param>
    public LevelRulesIntegrationImpl(
        Func<LevelPhase> getCurrentPhase,
        Action<LevelPhase> setCurrentPhase,
        Func<bool> getIsSuccessPhaseActive,
        Action<bool> setIsSuccessPhaseActive,
        Action<string>? logInfo = null,
        Action<string>? logError = null,
        Action<string>? logWarn = null,
        Action<string>? logDebug = null)
    {
        _getCurrentPhase = getCurrentPhase;
        _setCurrentPhase = setCurrentPhase;
        _getIsSuccessPhaseActive = getIsSuccessPhaseActive;
        _setIsSuccessPhaseActive = setIsSuccessPhaseActive;

        _logInfo = logInfo ?? (msg => { });
        _logError = logError ?? (msg => { });
        _logWarn = logWarn ?? (msg => { });
        _logDebug = logDebug ?? (msg => { });
    }

    #endregion

    #region ILevelRulesIntegration 接口实现

    /// <summary>初始化规则控制器</summary>
    public void Initialize()
    {
        _logInfo("════════════ 初始化关卡规则系统 ═══════════");

        try
        {
            var currentGameLevel = LevelChoose.CurrentGameLevel;
            _logInfo($"[LevelRulesIntegration] 当前GameLevel: {currentGameLevel}");

            _rulesController = LevelRulesController.CreateForLevel(currentGameLevel);

            _rulesController.TimeOut += OnLevelTimeOut;
            _rulesController.PhaseChanged += OnRulesPhaseChanged;
            _rulesController.TimerTick += OnTimerTick;

            _rulesControllerInitialized = true;

            _logInfo("[LevelRulesIntegration] ✓✓✓ 关卡规则控制器已初始化");
            _logInfo($"[LevelRulesIntegration]   配置: {_rulesController.Config.DisplayName}");
            _logInfo($"[LevelRulesIntegration]   时限: {_rulesController.Config.MaxTimeDisplay}");
            _logInfo($"[LevelRulesIntegration]   时间限制: {(_rulesController.Config.IsTimeLimited ? "已启用" : "未启用")}");

            if (currentGameLevel == GameLevel.LevelTest)
            {
                _logWarn("[LevelRulesIntegration] ⚠ 检测到测试关卡，使用10秒超时配置");
                _logWarn("[LevelRulesIntegration]   用于快速验证超时处理流程");
            }
        }
        catch (Exception ex)
        {
            _logError($"[LevelRulesIntegration] ❌ 规则控制器初始化失败: {ex.Message}");
            _logError("[LevelRulesIntegration]   将在无时间限制模式下运行");
            _rulesControllerInitialized = false;
        }

        _logInfo("════════════ 规则系统初始化完成 ═══════════");
    }

    /// <summary>每帧更新规则状态</summary>
    public void Update()
    {
        if (_rulesControllerInitialized && _rulesController != null && !_isGameCompleted)
        {
            _rulesController.Update();
        }
    }

    /// <summary>清理规则系统资源</summary>
    public void Cleanup()
    {
        if (_rulesController != null)
        {
            _rulesController.TimeOut -= OnLevelTimeOut;
            _rulesController.PhaseChanged -= OnRulesPhaseChanged;
            _rulesController.TimerTick -= OnTimerTick;
            _rulesController.ResetTimer();
            _rulesController = null;
            _logDebug("[LevelRulesIntegration] ✓ 规则控制器已清理");
        }

        _rulesControllerInitialized = false;
        _isGameCompleted = false;
        _logDebug("[LevelRulesIntegration] 规则集成器已清理");
    }

    /// <summary>获取是否已初始化</summary>
    public bool IsInitialized => _rulesControllerInitialized;

    #endregion

    #region 公开属性

    /// <summary>获取或设置是否已完成游戏</summary>
    public bool IsGameCompleted
    {
        get => _isGameCompleted;
        set => _isGameCompleted = value;
    }

    /// <summary>设置显示失败界面回调</summary>
    public Func<Task>? ShowDefeatUiCallback
    {
        set => _showDefeatUiCallback = value;
    }

    /// <summary>设置禁止玩家输入回调</summary>
    public Action? DisablePlayerInputCallback
    {
        set => _disablePlayerInputCallback = value;
    }

    /// <summary>获取当前阶段（只读，通过委托实时同步）</summary>
    public LevelPhase CurrentPhase => _getCurrentPhase();

    /// <summary>获取成功阶段激活标志（通过委托实时同步）</summary>
    public bool IsSuccessPhaseActive => _getIsSuccessPhaseActive();

    #endregion

    #region 私有方法 - 事件处理

    /// <summary>处理计时器超时事件</summary>
    private async void OnLevelTimeOut(long elapsedMs, long maxTimeMs)
    {
        _logError("════════════ ⏱ 开始处理关卡超时 ═══════════");
        _logError($"[LevelRulesIntegration] 已用时间: {elapsedMs / 1000.0:F3}秒");
        _logError($"[LevelRulesIntegration] 时间限制: {maxTimeMs / 1000.0:F3}秒");
        _logError($"[LevelRulesIntegration] 超出时长: {(elapsedMs - maxTimeMs) / 1000.0:F3}秒");

        try
        {
            _logInfo("[LevelRulesIntegration] 步骤1/3: 原子化更新关卡状态为Defeat...");
            UpdatePhaseToDefeat();

            _logInfo("[LevelRulesIntegration] 步骤2/3: 加载失败界面...");
            await ShowDefeatUiInternal();

            _logInfo("[LevelRulesIntegration] 步骤3/3: 禁止玩家输入...");
            DisablePlayerInputInternal();

            _logError("════════════ ✅ 超时处理完成 ═══════════");
            _logError("[LevelRulesIntegration] 当前状态: Defeat（失败界面显示中）");
            _logError("[LevelRulesIntegration] 允许的操作: 鼠标点击'再玩一次'或'返回主菜单'");
        }
        catch (Exception ex)
        {
            _logError($"[LevelRulesIntegration] ❌ 超时处理异常: {ex.Message}");
            _logError("[LevelRulesIntegration]   尝试强制显示失败界面...");

            try
            {
                await ShowDefeatUiInternal();
            }
            catch (Exception innerEx)
            {
                _logError($"[LevelRulesIntegration] ❌❌ 强制显示失败界面也失败: {innerEx.Message}");
                _logError("[LevelRulesIntegration]   建议: 检查level_defate_ui.tscn是否存在且配置正确");
            }
        }
    }

    /// <summary>原子化更新关卡阶段为Defeat状态</summary>
    private void UpdatePhaseToDefeat()
    {
        var oldPhase = _getCurrentPhase();
        _setCurrentPhase((LevelPhase)3);
        _isGameCompleted = true;
        _setIsSuccessPhaseActive(true);

        _logWarn($"[LevelRulesIntegration] 阶段已原子化更新: {oldPhase} → Defeat");
        _logWarn("[LevelRulesIntegration] IsSuccessPhaseActive = true（禁止玩家移动）");
    }

    /// <summary>内部显示失败界面</summary>
    private async Task ShowDefeatUiInternal()
    {
        if (_showDefeatUiCallback != null)
        {
            await _showDefeatUiCallback();
        }
    }

    /// <summary>内部禁止玩家输入</summary>
    private void DisablePlayerInputInternal()
    {
        _disablePlayerInputCallback?.Invoke();
    }

    /// <summary>处理规则系统的阶段变更事件</summary>
    private void OnRulesPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
    {
        _logInfo($"[LevelRulesIntegration] [规则系统] 阶段变更: {oldPhase} → {newPhase}");
    }

    /// <summary>处理计时器Tick事件（每秒一次，用于UI更新）</summary>
    private void OnTimerTick(long remainingMs)
    {
        if (remainingMs <= 5000 && remainingMs > 0)
        {
            _logWarn($"[LevelRulesIntegration] ⏱ 剩余时间: {remainingMs / 1000}秒");
        }
    }

    #endregion
}
