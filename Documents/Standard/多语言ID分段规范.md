# 多语言ID分段规范

## 总体架构

```
1000-9999        枚举绑定区（代码枚举值=多语言ID，禁止手动分配）
100000-999999    自由分配区（按实体类型划分，策划可分配）
100000000+       服务器配置区
```

---

## 枚举绑定区 (1000-9999)

**核心原则**：此区域ID由代码枚举值决定，禁止在策划表中手动分配。

### 1000-1099: Life基础

| ID范围 | 枚举 | 成员 |
|--------|------|------|
| 1001-1007 | Life.Attributes | Hp, Atk, Def, Agi, Mp, Ine, Con |
| 1010-1019 | Life.States | Normal, Unconscious, Battle |
| 1020-1029 | Life.Genders | Female, Male |

### 1100-1199: Part部位

| ID范围 | 用途 | 成员 |
|--------|------|------|
| 1100 | 默认 | Default |
| 1101-1104 | 道具部位 | Top, Bottom, Inside, Outside |
| 1110-1119 | 生物部位 | Head, Chest, Back, Wing, Waist, Hand, Leg, Foot, Claw, Tail |

### 2000-2099: Operation操作

| ID范围 | 枚举 | 成员 |
|--------|------|------|
| 2000-2029 | Operation.Type | Talk, Attack, Settings, Signs, Pick, Drop, Equip, UnEquip, Give, Abandon, Use, Follow, UnFollow, Buy, Sell, Cook, Brew, Forge, Sewing, Alchemy, Enter, Mall |

### 3000-3099: Crime犯罪类型

| ID范围 | 枚举 | 成员 |
|--------|------|------|
| 3000-3007 | Life.Crime | Murder, Assault, Theft, Robbery, AssaultOfficer, PrisonBreak, JailBreak, Kidnap |

### 4000-4099: Channel频道

| ID范围 | 枚举 | 成员 |
|--------|------|------|
| 4000-4006 | Logic.Channel | System, Private, Local, Battle, All, Rumor, Automation |

### 5000-6999: 人物描述组合

性别+年龄组合，用于生成人物描述文本。

---

## 自由分配区 (100000-999999)

**核心原则**：策划按实体类型在对应区间分配ID。

| ID范围 | 用途 | 说明 |
|--------|------|------|
| 100000-109999 | 场景名称 | Scene的name字段 |
| 110000-119999 | 地图名称 | Map的name字段 |
| 120000-149999 | 道具 | 偶数=名称，奇数=描述 |
| 150000-159999 | 动物名称 | Animal类Life的name |
| 160000-179999 | NPC | 名称+台词，每个NPC占用连续ID |
| 180000-199999 | 技能/招式 | Skill和Movement的name |
| 200000-249999 | UI标签 | 按钮文本、简单提示词 |
| 250000-299999 | 广播模板 | 带占位符的消息模板 |
| 300000-399999 | 剧情文本 | Plot相关文本 |
| 400000-499999 | 任务文本 | Task相关文本 |
| 500000-999999 | 预留扩展 | - |

---

## 服务器配置区 (100000000+)

| ID范围 | 用途 |
|--------|------|
| 100000001+ | 服务器名称 |

---

## 重要规则

1. **枚举绑定区是禁区** - 新增多语言条目时，禁止使用1000-9999范围的ID
2. **新增枚举同步更新** - 代码中新增枚举值时，必须同步添加多语言条目
3. **ID唯一性** - 每个ID全局唯一，严禁重复
4. **道具ID规则** - 道具使用偶数作为名称ID，奇数作为描述ID（如120000=名称，120001=描述）


