# Soft Fluid Puzzle Game

基于Unity引擎构建的软体流体交互解谜游戏，采用模块化架构设计，支持Windows和Mac平台。

## 项目概述

本项目是一款物理解谜游戏，玩家需要通过操控软体物体和流体粒子来解决各种谜题。游戏核心基于质点-弹簧系统的软体物理模拟和SPH（光滑粒子流体动力学）方法的流体模拟。

## 核心模块

### 1. 物理模拟模块 (PhysicsSimulation)
位于 `Assets/Scripts/PhysicsSimulation/`

- **SoftBodyParticle.cs**: 软体粒子类，每个粒子拥有质量、速度、碰撞检测等属性
- **SoftBodySpring.cs**: 弹簧连接类，模拟粒子间的弹性力，支持断裂效果
- **SoftBody.cs**: 软体物体主控制器，生成粒子网格、弹簧网络，实时更新网格变形
- **PhysicsSimulationManager.cs**: 物理模拟管理器，全局管理所有软体物体

**技术特点**:
- 基于质点-弹簧系统(Mass-Spring System)的实时物理模拟
- 支持结构弹簧、剪切弹簧和弯曲弹簧
- 内置压力模拟，可模拟充气效果
- 支持弹簧断裂修复
- 碰撞检测与响应

### 2. 流体渲染模块 (FluidRendering)
位于 `Assets/Scripts/FluidRendering/`

- **FluidParticle.cs**: 流体粒子数据结构
- **FluidSystem.cs**: 流体系统，包含SPH求解器和空间网格优化
- **FluidRenderer.cs**: 流体渲染器，支持GPU实例化渲染
- **FluidEmitter.cs**: 流体发射器，支持多种发射形状
- **FluidRenderingManager.cs**: 流体渲染管理器
- **FluidSoftBodyInteraction.cs**: 流体与软体的双向交互

**技术特点**:
- 基于SPH的流体物理模拟
- 密度-压力求解
- 粘性力计算
- 空间网格加速邻居搜索
- 支持GPU Instancing批量渲染
- 流体-软体双向耦合交互

### 3. 玩家控制模块 (PlayerControl)
位于 `Assets/Scripts/PlayerControl/`

- **PlayerInput.cs**: 输入处理组件
- **PlayerController.cs**: 角色控制器，包含移动、跳跃、相机跟随
- **PlayerInteraction.cs**: 玩家交互系统，支持抓取、交互
- **PlayerCamera.cs**: 第三人称相机控制器
- **PlayerFluidInteraction.cs**: 玩家与流体的交互

**技术特点**:
- 第三人称角色控制
- 平滑移动与加速度
- 土狼时间(Coyote Time)与跳跃缓冲
- 相机碰撞检测
- 物体抓取系统
- 流体浮力与阻力

### 4. 关卡管理模块 (LevelManagement)
位于 `Assets/Scripts/LevelManagement/`

- **LevelData.cs**: 关卡数据结构
- **SaveData.cs**: 存档数据结构（支持序列化）
- **SaveSystem.cs**: 存档系统，基于二进制序列化
- **LevelManager.cs**: 关卡管理器，负责关卡切换、进度跟踪
- **LevelInitializer.cs**: 关卡初始化器
- **PressurePlate.cs**: 压力板解谜元素
- **FluidCollector.cs**: 流体收集器解谜元素
- **GoalZone.cs**: 目标区域

**技术特点**:
- 多关卡管理
- 自动保存系统
- 星级评价系统
- 检查点系统
- 可扩展的解谜元素
- 支持目标系统

## 项目结构

```
Assets/
├── Scripts/
│   ├── Core/                 # 核心基础类
│   │   ├── Singleton.cs      # 单例模式基类
│   │   ├── EventBus.cs       # 事件总线
│   │   ├── GameEvents.cs     # 游戏事件定义
│   │   └── GameManager.cs    # 游戏管理器
│   ├── PhysicsSimulation/    # 物理模拟模块
│   ├── FluidRendering/       # 流体渲染模块
│   ├── PlayerControl/        # 玩家控制模块
│   └── LevelManagement/      # 关卡管理模块
├── Scenes/                   # 场景文件
├── Prefabs/                  # 预制体
├── Materials/                # 材质
└── Resources/                # 资源文件
```

## 快速开始

### 环境要求
- Unity 2022.3.20f1 或更高版本
- Windows 10+ / macOS 10.15+

### 安装步骤

1. 使用Unity Hub打开项目
2. 等待依赖包自动导入
3. 打开 `Assets/Scenes/MainMenu.unity` 场景
4. 点击播放按钮开始游戏

### 操作说明

| 按键 | 功能 |
|------|------|
| W/A/S/D 或 方向键 | 移动 |
| 空格 | 跳跃 |
| 鼠标移动 | 视角控制 |
| 鼠标左键 | 抓取/释放 |
| E | 交互 |
| ESC | 暂停 |

## 关卡设计

### 关卡类型
1. **入门教学**: 基础操作与软体物体介绍
2. **流体引导**: 学习流体物理与收集器
3. **软体变形**: 利用软体变形解谜
4. **综合挑战**: 软体与流体结合解谜
5. **终极考验**: 高难度综合关卡

### 解谜元素
- **压力板**: 需要重量触发的开关
- **流体收集器**: 收集指定量流体完成目标
- **目标区域**: 到达指定位置通关
- **软体物体**: 可变形的互动元素
- **流体粒子**: 可推动和引导的流体

## 平台支持

### Windows
- 支持Windows 10及以上版本
- 支持DirectX 11/12
- 推荐配置：独立显卡，4GB以上显存

### Mac
- 支持macOS 10.15及以上版本
- 支持Metal图形API
- 支持Apple Silicon原生运行

## 性能优化建议

1. **软体性能**:
   - 降低软体分辨率以提升性能
   - 减少弹簧数量
   - 使用更简单的碰撞体

2. **流体性能**:
   - 控制最大粒子数量
   - 增大平滑半径减少邻居数量
   - 使用GPU Instancing渲染

3. **通用优化**:
   - 合理设置物理帧率
   - 使用对象池管理粒子
   - 启用层级剔除

## 扩展开发

### 添加新的解谜元素

1. 创建新类并实现 `IInteractable` 接口
2. 实现交互逻辑
3. 在关卡中放置并配置参数

### 添加新关卡

1. 创建新场景
2. 添加 `LevelInitializer` 组件
3. 配置关卡目标和谜题
4. 在 `LevelManager` 中注册新关卡

### 自定义流体效果

1. 调整 `FluidSystem` 的物理参数
2. 自定义 `FluidRenderer` 的渲染效果
3. 扩展 `FluidEmitter` 实现特殊发射模式

## 技术架构

### 设计模式
- **单例模式**: 全局管理器使用单例
- **事件驱动**: 模块间通过事件总线通信
- **组件化**: 基于MonoBehaviour的组件式设计
- **策略模式**: 可配置的交互和行为策略

### 模块解耦
各模块通过 `EventBus` 进行通信，减少直接依赖：
- 物理模块发布形变事件
- 流体模块发布体积变化事件
- 玩家模块发布移动和交互事件
- 关卡模块监听所有事件更新进度

## 许可证

本项目仅供学习和研究使用。

## 更新日志

### v1.0.0
- 初始版本发布
- 实现软体物理模拟模块
- 实现流体渲染模块
- 实现玩家控制模块
- 实现关卡管理模块
- 支持Windows和Mac平台
