using GFramework.Core.Coroutine.Instructions;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.level.config;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.level.ui;
using Godot;

namespace GFrameworkGodotTemplate.scripts.core.ui.level;

/// <summary>
///     关卡UI管理器实现（从BaseLevelController提取）
///     <para>
///         负责关卡场景中所有UI界面的加载、切换、清理和状态管理
///         遵循单一职责原则(SRP)，专注于UI生命周期管理
///     </para>
/// </summary>
public class LevelUiManagerImpl : ILevelUiManager
{
    #region 私有字段

    /// <summary>UI路由器引用</summary>
    private readonly IUiRouter? _uiRouter;

    /// <summary>日志记录器</summary>
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logWarn;
    private readonly Action<string> _logError;
    private readonly Action<string> _logDebug;

    /// <summary>所属的Node2D节点（用于添加子节点）</summary>
    private readonly Node2D _ownerNode;

    /// <summary>获取当前阶段状态的委托（与BaseLevelController实时同步）</summary>
    private readonly Func<LevelPhase> _getCurrentPhase;

    /// <summary>设置当前阶段的回调（通知BaseLevelController更新状态）</summary>
    private readonly Action<LevelPhase> _setCurrentPhase;

    /// <summary>获取构建阶段静态标志的委托</summary>
    private readonly Func<bool> _getIsBuildPhaseActive;

    /// <summary>设置构建阶段静态标志的回调</summary>
    private readonly Action<bool> _setIsBuildPhaseActive;

    /// <summary>关卡Build UI场景路径（备用直接加载）</summary>
    private const string LevelBuildUiScenePath = "res://scenes/level/level_ui/level_build_ui.tscn";

    /// <summary>关卡失败UI场景路径</summary>
    private const string LevelDefateUiScenePath = "res://scenes/level/level_ui/level_defate_ui.tscn";

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建关卡UI管理器实例
    /// </summary>
    /// <param name="uiRouter">UI路由器</param>
    /// <param name="ownerNode">所属节点</param>
    /// <param name="getCurrentPhase">获取当前阶段状态的委托</param>
    /// <param name="setCurrentPhase">设置当前阶段的回调</param>
    /// <param name="getIsBuildPhaseActive">获取构建阶段标志的委托</param>
    /// <param name="setIsBuildPhaseActive">设置构建阶段标志的回调</param>
    /// <param name="logInfo">信息日志</param>
    /// <param name="logWarn">警告日志</param>
    /// <param name="logError">错误日志</param>
    /// <param name="logDebug">调试日志</param>
    public LevelUiManagerImpl(
        IUiRouter? uiRouter,
        Node2D ownerNode,
        Func<LevelPhase> getCurrentPhase,
        Action<LevelPhase> setCurrentPhase,
        Func<bool> getIsBuildPhaseActive,
        Action<bool> setIsBuildPhaseActive,
        Action<string>? logInfo = null,
        Action<string>? logWarn = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null)
    {
        _uiRouter = uiRouter;
        _ownerNode = ownerNode;
        _getCurrentPhase = getCurrentPhase;
        _setCurrentPhase = setCurrentPhase;
        _getIsBuildPhaseActive = getIsBuildPhaseActive;
        _setIsBuildPhaseActive = setIsBuildPhaseActive;

        _logInfo = logInfo ?? (msg => { });
        _logWarn = logWarn ?? (msg => { });
        _logError = logError ?? (msg => { });
        _logDebug = logDebug ?? (msg => { });
    }

    #endregion

    #region ILevelUiManager 接口实现

