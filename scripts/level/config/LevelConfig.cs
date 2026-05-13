// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace GFrameworkGodotTemplate.scripts.level.config;

/// <summary>
///     关卡配置数据模型
///     <para>
///         定义单个关卡的完整配置信息，包括时间限制、得分规则等
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         核心功能:
///         1. 存储关卡的最大游戏时长（毫秒级精度）
///         2. 定义得分计算规则和权重
///         3. 支持灵活的多维度评分体系
///         
///         设计原则:
///         - 类型安全：使用强类型避免配置错误
///         - 不可变性：配置创建后不应被修改
///         - 可序列化：支持持久化存储和加载
///         
///         使用场景:
///         - LevelRulesController读取并应用配置
///         - BaseLevelController初始化时加载
///         - GameTest测试验证使用
///     </description>
/// </summary>
public class LevelConfig
{
	#region 属性定义

	/// <summary>
	///     关卡唯一标识符
	///     <para>
	///         对应GameLevel枚举值，用于配置索引和查找
	 ///     </para>
	/// </summary>
	public int LevelId { get; init; }

	/// <summary>
	///     关卡显示名称
	///     <para>
	///         用于日志输出和调试信息显示
	 ///     </para>
	/// </summary>
	public string DisplayName { get; init; } = string.Empty;

	/// <summary>
	///     最大允许游戏时长（毫秒）
	///     <para>
	///         当玩家在Play阶段的时间超过此阈值时，
	 ///         LevelRulesController将触发超时事件
	 ///         
	 ///         时间精度: 毫秒级（1ms = 0.001s）
	 ///         示例: 10000ms = 10秒
	 ///         
	 ///         约束条件:
	 ///         - 必须大于0（无效值将导致计时器不启动）
	 ///         - 建议最小值: 5000ms（5秒）
	 ///         - 建议最大值: 600000ms（10分钟）
	 ///     </para>
	///     <remarks>
	///         特殊值说明:
	 ///         - 0 或负数: 表示无时间限制（计时器不会启动）
	 ///         - 测试关卡建议设置为10000ms（10秒）用于快速验证
	 ///     </remarks>
	/// </summary>
	public long MaxTimeMs { get; init; }

	/// <summary>
	///     基础得分权重系数
	///     <para>
	///         用于计算时间得分的乘数因子
	 ///         实际得分 = 基础分 × 时间权重 × 难度系数
	 ///     </para>
	///     <remarks>
	///         默认值为1.0，表示标准计分
	///         大于1.0表示高奖励关卡
	///         小于1.0表示低奖励关卡
	 ///     </remarks>
	/// </summary>
	public float ScoreWeight { get; init; } = 1.0f;

	/// <summary>
	///     难度等级（1-5星）
	///     <para>
	///         影响最终得分计算的难度修正因子
	///         1星=0.8x, 2星=0.9x, 3星=1.0x, 4星=1.2x, 5星=1.5x
	 ///     </para>
	/// </summary>
	public int DifficultyStars { get; init; } = 3;

	/// <summary>
	///     是否启用时间限制
	///     <para>
	///         当为false时，即使MaxTimeMs有值也不会启动计时器
	///         用于开发调试或无限时模式
	 ///     </para>
	/// </summary>
	public bool IsTimeLimited { get; init; } = true;

	#endregion

	#region 计算属性

	/// <summary>
	///     获取最大时间的可读格式字符串
	///     <example>"10.000秒" 或 "01:30.000"</example>
	/// </summary>
	public string MaxTimeDisplay => FormatTime(MaxTimeMs);

	/// <summary>
	///     获取难度修正因子
	///     <para>
	///         根据星级返回对应的乘数系数
	 ///     </para>
	/// </summary>
	public float DifficultyMultiplier => DifficultyStars switch
	{
		1 => 0.8f,
		2 => 0.9f,
		3 => 1.0f,
		4 => 1.2f,
		5 => 1.5f,
		_ => 1.0f
	};

	#endregion

	#region 工厂方法

