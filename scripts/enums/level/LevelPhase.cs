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
	///     关卡阶段枚举，定义UI和输入控制的三个主要阶段
	/// </summary>
	public enum LevelPhase
	{
		/// <summary>构建阶段：显示BuildUI，禁用键盘输入</summary>
		Build,

		/// <summary>游玩阶段：显示PlayUI，恢复全部输入</summary>
		Play,

		/// <summary>成功阶段：显示SuccessUI，禁用全部输入</summary>
		Success,
		/// <summary>失败阶段：显示DefeatUI，禁用全部输入</summary>
		Defeat
	}