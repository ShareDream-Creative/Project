using Godot;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.level;

namespace GFrameworkGodotTemplate.scripts.utility;

/// <summary>
///     Godot节点树遍历辅助工具类
///     <para>
///         提供通用的节点查找和遍历方法
///         封装常用的父节点搜索逻辑，避免代码重复
///         
///         设计目的:
///         - 统一节点查找策略（Owner优先、当前节点后备）
///         - 提供类型安全的泛型搜索方法
///         - 支持深度限制防止无限循环
///         - 提供详细的日志输出便于调试
///     </para>
/// </summary>
/// <author>AI Assistant</author>
/// <version>1.0.0</version>
/// <date>2026-05-15</date>
/// <description>
///     功能特性:
///     - FindParentOfType<T>(): 向上遍历查找指定类型的父节点
///     - FindController<T>(): 查找关卡控制器（多种策略）
///     - GetGlobalInputService(): 获取全局输入服务
///     
///     使用场景:
///     - UI组件需要引用父级控制器时
///     - 需要在场景树中定位特定类型节点时
///     - 需要访问全局单例服务时
///     
///     设计原则:
///     - 静态工具类: 无需实例化，直接调用
///     - 泛型方法: 类型安全，编译期检查
///     - 防御编程: null检查、深度限制、异常捕获
///     - 日志友好: 可选的详细日志输出
/// </description>
/// <remarks>
///     使用示例:
///     <code>
///     // 查找BaseLevelController
///     var controller = NodeTreeHelper.FindParentOfType&lt;BaseLevelController&gt;(this);
///     
///     // 通过Owner查找（更可靠）
///     var controller2 = NodeTreeHelper.FindParentOfType&lt;BaseLevelController&gt;(Owner ?? this);
///     
///     // 获取全局输入服务
///     var inputService = NodeTreeHelper.GetGlobalInputService(this);
///     </code>
///     
///     性能说明:
///     - 时间复杂度: O(n) 其中n为向上遍历的层级数
///     - 空间复杂度: O(1) 无额外内存分配
///     - 最大遍历深度: 20层（可通过参数自定义）
/// </remarks>
public static class NodeTreeHelper
{
	#region 常量

	/// <summary>默认最大遍历深度</summary>
	private const int DefaultMaxDepth = 20;

	#endregion

	#region 公共方法 - 父节点查找

	/// <summary>
	///     从指定节点开始向上遍历，查找目标类型的父节点
	 ///     <param name="startNode">起始节点</param>
	 ///     <param name="maxDepth">最大遍历深度（默认20）</param>
	 ///     <typeparam name="T">目标节点类型</typeparam>
	 ///     <returns>找到的节点，如果未找到则返回null</returns>
	 ///     <remarks>
	 ///         查找策略:
	 ///         1. 从 startNode 开始向上遍历
	 ///         2. 检查每个父节点是否为目标类型 T
	 ///         3. 找到第一个匹配的节点立即返回
	 ///         4. 达到最大深度或到达根节点时停止
	 ///         
	 ///         安全机制:
	 ///         - maxDepth限制: 防止无限循环（理论上不会发生）
	 ///         - null检查: startNode为null时安全返回null
	 ///         - 类型安全: 泛型约束确保编译期类型检查
	 ///     </remarks>
	/// </summary>
	public static T? FindParentOfType<T>(Node? startNode, int maxDepth = DefaultMaxDepth) where T : Node
	{
		if (startNode == null)
		{
			return null;
		}

		var current = startNode;
		var depth = 0;

		while (current != null && depth < maxDepth)
		{
			if (current is T target)
			{
				return target;
			}

			current = current.GetParent();
			depth++;
		}

		if (depth >= maxDepth)
		{
			GD.Print($"[NodeTreeHelper] ⚠️ 向上遍历超过最大深度({maxDepth})，停止搜索");
		}

		return null;
	}

	#endregion

	#region 公共方法 - 控制器查找