    /// <summary>显示构建界面</summary>
    public async Task ShowBuildUiAsync()
    {
        _logInfo("════════════ 显示构建界面 ═══════════");
        _logInfo("[LevelUiManager] 正在加载 LevelBuildUi...");

        if (_uiRouter == null)
        {
            _logError("[LevelUiManager] ✗ 致命错误：UI路由器未初始化！");
            _logError("[LevelUiManager] 请检查：");
            _logError("  1. GFramework是否正确初始化");
            _logError("  2. IUiRouter服务是否已注册");
            _logError("  3. GameEntryPoint配置是否完整");

            _setCurrentPhase(LevelPhase.Play);
            return;
        }

        try
        {
            var uiKeyName = nameof(UiKey.LevelBuildUi);
            _logDebug($"[LevelUiManager] UI Key: {uiKeyName}");
            _logDebug("[LevelUiManager] 尝试通过 UiRouter.PushAsync() 加载...");

            await _uiRouter.PushAsync(uiKeyName).AsTask();

            _setCurrentPhase(LevelPhase.Build);
            _setIsBuildPhaseActive(true);

            _logInfo("[LevelUiManager] ✓✓✓ LevelBuildUi 加载成功！");
            _logInfo("[LevelUiManager] 当前阶段: Build（输入限制已启用）");
            _logInfo($"[LevelUiManager] ⚠ IsBuildPhaseActive = {_getIsBuildPhaseActive()}（移动限制已激活）");
            _logInfo("[LevelUiManager] 允许的操作: 鼠标点击 + ESC键");
            _logInfo("[LevelUiManager] 禁止的操作: 键盘/手柄输入");

            ConnectBuildFinishedSignal();
        }
        catch (Exception ex) when (ex.Message.Contains("not registered", StringComparison.OrdinalIgnoreCase))
        {
            await HandleUiNotRegisteredError(ex, "LevelBuildUi", LevelBuildUiScenePath);
        }
        catch (Exception ex)
        {
            _logError("[LevelUiManager] ✗ 显示构建界面时发生未知异常");
            _logError($"[LevelUiManager] 异常类型: {ex.GetType().Name}");
            _logError($"[LevelUiManager] 异常消息: {ex.Message}");
            _logError($"[LevelUiManager] 堆栈跟踪: {ex.StackTrace}");

            _logWarn("[LevelUiManager] ⚠ 尝试备用直接加载方案...");

            if (await TryDirectLoadUiAsync(LevelBuildUiScenePath, "LevelBuildUi"))
            {
                _setCurrentPhase(LevelPhase.Build);
                _setIsBuildPhaseActive(true);

                ConnectBuildFinishedSignal();
            }
            else
            {
                _setCurrentPhase(LevelPhase.Play);
                _logWarn("[LevelUiManager] ⚠ 由于加载失败，已自动切换到游玩模式");
            }
        }
    }

