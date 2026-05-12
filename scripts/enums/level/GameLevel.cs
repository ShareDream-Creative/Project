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

namespace GFrameworkGodotTemplate.scripts.enums;

/// <summary>
///     定义游戏关卡状态枚举，用于系统判定当前所处的关卡。
///     包含无关卡状态、五个正式关卡和一个测试关卡。
/// </summary>
public enum GameLevel
{
	/// <summary>
	///     无关卡状态，表示当前未选择或未进入任何关卡。
	/// </summary>
	None,

	/// <summary>
	///     第一关状态，对应Level1按钮。
	/// </summary>
	Level1,

	/// <summary>
	///     第二关状态，对应Level2按钮。
	/// </summary>
	Level2,

	/// <summary>
	///     第三关状态，对应Level3按钮。
	/// </summary>
	Level3,

	/// <summary>
	///     第四关状态，对应Level4按钮。
	/// </summary>
	Level4,

	/// <summary>
	///     第五关状态，对应Level5按钮。
	/// </summary>
	Level5,

	/// <summary>
	///     测试关卡状态，对应LevelTest按钮，用于开发测试的特殊关卡。
	/// </summary>
	LevelTest
}