	/// <summary>
	///     创建标准关卡配置（带时间限制）
	/// </summary>
	/// <param name="levelId">关卡ID</param>
	/// <param name="displayName">显示名称</param>
	/// <param name="maxTimeSeconds">最大时间（秒）</param>
	/// <param name="difficulty">难度星级（1-5）</param>
	/// <returns>完整的关卡配置实例</returns>
	public static LevelConfig CreateTimed(int levelId, string displayName, double maxTimeSeconds, int difficulty = 3)
	{
		return new LevelConfig
		{
			LevelId = levelId,
			DisplayName = displayName,
			MaxTimeMs = (long)(maxTimeSeconds * 1000),
			DifficultyStars = difficulty,
			IsTimeLimited = true
		};
	}

	/// <summary>
	///     创建无限时关卡配置
	/// </summary>
	/// <param name="levelId">关卡ID</param>
	/// <param name="displayName">显示名称</param>
	/// <param name="difficulty">难度星级（1-5）</param>
	/// <returns>无时间限制的关卡配置实例</returns>
	public static LevelConfig CreateUntimed(int levelId, string displayName, int difficulty = 3)
	{
		return new LevelConfig
		{
			LevelId = levelId,
			DisplayName = displayName,
			MaxTimeMs = 0,
			DifficultyStars = difficulty,
			IsTimeLimited = false
		};
	}

	/// <summary>
	///     创建测试用关卡配置（10秒超时）
	///     <para>
	///         专用于开发和测试阶段的功能验证
	///         固定10秒时间限制，便于快速迭代测试
	 ///     </para>
	/// </summary>
	/// <param name="levelId">关卡ID</param>
	/// <param name="displayName">显示名称</param>
	/// <returns>测试专用配置（10秒时限）</returns>
	public static LevelConfig CreateTestConfig(int levelId, string displayName)
	{
		return new LevelConfig
		{
			LevelId = levelId,
			DisplayName = displayName,
			MaxTimeMs = 10000,
			ScoreWeight = 0.5f,
			DifficultyStars = 1,
			IsTimeLimited = true
		};
	}

	#endregion

	#region 辅助方法

	/// <summary>
	///     格式化时间为可读字符串
	/// </summary>
	/// <param name="timeMs">时间（毫秒）</param>
	/// <returns>格式化的时间字符串</returns>
	private static string FormatTime(long timeMs)
	{
		if (timeMs <= 0) return "无限时";
		
		var totalSeconds = timeMs / 1000.0;
		if (totalSeconds < 60)
		{
			return $"{totalSeconds:F3}秒";
		}
		
		var minutes = (int)(totalSeconds / 60);
		var seconds = totalSeconds % 60;
		return $"{minutes:D2}:{seconds:F3}";
	}

	/// <summary>
	///     验证配置的有效性
	/// </summary>
	/// <returns>true如果配置有效，否则false</returns>
	public bool IsValid()
	{
		return LevelId >= 0 && 
		       !string.IsNullOrEmpty(DisplayName) && 
		       (!IsTimeLimited || MaxTimeMs > 0) &&
		       DifficultyStars is >= 1 and <= 5;
	}

	#endregion

	#region 系统预定义配置

	/// <summary>
	///     获取所有内置关卡配置字典
	///     <para>
	///         包含Level1-5和LevelTest的默认配置
	///         可通过LevelRulesController动态覆盖
	 ///     </para>
	/// </summary>
	/// <returns>键值对字典（LevelId → Config）</returns>
	public static Dictionary<int, LevelConfig> GetDefaultConfigs()
	{
		return new()
		{
			{ 1, CreateTimed(1, "第一关", 120, 2) },
			{ 2, CreateTimed(2, "第二关", 150, 2) },
			{ 3, CreateTimed(3, "第三关", 180, 3) },
			{ 4, CreateTimed(4, "第四关", 240, 4) },
			{ 5, CreateTimed(5, "第五关", 300, 5) },
			{ 99, CreateTestConfig(99, "测试关卡") }
		};
	}

	#endregion
}

