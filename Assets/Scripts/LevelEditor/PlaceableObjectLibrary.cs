using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.LevelEditor
{
    public class PlaceableObjectLibrary : Singleton<PlaceableObjectLibrary>
    {
        [Header("Placeable Objects")]
        public List<PlaceableObject> placeableObjects = new List<PlaceableObject>();

        [Header("Categories")]
        public string[] categories = new string[]
        {
            "Basic",
            "Physics",
            "Fluid",
            "Mechanisms",
            "Destructibles",
            "Decorations",
            "Triggers"
        };

        protected override void Awake()
        {
            base.Awake();
            InitializeLibrary();
        }

        private void InitializeLibrary()
        {
            if (placeableObjects.Count == 0)
            {
                GenerateDefaultObjects();
            }
        }

        private void GenerateDefaultObjects()
        {
            placeableObjects.Clear();

            AddBasicObjects();
            AddPhysicsObjects();
            AddFluidObjects();
            AddMechanismObjects();
            AddDestructibleObjects();
            AddDecorationObjects();
            AddTriggerObjects();
        }

        private void AddBasicObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "basic_cube",
                displayName = "立方体",
                category = "Basic",
                description = "基础立方体，可用于搭建场景",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "size", displayName = "大小", type = PropertyType.Vector3, vector3Value = Vector3.one },
                    new EditorProperty { propertyName = "color", displayName = "颜色", type = PropertyType.Color }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "basic_plane",
                displayName = "平面",
                category = "Basic",
                description = "基础平面，可作为地面或墙壁",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "basic_sphere",
                displayName = "球体",
                category = "Basic",
                description = "基础球体",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "basic_cylinder",
                displayName = "圆柱体",
                category = "Basic",
                description = "基础圆柱体",
                isEditable = true
            });
        }

        private void AddPhysicsObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "physics_box",
                displayName = "物理方块",
                category = "Physics",
                description = "受重力影响的可推动方块",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "mass", displayName = "质量", type = PropertyType.Float, floatValue = 1f },
                    new EditorProperty { propertyName = "bounciness", displayName = "弹性", type = PropertyType.Float, floatValue = 0.5f }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "softbody_cube",
                displayName = "软体方块",
                category = "Physics",
                description = "可变形的软体方块",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "size", displayName = "大小", type = PropertyType.Float, floatValue = 2f },
                    new EditorProperty { propertyName = "stiffness", displayName = "刚度", type = PropertyType.Float, floatValue = 60f },
                    new EditorProperty { propertyName = "resolution", displayName = "分辨率", type = PropertyType.Int, floatValue = 6f }
                }
            });
        }

        private void AddFluidObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "fluid_emitter",
                displayName = "流体发射器",
                category = "Fluid",
                description = "持续发射流体粒子",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "rate", displayName = "发射速率", type = PropertyType.Float, floatValue = 30f },
                    new EditorProperty { propertyName = "force", displayName = "发射力度", type = PropertyType.Float, floatValue = 5f },
                    new EditorProperty { propertyName = "color", displayName = "颜色", type = PropertyType.Color }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "fluid_basin",
                displayName = "流体容器",
                category = "Fluid",
                description = "盛放流体的容器",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "fluid_collector",
                displayName = "流体收集器",
                category = "Fluid",
                description = "收集流体积满后触发",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "targetVolume", displayName = "目标容量", type = PropertyType.Float, floatValue = 50f }
                }
            });
        }

        private void AddMechanismObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "pressure_sensor",
                displayName = "压力传感器",
                category = "Mechanisms",
                description = "检测流体压力并输出信号",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "activationPressure", displayName = "激活压力", type = PropertyType.Float, floatValue = 50f },
                    new EditorProperty { propertyName = "detectionRadius", displayName = "检测半径", type = PropertyType.Float, floatValue = 2f }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "mechanical_door",
                displayName = "机械门",
                category = "Mechanisms",
                description = "可开关的门",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "openOffset", displayName = "打开距离", type = PropertyType.Vector3, vector3Value = new Vector3(0, 3, 0) },
                    new EditorProperty { propertyName = "speed", displayName = "速度", type = PropertyType.Float, floatValue = 1f }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "moving_platform",
                displayName = "移动平台",
                category = "Mechanisms",
                description = "可移动的平台",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "fluid_valve",
                displayName = "流体阀门",
                category = "Mechanisms",
                description = "控制流体流量的阀门",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "pressure_plate",
                displayName = "压力板",
                category = "Mechanisms",
                description = "被重力触发的开关",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "activationForce", displayName = "激活力", type = PropertyType.Float, floatValue = 20f }
                }
            });
        }

        private void AddDestructibleObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "destructible_wall",
                displayName = "可破坏墙壁",
                category = "Destructibles",
                description = "可被破坏的墙壁",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "health", displayName = "生命值", type = PropertyType.Float, floatValue = 100f },
                    new EditorProperty { propertyName = "pressureThreshold", displayName = "压力阈值", type = PropertyType.Float, floatValue = 50f }
                }
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "destructible_crate",
                displayName = "可破坏木箱",
                category = "Destructibles",
                description = "可被打碎的木箱",
                isEditable = true,
                editableProperties = new EditorProperty[]
                {
                    new EditorProperty { propertyName = "health", displayName = "生命值", type = PropertyType.Float, floatValue = 50f },
                    new EditorProperty { propertyName = "fragmentCount", displayName = "碎片数量", type = PropertyType.Int, floatValue = 8f }
                }
            });
        }

        private void AddDecorationObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "deco_rock",
                displayName = "岩石",
                category = "Decorations",
                description = "装饰用岩石",
                isEditable = false
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "deco_pillar",
                displayName = "柱子",
                category = "Decorations",
                description = "装饰用柱子",
                isEditable = false
            });
        }

        private void AddTriggerObjects()
        {
            placeableObjects.Add(new PlaceableObject
            {
                id = "trigger_zone",
                displayName = "触发区域",
                category = "Triggers",
                description = "玩家进入后触发事件",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "goal_zone",
                displayName = "目标区域",
                category = "Triggers",
                description = "到达此处通关",
                isEditable = true
            });

            placeableObjects.Add(new PlaceableObject
            {
                id = "checkpoint",
                displayName = "检查点",
                category = "Triggers",
                description = "死亡后在此复活",
                isEditable = true
            });
        }

        public PlaceableObject GetObjectById(string id)
        {
            foreach (PlaceableObject obj in placeableObjects)
            {
                if (obj.id == id)
                {
                    return obj;
                }
            }
            return null;
        }

        public List<PlaceableObject> GetObjectsByCategory(string category)
        {
            List<PlaceableObject> result = new List<PlaceableObject>();

            foreach (PlaceableObject obj in placeableObjects)
            {
                if (obj.category == category)
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        public void AddPlaceableObject(PlaceableObject obj)
        {
            if (GetObjectById(obj.id) == null)
            {
                placeableObjects.Add(obj);
            }
        }
    }
}