	/// <summary>
	///     查找BaseLevelController（多策略）
	 ///     <param name="node">当前UI节点</param>
	 ///     <param name="logPrefix">日志前缀标识（如"[LevelSuccessUi]"）</param>
	 ///     <returns>找到的控制器，可能为null</returns>
	 ///     <remarks>
	 ///         查找策略(按优先级):
	 ///         1. 通过Owner属性向上遍历（最可靠）
	 ///         2. 从当前节点向上遍历（备用方案）
	 ///         3. 从当前场景根节点获取（最终备用）
	 ///         
	 ///         适用场景:
	 ///         - UI通过Router显示时（不在控制器子树内）
	 ///         - UI作为控制器子节点挂载时
	 ///         - UI在独立场景中加载时
	 ///         
	 ///         日志输出:
	 ///         会输出详细的查找过程和结果，便于调试
	 ///     </remarks>
	/// </summary>
	public static BaseLevelController? FindLevelController(Node node, string logPrefix = "[NodeTreeHelper]")
	{
		if (node == null)
		{
			GD.Print($"{logPrefix} ⚠️ 节点为null，无法查找控制器");
			return null;
		}

		BaseLevelController? controller = null;

		// 策略1: 通过Owner属性向上遍历（最可靠）
		if (node.Owner != null)
		{
			GD.Print($"{logPrefix} 尝试策略1: 通过Owner向上遍历...");
			controller = FindParentOfType<BaseLevelController>(node.Owner);

			if (controller != null)
			{
				GD.Print($"{logPrefix} ✓✓✓ 通过Owner找到BaseLevelController");
				GD.Print($"{logPrefix}   控制器路径: {controller.GetPath()}");
				return controller;
			}
		}

		// 策略2: 从当前节点向上遍历
		GD.Print($"{logPrefix} 尝试策略2: 从当前节点向上遍历...");
		controller = FindParentOfType<BaseLevelController>(node);

		if (controller != null)
		{
			GD.Print($"{logPrefix} ✓ 通过当前节点找到BaseLevelController");
			GD.Print($"{logPrefix}   控制器路径: {controller.GetPath()}");
			return controller;
		}

		// 策略3: 从场景根节点获取（适用于UI通过Router显示的情况）
		GD.Print($"{logPrefix} 尝试策略3: 从当前场景获取...");
		var currentScene = node.GetTree()?.CurrentScene;
		
		if (currentScene is BaseLevelController sceneController)
		{
			GD.Print($"{logPrefix} ✓ 从当前场景获取到BaseLevelController");
			return sceneController;
		}

		GD.Print($"{logPrefix} ⚠️ 未找到BaseLevelController（非致命错误）");
		return null;
	}

	#endregion

	#region 公共方法 - 全局服务获取

	/// <summary>
	///     获取全局游戏玩法输入服务
	 ///     <param name="node">任意场景中的节点</param>
	 ///     <returns>全局输入服务实例，失败时返回null</returns>
	 ///     <remarks>
	 ///         获取路径:
	 ///         node → SceneTree → Root → GlobalInputController → GameplayInputService
	 ///         
	 ///         使用场景:
	 ///         - UI需要直接设置输入阶段时（绕过BaseLevelController）
	 ///         - 全局单例访问
	 ///         
	 ///         错误处理:
	 ///         - SceneTree不可用: 返回null
	 ///         - GlobalInputController不存在: 返回null
	 ///         - GameplayInputService未初始化: 返回null
	 ///     </remarks>
	/// </summary>
	public static IGlobalGameplayInputService? GetGlobalInputService(Node? node)
	{
		try
		{
			if (node == null)
			{
				GD.Print("[NodeTreeHelper] ⚠️ 节点为null，无法获取全局输入服务");
				return null;
			}

			var tree = node.GetTree();
			if (tree == null)
			{
				GD.Print("[NodeTreeHelper] ⚠️ 无法获取SceneTree");
				return null;
			}

			var globalController = tree.Root.GetNode<GlobalInputController>("GlobalInputController");

			if (globalController == null)
			{
				GD.Print("[NodeTreeHelper] ⚠️ GlobalInputController不存在");
				return null;
			}

			if (globalController.GameplayInputService == null)
			{
				GD.Print("[NodeTreeHelper] ⚠️ GameplayInputService未初始化");
				return null;
			}

			return globalController.GameplayInputService;
		}
		catch (Exception ex)
		{
			GD.Print($"[NodeTreeHelper] ❌ 获取全局输入服务异常: {ex.Message}");
			return null;
		}
	}

	#endregion
}