    /// <summary>切换到游玩阶段</summary>
    public void OnBuildFinished()
    {
        _logInfo("════════════ 收到构建完成通知 ═══════════");
        _logInfo($"[LevelUiManager] 当前阶段: {_getCurrentPhase()}");

        if (_getCurrentPhase() != LevelPhase.Build)
        {
            _logWarn($"[LevelUiManager] ⚠ 当前不在Build阶段（{_getCurrentPhase()}），忽略切换请求");
            return;
        }

        _logInfo("[LevelUiManager] 开始切换到游玩阶段...");

        try
        {
            SwitchToPlayPhaseCoroutine().RunCoroutine();
            _logInfo("[LevelUiManager] ✓ 切换协程已启动");
        }
        catch (Exception ex)
        {
            _logError("[LevelUiManager] ❌ 启动切换协程时发生异常");
            _logError($"[LevelUiManager] 异常类型: {ex.GetType().Name}");
            _logError($"[LevelUiManager] 异常消息: {ex.Message}");
            _logError($"[LevelUiManager] 堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>显示成功界面</summary>
    public void ShowSuccessUi()
    {
        ShowSuccessUiCoroutine().RunCoroutine();
    }

    /// <summary>显示失败界面</summary>
    public async Task ShowDefeatUiAsync()
    {
        if (_uiRouter == null)
        {
            _logError("[LevelUiManager] ✗ UI路由器不可用，无法加载失败界面！");
            return;
        }

        try
        {
            _logInfo("[LevelUiManager] → 清除当前UI...");
            await _uiRouter.ClearAsync();

            _logInfo("[LevelUiManager] → 加载失败界面 (level_defate_ui.tscn)...");
            await _uiRouter.PushAsync(nameof(UiKey.LevelDefateUi));

            _setCurrentPhase(LevelPhase.Defeat);
            _logInfo("[LevelUiManager] ★ 阶段已切换为 Defeat (ESC暂停已阻断)");

            _logInfo("[LevelUiManager] ✓✓✓ 失败界面已成功显示！");
            _logInfo("[LevelUiManager] 用户可选择:");
            _logInfo("[LevelUiManager]   • '再玩一次' - 重新开始当前关卡");
            _logInfo("[LevelUiManager]   • '返回主菜单' - 返回关卡选择界面");
        }
        catch (Exception ex)
        {
            _logError($"[LevelUiManager] ❌ 加载失败界面失败: {ex.Message}");
            _logError("[LevelUiManager] 可能的原因:");
            _logError("  1. UiKey枚举中未定义LevelDefateUi");
            _logError("  2. level_defate_ui.tscn文件不存在或损坏");
            _logError("  3. UI路由器内部错误");
            throw;
        }
    }

    /// <summary>清除现有UI</summary>
    public async Task ClearExistingUiAsync()
    {
        if (_uiRouter == null)
        {
            _logWarn("[LevelUiManager] UI路由器为空，跳过UI清理");
            return;
        }

        try
        {
            _logInfo("════════════ 清理现有UI ═══════════");
            _logInfo("[LevelUiManager] 步骤1: 开始清除所有现有UI...");

            var clearTask = _uiRouter.ClearAsync().AsTask();
            await clearTask;

            _logDebug("[LevelUiManager] ✓ UiRouter.ClearAsync() 完成");

            _logInfo("[LevelUiManager] 步骤2: 等待UI系统稳定（0.3秒）...");
            await Task.Delay(300);

            _logInfo("[LevelUiManager] ✓✓✓ 现有UI已完全清除");
            _logInfo("════════════ UI清理完成 ═══════════");
        }
        catch (Exception ex)
        {
            _logWarn($"[LevelUiManager] ⚠ 清除UI时出现异常: {ex.Message}");
            _logDebug($"[LevelUiManager] 异常类型: {ex.GetType().Name}");
        }
    }

    /// <summary>连接BuildFinished信号</summary>
    public void ConnectBuildFinishedSignal()
    {
        _logInfo("[LevelUiManager] 正在连接BuildFinished信号...");

        try
        {
            var buildUi = FindLevelBuildUi();

            if (buildUi == null)
            {
                _logWarn("[LevelUiManager] ⚠ 未找到LevelBuildUi节点，跳过信号连接");
                _logWarn("[LevelUiManager] 将依赖LevelBuildUi的直接调用方式");
                return;
            }

            if (buildUi.IsConnected(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished)))
            {
                _logDebug("[LevelUiManager] BuildFinished信号已连接（跳过重复连接）");
                return;
            }

            buildUi.Connect(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished));

            _logInfo("[LevelUiManager] ✓✓✓ BuildFinished信号已成功连接");
            _logInfo("[LevelUiManager] 双重保障机制已激活：直接调用 + 信号通知");
        }
        catch (Exception ex)
        {
            _logWarn($"[LevelUiManager] ⚠ 连接BuildFinished信号时出现异常: {ex.Message}");
            _logDebug($"[LevelUiManager] 异常类型: {ex.GetType().Name}");
        }
    }

    #endregion

    #region 私有方法 - UI加载和切换

    /// <summary>处理UI未注册错误</summary>
    private async Task HandleUiNotRegisteredError(Exception ex, string uiName, string scenePath)
    {
        _logError("╔══════════════════════════════════════════╗");
        _logError("║  ❌ 致命错误：UI Key 未注册！              ║");
        _logError("╚══════════════════════════════════════════╝");
        _logError($"[LevelUiManager] 错误详情: {ex.Message}");
        _logError("");
        _logError("【解决方案 - 必须在Godot编辑器中完成以下配置】");

        if (uiName == "LevelBuildUi")
        {
            OutputBuildUiConfigurationGuide();
        }

        _logError("");
        _logError("【技术细节】");
        _logError($"  异常类型: {ex.GetType().FullName}");
        _logError($"  堆栈跟踪: {ex.StackTrace}");

        _logWarn("[LevelUiManager] ⚠ UiRouter加载失败，尝试备用方案：直接加载场景...");

        if (await TryDirectLoadUiAsync(scenePath, uiName))
        {
            _logInfo($"[LevelUiManager] ✓✓✓ 备用方案成功！{uiName}已通过直接加载显示");
            _setCurrentPhase(LevelPhase.Build);
            _setIsBuildPhaseActive(true);

            ConnectBuildFinishedSignal();
        }
        else
        {
            _setCurrentPhase(LevelPhase.Play);
            _logWarn("[LevelUiManager] ⚠ 所有加载方案均失败，已自动切换到游玩模式（无UI）");
        }
    }

    /// <summary>输出BuildUI配置指南</summary>
    private void OutputBuildUiConfigurationGuide()
    {
        _logError("步骤1: 打开场景 scenes/main/GameEntryPoint.tscn");
        _logError("步骤2: 选择根节点 GameEntryPoint");
        _logError("步骤3: 在检查器中找到 'Ui Page Configs' 属性");
        _logError("步骤4: 找到或新增 UiPageConfig，设置以下值：");
        _logError("");
        _logError("  ┌─ 索引 8 (LevelBuildUi) ─┐");
        _logError("  │ Ui Key   : LevelBuildUi  │");
        _logError("  │ Scene    : [点击打开场景] │");
        _logError("  │          : 选择           │");
        _logError("  │  scenes/level/level_ui/   │");
        _logError("  │  level_build_ui.tscn      │");
        _logError("  └───────────────────────────┘");
        _logError("");
        _logError("  ┌─ 索引 9 (LevelPlayUi) ──┐");
        _logError("  │ Ui Key   : LevelPlayUi   │");
        _logError("  │ Scene    : [点击打开场景] │");
        _logError("  │          : 选择           │");
        _logError("  │  scenes/level/level_ui/   │");
        _logError("  │  level_play_ui.tscn       │");
        _logError("  └───────────────────────────┘");
        _logError("");
        _logError("  ┌─ 索引 10 (LevelSuccessUi) ┐");
        _logError("  │ Ui Key   : LevelSuccessUi│");
        _logError("  │ Scene    : [点击打开场景] │");
        _logError("  │          : 选择           │");
        _logError("  │  scenes/level/level_ui/   │");
        _logError("  │  level_success_ui.tscn    │");
        _logError("  └───────────────────────────┘");
        _logError("");
        _logError("步骤5: 保存场景 (Ctrl+S)");
        _logError("步骤6: 重新运行游戏");
    }

    /// <summary>备用方案：直接加载UI场景</summary>
    private async Task<bool> TryDirectLoadUiAsync(string scenePath, string uiName)
    {
        try
        {
            _logInfo($"[LevelUiManager] 🔄 启动备用加载方案: {uiName}");
            _logDebug($"[LevelUiManager] 场景路径: {scenePath}");

            if (!ResourceLoader.Exists(scenePath))
            {
                _logError($"[LevelUiManager] ✗ 场景文件不存在: {scenePath}");
                return false;
            }

            var packedScene = GD.Load<PackedScene>(scenePath);
            if (packedScene == null)
            {
                _logError($"[LevelUiManager] ✗ 无法加载PackedScene: {scenePath}");
                return false;
            }

            var uiInstance = packedScene.Instantiate<Control>();
            if (uiInstance == null)
            {
                _logError($"[LevelUiManager] ✗ 无法实例化UI节点: {scenePath}");
                return false;
            }

            _logDebug($"[LevelUiManager] UI实例类型: {uiInstance.GetType().Name}");

            _ownerNode.AddChild(uiInstance);

            _logInfo($"[LevelUiManager] ✓✓ 备用方案成功: {uiName} 已添加到场景");
            _logInfo($"[LevelUiManager] UI路径: {uiInstance.GetPath()}");

            return true;
        }
        catch (Exception directEx)
        {
            _logError($"[LevelUiManager] ✗ 备用加载方案也失败了: {directEx.Message}");
            _logError($"[LevelUiManager] 备用方案异常详情: {directEx.StackTrace}");
            return false;
        }
    }

    /// <summary>切换到游玩阶段的协程</summary>
    private IEnumerator<IYieldInstruction> SwitchToPlayPhaseCoroutine()
    {
        _logInfo("[LevelUiManager] ═══════════ 切换到游玩阶段 ═══════════");

        _logInfo("[LevelUiManager] 步骤1[关闭BuildUI]: 正在清除构建界面...");

        if (_uiRouter == null)
        {
            _logError("[LevelUiManager] ❌ UiRouter为NULL，无法清除UI");
            ForceSwitchToPlayPhase();
            yield break;
        }

        yield return _uiRouter.ClearAsync().AsTask().AsCoroutineInstruction();
        _logInfo("[LevelUiManager] ✓ BuildUI已关闭");

        _logInfo("[LevelUiManager] 步骤2[显示PlayUI]: 正在加载游玩界面...");
        yield return _uiRouter.PushAsync(nameof(UiKey.LevelPlayUi)).AsTask().AsCoroutineInstruction();
        _logInfo("[LevelUiManager] ✓ PlayUI加载完成");

        _setCurrentPhase(LevelPhase.Play);
        _setIsBuildPhaseActive(false);

        _logInfo("[LevelUiManager] ✓ PlayUI已显示");
        _logInfo($"[LevelUiManager] ⚠ IsBuildPhaseActive = {_getIsBuildPhaseActive()}（移动限制已解除）");
        _logInfo("[LevelUiManager] ✓ 输入控制已恢复");
        _logInfo("[LevelUiManager] ═══════════ 游玩阶段开始 ═══════════");
    }

    /// <summary>强制切换到Play阶段（错误恢复）</summary>
    private void ForceSwitchToPlayPhase()
    {
        _logWarn("[LevelUiManager] ⚠ 执行强制切换到Play阶段（错误恢复模式）");
        _setCurrentPhase(LevelPhase.Play);
        _setIsBuildPhaseActive(false);
        _logWarn("[LevelUiManager] ⚠ 状态已强制更新，但UI可能未正确切换");
    }

    /// <summary>显示成功界面的协程</summary>
    private IEnumerator<IYieldInstruction> ShowSuccessUiCoroutine()
    {
        _logInfo("[LevelUiManager] ═══════════ 显示成功界面 ═══════════");

        _logInfo("[LevelUiManager] 步骤1[清除当前UI]: 正在清除游玩界面...");
        yield return _uiRouter?.ClearAsync().AsTask().AsCoroutineInstruction();
        _logInfo("[LevelUiManager] ✓ 当前UI已清除");

        _logInfo("[LevelUiManager] 步骤2[显示SuccessUI]: 正在加载成功界面...");
        yield return _uiRouter?.PushAsync(nameof(UiKey.LevelSuccessUi)).AsTask().AsCoroutineInstruction();

        _setCurrentPhase(LevelPhase.Success);
        _logInfo("[LevelUiManager] ★ 阶段已切换为 Success (ESC暂停已阻断)");

        _logInfo("[LevelUiManager] ✓✓✓ SuccessUI已成功显示！");
        _logInfo("[LevelUiManager] ═══════════ 🎉 恭喜通关！🎉 ═══════════");
    }

    #endregion

    #region 私有方法 - 节点查找

    /// <summary>在场景树中查找LevelBuildUi节点</summary>
    private LevelBuildUi? FindLevelBuildUi()
    {
        _logDebug("[LevelUiManager] 正在场景树中查找LevelBuildUi...");

        var directChild = FindNodeOfType<LevelBuildUi>(_ownerNode);
        if (directChild != null)
        {
            _logDebug($"[LevelUiManager] 在子节点中找到: {directChild.GetPath()}");
            return directChild;
        }

        _logDebug("[LevelUiManager] 未在静态子节点中找到，尝试延迟查找...");

        Callable.From(() =>
        {
            var deferredResult = FindLevelBuildUiDeferred();
            if (deferredResult != null)
            {
                _logInfo($"[LevelUiManager] ✓✓✓ 延迟查找成功找到LevelBuildUi: {deferredResult.GetPath()}");
                ConnectBuildFinishedSignalToInstance(deferredResult);
            }
            else
            {
                _logWarn("[LevelUiManager] ⚠ 延迟查找仍未找到LevelBuildUi");
            }
        }).CallDeferred();

        return null;
    }

    /// <summary>延迟查找LevelBuildUi</summary>
    private LevelBuildUi? FindLevelBuildUiDeferred()
    {
        _logDebug("[LevelUiManager] 执行延迟查找LevelBuildUi...");

        foreach (var node in _ownerNode.GetTree().GetNodesInGroup("ui_page"))
        {
            if (node is LevelBuildUi buildUi)
            {
                _logDebug($"[LevelUiManager] 在ui_page组中找到: {buildUi.GetPath()}");
                return buildUi;
            }
        }

        foreach (var child in _ownerNode.GetTree().Root.GetChildren())
        {
            var result = FindNodeOfType<LevelBuildUi>(child);
            if (result != null)
            {
                _logDebug($"[LevelUiManager] 在场景树中找到: {result.GetPath()}");
                return result;
            }
        }

        _logDebug("[LevelUiManager] 延迟查找未找到LevelBuildUi");
        return null;
    }

    /// <summary>连接信号到特定实例</summary>
    private void ConnectBuildFinishedSignalToInstance(LevelBuildUi buildUi)
    {
        try
        {
            if (buildUi.IsConnected(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished)))
            {
                _logDebug("[LevelUiManager] BuildFinished信号已连接（跳过重复连接）");
                return;
            }

            buildUi.Connect(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished));

            _logInfo("[LevelUiManager] ✓✓✓ BuildFinished信号已成功连接（延迟模式）");
            _logInfo("[LevelUiManager] 双重保障机制已激活：直接调用 + 信号通知");
        }
        catch (Exception ex)
        {
            _logWarn($"[LevelUiManager] ⚠ 连接信号时异常: {ex.Message}");
        }
    }

    /// <summary>递归查找指定类型的节点</summary>
    private T? FindNodeOfType<T>(Node root) where T : Node
    {
        if (root is T target)
        {
            return target;
        }

        foreach (var child in root.GetChildren())
        {
            var result = FindNodeOfType<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    #endregion

    #region 静态属性访问器

    /// <summary>获取构建阶段激活标志（通过委托实时同步）</summary>
    public bool IsBuildPhaseActive => _getIsBuildPhaseActive();

    #endregion
}
