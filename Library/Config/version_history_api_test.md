# 版本历史管理接口测试文档

## 接口列表

### 1. 查询版本列表
```bash
# 查询所有版本
GET http://localhost:9000/api/administrator/versions

# 按日期范围查询
GET http://localhost:9000/api/administrator/versions?startDate=2025-10-01&endDate=2025-10-31
```

### 2. 新增版本记录
```bash
POST http://localhost:9000/api/administrator/versions
Content-Type: application/json

{
  "date": "2025-11-01",
  "version": "v10.2.13",
  "description": "新增公会系统",
  "details": {
    "releaseDate": "2025-11-01 14:30:00",
    "developer": "Zlex",
    "svnRevision": "r15300",
    "changes": [
      "新增公会创建功能",
      "新增公会聊天系统"
    ],
    "bugFixes": [
      "修复内存泄漏问题"
    ]
  }
}
```

### 3. 更新版本记录
```bash
PUT http://localhost:9000/api/administrator/versions/3
Content-Type: application/json

{
  "date": "2025-10-30",
  "version": "v10.2.12",
  "description": "性能优化(已更新)",
  "details": {
    "releaseDate": "2025-10-30 18:00:00",
    "developer": "Zlex",
    "svnRevision": "r15234",
    "changes": [
      "优化数据库查询性能",
      "优化内存使用",
      "新增后台版本管理功能",
      "新增性能监控"
    ],
    "bugFixes": [
      "修复背包满时崩溃问题",
      "修复登录超时问题",
      "修复物品系统内存泄漏"
    ]
  }
}
```

### 4. 删除版本记录
```bash
DELETE http://localhost:9000/api/administrator/versions/1
```

## 初始测试数据

已在 `version_history.json` 中预置了3条测试数据：
- id: 1 - v10.2.8 - 新增PVP系统 (2025-10-05)
- id: 2 - v10.2.10 - 开放新地图 (2025-10-18)
- id: 3 - v10.2.12 - 性能优化 (2025-10-30)

## 数据存储

- 文件位置: `Library/Config/version_history.json`
- 格式: JSON数组
- 自动保存: 每次增删改操作后自动保存

## 注意事项

1. 所有日期格式必须为 `YYYY-MM-DD`
2. id 字段由后端自动生成，新增时不需要提供
3. details 对象中的所有字段都是可选的
4. 接口支持 CORS，允许前端跨域访问

